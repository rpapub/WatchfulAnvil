---
name: WatchfulAnvil Registry
description: >-
  Manage WatchfulAnvil rule registry files (registry/<Package>/rules.yaml) — the
  source of truth for rule metadata, checked and consumed by tools/rule-inventory/run.py.
  Use when: verifying registry sync with C# source, adding stub entries for new rules,
  generating markdown or JSON documentation, or looking up the registry YAML schema
  (fields, scope values, tag conventions, parameters, and the process/library
  project-type tagging convention).
---

All commands run from the repository root with `uv run`.

---

## Check registry sync

Validates every rule registered in C# source exists in the registry, with matching `id`, severity, and enabled state.

```bash
# Check default registry (registry/Cpmf/rules.yaml)
uv run tools/rule-inventory/run.py --check

# Check all registries
uv run tools/rule-inventory/run.py --check --all
```

Exit code 0 = in sync. Exit code 1 = drift detected.

Drift categories reported:

| Label | Meaning |
|---|---|
| `OK` | Registry entry matches C# source |
| `MISSING FROM REGISTRY` | Class is registered in C# but absent from rules.yaml |
| `STALE IN REGISTRY` | rules.yaml entry whose class is not in RegisterAnalyzerConfiguration.cs |
| `ID MISMATCH` | Class matches but rule ID differs between C# and registry |
| `SEVERITY MISMATCH` | DefaultSeverity differs |
| `ENABLED MISMATCH` | IsEnabledByDefault differs |

---

## Add a stub entry for a new rule

Use when a rule ID and class name are decided but C# implementation is not yet written.

```bash
uv run tools/rule-inventory/run.py --add CPMF-X001 MyNewRule
```

Appends a stub with placeholder fields to `registry/Cpmf/rules.yaml`. After running:

1. Open `registry/Cpmf/rules.yaml` and fill in `name`, `description`, `recommendation`, `categoryCode`, `scope`.
2. Run `--check` — it reports `STALE IN REGISTRY` until the C# implementation and registration land.

To target a different registry:

```bash
uv run tools/rule-inventory/run.py --add CPMF-X001 MyNewRule --registry registry/Cpmf/rules.yaml
```

---

## Generate documentation

```bash
# Single registry (default)
uv run tools/rule-inventory/run.py --format markdown

# All registries
uv run tools/rule-inventory/run.py --format markdown --all

# Write to docs/rules.md
uv run tools/rule-inventory/run.py --format markdown --all > docs/rules.md
```

Override the Jinja2 template:

```bash
uv run tools/rule-inventory/run.py --format markdown --template path/to/my.j2
```

Template context variables: `package`, `namespace`, `version`, `categories`, `rules`, `scope_short`, `sev_abbrev`.

---

## Export rules as JSON

```bash
uv run tools/rule-inventory/run.py --format json
uv run tools/rule-inventory/run.py --format json --all > registry/Cpmf/rules.json
```

---

## Override paths

```bash
# Different registry file
uv run tools/rule-inventory/run.py --registry registry/Mc/rules.yaml

# Different C# source root
uv run tools/rule-inventory/run.py --src path/to/src
```

---

## Registry YAML schema

Top-level fields:

| Field | Type | Description |
|---|---|---|
| `package` | string | C# project name (must match `src/<package>/`) |
| `namespace` | string | C# root namespace |
| `version` | string | SemVer string |
| `categories` | list | Category definitions `{code, name, description}` |
| `rules` | list | Rule entries (see below) |

Each rule entry:

| Field | Required | Description |
|---|---|---|
| `id` | yes | Unique rule ID, e.g. `CPMF-N001` |
| `className` | yes | C# class name, e.g. `VariableNamingConventionRule` |
| `name` | yes | Short human-readable name |
| `description` | yes | One-line description of what the rule checks |
| `recommendation` | yes | Actionable guidance for the developer |
| `categoryCode` | yes | Single-letter code matching a `categories` entry |
| `scope` | yes | `IActivityModel`, `IWorkflowModel`, or `IProjectModel` |
| `type` | yes | Always `Rule` |
| `defaultSeverity` | yes | `Error`, `Warning`, `Info`, or `None` |
| `defaultIsEnabled` | yes | `true` (on by default) or `false` (opt-in) |
| `requiredFeature` | no | Feature flag string or `null` |
| `documentationLink` | no | URL or `null` |
| `tags` | no | Label strings. Use `process` and/or `library` to declare which project output types the rule is meaningful for. Omit when the rule applies to all types. |
| `parameters` | no | List of `{key, displayName, defaultValue}` |

---

## Project type applicability

Rules meaningful only for specific output types carry the reserved tags `process` or `library`. No tag = applies to all output types.

| Tag value | Meaning |
|---|---|
| `process` | Rule targets **Process** output type only |
| `library` | Rule targets **Library** output type only |
| *(none)* | Rule applies to all output types |

```yaml
- id: CPMF-FC001
  ...
  tags: [process]
```

**Runtime enforcement (project-scope rules only):** For `scope: IProjectModel` rules, mirror the tag with `TargetOutputTypes` in C# — Studio skips `Inspect` for non-matching projects. For `IWorkflowModel`/`IActivityModel` scopes the tag is informational only.

```csharp
// Mirrors tags: [process] in rules.yaml
protected override string[]? TargetOutputTypes => new[] { "Process" };
```
