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
    public class SpecificContentAssignRuleTests
    {
        private readonly SpecificContentAssignRule _rule = new SpecificContentAssignRule();

        private static Mock<IPropertyModel> Property(string displayName, string expression)
        {
            var m = new Mock<IPropertyModel>();
            m.Setup(p => p.DisplayName).Returns(displayName);
            m.Setup(p => p.DefinedExpression).Returns(expression);
            return m;
        }

        private static Mock<IActivityModel> AssignActivity(
            string toExpression,
            string toolboxName = "Assign",
            string type = "System.Activities.Statements.Assign",
            string displayName = "Assign")
        {
            var m = new Mock<IActivityModel>();
            m.Setup(a => a.ToolboxName).Returns(toolboxName);
            m.Setup(a => a.Type).Returns(type);
            m.Setup(a => a.DisplayName).Returns(displayName);
            m.Setup(a => a.Properties).Returns(new List<IPropertyModel>
            {
                Property("To", toExpression).Object,
                Property("Value", "[someValue]").Object
            });
            return m;
        }

        private static Mock<IActivityModel> NonAssignActivity(string toolboxName, string type = "")
        {
            var m = new Mock<IActivityModel>();
            m.Setup(a => a.ToolboxName).Returns(toolboxName);
            m.Setup(a => a.Type).Returns(type);
            m.Setup(a => a.DisplayName).Returns(toolboxName);
            m.Setup(a => a.Properties).Returns((IReadOnlyCollection<IPropertyModel>)null);
            return m;
        }

        // --- Registration ---

        [Fact]
        public void Initialize_RegistersRule_WithCorrectId()
        {
            var api = new Mock<IAnalyzerConfigurationService>();
            _rule.Initialize(api.Object);
            api.Verify(s => s.AddRule(
                It.Is<Rule<IActivityModel>>(r =>
                    r.Id == "CPMF-F001" &&
                    r.DefaultErrorLevel == TraceLevel.Error)));
        }

        // --- Pass cases ---

        [Fact]
        public void Pass_WhenAssignToRegularVariable()
        {
            var a = AssignActivity("[myVariable]");
            Assert.False(_rule.Get().Inspect(a.Object, _rule.Get()).HasErrors);
        }

        [Fact]
        public void Pass_WhenNotAnAssignActivity()
        {
            var a = NonAssignActivity("Log Message");
            Assert.False(_rule.Get().Inspect(a.Object, _rule.Get()).HasErrors);
        }

        [Fact]
        public void Pass_WhenMultipleAssignActivity()
        {
            var a = NonAssignActivity("Multiple Assign", "UiPath.Core.Activities.MultipleAssign");
            Assert.False(_rule.Get().Inspect(a.Object, _rule.Get()).HasErrors);
        }

        [Fact]
        public void Pass_WhenNoProperties()
        {
            var m = new Mock<IActivityModel>();
            m.Setup(a => a.ToolboxName).Returns("Assign");
            m.Setup(a => a.Type).Returns("System.Activities.Statements.Assign");
            m.Setup(a => a.DisplayName).Returns("Assign");
            m.Setup(a => a.Properties).Returns((IReadOnlyCollection<IPropertyModel>)null);
            Assert.False(_rule.Get().Inspect(m.Object, _rule.Get()).HasErrors);
        }

        // --- Fail cases ---

        [Fact]
        public void Fail_WhenAssignToSpecificContentByToolboxName()
        {
            var a = AssignActivity("[io_TransactionItem.SpecificContent(\"Crm.Case.CaseId\")]");
            var result = _rule.Get().Inspect(a.Object, _rule.Get());
            Assert.True(result.HasErrors);
            Assert.Contains(result.Messages, m => m.Contains("SpecificContent"));
        }

        [Fact]
        public void Fail_WhenAssignToSpecificContentByFullTypeName()
        {
            var a = AssignActivity(
                "[io_TransactionItem.SpecificContent(\"Key\")]",
                toolboxName: null,
                type: "System.Activities.Statements.Assign");
            var result = _rule.Get().Inspect(a.Object, _rule.Get());
            Assert.True(result.HasErrors);
        }

        [Fact]
        public void Fail_WhenAssignToSpecificContentByGenericTypeName()
        {
            var a = AssignActivity(
                "[transactionItem.SpecificContent(\"Key\")]",
                toolboxName: null,
                type: "System.Activities.Statements.Assign`1[System.Object]");
            var result = _rule.Get().Inspect(a.Object, _rule.Get());
            Assert.True(result.HasErrors);
        }

        [Fact]
        public void Fail_MessageIncludesActivityDisplayName()
        {
            var a = AssignActivity(
                "[io_TransactionItem.SpecificContent(\"Data.Value\")]",
                displayName: "Set SpecificContent Data.Value");
            var result = _rule.Get().Inspect(a.Object, _rule.Get());
            Assert.True(result.HasErrors);
            Assert.Contains(result.Messages, m => m.Contains("Set SpecificContent Data.Value"));
        }
    }
}
