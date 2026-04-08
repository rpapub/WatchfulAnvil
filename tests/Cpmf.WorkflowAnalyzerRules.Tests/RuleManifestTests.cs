using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Cpmf.WorkflowAnalyzerRules.Tests.Fakes;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using Xunit;

namespace Cpmf.WorkflowAnalyzerRules.Tests
{
    /// <summary>
    /// Manifest test: verifies that RegisterAnalyzerConfiguration registers exactly
    /// the rules declared by the individual rule classes in the assembly.
    ///
    /// No hardcoded ID list — expected set is discovered via reflection:
    /// every non-abstract IRegisterAnalyzerConfiguration in the rules assembly
    /// (except RegisterAnalyzerConfiguration itself) is initialized through the
    /// fake and contributes its rule(s) to the expected set.
    ///
    /// Adding a new rule class automatically updates both sides. Forgetting to
    /// wire it up in RegisterAnalyzerConfiguration breaks this test.
    /// </summary>
    public class RuleManifestTests
    {
        /// <summary>Rules registered by the central RegisterAnalyzerConfiguration.</summary>
        private static List<Rule> CaptureActual()
        {
            var rules = new List<Rule>();
            new RegisterAnalyzerConfiguration().Initialize(new FakeAnalyzerConfigurationService(rules));
            return rules;
        }

        /// <summary>
        /// Rules discovered by scanning the rules assembly for every individual
        /// IRegisterAnalyzerConfiguration implementor and initializing each one.
        /// </summary>
        private static List<Rule> CaptureExpected()
        {
            var rules = new List<Rule>();
            var fake = new FakeAnalyzerConfigurationService(rules);

            var rulesAssembly = typeof(RegisterAnalyzerConfiguration).Assembly;
            var individualTypes = rulesAssembly.GetTypes()
                .Where(t =>
                    !t.IsAbstract &&
                    typeof(IRegisterAnalyzerConfiguration).IsAssignableFrom(t) &&
                    t != typeof(RegisterAnalyzerConfiguration));

            foreach (var type in individualTypes)
            {
                var instance = (IRegisterAnalyzerConfiguration)System.Activator.CreateInstance(type);
                instance.Initialize(fake);
            }

            return rules;
        }

        [Fact]
        public void RegisteredRules_MatchesAllDeclaredRuleClasses()
        {
            var actual = CaptureActual().Select(r => r.Id).OrderBy(x => x).ToList();
            var expected = CaptureExpected().Select(r => r.Id).OrderBy(x => x).ToList();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void PipelineStructureRule_HasCorrectMetadata()
        {
            var rule = CaptureActual().Single(r => r.Id == "CPMF-PLN-001");
            Assert.Equal(TraceLevel.Error, rule.DefaultErrorLevel);
            Assert.Equal("Pipeline Structure", rule.Name);
        }

        [Fact]
        public void PipelinePresenceCounter_HasCorrectMetadata()
        {
            var rule = CaptureActual().Single(r => r.Id == "CPMF-PLN-C001");
            Assert.Equal(TraceLevel.Error, rule.DefaultErrorLevel);
            Assert.Equal("Pipeline Presence", rule.Name);
        }
    }
}
