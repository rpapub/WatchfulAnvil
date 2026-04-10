// <copyright file="ScopedRule.cs" company="Christian Prior-Mamulyan">
// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// </copyright>

using System.Diagnostics;
using System.Linq;

using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

using WatchfulAnvil.Sdk.Common;

namespace WatchfulAnvil.Sdk.Core;

/// <summary>
/// Declaration-style base class for Rule&lt;T&gt;.
/// Subclasses declare metadata as overridable properties; <see cref="Get"/> assembles the rule.
/// Prefer the scope-typed aliases: <see cref="ActivityRule"/>, <see cref="WorkflowRule"/>,
/// <see cref="ProjectRule"/>, <see cref="SummaryRule"/>.
/// </summary>
public abstract class ScopedRule<T> : RuleBase<T>
    where T : IInspectionObject
{
    protected abstract string Id { get; }

    protected abstract string Name { get; }

    protected abstract string Recommendation { get; }

    protected virtual TraceLevel DefaultSeverity => TraceLevel.Error;

    // Optional: omit to leave the documentation link unset.
    protected virtual string? DocumentationLink => null;

    /// <summary>
    /// Whether the rule is enabled by default. Override and return <c>false</c> to make the rule opt-in.
    /// When <c>false</c>, users must explicitly enable the rule per-project in Studio.
    /// </summary>
    protected virtual bool IsEnabledByDefault => true;

    /// <summary>
    /// <c>Inspect</c> is called only if at least one of these tags is present on the model annotation (OR).
    /// <c>null</c> (default) disables this filter.
    /// </summary>
    protected virtual string[]? RequiresAnyTag => null;

    /// <summary>
    /// <c>Inspect</c> is called only if all of these tags are present on the model annotation (AND).
    /// <c>null</c> (default) disables this filter.
    /// </summary>
    protected virtual string[]? RequiresAllTags => null;

    public sealed override Rule<T> Get()
    {
        var rule = new Rule<T>(Name, Id, FilteredInspect);
        rule.RecommendationMessage = Recommendation;
        rule.DefaultErrorLevel = DefaultSeverity;
        if (DocumentationLink != null)
        {
            rule.DocumentationLink = DocumentationLink;
        }

        if (!IsEnabledByDefault)
        {
            rule.DefaultIsEnabled = false;
        }

        ConfigureParameters(rule);
        return rule;
    }

    /// <summary>Override to add parameters to the rule.</summary>
    protected virtual void ConfigureParameters(Rule<T> rule)
    {
    }

    protected abstract InspectionResult Inspect(T model, Rule rule);

    private static string? GetAnnotation(T model) => model switch
    {
        IActivityModel a => a.AnnotationText,
        IWorkflowModel w => w.Root?.AnnotationText,
        _ => null,
    };

    private InspectionResult FilteredInspect(T model, Rule rule)
    {
        var annotation = GetAnnotation(model);

        // @nocheck suppresses all rules on this model.
        if (AnnotationReader.IsNoCheck(annotation))
            return Pass();

        // @suppress:RULE-ID suppresses this specific rule on this model.
        if (AnnotationReader.IsSuppressed(annotation, Id))
            return Pass();

        if (RequiresAllTags != null || RequiresAnyTag != null)
        {
            if (RequiresAllTags != null && !RequiresAllTags.All(t => AnnotationReader.HasTag(annotation, t)))
            {
                return Pass();
            }

            if (RequiresAnyTag != null && !RequiresAnyTag.Any(t => AnnotationReader.HasTag(annotation, t)))
            {
                return Pass();
            }
        }

        return Inspect(model, rule);
    }
}
