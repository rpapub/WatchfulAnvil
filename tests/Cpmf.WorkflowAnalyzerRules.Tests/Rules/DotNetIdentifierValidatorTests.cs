using Cpmf.Rules;
using Xunit;

namespace Cpmf.WorkflowAnalyzerRules.Tests.Rules
{
    public class DotNetIdentifierValidatorTests
    {
        // --- Valid ---

        [Theory]
        [InlineData("MyClass")]
        [InlineData("PopulateCase")]
        [InlineData("HandleRegistration")]
        [InlineData("MyProject2")]
        [InlineData("A")]
        public void Pass_WhenNameIsValidPascalCase(string name)
        {
            Assert.Null(DotNetIdentifierValidator.Validate(name));
        }

        // --- Starts with digit ---

        [Theory]
        [InlineData("1Process")]
        [InlineData("041_Handle")]
        [InlineData("2ndStep")]
        public void Fail_WhenNameStartsWithDigit(string name)
        {
            var result = DotNetIdentifierValidator.Validate(name);
            Assert.NotNull(result);
            Assert.Contains("digit", result);
        }

        // --- Invalid characters ---

        [Theory]
        [InlineData("My Class")]    // space
        [InlineData("My-Class")]    // hyphen
        [InlineData("My.Class")]    // dot
        [InlineData("My/Class")]    // slash
        public void Fail_WhenNameContainsInvalidCharacter(string name)
        {
            var result = DotNetIdentifierValidator.Validate(name);
            Assert.NotNull(result);
            Assert.Contains("not valid", result);
        }

        // --- Lowercase start (PascalCase violation) ---

        [Theory]
        [InlineData("myClass")]
        [InlineData("processItem")]
        public void Fail_WhenNameStartsWithLowercase(string name)
        {
            var result = DotNetIdentifierValidator.Validate(name);
            Assert.NotNull(result);
            Assert.Contains("lowercase", result);
        }

        // --- Empty ---

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Fail_WhenNameIsNullOrWhitespace(string name)
        {
            var result = DotNetIdentifierValidator.Validate(name);
            Assert.NotNull(result);
        }
    }
}
