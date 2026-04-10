// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using System.Collections.Generic;
using System.Diagnostics;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using WatchfulAnvil.Sdk.Core;

namespace Cpmf.Rules.Workflow
{
    public class VariableNamingRule : ActivityRule
    {
        protected override string Id => "CPMF-N001";
        protected override string Name => "Variable Naming Convention";
        protected override TraceLevel DefaultSeverity => TraceLevel.Warning;
        protected override string Recommendation =>
            "Variable names must follow .NET camelCase conventions: " +
            "start with a lowercase letter and contain no underscores. " +
            "Avoid Hungarian notation prefixes (str_, int_, bool_, dt_, etc.).";
        protected override string? DocumentationLink =>
            "https://github.com/rpapub/WatchfulAnvil/wiki/Rule-Documentation-CPMF-N001";

        protected override InspectionResult Inspect(IActivityModel activity, Rule rule)
        {
            if (activity.Variables == null)
            {
                return new InspectionResult { HasErrors = false };
            }

            var violations = new List<string>();

            foreach (var variable in activity.Variables)
            {
                var name = variable.DisplayName;
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                if (name.Contains('_'))
                {
                    violations.Add(
                        $"Variable '{name}' contains an underscore. " +
                        "Use camelCase without underscores (e.g. 'transactionItem' not 'str_TransactionItem').");
                }
                else if (char.IsUpper(name[0]))
                {
                    violations.Add(
                        $"Variable '{name}' starts with an uppercase letter. " +
                        "Use camelCase for local variables (e.g. 'transactionItem' not 'TransactionItem').");
                }
            }

            if (violations.Count == 0)
            {
                return new InspectionResult { HasErrors = false };
            }

            return new InspectionResult
            {
                HasErrors = true,
                RecommendationMessage = rule.RecommendationMessage,
                Messages = violations,
                ErrorLevel = rule.DefaultErrorLevel,
            };
        }
    }
}
