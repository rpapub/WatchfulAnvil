using System.Diagnostics;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace ACME.HelloWorld.WorkflowAnalyzerRules.Rules.Sample
{
    public class SampleRule : IRegisterAnalyzerConfiguration
    {
        private const string RuleId = "HWR-SAMPLE-001";

        public Rule<IActivityModel> Get()
        {
            return new Rule<IActivityModel>("Sample rule title", RuleId, InspectActivity)
            {
                RecommendationMessage = "Replace this sample rule with a real implementation.",
                DefaultErrorLevel = TraceLevel.Info,
                DocumentationLink = "https://github.com/rpapub/WatchfulAnvil/wiki/Rule-Documentation-HWR-SAMPLE-001"
            };
        }

        public void Initialize(IAnalyzerConfigurationService workflowAnalyzerConfigService)
        {
            workflowAnalyzerConfigService.AddRule<IActivityModel>(Get());
        }

        private InspectionResult InspectActivity(IActivityModel activity, Rule configuredRule)
        {
            return new InspectionResult { HasErrors = false };
        }
    }
}

