using System;
using System.IO;
using System.Collections.Generic;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using Moq;
using Xunit;
using CPRIMA.WorkflowAnalyzerRules.Rules.Naming;
using CPRIMA.WorkflowAnalyzerRules.Common;


namespace CPRIMA.WorkflowAnalyzerRules.Tests.Rules.Naming.Unit
{
    public class ArgumentPatternTests
    {
        [Theory]
        [InlineData("in_FileName", true)]
        [InlineData("out_Result", true)]
        [InlineData("io_Data", true)]
        [InlineData("input_File", false)]
        [InlineData("In_Data", false)]
        [InlineData("in_", false)]
        [InlineData("_in_data", false)]
        [InlineData("in_snake_case", false)]
        [InlineData("in_kebab-case", false)]
        [InlineData("in_camelCase", false)]
        public void ValidateArgumentNamingPatterns(string argumentName, bool shouldBeValid)
        {
            // Arrange
            var rule = new EnforceXamlArgumentNamingRule();
            var ruleInstance = rule.Get();
            
            // Determine expected type from name prefix
            string type = "InArgument";
            if (argumentName.StartsWith("out_")) type = "OutArgument";
            else if (argumentName.StartsWith("io_")) type = "InOutArgument";
            
            // Create test XAML file
            var testDir = Path.Combine(AppContext.BaseDirectory, "TestData", "Workflows");
            Directory.CreateDirectory(testDir);
            var xamlPath = Path.Combine(testDir, "test.xaml");
            
            string xamlTemplate = "<Activity xmlns='http://schemas.microsoft.com/netfx/2009/xaml/activities' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'><x:Members><x:Property Name='{0}' Type='{1}' /></x:Members></Activity>";
            var xaml = string.Format(xamlTemplate, argumentName, type);
            
            File.WriteAllText(xamlPath, xaml);
            
            // Create mock workflow
            var workflow = new Mock<IWorkflowModel>();
            workflow.Setup(w => w.RelativePath).Returns(xamlPath);
            
            // Act
            var result = ruleInstance.Inspect(workflow.Object, ruleInstance);
            
            // Assert
            Assert.Equal(shouldBeValid, !result.HasErrors);
            
            // Cleanup
            File.Delete(xamlPath);
        }
    }
}
