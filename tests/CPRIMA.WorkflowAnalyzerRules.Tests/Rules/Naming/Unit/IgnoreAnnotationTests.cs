using System.Diagnostics;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using Moq;
using Xunit;
using CPRIMA.WorkflowAnalyzerRules.Rules.Naming;

namespace CPRIMA.WorkflowAnalyzerRules.Tests.Rules.Naming.Unit
{
    public class IgnoreAnnotationTests
    {
        [Theory]
        [InlineData("@ignore CPRIMA-NMG-001", true)]
        [InlineData("@ignore cprima-nmg-001", true)]  // Case insensitive
        [InlineData("@ignore", false)]
        [InlineData("@ignore DIFFERENT-RULE", false)]
        [InlineData("", false)]  // Empty string instead of null
        public void ValidateIgnoreAnnotations(string annotation, bool shouldBeIgnored)
        {
            // Act
            var isIgnored = EnforceXamlArgumentNamingRule.IsIgnored(annotation);

            // Assert
            Assert.Equal(shouldBeIgnored, isIgnored);
        }
    }
}
