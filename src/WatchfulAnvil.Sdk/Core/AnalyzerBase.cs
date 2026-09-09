// <copyright file="AnalyzerBase.cs" company="Christian Prior-Mamulyan">
// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// </copyright>

using System.Collections.Generic;
using System.Diagnostics;

using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;

namespace WatchfulAnvil.Sdk.Core;

/// <summary>
/// Shared foundation for all analyzer components (Rule, Counter).
/// Provides feature-gate hook and InspectionResult convenience methods.
/// Does not prescribe implementation pattern — use tree-walking, accumulation,
/// predicates, extractors, or any combination as the rule requires.
/// </summary>
public abstract class AnalyzerBase
{
    /// <summary>
    /// Override to require a minimum Studio SDK feature level.
    /// Use values from <see cref="DesignFeatureKeys"/>, e.g.
    /// <c>DesignFeatureKeys.WorkflowAnalyzerV9</c>.
    /// When set, Initialize() silently skips registration if the feature is absent.
    /// </summary>
    protected virtual string? RequiredFeature => null;

    // ── InspectionResult factories ────────────────────────────────────────────
    protected static InspectionResult Pass()
        => new() { HasErrors = false };

    protected static InspectionResult Fail(Rule rule, IList<string> messages)
        => new()
        {
            HasErrors = true,
            RecommendationMessage = rule.RecommendationMessage,
            Messages = messages,
            ErrorLevel = rule.DefaultErrorLevel,
        };

    protected static InspectionResult Fail(Rule rule, string message)
        => Fail(rule, new List<string> { message });

    /// <summary>
    /// An informational, non-failing result carrying the rule's recommendation.
    /// </summary>
    /// <remarks>
    /// Takes the rule so it can set <c>RecommendationMessage</c>, which <see cref="Fail(Rule, string)"/>
    /// already does. Without it an Info result reached Studio with an empty recommendation
    /// column while every other result had one — the reader has no way to tell what the
    /// rule wanted, which for a purely informational result is the entire payload.
    /// </remarks>
    protected static InspectionResult Info(Rule rule, string message)
        => new()
        {
            HasErrors = false,
            RecommendationMessage = rule.RecommendationMessage,
            ErrorLevel = TraceLevel.Info,
            Messages = new List<string> { message },
        };

    // ── Feature gate helper ───────────────────────────────────────────────────
    protected bool IsFeatureAvailable(IAnalyzerConfigurationService api)
        => RequiredFeature is null || api.HasFeature(RequiredFeature);
}
