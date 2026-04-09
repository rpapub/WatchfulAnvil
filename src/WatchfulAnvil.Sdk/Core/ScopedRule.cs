using System.Diagnostics;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace WatchfulAnvil.Sdk.Core;

/// <summary>
/// Declaration-style base class for Rule&lt;T&gt;.
/// Subclasses declare metadata as overridable properties; <see cref="Get"/> assembles the rule.
/// Prefer the scope-typed aliases: <see cref="ActivityRule"/>, <see cref="WorkflowRule"/>,
/// <see cref="ProjectRule"/>, <see cref="SummaryRule"/>.
/// </summary>
public abstract class ScopedRule<T> : RuleBase<T> where T : IInspectionObject
{
    protected abstract string Id { get; }
    protected abstract string Name { get; }
    protected abstract string Recommendation { get; }
    protected virtual TraceLevel DefaultSeverity => TraceLevel.Error;
    protected virtual string? DocumentationLink => null;

    /// <summary>Override to add parameters to the rule.</summary>
    protected virtual void ConfigureParameters(Rule<T> rule) { }

    protected abstract InspectionResult Inspect(T model, Rule rule);

    public sealed override Rule<T> Get()
    {
        var rule = new Rule<T>(Name, Id, Inspect);
        rule.RecommendationMessage = Recommendation;
        rule.DefaultErrorLevel = DefaultSeverity;
        if (DocumentationLink != null)
            rule.DocumentationLink = DocumentationLink;
        ConfigureParameters(rule);
        return rule;
    }
}

/// <summary>Base class for rules that inspect individual activities.</summary>
public abstract class ActivityRule : ScopedRule<IActivityModel> { }

/// <summary>Base class for rules that inspect a single workflow file.</summary>
public abstract class WorkflowRule : ScopedRule<IWorkflowModel> { }

/// <summary>Base class for rules that inspect the full project model.</summary>
public abstract class ProjectRule : ScopedRule<IProjectModel> { }

/// <summary>Base class for rules that inspect the project summary.</summary>
public abstract class SummaryRule : ScopedRule<IProjectSummary> { }
