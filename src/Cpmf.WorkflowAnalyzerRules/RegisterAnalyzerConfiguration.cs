using Cpmf.Rules.Pipeline;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;

namespace Cpmf
{
    public sealed class RegisterAnalyzerConfiguration : IRegisterAnalyzerConfiguration
    {
        public void Initialize(IAnalyzerConfigurationService workflowAnalyzerConfigService)
        {
            workflowAnalyzerConfigService.AddRule<UiPath.Studio.Analyzer.Models.IWorkflowModel>(
                new PipelineSequenceOrderRule().Get());
        }
    }
}
