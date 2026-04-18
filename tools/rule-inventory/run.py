# /// script
# requires-python = ">=3.11"
# dependencies = ["pyyaml>=6.0"]
# ///
"""
WatchfulAnvil Rule Inventory Tool

Validates registry/Cpmf/rules.yaml against C# source, and exports rule
metadata for documentation and roadmap planning.

Usage (from WatchfulAnvil repo root):
    uv run tools/rule-inventory/run.py [options]

Options:
    (no args)            Run --check
    --check              Validate registry against C# source; exit 1 on drift
    --format markdown    Print rules as a markdown table to stdout
    --format json        Print rules as a JSON array to stdout
    --add <ID> <CLASS>   Append a stub entry to the registry YAML
    --registry <path>    Override registry file (default: registry/Cpmf/rules.yaml)
    --src <path>         Override src root (default: src/)
"""

import argparse
import glob
import json
import re
import sys
from pathlib import Path

import yaml

# ---------------------------------------------------------------------------
# C# source parsing
# ---------------------------------------------------------------------------

_RE_REGISTERED = re.compile(r"new\s+(\w+)\(\)\.Initialize")
_RE_RULE_ID = re.compile(r'protected override string Id\s*=>\s*"(.+?)"')
_RE_SEVERITY = re.compile(r"protected override TraceLevel DefaultSeverity\s*=>\s*TraceLevel\.(\w+)")
_RE_ENABLED = re.compile(r"protected override bool IsEnabledByDefault\s*=>\s*(true|false)")


def find_registered_classes(src_root: Path, package: str) -> list[tuple[str, Path]]:
    """Return [(className, cs_file)] from <package>/RegisterAnalyzerConfiguration.cs."""
    config_file = src_root / package / "RegisterAnalyzerConfiguration.cs"
    if not config_file.exists():
        # Fall back to glob if the direct path doesn't exist
        pattern = str(src_root / "**" / "RegisterAnalyzerConfiguration.cs")
        matches = glob.glob(pattern, recursive=True)
        if not matches:
            raise RuntimeError(f"RegisterAnalyzerConfiguration.cs not found under {src_root}")
        config_file = Path(matches[0])
    text = config_file.read_text(encoding="utf-8")
    class_names = _RE_REGISTERED.findall(text)

    results = []
    for cls in class_names:
        cs_pattern = str(src_root / "**" / f"{cls}.cs")
        cs_matches = glob.glob(cs_pattern, recursive=True)
        cs_path = Path(cs_matches[0]) if cs_matches else None
        results.append((cls, cs_path))
    return results


def extract_rule_id(cs_path: Path) -> str | None:
    if cs_path is None or not cs_path.exists():
        return None
    text = cs_path.read_text(encoding="utf-8")
    m = _RE_RULE_ID.search(text)
    return m.group(1) if m else None


def extract_severity(cs_path: Path) -> str | None:
    if cs_path is None or not cs_path.exists():
        return None
    text = cs_path.read_text(encoding="utf-8")
    m = _RE_SEVERITY.search(text)
    return m.group(1) if m else None  # "Warning", "Error", etc. — or None = default Error


def extract_is_enabled(cs_path: Path) -> bool | None:
    if cs_path is None or not cs_path.exists():
        return None
    text = cs_path.read_text(encoding="utf-8")
    m = _RE_ENABLED.search(text)
    return (m.group(1) == "true") if m else None  # None = default True


# ---------------------------------------------------------------------------
# Check mode
# ---------------------------------------------------------------------------

def cmd_check(registry_path: Path, src_root: Path) -> int:
    with open(registry_path, encoding="utf-8") as f:
        registry = yaml.safe_load(f)

    reg_by_class: dict[str, dict] = {
        r["className"]: r for r in registry.get("rules", [])
    }

    package = registry.get("package", "")
    registered = find_registered_classes(src_root, package)
    issues = 0

    print(f"Registry : {registry_path}")
    print(f"Src root : {src_root}")
    print(f"Registered classes: {len(registered)}")
    print()

    for cls, cs_path in registered:
        cs_id = extract_rule_id(cs_path)
        reg_entry = reg_by_class.get(cls)

        if reg_entry is None:
            id_hint = f"  ({cs_id})" if cs_id else ""
            print(f"  MISSING FROM REGISTRY  {cls}{id_hint}")
            issues += 1
            continue

        if cs_id and cs_id != reg_entry["id"]:
            print(f"  ID MISMATCH  {cls}: C#={cs_id!r}  registry={reg_entry['id']!r}")
            issues += 1
            continue

        cs_sev = extract_severity(cs_path)
        reg_sev = reg_entry.get("defaultSeverity") or "Error"
        if cs_sev and cs_sev != reg_sev:
            print(f"  SEVERITY MISMATCH  {cls} ({reg_entry['id']}): C#={cs_sev!r}  registry={reg_sev!r}")
            issues += 1
            continue

        cs_enabled = extract_is_enabled(cs_path)
        reg_enabled = reg_entry.get("defaultIsEnabled", True)
        if cs_enabled is not None and cs_enabled != reg_enabled:
            print(f"  ENABLED MISMATCH  {cls} ({reg_entry['id']}): C#={cs_enabled}  registry={reg_enabled}")
            issues += 1
            continue

        print(f"  OK  {reg_entry['id']}  {cls}")

    # Check for stale registry entries
    registered_classes = {cls for cls, _ in registered}
    for cls, entry in reg_by_class.items():
        if cls not in registered_classes:
            print(f"  STALE IN REGISTRY  {cls}  ({entry['id']}) — not in RegisterAnalyzerConfiguration.cs")
            issues += 1

    print()
    if issues:
        print(f"FAIL — {issues} issue(s) found")
        return 1
    print("OK — registry is in sync with C# source")
    return 0


