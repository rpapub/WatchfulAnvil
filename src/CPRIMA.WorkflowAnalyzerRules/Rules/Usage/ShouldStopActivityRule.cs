using System.Diagnostics;
using System.Linq;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using CPRIMA.WorkflowAnalyzerRules.LocalizationResources;
using WatchfulAnvil.Sdk.Core;

namespace CPRIMA.WorkflowAnalyzerRules.Rules.Usage
{
    public class ShouldStopActivityRule : ProjectRule
    {
        protected override string Id => "CPRIMA-USG-001";
        protected override string Name => Strings.CPRIMA_USG_001_Name;
        protected override string Recommendation => Strings.CPRIMA_USG_001_Recommendation;
        protected override string? DocumentationLink => "https://github.com/rpapub/WatchfulAnvil";

        protected override InspectionResult Inspect(IProjectModel projectModel, Rule rule)
        {
            bool foundShouldStop = AnyWorkflowContainsShouldStop(projectModel);

            return foundShouldStop
                ? new InspectionResult { HasErrors = false }
                : new InspectionResult
                {
                    HasErrors = true,
                    RecommendationMessage = rule.RecommendationMessage,
                    ErrorLevel = TraceLevel.Error
                };
        }

        private bool AnyWorkflowContainsShouldStop(IProjectModel projectModel)
        {
            foreach (var workflow in projectModel.Workflows)
            {
                if (workflow.Root != null && ContainsShouldStopActivity(workflow.Root))
                {
                    return true;
                }
            }
            return false;
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
