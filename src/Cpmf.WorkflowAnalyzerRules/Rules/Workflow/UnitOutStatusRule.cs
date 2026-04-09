using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using WatchfulAnvil.Sdk.Core;

namespace Cpmf.Rules.Workflow
{
    public class UnitOutStatusRule : WorkflowRule
    {
        private const string ArgumentName = "out_Status";

        protected override string Id => "CPMF-WFL-002";
        protected override string Name => "Unit Out Status Argument";
        protected override string? RequiredFeature => DesignFeatureKeys.WorkflowAnalyzerV9;
        protected override string Recommendation =>
            "Workflows annotated @unit must declare an Out argument named 'out_Status'.";
        protected override string? DocumentationLink =>
            "https://github.com/rpapub/WatchfulAnvil/wiki/Rule-Documentation-CPMF-WFL-002";

        protected override InspectionResult Inspect(IWorkflowModel workflow, Rule rule)
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
                Messages = new System.Collections.Generic.List<string>
                {
                    $"Workflow annotated @unit is missing required Out argument '{ArgumentName}'."
                },
                ErrorLevel = rule.DefaultErrorLevel
            };
        }
    }
}
