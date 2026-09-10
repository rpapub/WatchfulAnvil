# /// script
# requires-python = ">=3.11"
# dependencies = ["pyyaml>=6.0", "jinja2>=3.0"]
# ///
"""
WatchfulAnvil Rule Inventory Tool

Validates registry/Cpmf/rules.yaml against C# source, and exports rule
metadata for documentation and roadmap planning.

Usage (from WatchfulAnvil repo root):
    uv run tools/rule-inventory/run.py [options]

Options:
    (no args)              Run --check
    --check                Validate registry against C# source; exit 1 on drift
    --format markdown      Render rules via the built-in Jinja2 template
    --format json          Print rules as a JSON array to stdout
    --template <path>      Override the Jinja2 template used by --format markdown
    --add <ID> <CLASS>     Append a stub entry to the registry YAML
    --registry <path>      Override registry file (default: registry/Cpmf/rules.yaml)
    --src <path>           Override src root (default: src/)

Template context variables (available in custom templates):
    package       str   — registry package name
    namespace     str   — registry namespace
    version       str   — registry version string
    categories    list  — category dicts {code, name, description}
    rules         list  — rule dicts (all fields from rules.yaml)
    scope_short   dict  — maps IWorkflowModel etc. to short labels
    sev_abbrev    dict  — maps Error/Warning/Info/None to short labels
"""

import argparse
import glob
import json
import os
import re
import sys
from pathlib import Path

import yaml
from jinja2 import Environment

# ---------------------------------------------------------------------------
# C# source parsing
# ---------------------------------------------------------------------------

_RE_REGISTERED = re.compile(r"new\s+(\w+)\(\)\.Initialize")
_RE_RULE_ID = re.compile(r'protected override string Id\s*=>\s*"(.+?)"')
_RE_SEVERITY = re.compile(r"protected override TraceLevel DefaultSeverity\s*=>\s*TraceLevel\.(\w+)")
_RE_ENABLED = re.compile(r"protected override bool IsEnabledByDefault\s*=>\s*(true|false)")


def find_registration_file(src_roots: list[Path], package: str) -> Path:
    """Locate <root>/<package>/RegisterAnalyzerConfiguration[.g].cs across the roots.

    Deliberately no glob fallback. The previous version, when the direct path was
    missing, globbed for any RegisterAnalyzerConfiguration.cs and took matches[0] -- an
    arbitrary file. registry/CpmfTap declared package: WatchfulAnvil.Sdk, which has no
    registration file, so the check silently validated the TAP registry against
    CPM.WorkflowAnalyzerRules and reported six bogus issues. A wrong answer that looks
    like a real result is worse than no answer.
    """
    tried = []
    for root in src_roots:
        for name in ("RegisterAnalyzerConfiguration.cs", "RegisterAnalyzerConfiguration.g.cs"):
            candidate = root / package / name
            tried.append(candidate)
            if candidate.exists():
                return candidate

    raise RuntimeError(
        f"No registration file for package '{package}'. Looked for:\n"
        + "\n".join(f"  {p}" for p in tried)
        + "\nCheck the 'package:' field in the registry - it must name the directory "
          "holding RegisterAnalyzerConfiguration[.g].cs."
    )


def find_registered_classes(src_roots: list[Path], package: str) -> list[tuple[str, Path]]:
    """Return [(className, cs_file)] from the package's registration file."""
    config_file = find_registration_file(src_roots, package)
    text = config_file.read_text(encoding="utf-8")
    class_names = _RE_REGISTERED.findall(text)

    results = []
    for cls in class_names:
        cs_path = None
        for root in src_roots:
            cs_matches = glob.glob(str(root / "**" / f"{cls}.cs"), recursive=True)
            cs_matches = [m for m in cs_matches if f"{os.sep}bin{os.sep}" not in m
                          and f"{os.sep}obj{os.sep}" not in m]
            if cs_matches:
                cs_path = Path(cs_matches[0])
                break
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

def cmd_check_one(registry_path: Path, src_roots: list[Path]) -> int:
    with open(registry_path, encoding="utf-8") as f:
        registry = yaml.safe_load(f)

    package = registry.get("package", "")
    rules = registry.get("rules", [])

    # Skip empty libraries that have no registration file yet
    try:
        find_registration_file(src_roots, package)
        has_registration = True
    except RuntimeError:
        has_registration = False

    if not has_registration and not rules:
        print(f"Registry : {registry_path}")
        print("  SKIP — empty library, no RegisterAnalyzerConfiguration.cs")
        print()
        return 0

    reg_by_class: dict[str, dict] = {r["className"]: r for r in rules}
    try:
        registered = find_registered_classes(src_roots, package)
    except RuntimeError as e:
        # A registry naming a package with no registration file is a registry error,
        # not a reason to fall back to some other package's file.
        print(f"Registry : {registry_path}")
        print(f"  ERROR — {e}")
        print()
        return 1

    issues = 0

    print(f"Registry : {registry_path}")
    print(f"Src roots: {', '.join(str(r) for r in src_roots)}")
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


