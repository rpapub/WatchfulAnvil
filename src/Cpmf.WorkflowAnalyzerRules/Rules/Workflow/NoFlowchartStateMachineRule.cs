using System.Collections.Generic;
using System.Diagnostics;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace Cpmf.Rules.Workflow
{
    public class NoFlowchartStateMachineRule : IRegisterAnalyzerConfiguration
    {
        private const string RuleId = "CPMF-WFL-004";

        public void Initialize(IAnalyzerConfigurationService api)
        {
            // Type is available from WorkflowAnalyzerV4 (sdk-capabilities: 20.4.0+).
            // ToolboxName is V9 but the Inspect method is null-safe — degrades gracefully on V4.
            api.AddRule<IActivityModel>(Get());
        }

        public Rule<IActivityModel> Get() =>
            new Rule<IActivityModel>("No Flowchart or State Machine", RuleId, Inspect)
            {
                RecommendationMessage =
                    "Flowchart and State Machine activities are not permitted. Use Sequence instead.",
                DefaultErrorLevel = TraceLevel.Error,
                DocumentationLink = "https://github.com/rpapub/WatchfulAnvil/wiki/Rule-Documentation-CPMF-WFL-004"
            };

        private static InspectionResult Inspect(IActivityModel activity, Rule rule)
        {
            if (IsFlowchartOrStateMachine(activity))
                return new InspectionResult
                {
                    HasErrors = true,
                    RecommendationMessage = rule.RecommendationMessage,
                    Messages = new List<string>
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
