using System.Diagnostics;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using Moq;
using Xunit;
using CPRIMA.WorkflowAnalyzerRules.Rules.Naming;

namespace CPRIMA.WorkflowAnalyzerRules.Tests.Rules.Naming.Unit
{
    public class ArgumentPatternTests
    {
        [Theory]
        [InlineData("in_fileName", true)]
        [InlineData("out_result", true)]
        [InlineData("io_data", true)]
        [InlineData("input_file", false)]
        [InlineData("In_data", false)]
        [InlineData("in_", false)]
        [InlineData("_in_data", false)]
        public void ValidateArgumentNamingPatterns(string argumentName, bool shouldBeValid)
        {
            // Arrange
            var configService = new Mock<IAnalyzerConfigurationService>();
            var rule = new EnforceXamlArgumentNamingRule();
            var workflow = new Mock<IWorkflowModel>();
            
            // Use test XAML file
            var xamlPath = Path.Combine(AppContext.BaseDirectory, "TestData", "Workflows", argumentName + ".xaml");
            workflow.Setup(w => w.RelativePath).Returns(xamlPath);
            
            // Act
            var ruleInstance = rule.Get();
            var result = ruleInstance.Inspect(workflow.Object, ruleInstance);

            // Assert
            Assert.Equal(shouldBeValid, !result.HasErrors);
        }
    }
}