# ---------------------------------------------------------------------------
# Format mode
# ---------------------------------------------------------------------------

SEV_ABBREV = {"Error": "error", "Warning": "warn", "Info": "info", None: "-"}
SCOPE_SHORT = {
    "IActivityModel": "Activity",
    "IWorkflowModel": "Workflow",
    "IProjectModel": "Project",
    "IProjectSummary": "Summary",
}


def cmd_format_markdown(registry_path: Path) -> None:
    with open(registry_path, encoding="utf-8") as f:
        registry = yaml.safe_load(f)

    rules = registry.get("rules", [])
    pkg = registry.get("package", "")
    ver = registry.get("version", "")

    print(f"## {pkg} - Rule Reference (v{ver})\n")
    print("| ID | Name | Scope | Cat | Sev | On | Params |")
    print("|---|---|---|---|---|---|---|")
    for r in rules:
        sev = SEV_ABBREV.get(r.get("defaultSeverity"), r.get("defaultSeverity", "-"))
        scope = SCOPE_SHORT.get(r.get("scope", ""), r.get("scope", ""))
        enabled = "yes" if r.get("defaultIsEnabled", True) else "opt-in"
        params = len(r.get("parameters") or [])
        params_str = str(params) if params else "-"
        print(f"| {r['id']} | {r['name']} | {scope} | {r.get('categoryCode','')} | {sev} | {enabled} | {params_str} |")

    params_rows = [
        (r["id"], p["key"], p.get("displayName", ""), p.get("defaultValue", ""))
        for r in rules
        for p in (r.get("parameters") or [])
    ]
    if params_rows:
        print()
        print("### Parameters\n")
        print("| Rule | Key | Display Name | Default |")
        print("|---|---|---|---|")
        for rule_id, key, display, default in params_rows:
            print(f"| {rule_id} | `{key}` | {display} | `{default}` |")


def cmd_format_json(registry_path: Path) -> None:
    with open(registry_path, encoding="utf-8") as f:
        registry = yaml.safe_load(f)
    print(json.dumps(registry.get("rules", []), indent=2, ensure_ascii=False))


# ---------------------------------------------------------------------------
# Add stub mode
# ---------------------------------------------------------------------------

_STUB_TEMPLATE = """\

  - id: {id}
    className: {cls}
    name: "TODO: rule name"
    description: "TODO: one-line description."
    recommendation: "TODO: actionable guidance."
    categoryCode: "TODO"
    scope: IWorkflowModel
    type: Rule
    defaultSeverity: Error
    defaultIsEnabled: true
    requiredFeature: null
    documentationLink: null
    parameters: []
"""


def cmd_add(registry_path: Path, rule_id: str, class_name: str) -> None:
    text = registry_path.read_text(encoding="utf-8")
    stub = _STUB_TEMPLATE.format(id=rule_id, cls=class_name)
    registry_path.write_text(text.rstrip() + "\n" + stub, encoding="utf-8")
    print(f"Added stub for {rule_id} ({class_name}) to {registry_path}")
    print("Edit the entry to fill in name, description, recommendation, categoryCode, scope.")


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main() -> None:
    repo_root = Path(__file__).resolve().parent.parent.parent
    default_registry = repo_root / "registry" / "Cpmf" / "rules.yaml"
    default_src = repo_root / "src"

    parser = argparse.ArgumentParser(
        description="WatchfulAnvil rule inventory tool"
    )
    parser.add_argument("--check", action="store_true", help="Validate registry vs C# (default)")
    parser.add_argument("--format", choices=["markdown", "json"], help="Emit formatted output")
    parser.add_argument("--add", nargs=2, metavar=("ID", "CLASS"), help="Add a stub registry entry")
    parser.add_argument("--registry", type=Path, default=default_registry, help="Registry YAML path")
    parser.add_argument("--src", type=Path, default=default_src, help="C# src root")
    args = parser.parse_args()

    if not args.registry.exists():
        print(f"ERROR: registry not found: {args.registry}", file=sys.stderr)
        sys.exit(1)

    if args.format == "markdown":
        cmd_format_markdown(args.registry)
    elif args.format == "json":
        cmd_format_json(args.registry)
    elif args.add:
        cmd_add(args.registry, args.add[0], args.add[1])
    else:
        sys.exit(cmd_check(args.registry, args.src))


if __name__ == "__main__":
    main()
