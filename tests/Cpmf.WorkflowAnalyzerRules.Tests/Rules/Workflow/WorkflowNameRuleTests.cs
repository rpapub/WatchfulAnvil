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
    public class WorkflowNameRuleTests
    {
        private readonly WorkflowNameRule _rule = new WorkflowNameRule();

        private static Mock<IWorkflowModel> Workflow(string relativePath)
        {
            var m = new Mock<IWorkflowModel>();
            m.Setup(w => w.RelativePath).Returns(relativePath);
            return m;
        }

        [Fact]
        public void Initialize_RegistersRule_WithCorrectId()
        {
            var api = new Mock<IAnalyzerConfigurationService>();
            _rule.Initialize(api.Object);
            api.Verify(s => s.AddRule(
                It.Is<Rule<IWorkflowModel>>(r =>
                    r.Id == "CPMF-N002" &&
                    r.DefaultErrorLevel == TraceLevel.Error)));
        }

        [Theory]
        [InlineData("PopulateCase.xaml")]
        [InlineData("Process\\Crm\\Getter\\PopulateCase.xaml")]
        [InlineData("Main.xaml")]
        public void Pass_WhenFilenameIsValidPascalCase(string path)
        {
            Assert.False(_rule.Get().Inspect(Workflow(path).Object, _rule.Get()).HasErrors);
        }

        [Fact]
        public void Pass_WhenRelativePathIsEmpty()
        {
            Assert.False(_rule.Get().Inspect(Workflow("").Object, _rule.Get()).HasErrors);
        }

        [Theory]
        [InlineData("041_Handle_Registration_Postcard_eSAV.xaml")]
        [InlineData("Process\\Crm\\2ndStep.xaml")]
        public void Fail_WhenFilenameStartsWithDigit(string path)
        {
            var result = _rule.Get().Inspect(Workflow(path).Object, _rule.Get());
            Assert.True(result.HasErrors);
            Assert.Contains(result.Messages, m => m.Contains("digit"));
        }

        [Theory]
        [InlineData("My Process.xaml")]
        [InlineData("Get-Config.xaml")]
        public void Fail_WhenFilenameContainsInvalidCharacter(string path)
        {
            var result = _rule.Get().Inspect(Workflow(path).Object, _rule.Get());
            Assert.True(result.HasErrors);
        }

        [Theory]
        [InlineData("populateCase.xaml")]
        [InlineData("getConfig.xaml")]
        public void Fail_WhenFilenameStartsWithLowercase(string path)
        {
            var result = _rule.Get().Inspect(Workflow(path).Object, _rule.Get());
            Assert.True(result.HasErrors);
            Assert.Contains(result.Messages, m => m.Contains("lowercase"));
        }

        [Fact]
        public void Fail_MessageIncludesRelativePath()
        {
            var path = "Process\\041_Init.xaml";
            var result = _rule.Get().Inspect(Workflow(path).Object, _rule.Get());
            Assert.True(result.HasErrors);
            Assert.Contains(result.Messages, m => m.Contains(path));
        }
    }
}
