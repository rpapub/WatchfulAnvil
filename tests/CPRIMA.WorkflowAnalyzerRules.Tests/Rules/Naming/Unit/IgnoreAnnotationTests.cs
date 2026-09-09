using WatchfulAnvil.Sdk.Common;
using Xunit;

namespace CPRIMA.WorkflowAnalyzerRules.Tests.Rules.Naming.Unit
{
    /// <summary>
    /// Suppression of CPRIMA-NMG-001 via an argument annotation.
    ///
    /// This used to test a rule-local <c>EnforceXamlArgumentNamingRule.IsIgnored</c>
    /// with an <c>@ignore RULE-ID</c> syntax. Both are gone: suppression moved to the
    /// SDK's <see cref="AnnotationReader"/> and the directive is now
    /// <c>@suppress:RULE-ID</c>. The rule calls
    /// <c>AnnotationReader.IsSuppressed(arg, Id)</c>, so these cases cover the
    /// behaviour the old test was protecting.
    /// </summary>
    public class IgnoreAnnotationTests
    {
        private const string RuleId = "CPRIMA-NMG-001";

        [Theory]
        [InlineData("@suppress:CPRIMA-NMG-001", true)]
        [InlineData("@suppress:cprima-nmg-001", true)]      // rule ids match case-insensitively
        [InlineData("  @suppress:CPRIMA-NMG-001  ", true)]  // surrounding whitespace is tokenized away
        [InlineData("Some note. @suppress:CPRIMA-NMG-001", true)]
        [InlineData("@suppress:DIFFERENT-RULE", false)]     // must not suppress a different rule
        [InlineData("@suppress", false)]                    // bare directive carries no rule id
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsSuppressed_MatchesOnlyItsOwnRuleId(string? annotation, bool expected)
        {
            Assert.Equal(expected, AnnotationReader.IsSuppressed(annotation, RuleId));
        }

        [Fact]
        public void NoCheck_SuppressesEverything()
        {
            Assert.True(AnnotationReader.IsNoCheck("@nocheck"));
            Assert.False(AnnotationReader.IsNoCheck("@suppress:CPRIMA-NMG-001"));
        }
    }
}
