namespace Cpmf.Rules
{
    /// <summary>
    /// Validates that a name can be used as a .NET identifier (class name, namespace component).
    /// </summary>
    public static class DotNetIdentifierValidator
    {
        /// <summary>
        /// Returns null when valid, or an error message describing the first violation.
        /// </summary>
        public static string Validate(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Name is empty.";

            if (char.IsDigit(name[0]))
                return $"'{name}' starts with a digit. .NET identifiers must begin with a letter or underscore.";

            foreach (var ch in name)
            {
                if (!char.IsLetterOrDigit(ch) && ch != '_')
                    return $"'{name}' contains the character '{ch}', which is not valid in a .NET identifier.";
            }

            if (!char.IsUpper(name[0]))
                return $"'{name}' starts with a lowercase letter. Use PascalCase for class and namespace names.";

            return null; // valid
        }
    }
}
