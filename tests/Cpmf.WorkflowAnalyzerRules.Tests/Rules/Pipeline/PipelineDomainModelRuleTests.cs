using System.Diagnostics;
using Cpmf.Rules.Pipeline;
using Moq;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using Xunit;

namespace Cpmf.WorkflowAnalyzerRules.Tests.Rules.Pipeline
{
    public class PipelineDomainModelRuleTests
    {
        private readonly PipelineDomainModelRule _rule = new PipelineDomainModelRule();

        private static Mock<IWorkflowModel> Workflow(string annotation)
        {
            var root = new Mock<IActivityModel>();
            root.Setup(r => r.AnnotationText).Returns(annotation);

            var wf = new Mock<IWorkflowModel>();
            wf.Setup(w => w.Root).Returns(root.Object);
            return wf;
        }

        // --- Registration ---

        [Fact]
        public void Initialize_RegistersRule_WithCorrectId()
        {
            var api = new Mock<IAnalyzerConfigurationService>();
            api.Setup(a => a.HasFeature(DesignFeatureKeys.WorkflowAnalyzerV9)).Returns(true);
            _rule.Initialize(api.Object);
            api.Verify(s => s.AddRule(
                It.Is<Rule<IWorkflowModel>>(r =>
                    r.Id == "CPMF-PLN-002" &&
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
        public void Pass_WhenAnnotationIsNotPipeline()
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

        // --- Domain model checks ---

        [Fact]
        public void Pass_WhenPipelineAnnotationHasDomainModelLine()
        {
            var wf = Workflow("@pipeline\n@domain-model:MyNamespace.MyDomainModel");
            Assert.False(_rule.Get().Inspect(wf.Object, _rule.Get()).HasErrors);
        }

        [Fact]
        public void Pass_WhenDomainModelLineHasLeadingWhitespace()
        {
            var wf = Workflow("@pipeline\r\n  @domain-model:MyNamespace.MyDomainModel");
            Assert.False(_rule.Get().Inspect(wf.Object, _rule.Get()).HasErrors);
        }

        [Fact]
        public void Pass_WhenDomainModelLineHasSpaceAroundColon()
        {
            var wf = Workflow("@pipeline\n@domain-model: MyNamespace.MyDomainModel");
            Assert.False(_rule.Get().Inspect(wf.Object, _rule.Get()).HasErrors);
        }

        [Fact]
        public void Fail_WhenPipelineAnnotationMissesDomainModelLine()
        {
            var wf = Workflow("@pipeline");
            var result = _rule.Get().Inspect(wf.Object, _rule.Get());
            Assert.True(result.HasErrors);
            Assert.Contains(result.Messages, m => m.Contains("@domain-model"));
        }

        [Fact]
        public void Fail_WhenDomainModelTypeNameIsEmpty()
        {
            var wf = Workflow("@pipeline\n@domain-model:");
            var result = _rule.Get().Inspect(wf.Object, _rule.Get());
            Assert.True(result.HasErrors);
            Assert.Contains(result.Messages, m => m.Contains("empty"));
        }

        [Fact]
        public void Fail_WhenDomainModelTypeNameIsWhitespace()
        {
            var wf = Workflow("@pipeline\n@domain-model:   ");
            var result = _rule.Get().Inspect(wf.Object, _rule.Get());
            Assert.True(result.HasErrors);
            Assert.Contains(result.Messages, m => m.Contains("empty"));
        }
    }
}
