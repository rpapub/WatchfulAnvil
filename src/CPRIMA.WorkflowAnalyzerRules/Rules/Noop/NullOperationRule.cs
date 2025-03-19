using System.Diagnostics;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using CPRIMA.WorkflowAnalyzerRules.LocalizationResources;

namespace CPRIMA.WorkflowAnalyzerRules.Rules.Noop
{
    public class NullOperationRule : IRegisterAnalyzerConfiguration
    {
        private const string RuleId = "CPM-NOOP-000";

        public Rule<IActivityModel> Get()
        {
            return new Rule<IActivityModel>(Strings.CPM_NOOP_000_Name, RuleId, InspectActivity)
            {
                RecommendationMessage = Strings.CPM_NOOP_000_Recommendation,
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
            // This rule does nothing, always returns a successful check
            return new InspectionResult { HasErrors = false };
        }
    }
}
