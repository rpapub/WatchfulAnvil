using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using ACME.HelloWorld.WorkflowAnalyzerRules.Rules.Sample;

namespace ACME.HelloWorld.WorkflowAnalyzerRules
{
    public sealed class RegisterAnalyzerConfiguration : IRegisterAnalyzerConfiguration
    {
        public void Initialize(IAnalyzerConfigurationService workflowAnalyzerConfigService)
        {
            new SampleRule().Initialize(workflowAnalyzerConfigService);
            new VariableNamingConventionRule().Initialize(workflowAnalyzerConfigService);
        }
    }
}
