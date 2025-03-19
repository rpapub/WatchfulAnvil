using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Analyzer.Models;
using CPRIMA.WorkflowAnalyzerRules.Rules.Usage;
using CPRIMA.WorkflowAnalyzerRules.Rules.Noop;

namespace CPRIMA.WorkflowAnalyzerRules
{
    public sealed class RegisterAnalyzerConfiguration : IRegisterAnalyzerConfiguration
    {
        public void Initialize(IAnalyzerConfigurationService workflowAnalyzerConfigService)
        {
            // Registering Project-Level Rules
            var shouldStopRule = new ShouldStopActivityRule().Get();
            workflowAnalyzerConfigService.AddRule<IProjectModel>(shouldStopRule);
            var nullOperationProjectRule = new NullOperationProjectRule().Get();
            workflowAnalyzerConfigService.AddRule<IProjectModel>(nullOperationProjectRule);

            // Registering Workflow-Level Rules
            var nullOperationWorkflowRule = new NullOperationWorkflowRule().Get();
            workflowAnalyzerConfigService.AddRule<IWorkflowModel>(nullOperationWorkflowRule);

            // Registering Activity-Level Rules
            var nullOperationRule = new NullOperationRule().Get();
            workflowAnalyzerConfigService.AddRule<IActivityModel>(nullOperationRule);
        }
    }
}
