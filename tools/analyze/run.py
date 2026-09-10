# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""
WatchfulAnvil Analyze Tool  (issue #50)

Runs the rule pack against an arbitrary UiPath project.json and surfaces
both the uipcli violations and the TAP inspection log written by the rules.

Usage (from WatchfulAnvil repo root):
    uv run tools/analyze/run.py --project path/to/project.json [options]

Options:
    --project <path>        Path to project.json (required)
    --feed <path>           Directory containing the rule-pack nupkg
                            (default: <repo-root>/nupkg)
    --pack <id>             Rule pack package id; repeatable. Supplying any --pack
                            replaces the default rather than adding to it, e.g.
                            --pack Cpmf.Standard --pack Cpmf.Tap
                            (default: Cpmf.WorkflowAnalyzerRules)
    --version <ver>         Pin a specific rule-pack version. Single pack only.
                            (default: latest nupkg by modification time)
    --uipcli <path>         Path to uipcli.exe
                            (default: newest under ~/AppData/Local/cpmf/tools/)
    --governance <path>     JSON governance file passed to uipcli
    --violations-json       Print raw uipcli JSON array to stdout
    --tap                   Also stream activity.jsonl + workflow.jsonl after violations
    --tap-dir               Print only the TAP run directory path (no violations output)

Exit codes:
    0   No violations found
    1   One or more violations found
    2   Tool / setup error
"""

import argparse
import glob
import json
import os
import subprocess
import sys
import tempfile
from datetime import datetime, timezone
from pathlib import Path

DEFAULT_RULE_PACK = "Cpmf.WorkflowAnalyzerRules"

# Rule packs no longer merge the SDK in; they declare a dependency on it. So the flat
# feed handed to uipcli must carry WatchfulAnvil.Sdk as well, or restore fails and the
# run reports "no violations" rather than "could not resolve".
SDK_PACKAGE = "WatchfulAnvil.Sdk"
SEV_MAP = {1: "error", 2: "warning"}

UIPCLI_SEARCH = [
    str(Path.home() / "AppData/Local/cpmf/tools/uipcli-*/uipcli.exe"),
    str(Path.home() / "AppData/Local/UiPath/uipathcli/modules/uipcli-win-*/tools/uipcli.exe"),
]

TAP_RUNS_ROOT = Path(os.environ.get("LOCALAPPDATA", "")) / "WatchfulAnvil" / "runs"


# ---------------------------------------------------------------------------
# uipcli discovery  (duplicated from tools/corpus-harness/run.py)
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
# nupkg discovery  (duplicated from tools/corpus-harness/run.py)
# ---------------------------------------------------------------------------

def find_latest_nupkg(
    nupkg_dir: Path, version_override: str | None, pack: str
) -> tuple[Path, str]:
    if version_override:
        p = nupkg_dir / f"{pack}.{version_override}.nupkg"
        if not p.exists():
            raise RuntimeError(f"nupkg not found: {p}")
        return p, version_override

    candidates = [
        Path(p)
        for p in glob.glob(str(nupkg_dir / f"{pack}.*.nupkg"))
        if "unpacked" not in p
    ]
    if not candidates:
        raise RuntimeError(
            f"No {pack} nupkg found in {nupkg_dir}\n"
            f"Run: just pack   (or: dotnet pack <project for {pack}> -c Release -o nupkg/)"
        )
    path = max(candidates, key=os.path.getmtime)
    version = path.stem.replace(f"{pack}.", "")
    return path, version


def assert_sdk_in_feed(nupkg_dir: Path) -> None:
    """Fail early when the feed lacks WatchfulAnvil.Sdk.

    Packs declare a dependency on the SDK rather than merging it. If it is absent from
    the flat feed, uipcli's restore fails and run_analyze returns no result file -- which
    surfaces as an empty violation list, i.e. a clean run. Better to say what is actually
    wrong than to report success.
    """
    found = [
        p for p in glob.glob(str(nupkg_dir / f"{SDK_PACKAGE}.*.nupkg"))
        if "unpacked" not in p
    ]
    if not found:
        raise RuntimeError(
            f"No {SDK_PACKAGE} nupkg in {nupkg_dir}\n"
            f"Rule packs depend on it rather than merging it, so restore will fail and "
            f"the run would report no violations instead of an error.\n"
            f"Run: just pack"
        )


# ---------------------------------------------------------------------------
# NuGet.config generation  (duplicated from tools/corpus-harness/run.py)
# ---------------------------------------------------------------------------

def make_nuget_config(nupkg_dir: Path, output_path: Path) -> None:
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
# project.json patching  (duplicated from tools/corpus-harness/run.py)
# ---------------------------------------------------------------------------

def patch_project_json(src: Path, dest: Path, versions: dict[str, str]) -> None:
    """Inject one or more rule packs as project dependencies.

    Takes a {pack: version} map so several packs can be analysed in one run -- the
    dist/ shape, where Cpmf.Standard and Cpmf.Tap are meant to be exercised together.
    """
    with open(src, encoding="utf-8") as f:
        data = json.load(f)
    deps = data.get("dependencies", {})
    deps.update(versions)
    data["dependencies"] = deps
    with open(dest, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
        f.write("\n")


# ---------------------------------------------------------------------------
# uipcli analyze  (duplicated from tools/corpus-harness/run.py)
# ---------------------------------------------------------------------------

def run_analyze(
    uipcli: Path,
    workspace: Path,
    nuget_config: Path,
    governance: Path | None,
    result_path: Path,
) -> tuple[list, str]:
    """Launch uipcli package analyze and return (violations, stderr_text)."""
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

    proc = subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    try:
        _, stderr_bytes = proc.communicate(timeout=300)
    except subprocess.TimeoutExpired:
        proc.kill()
        proc.communicate()
        raise RuntimeError("uipcli timed out after 300 s")

    stderr_text = stderr_bytes.decode("utf-8", errors="replace").strip()

    if not result_path.exists():
        return [], stderr_text
    with open(result_path, encoding="utf-8") as f:
        return json.load(f), stderr_text


# ---------------------------------------------------------------------------
# Normalization  (duplicated from tools/corpus-harness/run.py)
# ---------------------------------------------------------------------------

def normalize(raw: list) -> list:
    seen: set = set()
    out = []
    for r in raw:
        key = (r.get("ErrorCode"), r.get("FilePath"), r.get("Description"))
        if key in seen:
            continue
        seen.add(key)
        fp = (r.get("FilePath") or "").replace("\\", "/")
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
# TAP run directory discovery
# ---------------------------------------------------------------------------

def find_tap_run(project_path: Path, after: datetime) -> Path | None:
    """
    Scan TAP_RUNS_ROOT for the run whose run-meta.json matches the analyzed
    project and started at or after `after` (UTC). Returns the newest match,
    or None if the TAP rules did not fire (e.g. not in this nupkg).
    """
    if not TAP_RUNS_ROOT.is_dir():
        return None

    project_dir = str(project_path.parent).replace("\\", "/").lower()
    best: tuple[datetime, Path] | None = None

    for run_dir in TAP_RUNS_ROOT.iterdir():
        if not run_dir.is_dir():
            continue
        meta_path = run_dir / "run-meta.json"
        if not meta_path.exists():
            continue
        try:
            with open(meta_path, encoding="utf-8") as f:
                meta = json.load(f)
            started_str = meta.get("startedAt", "")
            started = datetime.fromisoformat(started_str.replace("Z", "+00:00"))
            if started.tzinfo is None:
                started = started.replace(tzinfo=timezone.utc)
            if started < after:
                continue
            fp = meta.get("projectFilePath", "").replace("\\", "/").lower()
            if project_dir not in fp:
                continue
            if best is None or started > best[0]:
                best = (started, run_dir)
        except Exception:
            continue

    return best[1] if best else None


# ---------------------------------------------------------------------------
# Output helpers
# ---------------------------------------------------------------------------

def print_violations_table(violations: list) -> None:
    if not violations:
        print("No violations found.")
        return
    col_id = max(len(v["ruleId"]) for v in violations)
    col_sev = max(len(v["severity"]) for v in violations)
    col_wf = max((len(v["workflow"] or "-") for v in violations), default=1)
    header = f"{'Rule':<{col_id}}  {'Sev':<{col_sev}}  {'Workflow':<{col_wf}}  Description"
    print(header)
    print("-" * len(header))
    for v in violations:
        wf = v["workflow"] or "-"
        desc = v["description"][:100]
        print(f"{v['ruleId']:<{col_id}}  {v['severity']:<{col_sev}}  {wf:<{col_wf}}  {desc}")


def stream_tap_file(path: Path, label: str) -> None:
    if not path.exists():
        print(f"  (no {label})")
        return
    print(f"\n--- {label} ---")
    with open(path, encoding="utf-8") as f:
        for line in f:
            print(line, end="")


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main() -> None:
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(errors="replace")

    parser = argparse.ArgumentParser(
        description="Run WatchfulAnvil rules against a UiPath project (issue #50)"
    )
    parser.add_argument("--project", required=True, type=Path,
                        help="Path to project.json")
    parser.add_argument("--feed", type=Path, default=None,
                        help="Directory containing the rule-pack nupkg (default: <repo>/nupkg)")
    parser.add_argument("--version", default=None,
                        help="Pin rule-pack version")
    parser.add_argument("--pack", action="append", dest="packs", default=None,
                        help=f"Rule pack package id; repeatable "
                             f"(default: {DEFAULT_RULE_PACK}). Supplying any --pack "
                             f"replaces the default rather than adding to it.")
    parser.add_argument("--uipcli", type=Path, default=None,
                        help="Path to uipcli.exe")
    parser.add_argument("--governance", type=Path, default=None,
                        help="Governance JSON file")
    parser.add_argument("--violations-json", action="store_true",
                        help="Print raw uipcli JSON to stdout")
    parser.add_argument("--tap", action="store_true",
                        help="Stream TAP activity.jsonl + workflow.jsonl after violations")
    parser.add_argument("--tap-dir", action="store_true",
                        help="Print only the TAP run directory path")
    args = parser.parse_args()

    project_path = args.project.resolve()
    if not project_path.exists():
        print(f"ERROR: project.json not found: {project_path}", file=sys.stderr)
        sys.exit(2)

    repo_root = Path(__file__).resolve().parent.parent.parent
    nupkg_dir = args.feed.resolve() if args.feed else (repo_root / "nupkg")

    packs = args.packs or [DEFAULT_RULE_PACK]
    if args.version and len(packs) > 1:
        print("ERROR: --version pins a single pack; drop it when passing several --pack.",
              file=sys.stderr)
        sys.exit(2)

    try:
        uipcli = args.uipcli.resolve() if args.uipcli else find_uipcli()
        assert_sdk_in_feed(nupkg_dir)
        pack_versions = {
            pack: find_latest_nupkg(nupkg_dir, args.version, pack)[1] for pack in packs
        }
    except RuntimeError as e:
        print(f"ERROR: {e}", file=sys.stderr)
        sys.exit(2)

    governance = args.governance.resolve() if args.governance else None

    if not args.tap_dir:
        for pack, version in pack_versions.items():
            print(f"Rule pack : {pack} {version}")
        print(f"uipcli    : {uipcli}")
        print(f"Project   : {project_path}")
        if governance:
            print(f"Governance: {governance}")
        print()

    pre_run_time = datetime.now(tz=timezone.utc)

    # Read original project.json bytes for restore.
    original_project_json = project_path.read_bytes()

    def _restore():
        try:
            project_path.write_bytes(original_project_json)
        except Exception as e:
            print(f"WARNING: could not restore project.json: {e}", file=sys.stderr)

    with tempfile.TemporaryDirectory() as _tmpdir:
        tmpdir = Path(_tmpdir)
        nuget_config = tmpdir / "NuGet.config"
        make_nuget_config(nupkg_dir, nuget_config)

        result_file = tmpdir / "result.json"

        # Bound before the try: the only handler here is `finally`, so an exception
        # propagates past the reads below. Harmless today, a NameError the moment
        # anyone adds an `except`.
        raw: list = []
        stderr_text = ""

        try:
            patch_project_json(project_path, project_path, pack_versions)
            raw, stderr_text = run_analyze(
                uipcli, project_path.parent, nuget_config, governance, result_file,
            )
        finally:
            _restore()

    if not raw and stderr_text:
        print(f"WARNING: uipcli produced no results. stderr:\n{stderr_text}", file=sys.stderr)

    violations = normalize(raw)

    tap_run = find_tap_run(project_path, pre_run_time)

    if args.tap_dir:
        if tap_run:
            print(tap_run)
        else:
            print("(no TAP run directory found)", file=sys.stderr)
            sys.exit(2)
        sys.exit(0)

    if args.violations_json:
        print(json.dumps(raw, indent=2, ensure_ascii=False))
    else:
        print_violations_table(violations)

    if tap_run:
        print(f"\nTAP run   : {tap_run}")
    else:
        print("\nTAP run   : (none - TAP rules did not fire or run-meta.json not found)")

    if args.tap and tap_run:
        stream_tap_file(tap_run / "workflow.jsonl", "workflow.jsonl")
        stream_tap_file(tap_run / "activity.jsonl", "activity.jsonl")

    sys.exit(1 if violations else 0)


if __name__ == "__main__":
    main()
