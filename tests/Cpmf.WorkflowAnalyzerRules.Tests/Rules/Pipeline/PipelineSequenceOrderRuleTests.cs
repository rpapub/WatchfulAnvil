using System.Collections.Generic;
using System.Diagnostics;
using Cpmf.Rules.Pipeline;
using Moq;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using Xunit;

namespace Cpmf.WorkflowAnalyzerRules.Tests.Rules.Pipeline
{
    public class PipelineSequenceOrderRuleTests
    {
        private static readonly string[] AllStages =
            { "Initialize", "Ingest", "Enrich", "Decide", "Execute", "Complete", "Finalize" };

        private readonly PipelineSequenceOrderRule _rule = new PipelineSequenceOrderRule();

        private static Mock<IActivityModel> Child(string displayName)
        {
            var m = new Mock<IActivityModel>();
            m.Setup(c => c.DisplayName).Returns(displayName);
            return m;
        }

        private static Mock<IWorkflowModel> Workflow(string annotation, IReadOnlyCollection<IActivityModel> children)
        {
            var root = new Mock<IActivityModel>();
            root.Setup(r => r.AnnotationText).Returns(annotation);
            root.Setup(r => r.Children).Returns(children);

            var wf = new Mock<IWorkflowModel>();
            wf.Setup(w => w.Root).Returns(root.Object);
            wf.Setup(w => w.DisplayName).Returns("Process");
            return wf;
        }

        [Fact]
        public void Initialize_RegistersRule_WithCorrectId()
        {
            var config = new Mock<IAnalyzerConfigurationService>();
            _rule.Initialize(config.Object);
            config.Verify(s => s.AddRule(
                It.Is<Rule<IWorkflowModel>>(r =>
                    r.Id == "CPMF-PLN-001" &&
                    r.DefaultErrorLevel == TraceLevel.Error)));
        }

        [Fact]
        public void Pass_WhenNoAnnotation()
        {
            var children = new List<IActivityModel> { Child("Initialize").Object };
            var wf = Workflow("", children);
            var result = _rule.Get().Inspect(wf.Object, _rule.Get());
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void Pass_WhenAnnotationDoesNotContainPipeline()
        {
            var children = new List<IActivityModel> { Child("Initialize").Object };
            var wf = Workflow("@unit", children);
            var result = _rule.Get().Inspect(wf.Object, _rule.Get());
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void Pass_WhenAllStagesPresentInCorrectOrder()
        {
            var children = new List<IActivityModel>();
            foreach (var stage in AllStages)
                children.Add(Child(stage).Object);

            var wf = Workflow("@pipeline", children);
            var result = _rule.Get().Inspect(wf.Object, _rule.Get());
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void Pass_WhenNonPipelineChildrenInterspersed()
        {
            // LogMessage activities between stages should be ignored
            var children = new List<IActivityModel>
            {
                Child("Log Message Process Start").Object,
                Child("Initialize").Object,
                Child("Ingest").Object,
                Child("Enrich").Object,
                Child("Decide").Object,
                Child("Execute").Object,
                Child("Complete").Object,
                Child("Finalize").Object,
                Child("Log Message Process End").Object,
            };
            var wf = Workflow("@pipeline", children);
            var result = _rule.Get().Inspect(wf.Object, _rule.Get());
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void Fail_WhenStageIsMissing()
        {
            var children = new List<IActivityModel>
            {
                Child("Initialize").Object,
                Child("Ingest").Object,
                // Enrich is missing
                Child("Decide").Object,
                Child("Execute").Object,
                Child("Complete").Object,
                Child("Finalize").Object,
            };
            var wf = Workflow("@pipeline", children);
            var result = _rule.Get().Inspect(wf.Object, _rule.Get());
            Assert.True(result.HasErrors);
            Assert.Contains(result.Messages, m => m.Contains("Enrich"));
        }

        [Fact]
        public void Fail_WhenStagesOutOfOrder()
        {
            var children = new List<IActivityModel>
            {
                Child("Initialize").Object,
                Child("Enrich").Object,   // Ingest and Enrich swapped
                Child("Ingest").Object,
                Child("Decide").Object,
                Child("Execute").Object,
                Child("Complete").Object,
                Child("Finalize").Object,
            };
            var wf = Workflow("@pipeline", children);
            var result = _rule.Get().Inspect(wf.Object, _rule.Get());
            Assert.True(result.HasErrors);
            Assert.Contains(result.Messages, m => m.Contains("order"));
        }

        [Fact]
        public void Fail_WhenRootIsNull()
        {
            var wf = new Mock<IWorkflowModel>();
            wf.Setup(w => w.Root).Returns((IActivityModel)null);
            var result = _rule.Get().Inspect(wf.Object, _rule.Get());
            Assert.False(result.HasErrors);
        }
    }
}
