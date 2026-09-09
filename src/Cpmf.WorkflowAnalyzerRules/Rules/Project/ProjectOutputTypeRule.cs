// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using WatchfulAnvil.Sdk.Common;
using WatchfulAnvil.Sdk.Core;

namespace Cpmf.Rules.Project
{
    /// <summary>Asserts the project output type matches the configured expected value (default: Process).</summary>
    public class ProjectOutputTypeRule : ProjectRule
    {
        private const string OutputTypeKey = "OutputType";
        private const string DefaultOutputType = "Process";

        protected override string Id => "CPMF-G002";
        protected override string Name => "Project Output Type";
        protected override bool IsEnabledByDefault => false;
        protected override string Recommendation =>
            "The project output type does not match the expected value. " +
            "Update the output type in project settings, or change the OutputType parameter on this rule.";
        protected override string? DocumentationLink =>
            "https://github.com/rpapub/WatchfulAnvil/wiki/Rule-Documentation-CPMF-G002";

        protected override void ConfigureParameters(Rule<IProjectModel> rule)
        {
            rule.Parameters.Add(OutputTypeKey, new Parameter
            {
                Key = OutputTypeKey,
                DefaultValue = DefaultOutputType,
                Value = DefaultOutputType,
                LocalizedDisplayName = "Expected output type",
            });
        }

        protected override InspectionResult Inspect(IProjectModel model, Rule rule)
        {
            var ctx = ProjectContext.From(model);
            var expected = GetParameterValue(rule, OutputTypeKey, DefaultOutputType);
            if (!expected.Equals(ctx.OutputType, StringComparison.OrdinalIgnoreCase))
                return Fail(rule, $"Project '{ctx.Name}' has output type '{ctx.OutputType ?? "unknown"}'; expected '{expected}'.");
            return Pass();
        }

        private static string GetParameterValue(Rule rule, string key, string fallback)
        {
            var raw = rule.Parameters[key]?.Value;
            return string.IsNullOrWhiteSpace(raw) ? fallback : raw;
        }
    }
}
