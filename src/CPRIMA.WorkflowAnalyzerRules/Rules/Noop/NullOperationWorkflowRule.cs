using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace CPRIMA.WorkflowAnalyzerRules.Rules.Noop
{
    public class NullOperationWorkflowRule : IRegisterAnalyzerConfiguration
    {
        private const string RuleId = "ST-CPM-002";

        public void Initialize(IAnalyzerConfigurationService configService)
        {
            var nullOpRule = new Rule<IWorkflowModel>("Null Workflow Rule", RuleId, InspectWorkflow);
            nullOpRule.DefaultErrorLevel = System.Diagnostics.TraceLevel.Info;
            configService.AddRule<IWorkflowModel>(nullOpRule);
        }

        private InspectionResult InspectWorkflow(IWorkflowModel workflow, Rule rule)
        {
            return new InspectionResult { HasErrors = false };
        }
    }
}
