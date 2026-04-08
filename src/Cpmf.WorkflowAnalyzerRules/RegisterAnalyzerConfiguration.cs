using Cpmf.Rules.Pipeline;
using Cpmf.Rules.Workflow;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Analyzer.Models;

namespace Cpmf
{
    public sealed class RegisterAnalyzerConfiguration : IRegisterAnalyzerConfiguration
    {
        public void Initialize(IAnalyzerConfigurationService api)
        {
            if (api.HasFeature(DesignFeatureKeys.WorkflowAnalyzerV9))
            {
                // safe to use AnnotationText
                api.AddRule<IWorkflowModel>(new PipelineSequenceOrderRule().Get());
                api.AddRule<IWorkflowModel>(new PipelineDomainModelRule().Get());
                api.AddRule<IWorkflowModel>(new LogMessageBookendsRule().Get());
                api.AddRule<IProjectModel>(new PipelinePresenceCounter().Get());
            }
        }
    }
}
