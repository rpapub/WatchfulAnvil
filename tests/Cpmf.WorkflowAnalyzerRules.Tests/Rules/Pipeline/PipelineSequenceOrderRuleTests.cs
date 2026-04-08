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

        private static Mock<IArgumentModel> Arg(string name, ArgumentDirection direction, string type)
        {
            var m = new Mock<IArgumentModel>();
            m.Setup(a => a.DisplayName).Returns(name);
            m.Setup(a => a.Direction).Returns(direction);
            m.Setup(a => a.Type).Returns(type);
            return m;
        }

        private static IReadOnlyCollection<IArgumentModel> ValidArgs() =>
            new List<IArgumentModel>
            {
                Arg("in_TransactionItem", ArgumentDirection.In, "UiPath.Core.QueueItem").Object
            };

        private static Mock<IWorkflowModel> Workflow(
            string annotation,
            IReadOnlyCollection<IActivityModel> children,
            IReadOnlyCollection<IArgumentModel> arguments = null)
        {
            var root = new Mock<IActivityModel>();
            root.Setup(r => r.AnnotationText).Returns(annotation);
            root.Setup(r => r.Children).Returns(children);

            var wf = new Mock<IWorkflowModel>();
            wf.Setup(w => w.Root).Returns(root.Object);
            wf.Setup(w => w.DisplayName).Returns("Process");
            wf.Setup(w => w.Arguments).Returns(arguments ?? ValidArgs());
            return wf;
        }

        private List<IActivityModel> AllStageChildren()
        {
            var children = new List<IActivityModel>();
            foreach (var stage in AllStages)
                children.Add(Child(stage).Object);
            return children;
        }

        // --- Registration ---

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

        // --- Annotation gate ---

        [Fact]
        public void Pass_WhenNoAnnotation()
        {
            var wf = Workflow("", new List<IActivityModel> { Child("Initialize").Object });
            Assert.False(_rule.Get().Inspect(wf.Object, _rule.Get()).HasErrors);
        }

        [Fact]
        public void Pass_WhenAnnotationDoesNotContainPipeline()
        {
            var wf = Workflow("@unit", new List<IActivityModel> { Child("Initialize").Object });
            Assert.False(_rule.Get().Inspect(wf.Object, _rule.Get()).HasErrors);
        }

        [Fact]
        public void Pass_WhenRootIsNull()
        {
            var wf = new Mock<IWorkflowModel>();
            wf.Setup(w => w.Root).Returns((IActivityModel)null);
            Assert.False(_rule.Get().Inspect(wf.Object, _rule.Get()).HasErrors);
        }

        // --- Argument checks ---

        [Fact]
        public void Fail_WhenTransactionItemArgumentMissing()
        {
            var wf = Workflow("@pipeline", AllStageChildren(), new List<IArgumentModel>() as IReadOnlyCollection<IArgumentModel>);
            var result = _rule.Get().Inspect(wf.Object, _rule.Get());
            Assert.True(result.HasErrors);
            Assert.Contains(result.Messages, m => m.Contains("in_TransactionItem"));
        }

        [Fact]
        public void Fail_WhenTransactionItemHasWrongType()
        {
            var args = new List<IArgumentModel>
            {
                Arg("in_TransactionItem", ArgumentDirection.In, "System.String").Object
            } as IReadOnlyCollection<IArgumentModel>;
            var wf = Workflow("@pipeline", AllStageChildren(), args);
            var result = _rule.Get().Inspect(wf.Object, _rule.Get());
            Assert.True(result.HasErrors);
            Assert.Contains(result.Messages, m => m.Contains("in_TransactionItem"));
        }

        [Fact]
        public void Fail_WhenTransactionItemHasWrongDirection()
        {
            var args = new List<IArgumentModel>
            {
                Arg("in_TransactionItem", ArgumentDirection.Out, "UiPath.Core.QueueItem").Object
            } as IReadOnlyCollection<IArgumentModel>;
            var wf = Workflow("@pipeline", AllStageChildren(), args);
            var result = _rule.Get().Inspect(wf.Object, _rule.Get());
            Assert.True(result.HasErrors);
            Assert.Contains(result.Messages, m => m.Contains("in_TransactionItem"));
        }

        // --- Sequence checks ---

        [Fact]
        public void Pass_WhenAllStagesPresentInCorrectOrder()
        {
            var wf = Workflow("@pipeline", AllStageChildren());
            Assert.False(_rule.Get().Inspect(wf.Object, _rule.Get()).HasErrors);
        }

        [Fact]
        public void Pass_WhenNonPipelineChildrenInterspersed()
        {
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
            Assert.False(_rule.Get().Inspect(wf.Object, _rule.Get()).HasErrors);
        }

        [Fact]
        public void Fail_WhenStageIsMissing()
        {
            var children = new List<IActivityModel>
            {
                Child("Initialize").Object,
                Child("Ingest").Object,
                // Enrich missing
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
    }
}
