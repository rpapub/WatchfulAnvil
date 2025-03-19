using System.Diagnostics;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using CPRIMA.WorkflowAnalyzerRules.LocalizationResources;

namespace CPRIMA.WorkflowAnalyzerRules.Rules.Noop
{
    public class NullOperationProjectRule : IRegisterAnalyzerConfiguration
    {
        private const string RuleId = "CPM_NOOP_001";

        public Rule<IProjectModel> Get()
        {
            return new Rule<IProjectModel>(Strings.CPM_NOOP_001_Name, RuleId, InspectProject)
            {
                RecommendationMessage = Strings.CPM_NOOP_001_Recommendation,
                DefaultErrorLevel = TraceLevel.Info,
                DocumentationLink = "https://github.com/rpapub/WatchfulAnvil"
            };
        }

        public void Initialize(IAnalyzerConfigurationService workflowAnalyzerConfigService)
        {
            workflowAnalyzerConfigService.AddRule<IProjectModel>(Get());
        }

        private InspectionResult InspectProject(IProjectModel project, Rule configuredRule)
        {
            return new InspectionResult { HasErrors = false };
        }
    }
}
