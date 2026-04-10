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
    public class UnitOutStatusRuleTests
    {
        private readonly UnitOutStatusRule _rule = new UnitOutStatusRule();

        private static Mock<IArgumentModel> Arg(string name, ArgumentDirection direction)
        {
            var m = new Mock<IArgumentModel>();
            m.Setup(a => a.DisplayName).Returns(name);
            m.Setup(a => a.Direction).Returns(direction);
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

        // --- Registration ---

        [Fact]
        public void Initialize_RegistersRule_WithCorrectId()
        {
            var api = new Mock<IAnalyzerConfigurationService>();
            api.Setup(a => a.HasFeature(DesignFeatureKeys.WorkflowAnalyzerV9)).Returns(true);
            _rule.Initialize(api.Object);
            api.Verify(s => s.AddRule(
                It.Is<Rule<IWorkflowModel>>(r =>
                    r.Id == "CPMF-U001" &&
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
        public void Pass_WhenAnnotationIsPipeline()
        {
            var wf = Workflow("@pipeline");
            Assert.False(_rule.Get().Inspect(wf.Object, _rule.Get()).HasErrors);
        }

        [Fact]
        public void Pass_WhenRootIsNull()
        {
            var wf = new Mock<IWorkflowModel>();
            wf.Setup(w => w.Root).Returns((IActivityModel)null);
            Assert.False(_rule.Get().Inspect(wf.Object, _rule.Get()).HasErrors);
        }

        // --- out_Status checks ---

        [Fact]
        public void Pass_WhenUnitHasOutStatus()
        {
            var args = new List<IArgumentModel>
            {
                Arg("out_Status", ArgumentDirection.Out).Object
            } as IReadOnlyCollection<IArgumentModel>;
            var wf = Workflow("@unit", args);
            Assert.False(_rule.Get().Inspect(wf.Object, _rule.Get()).HasErrors);
        }

        [Fact]
        public void Pass_WhenUnitHasOutStatusAmongOtherArgs()
        {
            var args = new List<IArgumentModel>
            {
                Arg("in_Input", ArgumentDirection.In).Object,
                Arg("out_Status", ArgumentDirection.Out).Object,
                Arg("out_Result", ArgumentDirection.Out).Object,
            } as IReadOnlyCollection<IArgumentModel>;
            var wf = Workflow("@unit", args);
            Assert.False(_rule.Get().Inspect(wf.Object, _rule.Get()).HasErrors);
        }

        [Fact]
        public void Fail_WhenUnitHasNoArguments()
        {
            var wf = Workflow("@unit", new List<IArgumentModel>() as IReadOnlyCollection<IArgumentModel>);
            var result = _rule.Get().Inspect(wf.Object, _rule.Get());
            Assert.True(result.HasErrors);
            Assert.Contains(result.Messages, m => m.Contains("out_Status"));
        }

        [Fact]
        public void Fail_WhenUnitHasOutStatusWithWrongDirection()
        {
            var args = new List<IArgumentModel>
            {
                Arg("out_Status", ArgumentDirection.In).Object
            } as IReadOnlyCollection<IArgumentModel>;
            var wf = Workflow("@unit", args);
            var result = _rule.Get().Inspect(wf.Object, _rule.Get());
            Assert.True(result.HasErrors);
            Assert.Contains(result.Messages, m => m.Contains("out_Status"));
        }

        [Fact]
        public void Fail_WhenUnitArgumentsIsNull()
        {
            var wf = Workflow("@unit", null);
            var result = _rule.Get().Inspect(wf.Object, _rule.Get());
            Assert.True(result.HasErrors);
        }
    }
}
