using Xunit;
using Moq;
using System.Diagnostics;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using CPRIMA.WorkflowAnalyzerRules.Rules.Noop;

namespace CPRIMA.WorkflowAnalyzerRules.Tests
{
    public class NullOperationRuleTests
    {
        private readonly Mock<IAnalyzerConfigurationService> _mockConfigService;

        public NullOperationRuleTests()
        {
            _mockConfigService = new Mock<IAnalyzerConfigurationService>();
        }

        [Fact]
        public void Rule_Should_Register()
        {
            // Arrange
            var rule = new NullOperationActivityRule();  // Use any of your rules

            // Act
            rule.Initialize(_mockConfigService.Object);

            // Assert
            _mockConfigService.Verify(s => s.AddRule<IActivityModel>(It.IsAny<Rule<IActivityModel>>()), Times.Once);
        }

        [Fact]
        public void Rule_Should_Return_No_Errors()
        {
            // Arrange
            var mockActivity = new Mock<IActivityModel>();
            var configuredRule = new Rule<IActivityModel>("Test Rule", "ST-CPM-003", (a, r) => new InspectionResult { HasErrors = false })
            {
                DefaultErrorLevel = TraceLevel.Info
            };

            // Act
            var result = configuredRule.Inspect(mockActivity.Object, configuredRule);

            // Assert
            Assert.False(result.HasErrors);
        }
    }
}
