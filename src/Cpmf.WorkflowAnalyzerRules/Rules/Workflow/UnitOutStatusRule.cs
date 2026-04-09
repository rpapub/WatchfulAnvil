using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace Cpmf.Rules.Workflow
{
    public class UnitOutStatusRule : IRegisterAnalyzerConfiguration
    {
        private const string RuleId = "CPMF-WFL-002";
        private const string ArgumentName = "out_Status";

        public void Initialize(IAnalyzerConfigurationService api)
        {
            // AnnotationText requires WorkflowAnalyzerV9 (sdk-capabilities: 21.4.1+).
            if (!api.HasFeature(DesignFeatureKeys.WorkflowAnalyzerV9))
                return; // Studio < 21.4 — rule cannot function without AnnotationText.

            api.AddRule<IWorkflowModel>(Get());
        }

        public Rule<IWorkflowModel> Get() =>
            new Rule<IWorkflowModel>("Unit Out Status Argument", RuleId, Inspect)
            {
                RecommendationMessage =
                    "Workflows annotated @unit must declare an Out argument named 'out_Status'.",
                DefaultErrorLevel = TraceLevel.Error,
                DocumentationLink = "https://github.com/rpapub/WatchfulAnvil/wiki/Rule-Documentation-CPMF-WFL-002"
            };

        private static InspectionResult Inspect(IWorkflowModel workflow, Rule rule)
        {
            if (workflow.Root == null)
                return new InspectionResult { HasErrors = false };

            var annotation = workflow.Root.AnnotationText;
            if (string.IsNullOrWhiteSpace(annotation) || !annotation.Contains("@unit"))
                return new InspectionResult { HasErrors = false };

            var hasOutStatus = workflow.Arguments != null &&
                System.Linq.Enumerable.Any(workflow.Arguments, a =>
                    a.DisplayName == ArgumentName &&
                    a.Direction == ArgumentDirection.Out);

            if (hasOutStatus)
                return new InspectionResult { HasErrors = false };

            return new InspectionResult
            {
                HasErrors = true,
                RecommendationMessage = rule.RecommendationMessage,
                Messages = new List<string>
                {
                    $"Workflow annotated @unit is missing required Out argument '{ArgumentName}'."
                },
                ErrorLevel = rule.DefaultErrorLevel
            };
        }
    }
}
