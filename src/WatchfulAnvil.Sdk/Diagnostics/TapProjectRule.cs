// <copyright file="TapProjectRule.cs" company="Christian Prior-Mamulyan">
// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// </copyright>

using System.Diagnostics;

using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

using WatchfulAnvil.Sdk.Core;

namespace WatchfulAnvil.Sdk.Diagnostics
{
    /// <summary>
    /// Diagnostic tap rule that supplies project-level context to <see cref="RunContext"/>.
    /// Fires once per analysis run (not guaranteed to fire before or after activity/workflow rules).
    /// Project metadata (projectFilePath, outputType, etc.) is written to run-meta.json immediately
    /// and again on <c>AppDomain.ProcessExit</c> (with <c>completedAt</c> populated).
    /// Disabled by default. Enable per-project in Workflow Analyzer settings.
    /// </summary>
    public class TapProjectRule : ProjectRule
    {
        protected override string Id => "CPMF-TAP-PRJ-001";

        protected override string Name => "Tap Project (Diagnostics)";

        protected override string Recommendation => "Diagnostic tap — supplies project context for JSONL run-meta output.";

        protected override TraceLevel DefaultSeverity => TraceLevel.Info;

        protected override bool IsEnabledByDefault => false;

        protected override InspectionResult Inspect(IProjectModel project, Rule rule)
        {
            // Trigger RunContext singleton construction (if not already initialised by
            // an earlier activity or workflow rule in this run), then supply project-level
            // fields that are only available here.
            RunContext.Instance.SetProjectContext(project);

            return new InspectionResult { HasErrors = false };
        }
    }
}
