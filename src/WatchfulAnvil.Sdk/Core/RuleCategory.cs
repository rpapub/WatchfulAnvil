namespace WatchfulAnvil.Sdk.Core;

/// <summary>
/// Enumeration class representing a rule category.
/// Each category contributes its <see cref="Code"/> to the rule ID
/// (e.g. <c>CPMF-WFL-001</c> uses <c>Workflow.Code == "WFL"</c>).
/// The full set of built-in categories is also emitted to
/// <c>content/rule-categories.json</c> in the nupkg for tooling consumption.
/// </summary>
public sealed class RuleCategory
{
    /// <summary>Short uppercase abbreviation used as the category segment in rule IDs.</summary>
    public string Code { get; }

    /// <summary>Human-readable category name.</summary>
    public string Name { get; }

    /// <summary>One-line description of what rules in this category check.</summary>
    public string Description { get; }

    private RuleCategory(string code, string name, string description)
    {
        Code        = code;
        Name        = name;
        Description = description;
    }

    // ── Built-in categories ───────────────────────────────────────────────────

    /// <summary>Conventions for naming workflows, variables, arguments, and annotations.</summary>
    public static readonly RuleCategory Naming = new(
        "NAM",
        "Naming",
        "Conventions for naming workflows, variables, arguments, and annotations.");

    /// <summary>Structural and design rules applying to individual workflow files.</summary>
    public static readonly RuleCategory Workflow = new(
        "WFL",
        "Workflow Design",
        "Structural and design rules applying to individual workflow files.");

    /// <summary>Error handling, fault tolerance, and retry patterns.</summary>
    public static readonly RuleCategory Reliability = new(
        "REL",
        "Reliability",
        "Error handling, fault tolerance, and retry patterns.");

    /// <summary>Presence, level, and content of log message activities.</summary>
    public static readonly RuleCategory Logging = new(
        "LOG",
        "Logging",
        "Presence, level, and content of log message activities.");

    /// <summary>Detection of hardcoded credentials, sensitive data exposure, and unsafe patterns.</summary>
    public static readonly RuleCategory Security = new(
        "SEC",
        "Security",
        "Detection of hardcoded credentials, sensitive data exposure, and unsafe patterns.");

    /// <summary>Package reference hygiene, restricted or required activity packages.</summary>
    public static readonly RuleCategory Dependencies = new(
        "DEP",
        "Dependencies",
        "Package reference hygiene, restricted or required activity packages.");

    /// <summary>Project-level pipeline structure: stages, entry points, and lifecycle annotations.</summary>
    public static readonly RuleCategory Pipeline = new(
        "PLN",
        "Pipeline",
        "Project-level pipeline structure: stages, entry points, and lifecycle annotations.");

    /// <summary>Project settings, properties, and runtime configuration correctness.</summary>
    public static readonly RuleCategory Configuration = new(
        "CFG",
        "Configuration",
        "Project settings, properties, and runtime configuration correctness.");

    /// <summary>Presence and content of workflow annotations and descriptions.</summary>
    public static readonly RuleCategory Documentation = new(
        "DOC",
        "Documentation",
        "Presence and content of workflow annotations and descriptions.");

    /// <summary>Detection of patterns with known performance impact.</summary>
    public static readonly RuleCategory Performance = new(
        "PRF",
        "Performance",
        "Detection of patterns with known performance impact: delays, infinite loops, large waits.");

    // ── Convenience ──────────────────────────────────────────────────────────

    /// <inheritdoc cref="Code"/>
    public override string ToString() => Code;
}
