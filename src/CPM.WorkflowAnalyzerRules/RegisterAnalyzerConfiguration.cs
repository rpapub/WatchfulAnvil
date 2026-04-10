using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using CPM.WorkflowAnalyzerRules.Rules.Noop;

namespace CPM.WorkflowAnalyzerRules
{
    public sealed class RegisterAnalyzerConfiguration : IRegisterAnalyzerConfiguration
    {
        public void Initialize(IAnalyzerConfigurationService workflowAnalyzerConfigService)
        {
            new NullOperationRule().Initialize(workflowAnalyzerConfigService);
        }
    }
}
