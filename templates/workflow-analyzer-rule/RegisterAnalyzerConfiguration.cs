using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Analyzer.Models;
using {{NAMESPACE}}.Rules.Sample;

namespace {{NAMESPACE}}
{
    public sealed class RegisterAnalyzerConfiguration : IRegisterAnalyzerConfiguration
    {
        public void Initialize(IAnalyzerConfigurationService workflowAnalyzerConfigService)
        {
            // Registering Activity-Level Rules
            var sampleRule = new SampleRule().Get();
            workflowAnalyzerConfigService.AddRule<IActivityModel>(sampleRule);
        }
    }
}
