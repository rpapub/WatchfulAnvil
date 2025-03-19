using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace CPRIMA.WorkflowAnalyzerRules.Rules.Noop
{
    public class NullOperationProjectRule : IRegisterAnalyzerConfiguration
    {
        private const string RuleId = "ST-CPM-001";

        public void Initialize(IAnalyzerConfigurationService configService)
        {
            var nullOpRule = new Rule<IProjectModel>("Null Project Rule", RuleId, InspectProject);
            nullOpRule.DefaultErrorLevel = System.Diagnostics.TraceLevel.Info;
            configService.AddRule<IProjectModel>(nullOpRule);
        }

        private InspectionResult InspectProject(IProjectModel project, Rule rule)
        {
            return new InspectionResult { HasErrors = false };
        }
    }
}