def cmd_check(registry_paths: list[Path], src_roots: list[Path]) -> int:
    total = 0
    for path in registry_paths:
        total += cmd_check_one(path, src_roots)
    return min(total, 1)


# ---------------------------------------------------------------------------
# Render mode (Jinja2)
# ---------------------------------------------------------------------------

SEV_ABBREV = {"Error": "error", "Warning": "warn", "Info": "info", None: "-"}
SCOPE_SHORT = {
    "IActivityModel": "Activity",
    "IWorkflowModel": "Workflow",
    "IProjectModel": "Project",
    "IProjectSummary": "Summary",
}

# Default inline template — override with --template <path>
_DEFAULT_TEMPLATE = """\
## {{ package }} - Rule Reference (v{{ version }})

| ID | Name | Scope | Cat | Sev | On | Params |
|---|---|---|---|---|---|---|
{% for r in rules -%}
| {{ r.id }} | {{ r.name }} | {{ scope_short.get(r.scope, r.scope) }} | {{ r.categoryCode }} | {{ sev_abbrev.get(r.defaultSeverity, r.defaultSeverity or '-') }} | {{ 'yes' if r.get('defaultIsEnabled', True) else 'opt-in' }} | {{ (r.parameters or []) | length or '-' }} |
{% endfor %}
{% set rules_with_params = rules | selectattr('parameters') | list -%}
{% if rules_with_params %}

### Parameters

| Rule | Key | Display Name | Default |
|---|---|---|---|
{% for r in rules_with_params %}{% for p in r.parameters -%}
| {{ r.id }} | `{{ p.key }}` | {{ p.displayName }} | `{{ p.defaultValue }}` |
{% endfor %}{% endfor %}{% endif %}
"""


def _build_context(registry: dict) -> dict:
    return {
        "package":    registry.get("package", ""),
        "namespace":  registry.get("namespace", ""),
        "version":    registry.get("version", ""),
        "categories": registry.get("categories", []),
        "rules":      registry.get("rules", []),
        "scope_short": SCOPE_SHORT,
        "sev_abbrev":  SEV_ABBREV,
    }


def _load_registry(path: Path) -> dict:
    with open(path, encoding="utf-8") as f:
        return yaml.safe_load(f)


def _find_all_registries(repo_root: Path) -> list[Path]:
    return sorted((repo_root / "registry").glob("*/rules.yaml"))


def cmd_render(registry_paths: list[Path], template_path: Path | None) -> None:
    template_src = template_path.read_text(encoding="utf-8") if template_path else _DEFAULT_TEMPLATE
    env = Environment(trim_blocks=True, lstrip_blocks=True, keep_trailing_newline=True)
    template = env.from_string(template_src)

    first = True
    for path in registry_paths:
        registry = _load_registry(path)
        if not registry.get("rules"):
            continue
        if not first:
            print()
        print(template.render(_build_context(registry)), end="")
        first = False


def cmd_format_json(registry_paths: list[Path]) -> None:
    all_rules = []
    for path in registry_paths:
        registry = _load_registry(path)
        all_rules.extend(registry.get("rules", []))
    print(json.dumps(all_rules, indent=2, ensure_ascii=False))


# ---------------------------------------------------------------------------
# Governance mode
# ---------------------------------------------------------------------------

# Defaults for the full policy form, matching tools/governance/cpmf.policy.Development.json.
# Every setting is paired with <setting>-allow-edit, which decides whether a developer may
# override it locally in Studio: that pairing IS the governance model - a policy either
# suggests (allow-edit true) or enforces (false).
_GOVERNANCE_SCAFFOLD = {
    "core-allow-edit": True,
    "enforce-analyzer-before-run": False,
    "enforce-analyzer-before-run-allow-edit": True,
    "enforce-analyzer-before-publish": False,
    "enforce-analyzer-before-publish-allow-edit": True,
    "enforce-analyzer-before-push": False,
    "enforce-analyzer-before-push-allow-edit": True,
    "analyze-rpa-xamls-only": False,
    "analyze-rpa-xamls-only-allow-edit": True,
    "additional-analyzer-rule-path": None,
    "additional-analyzer-rule-path-allow-edit": True,
    "export-analyzer-results": False,
    "export-analyzer-results-allow-edit": True,
    "analyzer-allow-edit": True,
    "referenced-rules-config-file": None,
}


def _governance_entry(rule: dict, collection: str) -> dict:
    """One embedded-rules-config entry. Key names repeat the collection name, as UiPath writes them."""
    return {
        f"code-{collection}": rule.get("id"),
        f"is-enabled-{collection}": bool(rule.get("defaultIsEnabled", True)),
        "default-action": rule.get("defaultSeverity", "Error"),
        # Left empty deliberately. Every entry in every policy file in this repo has an
        # empty parameter array -- including rules that demonstrably declare parameters --
        # so the populated shape is unknown. A wrong guess yields a policy that loads and
        # silently fails to apply overrides, which is worse than an obvious gap.
        # TODO: configure a parameterised rule in Studio, export, and read back the shape.
        f"parameters-{collection}": [],
    }


