// <copyright file="SummaryRule.cs" company="Christian Prior-Mamulyan">
// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// </copyright>

using UiPath.Studio.Analyzer.Models;

namespace WatchfulAnvil.Sdk.Core;

/// <summary>Base class for rules that inspect the project summary.</summary>
public abstract class SummaryRule : ScopedRule<IProjectSummary>
{
}
