using System.Diagnostics;
using System.Linq;
using WatchfulAnvil.Sdk.Testing;
using Xunit;

namespace Cpmf.WorkflowAnalyzerRules.Tests
{
    /// <summary>
    /// Manifest test: RegisterAnalyzerConfiguration must register exactly the rules the
    /// rule classes in this assembly declare.
    ///
    /// The expected set is discovered by reflection rather than hardcoded, so adding a
    /// rule updates both sides at once and only forgetting to wire it up fails.
    ///
    /// The reflection and capture machinery now lives in WatchfulAnvil.Sdk.Testing so
    /// downstream packs get the same check; what stays here is the namespace this pack
    /// owns plus its own metadata expectations.
    /// </summary>
    public class RuleManifestTests
    {
        /// <summary>Rule classes live under this namespace; scoping the scan keeps referenced assemblies out.</summary>
        private const string RuleNamespace = "Cpmf.Rules";

        private static RegisterAnalyzerConfiguration Registration() => new RegisterAnalyzerConfiguration();

        [Fact]
        public void RegisteredRules_MatchesAllDeclaredRuleClasses()
        {
            var actual = RuleManifest.RegisteredIds(Registration());
            var expected = RuleManifest.DeclaredIds(Registration(), RuleNamespace);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void NoRuleClassIsLeftUnregistered()
        {
            // Same invariant stated as the failure it catches, so a break names the
            // offending rule instead of printing two long sorted lists.
            Assert.Empty(RuleManifest.Unregistered(Registration(), RuleNamespace));
        }

        [Fact]
        public void PipelineStructureRule_HasCorrectMetadata()
        {
            var rule = RuleManifest.Registered(Registration()).Single(r => r.Id == "CPMF-F002");
            Assert.Equal(TraceLevel.Error, rule.DefaultErrorLevel);
            Assert.Equal("Pipeline Structure", rule.Name);
        }

        [Fact]
        public void PipelinePresenceCounter_HasCorrectMetadata()
        {
            var rule = RuleManifest.Registered(Registration()).Single(r => r.Id == "CPMF-FC001");
            Assert.Equal(TraceLevel.Error, rule.DefaultErrorLevel);
            Assert.Equal("Pipeline Presence", rule.Name);
        }
    }
}