def cmd_format_governance(
    registry_paths: list[Path],
    form: str,
    product_name: str,
    policy_name: str | None,
) -> int:
    # A policy targets the rules actually installed in one project. Merging registries
    # would emit entries for rules that will never load, and the duplicate CPMF-G002
    # across registry/Cpmf and registry/Cpmf.Rules.Libs makes the result ambiguous.
    seen: dict[str, Path] = {}
    rules: list[dict] = []
    for path in registry_paths:
        for rule in _load_registry(path).get("rules", []):
            rule_id = rule.get("id")
            if rule_id in seen:
                print(
                    f"ERROR: duplicate rule id {rule_id!r} in {seen[rule_id]} and {path}.\n"
                    f"A governance policy targets one installed pack; generate per registry "
                    f"with --registry instead of --all.",
                    file=sys.stderr,
                )
                return 1
            seen[rule_id] = path
            rules.append(rule)

    # Counters are a distinct analyzer concept and belong in their own collection.
    counters = [r for r in rules if r.get("type") == "Counter"]
    plain = [r for r in rules if r.get("type") != "Counter"]

    data: dict = {}
    if form == "full":
        data.update(_GOVERNANCE_SCAFFOLD)

    data["embedded-rules-config-counter"] = [
        _governance_entry(r, "embedded-rules-config-counter") for r in counters
    ]
    data["embedded-rules-config-rules"] = [
        _governance_entry(r, "embedded-rules-config-rules") for r in plain
    ]

    policy: dict = {}
    if form == "full":
        policy["product-name"] = product_name
        policy["policy-name"] = policy_name or f"WatchfulAnvil Rules - {product_name}"
    policy["data"] = data

    print(json.dumps(policy, indent=2, ensure_ascii=False))
    return 0


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
    # Both roots by default: rule classes live under src/, but the generated
    # registration for the curated dist packs lives under dist/, so a src-only search
    # cannot resolve them.
    default_srcs = [repo_root / "src", repo_root / "dist"]

    parser = argparse.ArgumentParser(
        description="WatchfulAnvil rule inventory tool"
    )
    parser.add_argument("--check", action="store_true", help="Validate registry vs C# (default)")
    parser.add_argument("--format", choices=["markdown", "json", "governance"],
                        help="Emit formatted output")
    parser.add_argument("--governance-form", choices=["full", "minimal"], default="full",
                        help="Policy shape for --format governance: full includes the "
                             "product/policy names and the *-allow-edit scaffolding; "
                             "minimal emits only data.embedded-rules-config-* (default: full)")
    parser.add_argument("--product-name", default="Development",
                        help="product-name for --format governance --governance-form full")
    parser.add_argument("--policy-name", default=None,
                        help="policy-name for --format governance --governance-form full")
    parser.add_argument("--template", type=Path, default=None,
                        help="Jinja2 template file for --format markdown (default: built-in)")
    parser.add_argument("--all", action="store_true", dest="all_registries",
                        help="Include all registry/*/rules.yaml files")
    parser.add_argument("--add", nargs=2, metavar=("ID", "CLASS"), help="Add a stub registry entry")
    parser.add_argument("--registry", type=Path, default=None, help="Registry YAML path (default: registry/Cpmf/rules.yaml)")
    parser.add_argument("--src", type=Path, action="append", dest="srcs", default=None,
                        help="C# source root; repeatable (default: src/ and dist/)")
    args = parser.parse_args()

    if args.template and args.format != "markdown":
        print("ERROR: --template is only valid with --format markdown", file=sys.stderr)
        sys.exit(1)

    if args.template and not args.template.exists():
        print(f"ERROR: template not found: {args.template}", file=sys.stderr)
        sys.exit(1)

    # Resolve registry list
    if args.all_registries:
        registry_paths = _find_all_registries(repo_root)
        if not registry_paths:
            print("ERROR: no registry/*/rules.yaml files found", file=sys.stderr)
            sys.exit(1)
    else:
        single = args.registry or default_registry
        if not single.exists():
            print(f"ERROR: registry not found: {single}", file=sys.stderr)
            sys.exit(1)
        registry_paths = [single]

    if args.format == "markdown":
        cmd_render(registry_paths, args.template)
    elif args.format == "json":
        cmd_format_json(registry_paths)
    elif args.format == "governance":
        sys.exit(cmd_format_governance(
            registry_paths, args.governance_form, args.product_name, args.policy_name))
    elif args.add:
        if len(registry_paths) > 1:
            print("ERROR: --add requires --registry (cannot target multiple registries)", file=sys.stderr)
            sys.exit(1)
        cmd_add(registry_paths[0], args.add[0], args.add[1])
    else:
        sys.exit(cmd_check(registry_paths, args.srcs or default_srcs))


if __name__ == "__main__":
    main()
