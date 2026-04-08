using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace Cpmf.Rules.Pipeline
{
    public class PipelinePresenceCounter : IRegisterAnalyzerConfiguration
    {
        private const string CounterId = "CPMF-PLN-C001";

        public void Initialize(IAnalyzerConfigurationService config)
        {
            config.AddRule<IProjectModel>(Get());
        }

        public Rule<IProjectModel> Get() =>
            new Rule<IProjectModel>("Pipeline Presence", CounterId, Inspect)
            {
                RecommendationMessage =
                    "A CPMF project must contain at least one workflow annotated @pipeline.",
                DefaultErrorLevel = TraceLevel.Error,
                DocumentationLink = "https://github.com/rpapub/WatchfulAnvil/wiki/Rule-Documentation-CPMF-PLN-C001"
            };

        private static InspectionResult Inspect(IProjectModel project, Rule rule)
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
