using System.Diagnostics;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace CPRIMA.WorkflowAnalyzerRules.Rules.Noop
{
    public class NullOperationRule : IRegisterAnalyzerConfiguration
    {
        private const string RuleId = "ST-CPM-000";

        public void Initialize(IAnalyzerConfigurationService configService)
        {
            var nullRule = new Rule<IActivityModel>("Null Operation Rule", RuleId, InspectActivity);
            nullRule.DefaultErrorLevel = TraceLevel.Info; // Set to INFO so it doesn't trigger actual warnings/errors
            configService.AddRule<IActivityModel>(nullRule);
        }

        private InspectionResult InspectActivity(IActivityModel activity, Rule configuredRule)
        {
            // This rule does nothing, always returns a successful check
            return new InspectionResult { HasErrors = false };
        }
    }
}
