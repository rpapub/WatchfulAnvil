# /// script
# requires-python = ">=3.11"
# dependencies = ["pyyaml>=6.0"]
# ///
"""
WatchfulAnvil Corpus Test Harness  (issue #42)

Reads corpus-catalog.yaml, installs the latest Cpmf rule-pack DLL into the
uipcli Rules directory, runs `uipcli package analyze` against each enabled
test set, and asserts actual violations match the expected.yaml sidecar.

Usage (from WatchfulAnvil repo root):
    uv run tools/corpus-harness/run.py [options]

Options:
    --corpus <path>      Path to rpax-corpuses root
                         (default: ../rpax-corpuses relative to repo root)
    --version <ver>      Override rule pack version
                         (default: latest nupkg by modification time)
    --tags <tag,...>     Run only test sets whose tags include any of these
    --id <id,...>        Run only specific test set IDs
    --list               List matching test sets and exit
    --no-install         Skip DLL installation (use already-installed version)
"""

import argparse
import glob
import json
import os
import shutil
import subprocess
import sys
import tempfile
import zipfile
from pathlib import Path

import yaml

RULE_PACK = "Cpmf.WorkflowAnalyzerRules"
SEV_MAP = {1: "error", 2: "warning"}


# ---------------------------------------------------------------------------
# uipcli / DLL discovery
# ---------------------------------------------------------------------------

def find_uipcli() -> Path:
    pattern = str(
        Path.home()
        / "AppData/Local/UiPath/uipathcli/modules/uipcli-win-*/tools/uipcli.exe"
    )
    matches = sorted(glob.glob(pattern), key=os.path.getmtime, reverse=True)
    if not matches:
        raise RuntimeError(
            f"uipcli.exe not found. Searched: {pattern}\n"
            "Install UiPath CLI: https://docs.uipath.com/automation-ops/docs/uipath-cli"
        )
    return Path(matches[0])


def rules_net8_dir(uipcli_path: Path) -> Path:
    d = uipcli_path.parent / "Rules" / "net8.0"
    if not d.is_dir():
        raise RuntimeError(f"Rules/net8.0 not found at {d}")
    return d


def find_latest_nupkg(nupkg_dir: Path, version_override: str | None) -> Path:
    if version_override:
        p = nupkg_dir / f"{RULE_PACK}.{version_override}.nupkg"
        if not p.exists():
            raise RuntimeError(f"nupkg not found: {p}")
        return p
    candidates = [
        Path(p)
        for p in glob.glob(str(nupkg_dir / f"{RULE_PACK}.*.nupkg"))
        if "unpacked" not in p
    ]
    if not candidates:
        raise RuntimeError(
            f"No {RULE_PACK} nupkg found in {nupkg_dir}\n"
            "Run: dotnet pack src/Cpmf.WorkflowAnalyzerRules/"
            "Cpmf.WorkflowAnalyzerRules.csproj -c Release -o nupkg/"
        )
    return max(candidates, key=os.path.getmtime)


def install_dll(nupkg_path: Path, rules_dir: Path, tmpdir: Path) -> str:
    """Extract net8.0 DLL from nupkg and copy it into the uipcli Rules dir."""
    dll_name = f"{RULE_PACK}.dll"
    inner = f"lib/net8.0/{dll_name}"
    with zipfile.ZipFile(nupkg_path) as zf:
        zf.extract(inner, path=tmpdir)
    src = tmpdir / "lib" / "net8.0" / dll_name
    dest = rules_dir / dll_name
    shutil.copy2(src, dest)
    version = nupkg_path.stem.replace(f"{RULE_PACK}.", "")
    return version


# ---------------------------------------------------------------------------
# uipcli analyze
# ---------------------------------------------------------------------------

def run_analyze(uipcli: Path, project_json: Path, result_path: Path) -> list:
    cmd = [
        str(uipcli), "package", "analyze",
        str(project_json),
        "--resultPath", str(result_path),
        "--traceLevel", "None",
        "--analyzerTraceLevel", "Warning",
    ]
    subprocess.run(cmd, capture_output=True, timeout=180)
    if not result_path.exists():
        return []
    with open(result_path, encoding="utf-8") as f:
        return json.load(f)


# ---------------------------------------------------------------------------
# Normalization  (uipcli JSON → canonical dicts)
# ---------------------------------------------------------------------------

def normalize(raw: list) -> list:
    """
    Deduplicate and normalize raw uipcli results.

    uipcli emits each violation twice (once per analyzer pass); deduplicate
    on (ErrorCode, FilePath, Description) before returning.
    """
    seen: set = set()
    out = []
    for r in raw:
        key = (r.get("ErrorCode"), r.get("FilePath"), r.get("Description"))
        if key in seen:
            continue
        seen.add(key)
        fp = (r.get("FilePath") or "").replace("\\", "/")
        out.append({
            "ruleId":       r.get("ErrorCode", ""),
            "severity":     SEV_MAP.get(r.get("ErrorSeverity"), "unknown"),
            "filePath":     fp,
            "workflow":     fp.split("/")[-1] if fp else None,
            "description":  r.get("Description") or "",
        })
    return out


# ---------------------------------------------------------------------------
# Comparison
# ---------------------------------------------------------------------------

