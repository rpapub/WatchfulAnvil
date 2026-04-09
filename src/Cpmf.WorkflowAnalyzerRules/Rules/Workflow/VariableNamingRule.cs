using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace Cpmf.Rules.Workflow
{
    public class VariableNamingRule : IRegisterAnalyzerConfiguration
    {
        private const string RuleId = "CPMF-WFL-005";

        public void Initialize(IAnalyzerConfigurationService api)
        {
            // IActivityModel.Variables is available from WorkflowAnalyzerV4 (sdk-capabilities: 20.4.0+).
            api.AddRule<IActivityModel>(Get());
        }

        public Rule<IActivityModel> Get() =>
            new Rule<IActivityModel>("Variable Naming Convention", RuleId, Inspect)
            {
                RecommendationMessage =
                    "Variable names must follow .NET camelCase conventions: " +
                    "start with a lowercase letter and contain no underscores. " +
                    "Avoid Hungarian notation prefixes (str_, int_, bool_, dt_, etc.).",
                DefaultErrorLevel = TraceLevel.Warning,
                DocumentationLink = "https://github.com/rpapub/WatchfulAnvil/wiki/Rule-Documentation-CPMF-WFL-005"
            };

        private static InspectionResult Inspect(IActivityModel activity, Rule rule)
        {
            if (activity.Variables == null)
                return new InspectionResult { HasErrors = false };

            var violations = new List<string>();

            foreach (var variable in activity.Variables)
            {
                var name = variable.DisplayName;
                if (string.IsNullOrWhiteSpace(name))
                    continue;

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
                return new InspectionResult { HasErrors = false };

            return new InspectionResult
            {
                HasErrors = true,
                RecommendationMessage = rule.RecommendationMessage,
                Messages = violations,
                ErrorLevel = rule.DefaultErrorLevel
            };
        }
    }
}
