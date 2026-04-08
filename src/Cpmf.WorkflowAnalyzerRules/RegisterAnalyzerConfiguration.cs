using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Analyzer.Models;
using Cpmf.Rules.Sample;

namespace Cpmf
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

