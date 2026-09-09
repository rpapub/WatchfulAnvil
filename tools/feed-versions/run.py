# /// script
# requires-python = ">=3.11"
# dependencies = ["packaging>=23.0"]
# ///
"""
WatchfulAnvil Feed Versions Tool

Asks the remote feeds what versions exist for the packages this repo pins, so a
new UiPath.Activities.Api (or any other dependency) release is visible without
hunting through nuget.org by hand.

By default it discovers what the repo pins -- the UiPath.Activities.Api matrix in
build/WatchfulAnvil.Build/build/WatchfulAnvil.Build.props plus every literal
PackageReference version across the csproj files -- then reports, per package,
what is pinned versus what the feed offers.

Usage (from WatchfulAnvil repo root):
    uv run tools/feed-versions/run.py                    # what we pin vs what exists
    uv run tools/feed-versions/run.py --check            # exit 1 if anything is behind
    uv run tools/feed-versions/run.py --package UiPath.Activities.Api --all
    uv run tools/feed-versions/run.py --prerelease --format json

Options:
    --package <id>       Query this package instead of the discovered set. Repeatable.
    --all                List every version, not just the newest.
    --prerelease         Include prerelease versions when picking "latest".
    --check              Exit 1 if any pinned package has a newer release.
    --format {table,json}
    --source <url|name>  Override the feed. Names: uipath, nuget. Repeatable;
                         each package is tried against every source until found.
    --timeout <seconds>  HTTP timeout (default 20).

Exit codes:
    0   OK (or, without --check, always when the queries succeeded)
    1   --check was passed and at least one package is behind
    2   Tool / network error
"""

import argparse
import json
import re
import sys
import urllib.error
import urllib.request
from pathlib import Path

from packaging.version import InvalidVersion, Version

# ---------------------------------------------------------------------------
# Feeds
# ---------------------------------------------------------------------------

SOURCES = {
    "uipath": "https://uipath.pkgs.visualstudio.com/Public.Feeds/_packaging/UiPath-Official/nuget/v3/index.json",
    "nuget": "https://api.nuget.org/v3/index.json",
}

# UiPath packages only exist on the UiPath feed; everything else on nuget.org.
# Order matters: the first source that has the package wins.
def default_sources_for(package_id: str) -> list[str]:
    if package_id.lower().startswith("uipath."):
        return [SOURCES["uipath"], SOURCES["nuget"]]
    return [SOURCES["nuget"], SOURCES["uipath"]]


_service_index_cache: dict[str, str] = {}


def flat_container_base(service_index_url: str, timeout: int) -> str:
    """Resolve a v3 service index to its PackageBaseAddress (flat container) root."""
    if service_index_url in _service_index_cache:
        return _service_index_cache[service_index_url]

    with urllib.request.urlopen(service_index_url, timeout=timeout) as resp:
        index = json.load(resp)

    for res in index.get("resources", []):
        if res.get("@type", "").startswith("PackageBaseAddress/3.0.0"):
            base = res["@id"]
            if not base.endswith("/"):
                base += "/"
            _service_index_cache[service_index_url] = base
            return base

    raise RuntimeError(f"No PackageBaseAddress resource in service index: {service_index_url}")


def fetch_versions(package_id: str, sources: list[str], timeout: int) -> tuple[list[str], str | None]:
    """Return (versions, source_url). Empty list if the package is on no source."""
    for src in sources:
        try:
            base = flat_container_base(src, timeout)
            url = f"{base}{package_id.lower()}/index.json"
            with urllib.request.urlopen(url, timeout=timeout) as resp:
                return json.load(resp).get("versions", []), src
        except urllib.error.HTTPError as e:
            if e.code == 404:
                continue  # not on this feed; try the next
            raise
    return [], None


# ---------------------------------------------------------------------------
# Version handling
# ---------------------------------------------------------------------------

def parse(v: str) -> Version | None:
    try:
        return Version(v)
    except InvalidVersion:
        return None


def newest(versions: list[str], allow_prerelease: bool) -> str | None:
    parsed = [(parse(v), v) for v in versions]
    usable = [(p, raw) for p, raw in parsed if p is not None]
    if not allow_prerelease:
        usable = [(p, raw) for p, raw in usable if not p.is_prerelease]
    if not usable:
        return None
    return max(usable, key=lambda t: t[0])[1]


def count_newer(versions: list[str], pinned: str, allow_prerelease: bool) -> int:
    pin = parse(pinned)
    if pin is None:
        return 0
    n = 0
    for v in versions:
        p = parse(v)
        if p is None or (p.is_prerelease and not allow_prerelease):
            continue
        if p > pin:
            n += 1
    return n


# ---------------------------------------------------------------------------
# Discovery: what does this repo pin?
# ---------------------------------------------------------------------------

_RE_PKGREF = re.compile(
    r'<PackageReference\s+[^>]*?Include="([^"]+)"[^>]*?Version="([^"]+)"', re.IGNORECASE
)
_RE_API_PROP = re.compile(
    r"<WatchfulAnvilApiVersion(?:Net\w+)?>([^<]+)</WatchfulAnvilApiVersion(?:Net\w+)?>",
    re.IGNORECASE,
)
_RE_XML_COMMENT = re.compile(r"<!--.*?-->", re.DOTALL)


def _strip_comments(text: str) -> str:
    """Drop XML comments before scanning.

    Without this, a documented example such as
        <WatchfulAnvilApiVersionNet8>25.4.1</WatchfulAnvilApiVersionNet8>
    inside a comment is reported as a real pin, and then shows as a phantom
    version that no feed has ever published.
    """
    return _RE_XML_COMMENT.sub("", text)


