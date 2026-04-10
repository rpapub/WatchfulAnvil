using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using WatchfulAnvil.Sdk.Core;

namespace ACME.HelloWorld.WorkflowAnalyzerRules.Rules.Sample
{
    public class VariableNamingConventionRule : ActivityRule
    {
        private const string RegexKey = "Regex";
        private const string DefaultRegex = @"^([A-Z]|[a-z])+([0-9])*$";

        protected override string Id => "HWR-VNC-001";
        protected override string Name => "Variable Naming Convention";
        protected override TraceLevel DefaultSeverity => TraceLevel.Warning;
        protected override string Recommendation =>
            "Variable names must match the configured regex pattern.";
        protected override string? DocumentationLink =>
            "https://github.com/rpapub/WatchfulAnvil/wiki/Rule-Documentation-HWR-VNC-001";

        protected override void ConfigureParameters(Rule<IActivityModel> rule)
        {
            rule.Parameters.Add(RegexKey, new Parameter
            {
                Key = RegexKey,
                DefaultValue = DefaultRegex,
                Value = DefaultRegex,
                LocalizedDisplayName = "Variable name regex"
            });
        }

        protected override InspectionResult Inspect(IActivityModel activity, Rule rule)
        {
            var pattern = rule.Parameters[RegexKey]?.Value;
            if (string.IsNullOrWhiteSpace(pattern))
                pattern = DefaultRegex;

            var messages = new List<string>();
            foreach (var variable in activity.Variables ?? Enumerable.Empty<IVariableModel>())
            {
                if (!Regex.IsMatch(variable.DisplayName, pattern))
                    messages.Add($"Variable '{variable.DisplayName}' does not match pattern '{pattern}'.");
            }

            if (messages.Count == 0)
                return new InspectionResult { HasErrors = false };

            return new InspectionResult
            {
                HasErrors = true,
                RecommendationMessage = rule.RecommendationMessage,
                ErrorLevel = rule.DefaultErrorLevel,
                Messages = messages,
            };
        }
    }
}
