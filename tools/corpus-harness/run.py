# /// script
# requires-python = ">=3.11"
# dependencies = ["pyyaml>=6.0"]
# ///
"""
WatchfulAnvil Corpus Test Harness  (issue #42)

Reads corpus-catalog.yaml, generates a per-run NuGet.config that points at
the local nupkg build output, copies each corpus project into a temp
directory, then calls `uipcli package analyze` and asserts that the actual
violations match the expected.yaml sidecar.

Usage (from WatchfulAnvil repo root):
    uv run tools/corpus-harness/run.py [options]

Options:
    --corpus <path>      Path to rpax-corpuses root
                         (default: ../rpax-corpuses relative to repo root)
    --version <ver>      Override rule pack version (patches project.json)
                         (default: latest nupkg by modification time)
    --feed <path>        Directory that contains the rule-pack nupkg
                         (default: <repo-root>/nupkg)
    --uipcli <path>      Path to uipcli.exe
                         (default: newest under ~/AppData/Local/cpmf/tools/)
    --governance <path>  JSON governance file passed to uipcli
                         (default: none — rules fire with built-in defaults)
    --tags <tag,...>     Run only test sets whose tags include any of these
    --id <id,...>        Run only specific test set IDs
    --list               List matching test sets and exit
"""

import argparse
import glob
import json
import os
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

import yaml

RULE_PACK = "Cpmf.WorkflowAnalyzerRules"
SEV_MAP = {1: "error", 2: "warning"}

# Candidate patterns for uipcli.exe, tried in order (newest match wins).
UIPCLI_SEARCH = [
    # cpmf convention: managed installs under ~/.cpmf/tools/
    str(Path.home() / "AppData/Local/cpmf/tools/uipcli-*/uipcli.exe"),
    # standard UiPath CLI location (older)
    str(Path.home() / "AppData/Local/UiPath/uipathcli/modules/uipcli-win-*/tools/uipcli.exe"),
]


# ---------------------------------------------------------------------------
# uipcli discovery
# ---------------------------------------------------------------------------

def find_uipcli() -> Path:
    for pattern in UIPCLI_SEARCH:
        matches = sorted(glob.glob(pattern), key=os.path.getmtime, reverse=True)
        if matches:
            return Path(matches[0])
    raise RuntimeError(
        "uipcli.exe not found. Searched:\n"
        + "\n".join(f"  {p}" for p in UIPCLI_SEARCH)
        + "\nInstall UiPath CLI and re-run, or pass --uipcli <path>."
    )


# ---------------------------------------------------------------------------
# nupkg discovery
# ---------------------------------------------------------------------------

def find_latest_nupkg(nupkg_dir: Path, version_override: str | None) -> tuple[Path, str]:
    """Return (path, version_string) for the rule-pack nupkg to test."""
    if version_override:
        p = nupkg_dir / f"{RULE_PACK}.{version_override}.nupkg"
        if not p.exists():
            raise RuntimeError(f"nupkg not found: {p}")
        return p, version_override

    candidates = [
        Path(p)
        for p in glob.glob(str(nupkg_dir / f"{RULE_PACK}.*.nupkg"))
        if "unpacked" not in p
    ]
    if not candidates:
        raise RuntimeError(
            f"No {RULE_PACK} nupkg found in {nupkg_dir}\n"
            f"Run: dotnet pack src/Cpmf.WorkflowAnalyzerRules/"
            f"Cpmf.WorkflowAnalyzerRules.csproj -c Release -o nupkg/"
        )
    path = max(candidates, key=os.path.getmtime)
    version = path.stem.replace(f"{RULE_PACK}.", "")
    return path, version


# ---------------------------------------------------------------------------
# NuGet.config generation
# ---------------------------------------------------------------------------

