using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using WatchfulAnvil.Sdk.Core;

namespace Cpmf.Rules.Workflow
{
    public class NoFlowchartStateMachineRule : ActivityRule
    {
        protected override string Id => "CPMF-WFL-004";
        protected override string Name => "No Flowchart or State Machine";
        protected override string Recommendation =>
            "Flowchart and State Machine activities are not permitted. Use Sequence instead.";
        protected override string? DocumentationLink =>
            "https://github.com/rpapub/WatchfulAnvil/wiki/Rule-Documentation-CPMF-WFL-004";

        protected override InspectionResult Inspect(IActivityModel activity, Rule rule)
        {
            if (IsFlowchartOrStateMachine(activity))
                return new InspectionResult
                {
                    HasErrors = true,
                    RecommendationMessage = rule.RecommendationMessage,
                    Messages = new System.Collections.Generic.List<string>
                    {
                        $"Activity '{activity.DisplayName}' is a {FriendlyName(activity)}, which is not permitted. " +
                        "Use Sequence instead."
                    },
                    ErrorLevel = rule.DefaultErrorLevel
                };

            return new InspectionResult { HasErrors = false };
        }

        private static bool IsFlowchartOrStateMachine(IActivityModel activity)
        {
            var type = activity.Type ?? string.Empty;
            var toolbox = activity.ToolboxName ?? string.Empty;

            return type == "Flowchart" || type == "System.Activities.Statements.Flowchart" ||
                   type == "StateMachine" || type == "System.Activities.Statements.StateMachine" ||
                   toolbox == "Flowchart" || toolbox == "State Machine";
        }

        private static string FriendlyName(IActivityModel activity)
        {
            var type = activity.Type ?? activity.ToolboxName ?? string.Empty;
            return type.Contains("StateMachine") || activity.ToolboxName == "State Machine"
                ? "State Machine"
                : "Flowchart";
        }
    }
}