def _matches_one(actual: dict, exp: dict) -> bool:
    """Return True if `actual` satisfies every constraint specified in `exp`."""
    if actual["ruleId"] != exp["ruleId"]:
        return False

    if "severity" in exp:
        if actual["severity"] != exp["severity"].lower():
            return False

    scope = exp.get("scope")
    if scope == "project":
        # project-level violations have no workflow file in the output
        if actual["workflow"] is not None:
            return False
    elif "workflow" in exp:
        if actual["workflow"] != exp["workflow"]:
            return False

    # activity / variable are matched against the Description text, because
    # uipcli does not populate ActivityDisplayName for our custom rules.
    if "activity" in exp:
        if exp["activity"] not in actual["description"]:
            return False

    if "variable" in exp:
        if exp["variable"] not in actual["description"]:
            return False

    return True


def compare(actual: list, expected: list) -> list:
    """
    Returns a list of failure strings (empty list = PASS).

    - Every expected entry must be matched by at least one actual result.
    - Every actual result must be matched by at least one expected entry.
      (Unexpected violations also cause a FAIL.)
    """
    failures = []

    for exp in expected:
        if not any(_matches_one(a, exp) for a in actual):
            failures.append(f"MISSING  expected: {exp}")

    for act in actual:
        if not any(_matches_one(act, exp) for exp in expected):
            failures.append(
                f"UNEXPECTED actual: ruleId={act['ruleId']}"
                f" severity={act['severity']}"
                f" workflow={act['workflow']}"
                f" | {act['description'][:80]}"
            )

    return failures


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main() -> None:
    parser = argparse.ArgumentParser(
        description="WatchfulAnvil corpus test harness (issue #42)"
    )
    parser.add_argument("--corpus", help="Path to rpax-corpuses root")
    parser.add_argument("--version", help="Rule pack version override")
    parser.add_argument("--tags", help="Comma-separated tag filter")
    parser.add_argument("--id", dest="ids", help="Comma-separated test set ID filter")
    parser.add_argument("--list", action="store_true", help="List test sets and exit")
    parser.add_argument(
        "--no-install",
        action="store_true",
        help="Skip DLL installation (use already-installed version)",
    )
    args = parser.parse_args()

    # Locate repos
    repo_root = Path(__file__).resolve().parent.parent.parent
    corpus_root = (
        Path(args.corpus).resolve()
        if args.corpus
        else (repo_root.parent / "rpax-corpuses").resolve()
    )

    if not corpus_root.is_dir():
        print(f"ERROR: corpus root not found: {corpus_root}", file=sys.stderr)
        sys.exit(1)

    catalog_path = corpus_root / "corpus-catalog.yaml"
    with open(catalog_path, encoding="utf-8") as f:
        catalog = yaml.safe_load(f)

    test_sets = catalog.get("testSets", [])

    # Apply filters
    tag_filter = set(args.tags.split(",")) if args.tags else None
    id_filter = set(args.ids.split(",")) if args.ids else None

    def should_run(ts: dict) -> bool:
        if not ts.get("enabled", False):
            return False
        if id_filter and ts["id"] not in id_filter:
            return False
        if tag_filter:
            ts_tags = set(ts.get("tags") or [])
            if not tag_filter & ts_tags:
                return False
        return True

    active = [ts for ts in test_sets if should_run(ts)]

    if args.list:
        print(f"{'ID':<25} {'NAME'}")
        for ts in active:
            print(f"  {ts['id']:<23} {ts['name']}")
        print(f"\n{len(active)} test set(s)")
        return

    if not active:
        print("No test sets match the given filters.")
        sys.exit(0)

    uipcli = find_uipcli()
    rules_dir = rules_net8_dir(uipcli)

    with tempfile.TemporaryDirectory() as _tmpdir:
        tmpdir = Path(_tmpdir)
        result_file = tmpdir / "result.json"

        if not args.no_install:
            nupkg_dir = repo_root / "nupkg"
            nupkg_path = find_latest_nupkg(nupkg_dir, args.version)
            version = install_dll(nupkg_path, rules_dir, tmpdir)
            print(f"Rule pack : {RULE_PACK} {version}")
        else:
            version = "(pre-installed)"
            print(f"Rule pack : {RULE_PACK} {version}")

        print(f"uipcli    : {uipcli}")
        print(f"Corpus    : {corpus_root}")
        print(f"Test sets : {len(active)}")
        print()

        passed = failed = skipped = 0

        for ts in active:
            ts_id = ts["id"]
            corpus_subdir = ts["corpus"].lstrip("./").replace("/", os.sep)
            corpus_dir = corpus_root / corpus_subdir
            project_json = corpus_dir / "project.json"
            sidecar_path = corpus_dir / "expected.yaml"

            if not project_json.exists():
                print(f"  SKIP  {ts_id}  (no project.json)")
                skipped += 1
                continue

            if not sidecar_path.exists():
                print(f"  SKIP  {ts_id}  (no expected.yaml sidecar)")
                skipped += 1
                continue

            with open(sidecar_path, encoding="utf-8") as f:
                sidecar_data = yaml.safe_load(f)
            expected_violations = sidecar_data.get("expectedViolations") or []

            if result_file.exists():
                result_file.unlink()

            raw = run_analyze(uipcli, project_json, result_file)
            actual = normalize(raw)

            failures = compare(actual, expected_violations)

            if not failures:
                print(f"  PASS  {ts_id}")
                passed += 1
            else:
                print(f"  FAIL  {ts_id}  —  {ts['name']}")
                for msg in failures:
                    print(f"        {msg}")
                failed += 1

    print()
    print(f"Results: {passed} passed, {failed} failed, {skipped} skipped")

    if failed:
        sys.exit(1)


if __name__ == "__main__":
    main()
