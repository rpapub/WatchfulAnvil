using WatchfulAnvil.Sdk.Common;
using Xunit;

namespace Cpmf.WorkflowAnalyzerRules.Tests.Common
{
    /// <summary>
    /// Directive parsing in <see cref="AnnotationReader"/>.
    ///
    /// The multi-directive cases are the point: suppression used to be resolved through
    /// GetTagValue, which returns the first match and stops, so a second @suppress on the
    /// same annotation was unreachable.
    /// </summary>
    public class AnnotationReaderTests
    {
        [Theory]
        [InlineData("@suppress:RULE-A", "RULE-A", true)]
        [InlineData("@suppress:RULE-A", "RULE-B", false)]
        [InlineData("@suppress:rule-a", "RULE-A", true)]          // ids match case-insensitively
        [InlineData("@suppress: RULE-A", "RULE-A", true)]         // space-separated form
        [InlineData("Note. @suppress:RULE-A more text", "RULE-A", true)]
        [InlineData("@suppress", "RULE-A", false)]                // bare directive carries no id
        [InlineData("", "RULE-A", false)]
        [InlineData(null, "RULE-A", false)]
        public void IsSuppressed_SingleDirective(string? annotation, string ruleId, bool expected)
        {
            Assert.Equal(expected, AnnotationReader.IsSuppressed(annotation, ruleId));
        }

        [Theory]
        [InlineData("RULE-A")]
        [InlineData("RULE-B")]
        [InlineData("RULE-C")]
        public void IsSuppressed_HonoursEveryDirective_NotJustTheFirst(string ruleId)
        {
            const string annotation = "@suppress:RULE-A @suppress:RULE-B @suppress:RULE-C";

            Assert.True(AnnotationReader.IsSuppressed(annotation, ruleId));
        }

        [Fact]
        public void IsSuppressed_StillRejectsAnAbsentIdAmongSeveral()
        {
            const string annotation = "@suppress:RULE-A @suppress:RULE-B";

            Assert.False(AnnotationReader.IsSuppressed(annotation, "RULE-Z"));
        }

        [Theory]
        [InlineData("RULE-A")]
        [InlineData("RULE-B")]
        public void IsViolates_HonoursEveryDirective_NotJustTheFirst(string ruleId)
        {
            const string annotation = "@violates:RULE-A @violates:RULE-B";

            Assert.True(AnnotationReader.IsViolates(annotation, ruleId));
        }

        [Fact]
        public void GetTagValue_StillReturnsTheFirstOccurrenceOnly()
        {
            // Documents the deliberate asymmetry: GetTagValue is a "read the value" helper
            // and keeps first-match semantics; HasTagValue is the "does it carry this value"
            // test and scans all occurrences.
            Assert.Equal("RULE-A", AnnotationReader.GetTagValue("@suppress:RULE-A @suppress:RULE-B", "@suppress"));
        }

        [Theory]
        [InlineData("@unit", "@unit", true)]                      // bare form
        [InlineData("@unit:Login", "@unit", true)]                // valued form still IS the tag
        [InlineData("@UNIT:Login", "@unit", true)]                // case-insensitive
        [InlineData("@pipeline\n@unit:Login", "@unit", true)]     // newline-separated
        [InlineData("@units", "@unit", false)]                    // must not match a longer tag
        [InlineData("@domain-model:X", "@domain", false)]         // prefix tested is "@domain:"
        [InlineData("@module", "@unit", false)]
        [InlineData("", "@unit", false)]
        [InlineData(null, "@unit", false)]
        public void HasTag_AcceptsBareAndValuedForms(string? annotation, string tag, bool expected)
        {
            Assert.Equal(expected, AnnotationReader.HasTag(annotation, tag));
        }

        [Fact]
        public void HasTag_AndGetTagValue_AgreeOnTheValuedForm()
        {
            // These two used to disagree: GetTagValue understood @unit:Login while HasTag
            // reported the same annotation as untagged.
            const string annotation = "@unit:Login";

            Assert.True(AnnotationReader.HasTag(annotation, "@unit"));
            Assert.Equal("Login", AnnotationReader.GetTagValue(annotation, "@unit"));
        }

        [Fact]
        public void NoCheck_IsIndependentOfSuppress()
        {
            Assert.True(AnnotationReader.IsNoCheck("@nocheck"));
            Assert.True(AnnotationReader.IsNoCheck("@suppress:RULE-A @nocheck"));
            Assert.False(AnnotationReader.IsNoCheck("@suppress:RULE-A"));
        }
    }
}
