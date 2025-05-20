using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using CPRIMA.WorkflowAnalyzerRules.Common;

namespace CPRIMA.WorkflowAnalyzerRules.Rules.Naming
{
    /// <summary>
    /// Analyzer rule that enforces a naming convention for arguments defined in the x:Members section of a XAML file.
    /// </summary>
    /// <remarks>
    /// This rule parses the raw XAML file to access argument definitions and annotations,
    /// as these are not exposed by the standard UiPath SDK.
    /// </remarks>
    public class EnforceXamlArgumentNamingRule : IRegisterAnalyzerConfiguration
    {
        private const string RuleId = "CPRIMA-NMG-001";

        // Naming patterns
        private const string PATTERN_PASCAL_CASE = @"^[A-Z][a-zA-Z0-9]*$";
        private const string PATTERN_SNAKE_CASE = @"^[a-z][a-z0-9_]*$";
        private const string PATTERN_KEBAB_CASE = @"^[a-z][a-z0-9-]*$";

        // Pattern names for messages
        private static readonly Dictionary<string, string> PATTERN_NAMES = new Dictionary<string, string>
        {
            { PATTERN_PASCAL_CASE, "PascalCase" },
            { PATTERN_SNAKE_CASE, "snake_case" },
            { PATTERN_KEBAB_CASE, "kebab-case" }
        };

        // Current active pattern - easily changeable
        private static readonly string ACTIVE_PATTERN = PATTERN_PASCAL_CASE;

        // TODO: Move regex patterns and all user-facing strings to localization resources.
        private static readonly Regex NameSuffixPattern = new Regex(ACTIVE_PATTERN, RegexOptions.Compiled);
        public static bool IsIgnored(string annotation)
        {
            if (string.IsNullOrEmpty(annotation)) return false;
            var parts = annotation.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0 || !parts[0].Equals("@ignore", StringComparison.OrdinalIgnoreCase)) return false;
            if (parts.Length == 1) return false; // Just @ignore without a rule ID
            return parts[1].Equals(RuleId, StringComparison.OrdinalIgnoreCase);
        }

        public void Initialize(IAnalyzerConfigurationService config) =>
            config.AddRule<IWorkflowModel>(Get());

        public Rule<IWorkflowModel> Get() =>
            new Rule<IWorkflowModel>("Enforce XAML Argument Naming", RuleId, InspectXamlForNaming)
            {
                // TODO: Move this recommendation message to localization resources.
                RecommendationMessage = "Arguments in x:Members must follow naming convention (e.g., in_filePath, out_status).",
                DefaultErrorLevel = TraceLevel.Warning
            };

        /// <summary>
        /// Inspects the XAML file for argument naming compliance.
        /// </summary>
        /// <param name="workflow">The workflow model.</param>
        /// <param name="rule">The rule metadata.</param>
        /// <returns>InspectionResult indicating if any errors were found.</returns>
        private Tuple<bool, string> ValidateArgumentName(string name, string type)
        {
            // Check if name starts with required prefix
            string requiredPrefix = null;
            if (!string.IsNullOrEmpty(type))
            {
                if (type.StartsWith("InArgument"))
                {
                    requiredPrefix = "in_";
                }
                else if (type.StartsWith("OutArgument"))
                {
                    requiredPrefix = "out_";
                }
                else if (type.StartsWith("InOutArgument"))
                {
                    requiredPrefix = "io_";
                }
            }

            RuleLogger.LogAndReturn("ValidationCheck", $"Checking name={name}, type={type}, requiredPrefix={requiredPrefix ?? "<none>"}");

            if (requiredPrefix == null)
            {
                RuleLogger.LogAndReturn("ValidationSkip", $"Skipping validation for unknown type: {type}");
                return Tuple.Create(true, "Unknown type");
            }

            if (!name.StartsWith(requiredPrefix, StringComparison.Ordinal))
            {
                RuleLogger.LogAndReturn("ValidationFail", $"Name '{name}' does not start with required prefix '{requiredPrefix}'");
                return Tuple.Create(false, $"Must start with {requiredPrefix}");
            }

            // Check if the part after the prefix follows the naming pattern
            // Check if name starts with underscore
            if (name.StartsWith("_"))
            {
                return Tuple.Create(false, "Name cannot start with underscore");
            }

            // Get and validate suffix
            string nameSuffix = name.Substring(requiredPrefix.Length);
            if (string.IsNullOrEmpty(nameSuffix))
            {
                return Tuple.Create(false, "Name must have a suffix after prefix");
            }

            bool isValidSuffix = NameSuffixPattern.IsMatch(nameSuffix);
            RuleLogger.LogAndReturn("ValidationSuffix", $"Checking suffix '{nameSuffix}' against pattern '{NameSuffixPattern}': {(isValidSuffix ? "valid" : "invalid")}");
            
            return Tuple.Create(isValidSuffix, 
                isValidSuffix ? "Valid" : $"Suffix must be in {PATTERN_NAMES[ACTIVE_PATTERN]} format");
        }

        private InspectionResult InspectXamlForNaming(IWorkflowModel workflow, Rule rule)
        {
            var result = new InspectionResult { ErrorLevel = TraceLevel.Warning };
            try
            {
                var fullPath = workflow.RelativePath;
                if (!File.Exists(fullPath))
                {
                    // TODO: Move this log message to localization resources.
                    RuleLogger.LogAndReturn("NamingCheck", $"File not found: {fullPath}");
                    return result;
                }

                // Use the shared XamlParser utility to extract argument info.
                var arguments = XamlParser.ParseArguments(fullPath);
                foreach (var arg in arguments)
                {
                    // If annotation contains the ignore pattern, skip this argument.
                    if (!string.IsNullOrEmpty(arg.Annotation) && IsIgnored(arg.Annotation))
                    {
                        // TODO: Move this log message to localization resources.
                        RuleLogger.LogAndReturn("IgnoredArgument", $"Name={arg.Name} skipped due to ignore tag.");
                        continue;
                    }

                    // Validate the argument name
                    var validation = ValidateArgumentName(arg.Name, arg.Type);
                    var isValid = validation.Item1;
                    var reason = validation.Item2;
                    if (!isValid)
                    {
                        // TODO: Move this log message to localization resources.
                        RuleLogger.LogAndReturn("InvalidArgumentName", $"Name={arg.Name}, Type={arg.Type}, Reason={reason}, Annotation={arg.Annotation}");
                        result.HasErrors = true;
                        result.ErrorLevel = rule.DefaultErrorLevel;
                        result.RecommendationMessage = $"{rule.RecommendationMessage} Error: {reason}";
                    }
                }
            }
            catch (Exception ex)
            {
                // TODO: Move this log message to localization resources.
                RuleLogger.LogAndReturn("XamlParsingError", ex.Message);
            }

            return result;
        }

        // TODO: Write and publish documentation for this rule at a public URL.
        //       Add the documentation URL to the rule metadata or as a comment here.
    }
}
