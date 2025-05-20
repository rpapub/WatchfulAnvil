using System.Diagnostics;
using System.IO;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using Moq;
using Xunit;
using CPRIMA.WorkflowAnalyzerRules.Rules.Naming;

namespace CPRIMA.WorkflowAnalyzerRules.Tests.Rules.Naming.Integration
{
    public class WorkflowArgumentNamingTests
    {
        private readonly string _testDataPath = Path.Combine("TestData", "Workflows");
        private readonly EnforceXamlArgumentNamingRule _rule;

        public WorkflowArgumentNamingTests()
        {
            _rule = new EnforceXamlArgumentNamingRule();
        }

        private IWorkflowModel CreateWorkflowMock(string xamlFileName)
        {
            var workflow = new Mock<IWorkflowModel>();
            workflow.Setup(w => w.RelativePath)
                   .Returns(Path.Combine(_testDataPath, xamlFileName));
            return workflow.Object;
        }

        [Fact]
        public void ValidWorkflow_HasNoErrors()
        {
            // Arrange
            var workflow = CreateWorkflowMock("ValidArguments.xaml");

            // Act
            var result = _rule.Get().Inspect(workflow, _rule.Get());

            // Assert
            Assert.False(result.HasErrors);
            Assert.Equal(TraceLevel.Warning, result.ErrorLevel);
        }

        [Fact]
        public void InvalidWorkflow_DetectsAllErrors()
        {
            // Arrange
            var workflow = CreateWorkflowMock("InvalidArguments.xaml");

            // Act
            var result = _rule.Get().Inspect(workflow, _rule.Get());

            // Assert
            Assert.True(result.HasErrors);
            Assert.Equal(TraceLevel.Warning, result.ErrorLevel);
            Assert.NotNull(result.RecommendationMessage);
        }

        [Fact]
        public void IgnoredWorkflow_OnlyReportsNonIgnoredErrors()
        {
            // Arrange
            var workflow = CreateWorkflowMock("IgnoredArguments.xaml");

            // Act
            var result = _rule.Get().Inspect(workflow, _rule.Get());

            // Assert
            Assert.False(result.HasErrors); // All arguments are ignored with correct rule ID
            Assert.Equal(TraceLevel.Warning, result.ErrorLevel);
        }
    }
}
