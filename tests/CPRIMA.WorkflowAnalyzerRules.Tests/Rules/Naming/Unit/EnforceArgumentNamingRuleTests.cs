using System.Diagnostics;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using Moq;
using Xunit;
using CPRIMA.WorkflowAnalyzerRules.Rules.Naming;

namespace CPRIMA.WorkflowAnalyzerRules.Tests.Rules.Naming.Unit
{
    public class EnforceArgumentNamingRuleTests
    {
        [Fact]
        public void Initialize_RegistersRuleWithCorrectId()
        {
            // Arrange
            var configService = new Mock<IAnalyzerConfigurationService>();
            var rule = new EnforceXamlArgumentNamingRule();

            // Act
            rule.Initialize(configService.Object);

            // Verify
            configService.Verify(s => s.AddRule(
                It.Is<Rule<IWorkflowModel>>(r => 
                    r.Id == "CPRIMA-NMG-001" && 
                    r.DefaultErrorLevel == TraceLevel.Warning
                )
            ));
        }
    }
}
