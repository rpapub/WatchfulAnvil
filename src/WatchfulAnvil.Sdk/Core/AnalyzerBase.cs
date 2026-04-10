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
public abstract class AnalyzerBase : IRegisterAnalyzerConfiguration
{
    /// <summary>
    /// Override to require a minimum Studio SDK feature level.
    /// Use values from <see cref="DesignFeatureKeys"/>, e.g.
    /// <c>DesignFeatureKeys.WorkflowAnalyzerV9</c>.
    /// When set, Initialize() silently skips registration if the feature is absent.
    /// </summary>
    protected virtual string? RequiredFeature => null;

    public abstract void Initialize(IAnalyzerConfigurationService api);

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

    protected static InspectionResult Info(string message)
        => new()
        {
            HasErrors = false,
            ErrorLevel = TraceLevel.Info,
            Messages = new List<string> { message },
        };

    // ── Feature gate helper ───────────────────────────────────────────────────
    protected bool IsFeatureAvailable(IAnalyzerConfigurationService api)
        => RequiredFeature is null || api.HasFeature(RequiredFeature);
}
