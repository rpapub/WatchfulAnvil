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
    public class LogMessageBookendsRuleTests
    {
        private readonly LogMessageBookendsRule _rule = new LogMessageBookendsRule();

        private static Mock<IActivityModel> LogMessage(string message)
        {
            var msgProp = new Mock<IPropertyModel>();
            msgProp.Setup(p => p.DisplayName).Returns("Message");
            msgProp.Setup(p => p.DefinedExpression).Returns($"\"{message}\"");

            var m = new Mock<IActivityModel>();
            m.Setup(a => a.ToolboxName).Returns("Log Message");
            m.Setup(a => a.DisplayName).Returns($"Log Message");
            m.Setup(a => a.Properties).Returns(
                new List<IPropertyModel> { msgProp.Object } as IReadOnlyCollection<IPropertyModel>);
            return m;
        }

        private static Mock<IActivityModel> OtherActivity(string displayName = "Do Something")
        {
            var m = new Mock<IActivityModel>();
            m.Setup(a => a.ToolboxName).Returns("Assign");
            m.Setup(a => a.DisplayName).Returns(displayName);
            m.Setup(a => a.Properties).Returns(
                new List<IPropertyModel>() as IReadOnlyCollection<IPropertyModel>);
            return m;
        }

        private static Mock<IWorkflowModel> Workflow(
            string annotation,
            IReadOnlyCollection<IActivityModel> children)
        {
            var root = new Mock<IActivityModel>();
            root.Setup(r => r.AnnotationText).Returns(annotation);
            root.Setup(r => r.Children).Returns(children);

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
                    r.Id == "CPMF-WFL-001" &&
                    r.DefaultErrorLevel == TraceLevel.Warning)));
        }

        // --- Annotation gate ---

        [Fact]
        public void Pass_WhenNoAnnotation()
        {
            var wf = Workflow("", new List<IActivityModel>() as IReadOnlyCollection<IActivityModel>);
            Assert.False(_rule.Get().Inspect(wf.Object, _rule.Get()).HasErrors);
        }

        [Fact]
        public void Pass_WhenAnnotationIsPipeline()
        {
            var children = new List<IActivityModel>
            {
                OtherActivity().Object,
                OtherActivity().Object
            } as IReadOnlyCollection<IActivityModel>;
            var wf = Workflow("@pipeline", children);
            Assert.False(_rule.Get().Inspect(wf.Object, _rule.Get()).HasErrors);
        }

        [Fact]
        public void Pass_WhenRootIsNull()
        {
            var wf = new Mock<IWorkflowModel>();
            wf.Setup(w => w.Root).Returns((IActivityModel)null);
            Assert.False(_rule.Get().Inspect(wf.Object, _rule.Get()).HasErrors);
        }

        // --- @module and @unit ---

        [Fact]
        public void Pass_WhenModuleHasCorrectBookends()
        {
            var children = new List<IActivityModel>
            {
                LogMessage("Going to process the item").Object,
                OtherActivity().Object,
                LogMessage("Finished processing the item").Object,
            } as IReadOnlyCollection<IActivityModel>;
            var wf = Workflow("@module", children);
            Assert.False(_rule.Get().Inspect(wf.Object, _rule.Get()).HasErrors);
        }

        [Fact]
        public void Pass_WhenUnitHasCorrectBookends()
        {
            var children = new List<IActivityModel>
            {
                LogMessage("Going to validate input").Object,
                OtherActivity().Object,
                LogMessage("Finished validating input").Object,
            } as IReadOnlyCollection<IActivityModel>;
            var wf = Workflow("@unit", children);
            Assert.False(_rule.Get().Inspect(wf.Object, _rule.Get()).HasErrors);
        }

        [Fact]
        public void Fail_WhenFirstActivityIsNotLogMessage()
        {
            var children = new List<IActivityModel>
            {
                OtherActivity("Do Something").Object,
                LogMessage("Finished").Object,
            } as IReadOnlyCollection<IActivityModel>;
            var wf = Workflow("@module", children);
            var result = _rule.Get().Inspect(wf.Object, _rule.Get());
            Assert.True(result.HasErrors);
            Assert.Contains(result.Messages, m => m.Contains("first"));
        }

        [Fact]
        public void Fail_WhenLastActivityIsNotLogMessage()
        {
            var children = new List<IActivityModel>
            {
                LogMessage("Going to run").Object,
                OtherActivity("Do Something").Object,
            } as IReadOnlyCollection<IActivityModel>;
            var wf = Workflow("@unit", children);
            var result = _rule.Get().Inspect(wf.Object, _rule.Get());
            Assert.True(result.HasErrors);
            Assert.Contains(result.Messages, m => m.Contains("last"));
        }

        [Fact]
        public void Fail_WhenFirstLogMessageHasWrongPrefix()
        {
            var children = new List<IActivityModel>
            {
                LogMessage("Starting the process").Object,
                OtherActivity().Object,
                LogMessage("Finished the process").Object,
            } as IReadOnlyCollection<IActivityModel>;
            var wf = Workflow("@module", children);
            var result = _rule.Get().Inspect(wf.Object, _rule.Get());
            Assert.True(result.HasErrors);
            Assert.Contains(result.Messages, m => m.Contains("Going to"));
        }

        [Fact]
        public void Fail_WhenLastLogMessageHasWrongPrefix()
        {
            var children = new List<IActivityModel>
            {
                LogMessage("Going to run").Object,
                OtherActivity().Object,
                LogMessage("Done with everything").Object,
            } as IReadOnlyCollection<IActivityModel>;
            var wf = Workflow("@unit", children);
            var result = _rule.Get().Inspect(wf.Object, _rule.Get());
            Assert.True(result.HasErrors);
            Assert.Contains(result.Messages, m => m.Contains("Finished"));
        }
    }
}
