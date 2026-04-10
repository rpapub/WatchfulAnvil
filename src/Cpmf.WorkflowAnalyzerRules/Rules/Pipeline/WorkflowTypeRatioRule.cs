// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using WatchfulAnvil.Sdk.Common;
using WatchfulAnvil.Sdk.Core;

namespace Cpmf.Rules.Pipeline
{
    public class WorkflowTypeRatioRule : ProjectRule
    {
        private const string MinModulesPerPipelineKey = "MinModulesPerPipeline";
        private const string MaxModulesPerPipelineKey = "MaxModulesPerPipeline";
        private const string MinUnitsPerModuleKey = "MinUnitsPerModule";
        private const string MaxUnitsPerModuleKey = "MaxUnitsPerModule";

        private const int DefaultMinModulesPerPipeline = 5;
        private const int DefaultMaxModulesPerPipeline = 10;
        private const int DefaultMinUnitsPerModule = 1;
        private const int DefaultMaxUnitsPerModule = 5;

        protected override string Id => "CPMF-FC002";
        protected override string Name => "Workflow Type Ratio";
        protected override TraceLevel DefaultSeverity => TraceLevel.Warning;
        protected override string? RequiredFeature => DesignFeatureKeys.WorkflowAnalyzerV9;
        protected override string Recommendation =>
            "A CPMF project should have between MinModulesPerPipeline and MaxModulesPerPipeline " +
            "modules per pipeline, and between MinUnitsPerModule and MaxUnitsPerModule units per module.";
        protected override string? DocumentationLink =>
            "https://github.com/rpapub/WatchfulAnvil/wiki/Rule-Documentation-CPMF-FC002";

        /// <summary>
        /// Opt-in: ratio thresholds are team-specific. Enable and configure per-project in Studio.
        /// </summary>
        protected override bool IsEnabledByDefault => false;

        protected override void ConfigureParameters(Rule<IProjectModel> rule)
        {
            rule.Parameters.Add(MinModulesPerPipelineKey, new Parameter
            {
                Key = MinModulesPerPipelineKey,
                DefaultValue = DefaultMinModulesPerPipeline.ToString(),
                Value = DefaultMinModulesPerPipeline.ToString(),
                LocalizedDisplayName = "Min modules per pipeline"
            });
            rule.Parameters.Add(MaxModulesPerPipelineKey, new Parameter
            {
                Key = MaxModulesPerPipelineKey,
                DefaultValue = DefaultMaxModulesPerPipeline.ToString(),
                Value = DefaultMaxModulesPerPipeline.ToString(),
                LocalizedDisplayName = "Max modules per pipeline"
            });
            rule.Parameters.Add(MinUnitsPerModuleKey, new Parameter
            {
                Key = MinUnitsPerModuleKey,
                DefaultValue = DefaultMinUnitsPerModule.ToString(),
                Value = DefaultMinUnitsPerModule.ToString(),
                LocalizedDisplayName = "Min units per module"
            });
            rule.Parameters.Add(MaxUnitsPerModuleKey, new Parameter
            {
                Key = MaxUnitsPerModuleKey,
                DefaultValue = DefaultMaxUnitsPerModule.ToString(),
                Value = DefaultMaxUnitsPerModule.ToString(),
                LocalizedDisplayName = "Max units per module"
            });
        }

        protected override InspectionResult Inspect(IProjectModel project, Rule rule)
        {
            var workflows = project.Workflows;
            if (workflows == null)
                return Pass();

            var wfList = workflows.ToList();

            var pipelineCount = wfList.Count(w =>
                w.Root != null && AnnotationReader.HasTag(w.Root.AnnotationText, "@pipeline"));
            var moduleCount = wfList.Count(w =>
                w.Root != null && AnnotationReader.HasTag(w.Root.AnnotationText, "@module"));
            var unitCount = wfList.Count(w =>
                w.Root != null && AnnotationReader.HasTag(w.Root.AnnotationText, "@unit"));

            var minModPerPipeline = ParseIntParam(rule, MinModulesPerPipelineKey, DefaultMinModulesPerPipeline);
            var maxModPerPipeline = ParseIntParam(rule, MaxModulesPerPipelineKey, DefaultMaxModulesPerPipeline);
            var minUnitsPerModule = ParseIntParam(rule, MinUnitsPerModuleKey, DefaultMinUnitsPerModule);
            var maxUnitsPerModule = ParseIntParam(rule, MaxUnitsPerModuleKey, DefaultMaxUnitsPerModule);

            var messages = new List<string>();

            if (pipelineCount > 0)
            {
                var ratio = moduleCount / (double)pipelineCount;
                if (ratio < minModPerPipeline)
                    messages.Add(
                        $"Project has {pipelineCount} pipeline(s) and {moduleCount} module(s) " +
                        $"(ratio {ratio:F1}). Expected at least {minModPerPipeline} module(s) per pipeline.");
                if (ratio > maxModPerPipeline)
                    messages.Add(
                        $"Project has {pipelineCount} pipeline(s) and {moduleCount} module(s) " +
                        $"(ratio {ratio:F1}). Expected at most {maxModPerPipeline} module(s) per pipeline.");
            }

            if (moduleCount > 0)
            {
                var ratio = unitCount / (double)moduleCount;
                if (ratio < minUnitsPerModule)
                    messages.Add(
                        $"Project has {moduleCount} module(s) and {unitCount} unit(s) " +
                        $"(ratio {ratio:F1}). Expected at least {minUnitsPerModule} unit(s) per module.");
                if (ratio > maxUnitsPerModule)
                    messages.Add(
                        $"Project has {moduleCount} module(s) and {unitCount} unit(s) " +
                        $"(ratio {ratio:F1}). Expected at most {maxUnitsPerModule} unit(s) per module.");
            }

            if (messages.Count > 0)
                return Fail(rule, messages);

            return Pass();
        }

        private static int ParseIntParam(Rule rule, string key, int fallback)
        {
            var raw = rule.Parameters[key]?.Value;
            if (string.IsNullOrWhiteSpace(raw))
                return fallback;
            return int.TryParse(raw, out var result) ? result : fallback;
        }
    }
}
