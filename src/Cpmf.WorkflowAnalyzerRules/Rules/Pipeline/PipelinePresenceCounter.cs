using System.Collections.Generic;
using System.Linq;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using WatchfulAnvil.Sdk.Core;

namespace Cpmf.Rules.Pipeline
{
    public class PipelinePresenceCounter : ProjectRule
    {
        protected override string Id => "CPMF-PLN-C001";
        protected override string Name => "Pipeline Presence";
        protected override string? RequiredFeature => DesignFeatureKeys.WorkflowAnalyzerV9;
        protected override string Recommendation =>
            "A CPMF project must contain at least one workflow annotated @pipeline.";
        protected override string? DocumentationLink =>
            "https://github.com/rpapub/WatchfulAnvil/wiki/Rule-Documentation-CPMF-PLN-C001";

        protected override InspectionResult Inspect(IProjectModel project, Rule rule)
        {
            var workflows = project.Workflows;
            if (workflows == null)
                return new InspectionResult { HasErrors = false };

            var pipelineCount = workflows.Count(w =>
                w.Root != null &&
                !string.IsNullOrWhiteSpace(w.Root.AnnotationText) &&
                w.Root.AnnotationText.Contains("@pipeline"));

            if (pipelineCount == 0)
                return new InspectionResult
                {
                    HasErrors = true,
                    RecommendationMessage = rule.RecommendationMessage,
                    Messages = new List<string>
                    {
                        "No workflow annotated @pipeline was found in this project. " +
                        "At least one pipeline workflow is required."
                    },
                    ErrorLevel = rule.DefaultErrorLevel
                };

            return new InspectionResult { HasErrors = false };
        }
    }
}
