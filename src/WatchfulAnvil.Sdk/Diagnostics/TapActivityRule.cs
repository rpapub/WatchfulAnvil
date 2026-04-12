// <copyright file="TapActivityRule.cs" company="Christian Prior-Mamulyan">
// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// </copyright>

using System.Diagnostics;

using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

using WatchfulAnvil.Sdk.Core;

namespace WatchfulAnvil.Sdk.Diagnostics
{
    /// <summary>
    /// Diagnostic tap rule that captures detailed activity-level metadata as JSONL records.
    /// One record per activity per analysis run, written to
    /// <c>%LOCALAPPDATA%\WatchfulAnvil\runs\{runId}\activity.jsonl</c>
    /// and fanned out to <c>by-package\{sourceId}\{sourceVersion}\activity.jsonl</c>.
    /// Disabled by default. Enable per-project in Workflow Analyzer settings.
    /// </summary>
    public class TapActivityRule : ActivityRule
    {
        protected override string Id => "CPMF-TAP-ACT-001";

        protected override string Name => "Tap Activity (Diagnostics)";

        protected override string Recommendation => "Diagnostic tap — captures activity metadata to JSONL output.";

        protected override TraceLevel DefaultSeverity => TraceLevel.Info;

        protected override bool IsEnabledByDefault => false;

        protected override InspectionResult Inspect(IActivityModel activity, Rule rule)
        {
            var run = RunContext.Instance;
            var json = ActivityRecordBuilder.Build(activity, run);

            // sourceId / sourceVersion are embedded in the JSON; extract for fan-out path.
            // Use activity.PackageBindings directly — the builder already serialised them.
            string? sourceId = null;
            string? sourceVersion = null;
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;
                sourceId = root.TryGetProperty("sourceId", out var sid) ? sid.GetString() : null;
                sourceVersion = root.TryGetProperty("sourceVersion", out var sver) ? sver.GetString() : null;
            }
            catch { }

            JsonlWriter.AppendActivity(json, run, sourceId, sourceVersion);

            return new InspectionResult { HasErrors = false };
        }
    }
}