def make_nuget_config(nupkg_dir: Path, output_path: Path) -> None:
    """Write a minimal NuGet.config that points at the local nupkg directory."""
    xml = (
        '<?xml version="1.0" encoding="utf-8"?>\n'
        "<configuration>\n"
        "  <packageSources>\n"
        f'    <add key="local-nupkg" value="{nupkg_dir}" />\n'
        '    <add key="UiPath-Official" value="https://uipath.pkgs.visualstudio.com/'
        'Public.Feeds/_packaging/UiPath-Official/nuget/v3/index.json" />\n'
        '    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />\n'
        "  </packageSources>\n"
        "</configuration>\n"
    )
    output_path.write_text(xml, encoding="utf-8")


# ---------------------------------------------------------------------------
# project.json patching
# ---------------------------------------------------------------------------

def patch_project_json(src: Path, dest: Path, version: str) -> None:
    """Copy project.json to dest, replacing the rule-pack version."""
    with open(src, encoding="utf-8") as f:
        data = json.load(f)
    deps = data.get("dependencies", {})
    # Accept both bare version strings and bracket-pinned "[x.y.z]" forms.
    deps[RULE_PACK] = version
    data["dependencies"] = deps
    with open(dest, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
        f.write("\n")


# ---------------------------------------------------------------------------
# uipcli analyze
# ---------------------------------------------------------------------------

def run_analyze(
    uipcli: Path,
    workspace: Path,
    nuget_config: Path,
    governance: Path | None,
    result_path: Path,
) -> list:
    cmd = [
        str(uipcli),
        "package",
        "analyze",
        str(workspace),
        "--resultPath",
        str(result_path),
        "--traceLevel",
        "None",
        "--analyzerTraceLevel",
        "Warning",
        "--nugetConfigFilePath",
        str(nuget_config),
    ]
    if governance and governance.exists():
        cmd += ["--governanceFilePath", str(governance)]

    subprocess.run(cmd, capture_output=True, timeout=300)

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

    uipcli may emit each violation more than once; deduplicate on
    (ErrorCode, FilePath, Description) before returning.
    """
    seen: set = set()
    out = []
    for r in raw:
        key = (r.get("ErrorCode"), r.get("FilePath"), r.get("Description"))
        if key in seen:
            continue
        seen.add(key)
        fp = (r.get("FilePath") or "").replace("\\", "/")
        # Derive the workflow filename.  uipcli sets FilePath to the workspace
        # directory (not a .xaml file) for project-level violations, so only
        # treat it as a file reference when the path ends with ".xaml".
        basename = fp.split("/")[-1] if fp else None
        workflow = basename if (basename and basename.lower().endswith(".xaml")) else None
        out.append(
            {
                "ruleId": r.get("ErrorCode", ""),
                "severity": SEV_MAP.get(r.get("ErrorSeverity"), "unknown"),
                "filePath": fp,
                "workflow": workflow,
                "description": r.get("Description") or "",
            }
        )
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

    # activity / variable are matched against the Description text because
    # uipcli does not populate ActivityDisplayName for custom rules.
    if "activity" in exp:
        if exp["activity"] not in actual["description"]:
            return False

    if "variable" in exp:
        if exp["variable"] not in actual["description"]:
            return False

    return True


def compare(actual: list, expected: list) -> list:
    """
    Return a list of failure strings (empty list = PASS).

    - Every expected entry must be matched by at least one actual result.
    - Every actual result must be matched by at least one expected entry.
    """
    failures = []

    for exp in expected:
        if not any(_matches_one(a, exp) for a in actual):
            failures.append(f"MISSING  expected: {exp}")

    for act in actual:
        if not any(_matches_one(act, exp) for exp in expected):
            failures.append(
                f"UNEXPECTED  ruleId={act['ruleId']}"
                f"  severity={act['severity']}"
                f"  workflow={act['workflow']}"
                f"  | {act['description'][:80]}"
            )

    return failures


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main() -> None:
    # Ensure Unicode characters in test-set names print cleanly on Windows terminals.
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(errors="replace")

    parser = argparse.ArgumentParser(
        description="WatchfulAnvil corpus test harness (issue #42)"
    )
    parser.add_argument("--corpus", help="Path to rpax-corpuses root")
    parser.add_argument("--version", help="Rule pack version override")
    parser.add_argument(
        "--feed",
        help="Directory containing the rule-pack nupkg (default: <repo>/nupkg)",
    )
    parser.add_argument("--uipcli", help="Path to uipcli.exe")
    parser.add_argument(
        "--governance",
        help="JSON governance file passed to uipcli (default: none)",
    )
    parser.add_argument("--tags", help="Comma-separated tag filter")
    parser.add_argument("--id", dest="ids", help="Comma-separated test set ID filter")
    parser.add_argument("--list", action="store_true", help="List test sets and exit")
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
        print(f"{'ID':<25} NAME")
        for ts in active:
            print(f"  {ts['id']:<23} {ts['name']}")
        print(f"\n{len(active)} test set(s)")
        return

    if not active:
        print("No test sets match the given filters.")
        sys.exit(0)

    # Resolve uipcli
    uipcli = Path(args.uipcli).resolve() if args.uipcli else find_uipcli()
    if not uipcli.exists():
        print(f"ERROR: uipcli not found: {uipcli}", file=sys.stderr)
        sys.exit(1)

    # Resolve nupkg
    nupkg_dir = Path(args.feed).resolve() if args.feed else (repo_root / "nupkg")
    nupkg_path, version = find_latest_nupkg(nupkg_dir, args.version)

    # Resolve optional governance file
    governance: Path | None = Path(args.governance).resolve() if args.governance else None

    print(f"Rule pack : {RULE_PACK} {version}")
    print(f"nupkg     : {nupkg_path}")
    print(f"uipcli    : {uipcli}")
    print(f"Corpus    : {corpus_root}")
    if governance:
        print(f"Governance: {governance}")
    print(f"Test sets : {len(active)}")
    print()

    passed = failed = skipped = 0

    with tempfile.TemporaryDirectory() as _tmpdir:
        tmpdir = Path(_tmpdir)

        # Single NuGet.config for the whole run.
        nuget_config = tmpdir / "NuGet.config"
        make_nuget_config(nupkg_dir, nuget_config)

        result_file = tmpdir / "result.json"

        for ts in active:
            ts_id = ts["id"]
            corpus_subdir = ts["corpus"].lstrip("./").replace("/", os.sep)
            corpus_dir = corpus_root / corpus_subdir
            project_json = corpus_dir / "project.json"
            sidecar_path = corpus_dir / "expected.yaml"

            if not project_json.exists():
                print(f"  SKIP  {ts_id}  (no project.json at {corpus_dir})")
                skipped += 1
                continue

            if not sidecar_path.exists():
                print(f"  SKIP  {ts_id}  (no expected.yaml sidecar)")
                skipped += 1
                continue

            with open(sidecar_path, encoding="utf-8") as f:
                sidecar_data = yaml.safe_load(f)
            expected_violations = sidecar_data.get("expectedViolations") or []

            # Copy corpus to a temp workspace so we can patch project.json
            # without touching the original.
            workspace = tmpdir / ts_id
            if workspace.exists():
                shutil.rmtree(workspace)
            shutil.copytree(corpus_dir, workspace)

            patch_project_json(project_json, workspace / "project.json", version)

            if result_file.exists():
                result_file.unlink()

            raw = run_analyze(uipcli, workspace, nuget_config, governance, result_file)
            actual = normalize(raw)

            failures = compare(actual, expected_violations)

            if not failures:
                print(f"  PASS  {ts_id}")
                passed += 1
            else:
                print(f"  FAIL  {ts_id}  --  {ts['name']}")
                for msg in failures:
                    print(f"        {msg}")
                failed += 1

    print()
    print(f"Results: {passed} passed, {failed} failed, {skipped} skipped")

    if failed:
        sys.exit(1)


if __name__ == "__main__":
    main()
