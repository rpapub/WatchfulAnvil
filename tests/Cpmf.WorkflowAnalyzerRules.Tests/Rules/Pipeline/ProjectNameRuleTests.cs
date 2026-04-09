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
    public class ProjectNameRuleTests
    {
        private readonly ProjectNameRule _rule = new ProjectNameRule();

        private static Mock<IProjectModel> Project(string displayName)
        {
            var m = new Mock<IProjectModel>();
            m.Setup(p => p.DisplayName).Returns(displayName);
            return m;
        }

        [Fact]
        public void Initialize_RegistersRule_WithCorrectId()
        {
            var api = new Mock<IAnalyzerConfigurationService>();
            _rule.Initialize(api.Object);
            api.Verify(s => s.AddRule(
                It.Is<Rule<IProjectModel>>(r =>
                    r.Id == "CPMF-PLN-003" &&
                    r.DefaultErrorLevel == TraceLevel.Error)));
        }

        [Theory]
        [InlineData("MyProject")]
        [InlineData("HandleRegistration")]
        public void Pass_WhenProjectNameIsValidPascalCase(string name)
        {
            Assert.False(_rule.Get().Inspect(Project(name).Object, _rule.Get()).HasErrors);
        }

        [Theory]
        [InlineData("041_Handle_Registration")]
        [InlineData("1MyProject")]
        public void Fail_WhenProjectNameStartsWithDigit(string name)
        {
            var result = _rule.Get().Inspect(Project(name).Object, _rule.Get());
            Assert.True(result.HasErrors);
            Assert.Contains(result.Messages, m => m.Contains("digit"));
        }

        [Theory]
        [InlineData("My Project")]
        [InlineData("My-Project")]
        public void Fail_WhenProjectNameContainsInvalidCharacter(string name)
        {
            var result = _rule.Get().Inspect(Project(name).Object, _rule.Get());
            Assert.True(result.HasErrors);
        }

        [Fact]
        public void Fail_WhenProjectNameStartsWithLowercase()
        {
            var result = _rule.Get().Inspect(Project("myProject").Object, _rule.Get());
            Assert.True(result.HasErrors);
            Assert.Contains(result.Messages, m => m.Contains("lowercase"));
        }
    }
}
