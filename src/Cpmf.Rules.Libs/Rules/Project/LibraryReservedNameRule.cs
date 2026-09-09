// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using System;
using System.Linq;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using WatchfulAnvil.Sdk.Common;
using WatchfulAnvil.Sdk.Core;

namespace Cpmf.Rules.Project
{
    public class LibraryReservedNameRule : ProjectRule
    {
        private const string AdditionalNamesKey = "AdditionalReservedNames";

        protected override string Id => "CPMF-N005";
        protected override string Name => "Library Reserved Name";
        protected override string[] TargetOutputTypes => new[] { "Library" };
        protected override string Recommendation =>
            "Rename the project. The configured reserved names cause compile errors in consuming projects.";
        protected override string? DocumentationLink =>
            "https://github.com/rpapub/WatchfulAnvil/wiki/Rule-Documentation-CPMF-N005";

        protected override void ConfigureParameters(Rule<IProjectModel> rule)
        {
            rule.Parameters.Add(AdditionalNamesKey, new Parameter
            {
                Key = AdditionalNamesKey,
                DefaultValue = string.Empty,
                Value = string.Empty,
                LocalizedDisplayName = "Additional reserved names (comma-separated)",
            });
        }

        protected override InspectionResult Inspect(IProjectModel model, Rule rule)
        {
            var ctx = ProjectContext.From(model);

            if (ctx.Name.Equals("lib", StringComparison.OrdinalIgnoreCase))
                return Fail(rule, $"Project name 'lib' is reserved and causes compile errors in consuming projects.");

            var extra = rule.Parameters[AdditionalNamesKey]?.Value ?? string.Empty;
            foreach (var name in extra.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                      .Select(s => s.Trim())
                                      .Where(s => s.Length > 0))
            {
                if (ctx.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return Fail(rule, $"Project name '{ctx.Name}' is reserved and causes compile errors in consuming projects.");
            }

            return Pass();
        }
    }
}