def discover_pins(repo_root: Path) -> dict[str, set[str]]:
    """Map package id -> set of pinned version strings found across the repo."""
    pins: dict[str, set[str]] = {}

    # The API version matrix lives in the shared props, not in the csproj files.
    props = repo_root / "build" / "WatchfulAnvil.Build" / "build" / "WatchfulAnvil.Build.props"
    if props.exists():
        for v in _RE_API_PROP.findall(_strip_comments(props.read_text(encoding="utf-8"))):
            pins.setdefault("UiPath.Activities.Api", set()).add(v.strip())

    # .props too: StyleCop and friends are declared in Directory.Build.props,
    # not in any csproj.
    candidates = [
        p for pat in ("*.csproj", "*.props", "*.targets") for p in repo_root.rglob(pat)
    ]
    for csproj in candidates:
        if any(part in {"bin", "obj"} for part in csproj.parts):
            continue
        for pkg, ver in _RE_PKGREF.findall(_strip_comments(csproj.read_text(encoding="utf-8"))):
            ver = ver.strip()
            # Unexpanded MSBuild properties are resolved from the props file above.
            if ver.startswith("$("):
                continue
            pins.setdefault(pkg.strip(), set()).add(ver)

    return pins


# ---------------------------------------------------------------------------
# Reporting
# ---------------------------------------------------------------------------

def build_report(pins, packages, allow_prerelease, sources_override, timeout):
    rows = []
    for pkg in packages:
        sources = sources_override or default_sources_for(pkg)
        versions, src = fetch_versions(pkg, sources, timeout)
        pinned = sorted(pins.get(pkg, set()))
        latest_stable = newest(versions, allow_prerelease=False)
        latest_any = newest(versions, allow_prerelease=True)
        target = latest_any if allow_prerelease else latest_stable

        behind = 0
        if pinned and versions:
            behind = max(count_newer(versions, p, allow_prerelease) for p in pinned)

        rows.append({
            "package": pkg,
            "pinned": pinned,
            "latest_stable": latest_stable,
            "latest_prerelease": latest_any if latest_any != latest_stable else None,
            "newer_than_pinned": behind,
            "target": target,
            "found": bool(versions),
            "source": src,
            "versions": versions,
        })
    return rows


def print_table(rows, show_all):
    if not rows:
        print("No packages to report.")
        return

    def fmt_pinned(r):
        return ", ".join(r["pinned"]) if r["pinned"] else "-"

    def fmt_status(r):
        if not r["found"]:
            return "not on feed"
        if not r["pinned"]:
            return "-"
        return "current" if r["newer_than_pinned"] == 0 else f"{r['newer_than_pinned']} newer"

    w_pkg = max(len("PACKAGE"), *(len(r["package"]) for r in rows))
    w_pin = max(len("PINNED"), *(len(fmt_pinned(r)) for r in rows))
    w_lat = max(len("LATEST"), *(len(r["latest_stable"] or "-") for r in rows))
    w_pre = max(len("PRERELEASE"), *(len(r["latest_prerelease"] or "-") for r in rows))

    print(f"{'PACKAGE':<{w_pkg}}  {'PINNED':<{w_pin}}  {'LATEST':<{w_lat}}  {'PRERELEASE':<{w_pre}}  STATUS")
    print(f"{'-' * w_pkg}  {'-' * w_pin}  {'-' * w_lat}  {'-' * w_pre}  ------")
    for r in sorted(rows, key=lambda x: (-x["newer_than_pinned"], x["package"])):
        print(
            f"{r['package']:<{w_pkg}}  {fmt_pinned(r):<{w_pin}}  "
            f"{(r['latest_stable'] or '-'):<{w_lat}}  {(r['latest_prerelease'] or '-'):<{w_pre}}  "
            f"{fmt_status(r)}"
        )

    if show_all:
        for r in sorted(rows, key=lambda x: x["package"]):
            print()
            print(f"{r['package']}  ({len(r['versions'])} versions)")
            for v in r["versions"]:
                marker = " <- pinned" if v in r["pinned"] else ""
                print(f"    {v}{marker}")


def main() -> int:
    repo_root = Path(__file__).resolve().parent.parent.parent

    ap = argparse.ArgumentParser(description="Report available versions from the remote feeds.")
    ap.add_argument("--package", action="append", dest="packages")
    ap.add_argument("--all", action="store_true", dest="show_all")
    ap.add_argument("--prerelease", action="store_true")
    ap.add_argument("--check", action="store_true")
    ap.add_argument("--format", choices=["table", "json"], default="table")
    ap.add_argument("--source", action="append", dest="sources")
    ap.add_argument("--timeout", type=int, default=20)
    args = ap.parse_args()

    sources_override = None
    if args.sources:
        sources_override = [SOURCES.get(s, s) for s in args.sources]

    pins = discover_pins(repo_root)
    packages = args.packages or sorted(pins)

    if not packages:
        print("No packages discovered and none given with --package.", file=sys.stderr)
        return 2

    try:
        rows = build_report(pins, packages, args.prerelease, sources_override, args.timeout)
    except (urllib.error.URLError, RuntimeError, TimeoutError) as e:
        print(f"ERROR: feed query failed: {e}", file=sys.stderr)
        return 2

    if args.format == "json":
        payload = rows if args.show_all else [{k: v for k, v in r.items() if k != "versions"} for r in rows]
        print(json.dumps(payload, indent=2))
    else:
        print_table(rows, args.show_all)

    if args.check:
        behind = [r for r in rows if r["newer_than_pinned"] > 0]
        if behind:
            print(file=sys.stderr)
            for r in behind:
                print(
                    f"BEHIND: {r['package']} pins {', '.join(r['pinned'])}; "
                    f"newest is {r['target']}",
                    file=sys.stderr,
                )
            return 1

    return 0


if __name__ == "__main__":
    sys.exit(main())
