using System.Diagnostics;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using CPRIMA.WorkflowAnalyzerRules.LocalizationResources;

namespace CPRIMA.WorkflowAnalyzerRules.Rules.Noop
{
    public class NullOperationWorkflowRule : IRegisterAnalyzerConfiguration
    {
        private const string RuleId = "CPRIMA-NOOP-002";

        public Rule<IWorkflowModel> Get()
        {
            return new Rule<IWorkflowModel>(Strings.CPRIMA_NOOP_002_Name, RuleId, InspectWorkflow)
            {
                RecommendationMessage = Strings.CPRIMA_NOOP_002_Recommendation,
                DefaultErrorLevel = TraceLevel.Info,
                DocumentationLink = "https://github.com/rpapub/WatchfulAnvil"
            };
        }

        public void Initialize(IAnalyzerConfigurationService workflowAnalyzerConfigService)
        {
            workflowAnalyzerConfigService.AddRule<IWorkflowModel>(Get());
        }

        private InspectionResult InspectWorkflow(IWorkflowModel workflow, Rule configuredRule)
        {
            return new InspectionResult { HasErrors = false };
        }
    }
}
