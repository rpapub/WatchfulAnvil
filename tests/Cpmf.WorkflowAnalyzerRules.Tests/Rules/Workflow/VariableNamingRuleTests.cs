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
    public class VariableNamingRuleTests
    {
        private readonly VariableNamingRule _rule = new VariableNamingRule();

        private static Mock<IVariableModel> Variable(string name)
        {
            var m = new Mock<IVariableModel>();
            m.Setup(v => v.DisplayName).Returns(name);
            return m;
        }

        private static Mock<IActivityModel> Activity(params string[] variableNames)
        {
            var m = new Mock<IActivityModel>();
            var vars = new List<IVariableModel>();
            foreach (var name in variableNames)
                vars.Add(Variable(name).Object);
            m.Setup(a => a.Variables).Returns((IReadOnlyCollection<IVariableModel>)vars);
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
                    r.Id == "CPMF-WFL-005" &&
                    r.DefaultErrorLevel == TraceLevel.Warning)));
        }

        // --- Pass cases ---

        [Fact]
        public void Pass_WhenNoVariables()
        {
            var a = new Mock<IActivityModel>();
            a.Setup(x => x.Variables).Returns((IReadOnlyCollection<IVariableModel>)null);
            Assert.False(_rule.Get().Inspect(a.Object, _rule.Get()).HasErrors);
        }

        [Theory]
        [InlineData("transactionItem")]
        [InlineData("count")]
        [InlineData("isValid")]
        [InlineData("myDataTable")]
        [InlineData("queueItem")]
        [InlineData("result")]
        public void Pass_WhenNameIsCamelCase(string name)
        {
            var a = Activity(name);
            Assert.False(_rule.Get().Inspect(a.Object, _rule.Get()).HasErrors);
        }

        // --- Fail: underscore (Hungarian notation) ---

        [Theory]
        [InlineData("str_TransactionItem")]
        [InlineData("int_Count")]
        [InlineData("bool_IsValid")]
        [InlineData("dt_DataTable")]
        [InlineData("lst_Items")]
        [InlineData("arr_Results")]
        [InlineData("obj_Config")]
        [InlineData("dbl_Amount")]
        [InlineData("in_Something")]
        [InlineData("out_Status")]
        public void Fail_WhenNameContainsUnderscore(string name)
        {
            var a = Activity(name);
            var result = _rule.Get().Inspect(a.Object, _rule.Get());
            Assert.True(result.HasErrors);
            Assert.Contains(result.Messages, m => m.Contains(name) && m.Contains("underscore"));
        }

        // --- Fail: starts uppercase (PascalCase) ---

        [Theory]
        [InlineData("TransactionItem")]
        [InlineData("Count")]
        [InlineData("MyVariable")]
        public void Fail_WhenNameStartsWithUppercase(string name)
        {
            var a = Activity(name);
            var result = _rule.Get().Inspect(a.Object, _rule.Get());
            Assert.True(result.HasErrors);
            Assert.Contains(result.Messages, m => m.Contains(name) && m.Contains("uppercase"));
        }

        // --- Multiple violations ---

        [Fact]
        public void Fail_ReportsAllViolatingVariables()
        {
            var a = Activity("str_Foo", "goodName", "BadName", "int_Count");
            var result = _rule.Get().Inspect(a.Object, _rule.Get());
            Assert.True(result.HasErrors);
            Assert.Equal(3, result.Messages.Count);
        }
    }
}
