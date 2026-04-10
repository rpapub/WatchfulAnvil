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
    public class NoFlowchartStateMachineRuleTests
    {
        private readonly NoFlowchartStateMachineRule _rule = new NoFlowchartStateMachineRule();

        private static Mock<IActivityModel> Activity(string type, string toolboxName, string displayName = "Activity")
        {
            var m = new Mock<IActivityModel>();
            m.Setup(a => a.Type).Returns(type);
            m.Setup(a => a.ToolboxName).Returns(toolboxName);
            m.Setup(a => a.DisplayName).Returns(displayName);
            return m;
        }

        // --- Registration ---

        [Fact]
        public void Initialize_RegistersRule_WithCorrectId()
        {
            var api = new Mock<IAnalyzerConfigurationService>();
            api.Setup(a => a.HasFeature(DesignFeatureKeys.WorkflowAnalyzerV9)).Returns(true);
            _rule.Initialize(api.Object);
            api.Verify(s => s.AddRule(
                It.Is<Rule<IActivityModel>>(r =>
                    r.Id == "CPMF-U003" &&
                    r.DefaultErrorLevel == TraceLevel.Error)));
        }

        // --- Pass cases ---

        [Fact]
        public void Pass_WhenActivityIsSequence()
        {
            var a = Activity("System.Activities.Statements.Sequence", "Sequence");
            Assert.False(_rule.Get().Inspect(a.Object, _rule.Get()).HasErrors);
        }

        [Fact]
        public void Pass_WhenActivityIsAssign()
        {
            var a = Activity("System.Activities.Statements.Assign", "Assign");
            Assert.False(_rule.Get().Inspect(a.Object, _rule.Get()).HasErrors);
        }

        // --- Flowchart ---

        [Fact]
        public void Fail_WhenTypeIsFlowchartSimpleName()
        {
            var a = Activity("Flowchart", "Flowchart", "My Flowchart");
            var result = _rule.Get().Inspect(a.Object, _rule.Get());
            Assert.True(result.HasErrors);
            Assert.Contains(result.Messages, m => m.Contains("Flowchart"));
        }

        [Fact]
        public void Fail_WhenTypeIsFlowchartFullName()
        {
            var a = Activity("System.Activities.Statements.Flowchart", "Flowchart", "My Flowchart");
            var result = _rule.Get().Inspect(a.Object, _rule.Get());
            Assert.True(result.HasErrors);
        }

        [Fact]
        public void Fail_WhenToolboxNameIsFlowchart()
        {
            var a = Activity("", "Flowchart", "My Flowchart");
            var result = _rule.Get().Inspect(a.Object, _rule.Get());
            Assert.True(result.HasErrors);
        }

        // --- State Machine ---

        [Fact]
        public void Fail_WhenTypeIsStateMachineSimpleName()
        {
            var a = Activity("StateMachine", "State Machine", "My SM");
            var result = _rule.Get().Inspect(a.Object, _rule.Get());
            Assert.True(result.HasErrors);
            Assert.Contains(result.Messages, m => m.Contains("State Machine"));
        }

        [Fact]
        public void Fail_WhenTypeIsStateMachineFullName()
        {
            var a = Activity("System.Activities.Statements.StateMachine", "State Machine", "My SM");
            var result = _rule.Get().Inspect(a.Object, _rule.Get());
            Assert.True(result.HasErrors);
        }

        [Fact]
        public void Fail_WhenToolboxNameIsStateMachine()
        {
            var a = Activity("", "State Machine", "My SM");
            var result = _rule.Get().Inspect(a.Object, _rule.Get());
            Assert.True(result.HasErrors);
        }
    }
}
