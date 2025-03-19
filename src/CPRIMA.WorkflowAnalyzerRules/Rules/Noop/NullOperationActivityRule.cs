using System.Diagnostics;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using CPRIMA.WorkflowAnalyzerRules.LocalizationResources;

namespace CPRIMA.WorkflowAnalyzerRules.Rules.Noop
{
    public class NullOperationActivityRule : IRegisterAnalyzerConfiguration
    {
        private const string RuleId = "CPM_NOOP_003";

        public Rule<IActivityModel> Get()
        {
            return new Rule<IActivityModel>(Strings.CPM_NOOP_003_Name, RuleId, InspectActivity)
            {
                RecommendationMessage = Strings.CPM_NOOP_003_Recommendation,
                DefaultErrorLevel = TraceLevel.Info,
                DocumentationLink = "https://github.com/rpapub/WatchfulAnvil"
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
