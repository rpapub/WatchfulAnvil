// <copyright file="TapWorkflowRule.cs" company="Christian Prior-Mamulyan">
// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// </copyright>

using System.Diagnostics;

using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

using WatchfulAnvil.Sdk.Core;

namespace WatchfulAnvil.Sdk.Diagnostics
{
    /// <summary>
    /// Diagnostic tap rule that captures detailed workflow-level metadata as JSONL records.
    /// One record per workflow file per analysis run, written to
    /// <c>%LOCALAPPDATA%\WatchfulAnvil\runs\{runId}\workflow.jsonl</c>.
    /// Also registers the workflow root ID in <see cref="RunContext"/> so that
    /// activity records can resolve their <c>workflowRelativePath</c>.
    /// Disabled by default. Enable per-project in Workflow Analyzer settings.
    /// </summary>
    public class TapWorkflowRule : WorkflowRule
    {
        protected override string Id => "CPMF-TAP-WFL-001";

        protected override string Name => "Tap Workflow (Diagnostics)";

        protected override string Recommendation => "Diagnostic tap — captures workflow metadata to JSONL output.";

        protected override TraceLevel DefaultSeverity => TraceLevel.Info;

        protected override bool IsEnabledByDefault => false;

        protected override InspectionResult Inspect(IWorkflowModel workflow, Rule rule)
        {
            var run = RunContext.Instance;
            var json = WorkflowRecordBuilder.Build(workflow, run);
            JsonlWriter.AppendWorkflow(json, run);
            return new InspectionResult { HasErrors = false };
        }
    }
}
