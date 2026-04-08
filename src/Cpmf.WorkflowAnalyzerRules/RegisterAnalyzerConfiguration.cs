using Cpmf.Rules.Pipeline;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Analyzer.Models;

namespace Cpmf
{
    public sealed class RegisterAnalyzerConfiguration : IRegisterAnalyzerConfiguration
    {
        public void Initialize(IAnalyzerConfigurationService workflowAnalyzerConfigService)
        {
            workflowAnalyzerConfigService.AddRule<IWorkflowModel>(new PipelineSequenceOrderRule().Get());
            workflowAnalyzerConfigService.AddRule<IProjectModel>(new PipelinePresenceCounter().Get());
        }
    }
}
