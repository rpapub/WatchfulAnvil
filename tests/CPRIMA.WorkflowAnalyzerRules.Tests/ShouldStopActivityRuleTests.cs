using System.Collections.Generic;
using Moq;
using Xunit;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using CPRIMA.WorkflowAnalyzerRules.Rules.Usage;
using UiPath.Studio.Activities.Api.Analyzer;

namespace CPRIMA.WorkflowAnalyzerRules.Tests
{
    public class ShouldStopActivityRuleTests
    {
        private readonly ShouldStopActivityRule _rule;

        public ShouldStopActivityRuleTests()
        {
            _rule = new ShouldStopActivityRule();
        }

        [Fact]
        public void Rule_Should_Register()
        {
            // Arrange
            var mockConfigService = new Mock<IAnalyzerConfigurationService>();

            // Act
            _rule.Initialize(mockConfigService.Object);

            // Assert
            mockConfigService.Verify(s => s.AddRule<IProjectModel>(It.IsAny<Rule<IProjectModel>>()), Times.Once);
        }

        [Fact]
        public void Rule_Should_Pass_When_ShouldStop_Activity_Exists()
        {
            // Arrange
            var mockProject = new Mock<IProjectModel>();
            var mockWorkflow = new Mock<IWorkflowModel>();
            var mockActivity = new Mock<IActivityModel>();

            mockActivity.Setup(a => a.Type).Returns("UiPath.Core.Activities.ShouldStop");
            mockActivity.Setup(a => a.ToolboxName).Returns("ShouldStop");
            mockWorkflow.Setup(w => w.Root).Returns(mockActivity.Object);
            mockProject.Setup(p => p.Workflows).Returns(new List<IWorkflowModel> { mockWorkflow.Object });

            var rule = _rule.Get();

            // Act
            var result = rule.Inspect(mockProject.Object, rule);

            // Assert
            Assert.False(result.HasErrors); // The project should pass the rule
        }

        [Fact]
        public void Rule_Should_Fail_When_ShouldStop_Activity_Is_Missing()
        {
            // Arrange
            var mockProject = new Mock<IProjectModel>();
            var mockWorkflow = new Mock<IWorkflowModel>();
            var mockActivity = new Mock<IActivityModel>();

            mockActivity.Setup(a => a.Type).Returns("Some.Other.Activity");
            mockActivity.Setup(a => a.ToolboxName).Returns("OtherActivity");
            mockWorkflow.Setup(w => w.Root).Returns(mockActivity.Object);
            mockProject.Setup(p => p.Workflows).Returns(new List<IWorkflowModel> { mockWorkflow.Object });

            var rule = _rule.Get();

            // Act
            var result = rule.Inspect(mockProject.Object, rule);

            // Assert
            Assert.True(result.HasErrors); // The project should fail the rule
            Assert.Contains("ShouldStop", result.RecommendationMessage); // Verify correct message
        }
    }
}
