# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""
WatchfulAnvil TAP Inspection Tool  (issue #50)

Runs only the TAP inspection rules against an arbitrary UiPath project.json
and surfaces the inspection log (activity.jsonl, workflow.jsonl) written by
those rules. Requires uipcli >= 25.

Usage (from WatchfulAnvil repo root):
    uv run tools/analyze/tap.py --project path/to/project.json [options]

Options:
    --project <path>        Path to project.json (required)
    --feed <path>           Directory containing the rule-pack nupkg
                            (default: <repo-root>/nupkg)
    --version <ver>         Pin a specific rule-pack version
                            (default: latest nupkg by modification time)
    --uipcli <path>         Path to uipcli.exe (must be >= 25)
                            (default: newest under ~/AppData/Local/cpmf/tools/)
    --governance <path>     Override governance file
                            (default: tools/governance/tap-only.json)
    --tap                   Stream activity.jsonl + workflow.jsonl to stdout
    --tap-dir               Print only the TAP run directory path

Exit codes:
    0   Success
    2   Tool / setup error
"""

import argparse
import glob
import json
import os
import re
import subprocess
import sys
import tempfile
from datetime import datetime, timezone
from pathlib import Path

RULE_PACK = "Cpmf.Tap"
SEV_MAP = {1: "error", 2: "warning"}

UIPCLI_SEARCH = [
    str(Path.home() / "AppData/Local/cpmf/tools/uipcli-*/uipcli.exe"),
    str(Path.home() / "AppData/Local/UiPath/uipathcli/modules/uipcli-win-*/tools/uipcli.exe"),
]

TAP_RUNS_ROOT = Path(os.environ.get("LOCALAPPDATA", "")) / "WatchfulAnvil" / "runs"

_RE_UIPCLI_VERSION = re.compile(r"uipcli-(\d+)\.")


# ---------------------------------------------------------------------------
# uipcli discovery + version check
# ---------------------------------------------------------------------------

def find_uipcli(min_major: int = 25) -> Path:
    for pattern in UIPCLI_SEARCH:
        matches = sorted(glob.glob(pattern), key=os.path.getmtime, reverse=True)
        for m in matches:
            p = Path(m)
            ver_match = _RE_UIPCLI_VERSION.search(p.parts[-2])
            major = int(ver_match.group(1)) if ver_match else 0
            if major >= min_major:
                return p
    raise RuntimeError(
        f"uipcli.exe >= {min_major} not found. Searched:\n"
        + "\n".join(f"  {p}" for p in UIPCLI_SEARCH)
        + f"\nInstall UiPath CLI >= {min_major} and re-run, or pass --uipcli <path>."
    )


def check_uipcli_version(uipcli: Path, min_major: int = 25) -> None:
    ver_match = _RE_UIPCLI_VERSION.search(str(uipcli))
    if ver_match:
        if int(ver_match.group(1)) < min_major:
            raise RuntimeError(
                f"uipcli {uipcli} is below the required major version {min_major}."
            )


# ---------------------------------------------------------------------------
# nupkg discovery
# ---------------------------------------------------------------------------

def find_latest_nupkg(nupkg_dir: Path, version_override: str | None) -> tuple[Path, str]:
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



# ---------------------------------------------------------------------------
# project.json patching
# ---------------------------------------------------------------------------

def patch_project_json(src: Path, dest: Path, version: str) -> None:
    with open(src, encoding="utf-8") as f:
        data = json.load(f)
    deps = data.get("dependencies", {})
    deps[RULE_PACK] = version
    data["dependencies"] = deps
    with open(dest, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
        f.write("\n")


# ---------------------------------------------------------------------------
# uipcli analyze
# ---------------------------------------------------------------------------

LOCAL_NUGET_SOURCE = r"C:\Users\Public\Documents\myNugetPackages"


def make_nuget_config(nupkg_dir: Path, output_path: Path) -> None:
    xml = (
        '<?xml version="1.0" encoding="utf-8"?>\n'
        "<configuration>\n"
        "  <packageSources>\n"
        f'    <add key="local-nupkg" value="{nupkg_dir}" />\n'
        f'    <add key="myNugetPackages" value="{LOCAL_NUGET_SOURCE}" />\n'
        '    <add key="UiPath-Official" value="https://uipath.pkgs.visualstudio.com/'
        'Public.Feeds/_packaging/UiPath-Official/nuget/v3/index.json" />\n'
        '    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />\n'
        "  </packageSources>\n"
        "</configuration>\n"
    )
    output_path.write_text(xml, encoding="utf-8")


def run_analyze(
    uipcli: Path,
    workspace: Path,
    nuget_config: Path,
    governance: Path | None,
    result_path: Path,
) -> tuple[list, str]:
    """Launch uipcli package analyze and return (raw_results, stderr_text)."""
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
# TAP run directory discovery
# ---------------------------------------------------------------------------

def find_tap_run(after: datetime) -> Path | None:
    """Return the TAP run directory created at or after `after`, via latest.txt."""
    latest_txt = TAP_RUNS_ROOT / "latest.txt"
    if not latest_txt.exists():
        return None
    try:
        run_id = latest_txt.read_text(encoding="utf-8").strip()
        run_dir = TAP_RUNS_ROOT / run_id
        meta_path = run_dir / "run-meta.json"
        if not meta_path.exists():
            return None
        with open(meta_path, encoding="utf-8") as f:
            meta = json.load(f)
        started_str = meta.get("startedAt", "")
        started = datetime.fromisoformat(started_str.replace("Z", "+00:00"))
        if started.tzinfo is None:
            started = started.replace(tzinfo=timezone.utc)
        if started < after:
            return None
        return run_dir
    except Exception:
        return None


# ---------------------------------------------------------------------------
# Output helpers
# ---------------------------------------------------------------------------

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

    repo_root = Path(__file__).resolve().parent.parent.parent
    default_governance = repo_root / "tools" / "governance" / "tap-only.json"

    parser = argparse.ArgumentParser(
        description="Run TAP inspection rules against a UiPath project (issue #50)"
    )
    parser.add_argument("--project", required=True, type=Path,
                        help="Path to project.json")
    parser.add_argument("--feed", type=Path, default=None,
                        help="Directory containing the rule-pack nupkg (default: <repo>/nupkg)")
    parser.add_argument("--version", default=None,
                        help="Pin rule-pack version")
    parser.add_argument("--uipcli", type=Path, default=None,
                        help="Path to uipcli.exe (must be >= 25)")
    parser.add_argument("--governance", type=Path, default=None,
                        help=f"Override governance file (default: tools/governance/tap-only.json)")
    parser.add_argument("--tap", action="store_true",
                        help="Stream activity.jsonl + workflow.jsonl to stdout")
    parser.add_argument("--tap-dir", action="store_true",
                        help="Print only the TAP run directory path")
    args = parser.parse_args()

    project_path = args.project.resolve()
    if not project_path.exists():
        print(f"ERROR: project.json not found: {project_path}", file=sys.stderr)
        sys.exit(2)

    nupkg_dir = args.feed.resolve() if args.feed else (repo_root / "nupkg")
    governance = (args.governance or default_governance).resolve()

    if not governance.exists():
        print(f"ERROR: governance file not found: {governance}", file=sys.stderr)
        sys.exit(2)

    try:
        if args.uipcli:
            uipcli = args.uipcli.resolve()
            check_uipcli_version(uipcli)
        else:
            uipcli = find_uipcli(min_major=25)
        nupkg_path, version = find_latest_nupkg(nupkg_dir, args.version)
    except RuntimeError as e:
        print(f"ERROR: {e}", file=sys.stderr)
        sys.exit(2)

    if not args.tap_dir:
        print(f"Rule pack : {RULE_PACK} {version}")
        print(f"uipcli    : {uipcli}")
        print(f"Project   : {project_path}")
        print(f"Governance: {governance}")
        print()

    pre_run_time = datetime.now(tz=timezone.utc)

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

        try:
            patch_project_json(project_path, project_path, version)
            raw, stderr_text = run_analyze(
                uipcli, project_path.parent, nuget_config, governance, result_file,
            )
        finally:
            _restore()

    if not raw and stderr_text:
        print(f"WARNING: uipcli produced no results. stderr:\n{stderr_text}", file=sys.stderr)

    tap_run = find_tap_run(pre_run_time)

    if args.tap_dir:
        if tap_run:
            print(tap_run)
        else:
            print("(no TAP run directory found)", file=sys.stderr)
            sys.exit(2)
        sys.exit(0)

    if tap_run:
        print(f"TAP run   : {tap_run}")
        if args.tap:
            stream_tap_file(tap_run / "workflow.jsonl", "workflow.jsonl")
            stream_tap_file(tap_run / "activity.jsonl", "activity.jsonl")
    else:
        print("TAP run   : (none - TAP rules did not fire)")
        if stderr_text:
            print(f"\nuipcli stderr:\n{stderr_text}", file=sys.stderr)

    sys.exit(0)


if __name__ == "__main__":
    main()
