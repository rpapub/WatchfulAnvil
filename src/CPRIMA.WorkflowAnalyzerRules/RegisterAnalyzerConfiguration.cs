using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using CPRIMA.WorkflowAnalyzerRules.Rules.Usage;
using CPRIMA.WorkflowAnalyzerRules.Rules.Noop;

namespace CPRIMA.WorkflowAnalyzerRules
{
    public sealed class RegisterAnalyzerConfiguration : IRegisterAnalyzerConfiguration
    {
        public void Initialize(IAnalyzerConfigurationService workflowAnalyzerConfigService)
        {
            new ShouldStopActivityRule().Initialize(workflowAnalyzerConfigService);
            new NullOperationProjectRule().Initialize(workflowAnalyzerConfigService);
            new NullOperationWorkflowRule().Initialize(workflowAnalyzerConfigService);
            new NullOperationRule().Initialize(workflowAnalyzerConfigService);
        }
    }
}
