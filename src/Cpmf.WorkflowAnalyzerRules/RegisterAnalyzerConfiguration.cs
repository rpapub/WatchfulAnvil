using Cpmf.Rules.Pipeline;
using Cpmf.Rules.Workflow;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;

namespace Cpmf
{
    public sealed class RegisterAnalyzerConfiguration : IRegisterAnalyzerConfiguration
    {
        public void Initialize(IAnalyzerConfigurationService api)
        {
            new NoFlowchartStateMachineRule().Initialize(api);
            new VariableNamingRule().Initialize(api);
            new SpecificContentAssignRule().Initialize(api);
            new WorkflowNameRule().Initialize(api);
            new ProjectNameRule().Initialize(api);
            new StaleInvokeArgumentsRule().Initialize(api);
            new PipelineSequenceOrderRule().Initialize(api);
            new PipelineDomainModelRule().Initialize(api);
            new LogMessageBookendsRule().Initialize(api);
            new UnitOutStatusRule().Initialize(api);
            new ModuleCodedConfigRule().Initialize(api);
            new PipelinePresenceCounter().Initialize(api);
        }
    }
}
