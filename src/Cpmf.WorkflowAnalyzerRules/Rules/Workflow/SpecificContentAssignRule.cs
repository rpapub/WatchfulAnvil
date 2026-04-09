using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using WatchfulAnvil.Sdk.Core;

namespace Cpmf.Rules.Workflow
{
    public class SpecificContentAssignRule : ActivityRule
    {
        private const string SpecificContentPattern = ".SpecificContent(";

        protected override string Id => "CPMF-WFL-006";
        protected override string Name => "SpecificContent Assignment Must Use MultipleAssign";
        protected override string Recommendation =>
            "Assignments to TransactionItem.SpecificContent(...) must always be inside a Multiple Assign activity. " +
            "Group all SpecificContent writes into a single Multiple Assign.";
        protected override string? DocumentationLink =>
            "https://github.com/rpapub/WatchfulAnvil/wiki/Rule-Documentation-CPMF-WFL-006";

        protected override InspectionResult Inspect(IActivityModel activity, Rule rule)
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
                Messages = new System.Collections.Generic.List<string>
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
            return type == "System.Activities.Statements.Assign" ||
                   type.StartsWith("System.Activities.Statements.Assign`");
        }

        private static string? GetToExpression(IActivityModel activity)
        {
            if (activity.Properties == null)
                return null;
            var toProp = System.Linq.Enumerable.FirstOrDefault(
                activity.Properties, p => p.DisplayName == "To");
            return toProp?.DefinedExpression;
        }
    }
}
