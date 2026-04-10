// <copyright file="CounterBase.cs" company="Christian Prior-Mamulyan">
// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// </copyright>

using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace WatchfulAnvil.Sdk.Core;

/// <summary>
/// Base class for Counter&lt;T&gt; implementations.
/// Handles registration via api.AddCounter&lt;T&gt;() with optional feature gating.
/// Note: Counter Inspect receives IReadOnlyCollection&lt;T&gt;, not a single T.
/// ErrorLevel for counters is always Info.
/// </summary>
public abstract class CounterBase<T> : AnalyzerBase
    where T : IInspectionObject
{
    public abstract Counter<T> Get();

    public override void Initialize(IAnalyzerConfigurationService api)
    {
        if (!IsFeatureAvailable(api))
        {
            return;
        }

        api.AddCounter<T>(Get());
    }
}
