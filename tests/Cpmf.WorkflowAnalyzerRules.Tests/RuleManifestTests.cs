using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Cpmf.WorkflowAnalyzerRules.Tests.Fakes;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using Xunit;

namespace Cpmf.WorkflowAnalyzerRules.Tests
{
    /// <summary>
    /// Manifest test: verifies every rule registered by RegisterAnalyzerConfiguration.
    /// Uses FakeAnalyzerConfigurationService — no Moq, no Studio deployment needed.
    /// Break this test intentionally if you add or remove a rule from the library.
    /// </summary>
    public class RuleManifestTests
    {
        private static List<Rule> Capture()
        {
            var rules = new List<Rule>();
            var fake = new FakeAnalyzerConfigurationService(rules);
            new RegisterAnalyzerConfiguration().Initialize(fake);
            return rules;
        }

        [Fact]
        public void RegisteredRules_ContainsExactExpectedIds()
        {
            var ids = Capture().Select(r => r.Id).OrderBy(x => x).ToList();

            Assert.Equal(
                new[] { "CPMF-PLN-001", "CPMF-PLN-002", "CPMF-PLN-C001", "CPMF-WFL-001" }.OrderBy(x => x).ToList(),
                ids);
        }

        [Fact]
        public void PipelineStructureRule_HasCorrectMetadata()
        {
            var rule = Capture().Single(r => r.Id == "CPMF-PLN-001");
            Assert.Equal(TraceLevel.Error, rule.DefaultErrorLevel);
            Assert.Equal("Pipeline Structure", rule.Name);
        }

        [Fact]
        public void PipelinePresenceCounter_HasCorrectMetadata()
        {
            var rule = Capture().Single(r => r.Id == "CPMF-PLN-C001");
            Assert.Equal(TraceLevel.Error, rule.DefaultErrorLevel);
            Assert.Equal("Pipeline Presence", rule.Name);
        }
    }
}
