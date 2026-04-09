using System.Collections.Generic;
using System.Diagnostics;
using Cpmf.Rules.Workflow;
using Moq;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using Xunit;

namespace Cpmf.WorkflowAnalyzerRules.Tests.Rules.Workflow
{
    public class ModuleCodedConfigRuleTests
    {
        private readonly ModuleCodedConfigRule _rule = new ModuleCodedConfigRule();

        private static Mock<IArgumentModel> Arg(string name, ArgumentDirection direction, string type)
        {
            var m = new Mock<IArgumentModel>();
            m.Setup(a => a.DisplayName).Returns(name);
            m.Setup(a => a.Direction).Returns(direction);
            m.Setup(a => a.Type).Returns(type);
            return m;
        }

        private static Mock<IWorkflowModel> Workflow(
            string annotation,
            IReadOnlyCollection<IArgumentModel> arguments = null)
        {
            var root = new Mock<IActivityModel>();
            root.Setup(r => r.AnnotationText).Returns(annotation);

            var wf = new Mock<IWorkflowModel>();
            wf.Setup(w => w.Root).Returns(root.Object);
            wf.Setup(w => w.Arguments).Returns(arguments);
            return wf;
        }

        private static IReadOnlyCollection<IArgumentModel> WithCodedConfig(string fullType = "CodedConfig")
            => new List<IArgumentModel>
            {
                Arg("in_Config", ArgumentDirection.In, fullType).Object
            } as IReadOnlyCollection<IArgumentModel>;

        // --- Registration ---

        [Fact]
        public void Initialize_RegistersRule_WithCorrectId()
        {
            var api = new Mock<IAnalyzerConfigurationService>();
            api.Setup(a => a.HasFeature(DesignFeatureKeys.WorkflowAnalyzerV9)).Returns(true);
            _rule.Initialize(api.Object);
            api.Verify(s => s.AddRule(
                It.Is<Rule<IWorkflowModel>>(r =>
                    r.Id == "CPMF-WFL-003" &&
                    r.DefaultErrorLevel == TraceLevel.Error)));
        }

        // --- Annotation gate ---

        [Fact]
        public void Pass_WhenNoAnnotation()
        {
            var wf = Workflow("");
            Assert.False(_rule.Get().Inspect(wf.Object, _rule.Get()).HasErrors);
        }

        [Fact]
        public void Pass_WhenAnnotationIsUnit()
        {
            var wf = Workflow("@unit");
            Assert.False(_rule.Get().Inspect(wf.Object, _rule.Get()).HasErrors);
        }

        [Fact]
        public void Pass_WhenRootIsNull()
        {
            var wf = new Mock<IWorkflowModel>();
            wf.Setup(w => w.Root).Returns((IActivityModel)null);
            Assert.False(_rule.Get().Inspect(wf.Object, _rule.Get()).HasErrors);
        }

        // --- @module ---

        [Fact]
        public void Pass_WhenModuleHasCodedConfigSimpleName()
        {
            var wf = Workflow("@module", WithCodedConfig("CodedConfig"));
            Assert.False(_rule.Get().Inspect(wf.Object, _rule.Get()).HasErrors);
        }

        [Fact]
        public void Pass_WhenModuleHasCodedConfigQualified()
        {
            var wf = Workflow("@module", WithCodedConfig("MyProject.Config.CodedConfig"));
            Assert.False(_rule.Get().Inspect(wf.Object, _rule.Get()).HasErrors);
        }

        [Fact]
        public void Fail_WhenModuleMissingCodedConfig()
        {
            var args = new List<IArgumentModel>
            {
                Arg("in_Something", ArgumentDirection.In, "System.String").Object
            } as IReadOnlyCollection<IArgumentModel>;
            var wf = Workflow("@module", args);
            var result = _rule.Get().Inspect(wf.Object, _rule.Get());
            Assert.True(result.HasErrors);
            Assert.Contains(result.Messages, m => m.Contains("CodedConfig"));
        }

        [Fact]
        public void Fail_WhenModuleHasCodedConfigAsOutDirection()
        {
            var args = new List<IArgumentModel>
            {
                Arg("out_Config", ArgumentDirection.Out, "CodedConfig").Object
            } as IReadOnlyCollection<IArgumentModel>;
            var wf = Workflow("@module", args);
            var result = _rule.Get().Inspect(wf.Object, _rule.Get());
            Assert.True(result.HasErrors);
        }

        // --- @pipeline ---

        [Fact]
        public void Pass_WhenPipelineHasCodedConfigQualified()
        {
            var wf = Workflow("@pipeline", WithCodedConfig("Acme.Process.CodedConfig"));
            Assert.False(_rule.Get().Inspect(wf.Object, _rule.Get()).HasErrors);
        }

        [Fact]
        public void Fail_WhenPipelineMissingCodedConfig()
        {
            var args = new List<IArgumentModel>
            {
                Arg("in_TransactionItem", ArgumentDirection.In, "UiPath.Core.QueueItem").Object
            } as IReadOnlyCollection<IArgumentModel>;
            var wf = Workflow("@pipeline", args);
            var result = _rule.Get().Inspect(wf.Object, _rule.Get());
            Assert.True(result.HasErrors);
            Assert.Contains(result.Messages, m => m.Contains("CodedConfig"));
        }
    }
}
