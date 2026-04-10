using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using CPRIMA.WorkflowAnalyzerRules.Common;
using WatchfulAnvil.Sdk.Core;

namespace CPRIMA.WorkflowAnalyzerRules.Rules.Naming
{
    /// <summary>
    /// Analyzer rule that enforces a naming convention for arguments defined in the x:Members section of a XAML file.
    /// </summary>
    /// <remarks>
    /// This rule parses the raw XAML file to access argument definitions and annotations,
    /// as these are not exposed by the standard UiPath SDK.
    /// </remarks>
    public class EnforceXamlArgumentNamingRule : WorkflowRule
    {
        protected override string Id => "CPRIMA-NMG-001";
        protected override string Name => "Enforce XAML Argument Naming";
        protected override string Recommendation => "Arguments in x:Members must follow naming convention (e.g., in_filePath, out_status).";
        protected override TraceLevel DefaultSeverity => TraceLevel.Warning;

        // Naming patterns
        private const string PATTERN_PASCAL_CASE = @"^[A-Z][a-zA-Z0-9]*$";
        private const string PATTERN_SNAKE_CASE = @"^[a-z][a-z0-9_]*$";
        private const string PATTERN_KEBAB_CASE = @"^[a-z][a-z0-9-]*$";

        private static readonly Dictionary<string, string> PATTERN_NAMES = new Dictionary<string, string>
        {
            { PATTERN_PASCAL_CASE, "PascalCase" },
            { PATTERN_SNAKE_CASE, "snake_case" },
            { PATTERN_KEBAB_CASE, "kebab-case" }
        };

        private static readonly string ACTIVE_PATTERN = PATTERN_PASCAL_CASE;
        private static readonly Regex NameSuffixPattern = new Regex(ACTIVE_PATTERN, RegexOptions.Compiled);

        public static bool IsIgnored(string annotation)
        {
            if (string.IsNullOrEmpty(annotation)) return false;
            var parts = annotation.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0 || !parts[0].Equals("@ignore", StringComparison.OrdinalIgnoreCase)) return false;
            if (parts.Length == 1) return false;
            return parts[1].Equals("CPRIMA-NMG-001", StringComparison.OrdinalIgnoreCase);
        }

        protected override InspectionResult Inspect(IWorkflowModel workflow, Rule rule)
        {
            var result = new InspectionResult { ErrorLevel = TraceLevel.Warning };
            try
            {
                var fullPath = workflow.RelativePath;
                if (!File.Exists(fullPath))
                {
                    RuleLogger.LogAndReturn("NamingCheck", $"File not found: {fullPath}");
                    return result;
                }

                var arguments = XamlParser.ParseArguments(fullPath);
                foreach (var arg in arguments)
                {
                    if (!string.IsNullOrEmpty(arg.Annotation) && IsIgnored(arg.Annotation))
                    {
                        RuleLogger.LogAndReturn("IgnoredArgument", $"Name={arg.Name} skipped due to ignore tag.");
                        continue;
                    }

                    var validation = ValidateArgumentName(arg.Name, arg.Type);
                    var isValid = validation.Item1;
                    var reason = validation.Item2;
                    if (!isValid)
                    {
                        RuleLogger.LogAndReturn("InvalidArgumentName", $"Name={arg.Name}, Type={arg.Type}, Reason={reason}, Annotation={arg.Annotation}");
                        result.HasErrors = true;
                        result.ErrorLevel = rule.DefaultErrorLevel;
                        result.RecommendationMessage = $"{rule.RecommendationMessage} Error: {reason}";
                    }
                }
            }
            catch (Exception ex)
            {
                RuleLogger.LogAndReturn("XamlParsingError", ex.Message);
            }

            return result;
        }

        private Tuple<bool, string> ValidateArgumentName(string name, string type)
        {
            string requiredPrefix = "";
            if (!string.IsNullOrEmpty(type))
            {
                if (type.StartsWith("InArgument"))
                    requiredPrefix = "in_";
                else if (type.StartsWith("OutArgument"))
                    requiredPrefix = "out_";
                else if (type.StartsWith("InOutArgument"))
                    requiredPrefix = "io_";
            }

            RuleLogger.LogAndReturn("ValidationCheck", $"Checking name={name}, type={type}, requiredPrefix={requiredPrefix ?? "<none>"}");

            if (requiredPrefix == "")
            {
                RuleLogger.LogAndReturn("ValidationSkip", $"Skipping validation for unknown type: {type}");
                return Tuple.Create(true, "Unknown type");
            }

            if (!string.IsNullOrEmpty(requiredPrefix) && !name.StartsWith(requiredPrefix, StringComparison.Ordinal))
            {
                RuleLogger.LogAndReturn("ValidationFail", $"Name '{name}' does not start with required prefix '{requiredPrefix}'");
                return Tuple.Create(false, $"Must start with {requiredPrefix}");
            }

            if (name.StartsWith("_"))
                return Tuple.Create(false, "Name cannot start with underscore");

            string nameSuffix = name.Substring(requiredPrefix?.Length ?? 0);
            if (string.IsNullOrEmpty(nameSuffix))
                return Tuple.Create(false, "Name must have a suffix after prefix");

            bool isValidSuffix = NameSuffixPattern.IsMatch(nameSuffix);
            RuleLogger.LogAndReturn("ValidationSuffix", $"Checking suffix '{nameSuffix}' against pattern '{NameSuffixPattern}': {(isValidSuffix ? "valid" : "invalid")}");

            return Tuple.Create(isValidSuffix,
                isValidSuffix ? "Valid" : $"Suffix must be in {PATTERN_NAMES[ACTIVE_PATTERN]} format");
        }
    }
}
