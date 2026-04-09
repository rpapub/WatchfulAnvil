using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace Cpmf.Rules.Workflow
{
    public class SpecificContentAssignRule : IRegisterAnalyzerConfiguration
    {
        private const string RuleId = "CPMF-WFL-006";
        private const string SpecificContentPattern = ".SpecificContent(";

        public void Initialize(IAnalyzerConfigurationService api)
        {
            // IActivityModel.Type and .Properties are available from WorkflowAnalyzerV4 (sdk-capabilities: 20.4.0+).
            // ToolboxName is V9 but the Inspect method is null-safe — degrades gracefully on V4.
            api.AddRule<IActivityModel>(Get());
        }

        public Rule<IActivityModel> Get() =>
            new Rule<IActivityModel>("SpecificContent Assignment Must Use MultipleAssign", RuleId, Inspect)
            {
                RecommendationMessage =
                    "Assignments to TransactionItem.SpecificContent(...) must always be inside a Multiple Assign activity. " +
                    "Group all SpecificContent writes into a single Multiple Assign.",
                DefaultErrorLevel = TraceLevel.Error,
                DocumentationLink = "https://github.com/rpapub/WatchfulAnvil/wiki/Rule-Documentation-CPMF-WFL-006"
            };

        private static InspectionResult Inspect(IActivityModel activity, Rule rule)
        {
            if (!IsStandaloneAssign(activity))
                return new InspectionResult { HasErrors = false };

            var toExpr = GetToExpression(activity);
            if (toExpr == null || !toExpr.Contains(SpecificContentPattern))
                return new InspectionResult { HasErrors = false };

            return new InspectionResult
            {
                HasErrors = true,
                RecommendationMessage = rule.RecommendationMessage,
                Messages = new List<string>
                {
                    $"Assign '{activity.DisplayName}' assigns to '{toExpr.Trim('[', ']')}' which uses SpecificContent. " +
                    "Move this assignment into a Multiple Assign activity together with all other SpecificContent writes."
                },
                ErrorLevel = rule.DefaultErrorLevel
            };
        }

        private static bool IsStandaloneAssign(IActivityModel activity)
        {
            var toolbox = activity.ToolboxName ?? string.Empty;
            if (toolbox == "Assign")
                return true;

            var type = activity.Type ?? string.Empty;
            // Covers "System.Activities.Statements.Assign" and generic variants like
            // "System.Activities.Statements.Assign`1[System.Object]"
            return type == "System.Activities.Statements.Assign" ||
                   type.StartsWith("System.Activities.Statements.Assign`");
        }

        private static string GetToExpression(IActivityModel activity)
        {
            if (activity.Properties == null)
                return null;
            var toProp = System.Linq.Enumerable.FirstOrDefault(
                activity.Properties, p => p.DisplayName == "To");
            return toProp?.DefinedExpression;
        }
    }
}
