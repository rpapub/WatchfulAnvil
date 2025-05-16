using System.Diagnostics;
using System.Linq;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using CPRIMA.WorkflowAnalyzerRules.LocalizationResources;

namespace CPRIMA.WorkflowAnalyzerRules.Rules.Usage
{
    public class ShouldStopActivityRule : IRegisterAnalyzerConfiguration
    {
        private const string RuleId = "CPRIMA-USG-001";

        public Rule<IProjectModel> Get()
        {
            return new Rule<IProjectModel>(Strings.CPRIMA_USG_001_Name, RuleId, InspectProjectForShouldStop)
            {
                RecommendationMessage = Strings.CPRIMA_USG_001_Recommendation,
                DefaultErrorLevel = TraceLevel.Error,
                DocumentationLink = "https://github.com/rpapub/WatchfulAnvil"
            };
        }

        public void Initialize(IAnalyzerConfigurationService workflowAnalyzerConfigService)
        {
            workflowAnalyzerConfigService.AddRule<IProjectModel>(Get());
        }

        private InspectionResult InspectProjectForShouldStop(IProjectModel projectModel, Rule configuredRule)
        {
            bool foundShouldStop = AnyWorkflowContainsShouldStop(projectModel);

            return foundShouldStop
                ? new InspectionResult { HasErrors = false }
                : new InspectionResult
                {
                    HasErrors = true,
                    RecommendationMessage = configuredRule.RecommendationMessage,
                    ErrorLevel = TraceLevel.Error
                };
        }

        private bool AnyWorkflowContainsShouldStop(IProjectModel projectModel)
        {
            foreach (var workflow in projectModel.Workflows)
            {
                if (workflow.Root != null && ContainsShouldStopActivity(workflow.Root))
                {
                    return true; // Found at least one, return immediately
                }
            }
            return false; // No workflow contains 'ShouldStop'
        }

        private bool ContainsShouldStopActivity(IActivityModel activity)
        {
            if (activity.Type.Contains("ShouldStop") || activity.ToolboxName.Contains("ShouldStop"))
            {
                return true;
            }

            return activity.Children?.Any(ContainsShouldStopActivity) ?? false;
        }
    }
}
