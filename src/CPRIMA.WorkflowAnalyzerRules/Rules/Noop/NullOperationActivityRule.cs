using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace CPRIMA.WorkflowAnalyzerRules.Rules.Noop
{
    public class NullOperationActivityRule : IRegisterAnalyzerConfiguration
    {
        private const string RuleId = "ST-CPM-003";

        public void Initialize(IAnalyzerConfigurationService configService)
        {
            var nullOpRule = new Rule<IActivityModel>("Null Activity Rule", RuleId, InspectActivity);
            nullOpRule.DefaultErrorLevel = System.Diagnostics.TraceLevel.Info;
            configService.AddRule<IActivityModel>(nullOpRule);
        }

        private InspectionResult InspectActivity(IActivityModel activity, Rule rule)
        {
            return new InspectionResult { HasErrors = false };
        }
    }
}
