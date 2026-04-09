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
            // WorkflowAnalyzerV4 (sdk-capabilities: 20.4.0+)
            // Type, Children, Properties, Arguments, Variables, RelativePath available — no AnnotationText yet.
            api.AddRule<IActivityModel>(new NoFlowchartStateMachineRule().Get());
            api.AddRule<IActivityModel>(new VariableNamingRule().Get());
            api.AddRule<IActivityModel>(new SpecificContentAssignRule().Get());
            api.AddRule<IWorkflowModel>(new WorkflowNameRule().Get());
            api.AddRule<IProjectModel>(new ProjectNameRule().Get());

            // WorkflowAnalyzerV9 (sdk-capabilities: 21.4.1+)
            // Required for AnnotationText. All annotation-convention rules depend on this.
            if (!api.HasFeature(DesignFeatureKeys.WorkflowAnalyzerV9))
                return; // Studio < 21.4 — annotation-based rules cannot function without AnnotationText.

            api.AddRule<IWorkflowModel>(new StaleInvokeArgumentsRule().Get());
            api.AddRule<IWorkflowModel>(new PipelineSequenceOrderRule().Get());
            api.AddRule<IWorkflowModel>(new PipelineDomainModelRule().Get());
            api.AddRule<IWorkflowModel>(new LogMessageBookendsRule().Get());
            api.AddRule<IWorkflowModel>(new UnitOutStatusRule().Get());
            api.AddRule<IWorkflowModel>(new ModuleCodedConfigRule().Get());
            api.AddRule<IProjectModel>(new PipelinePresenceCounter().Get());
        }
    }
}
