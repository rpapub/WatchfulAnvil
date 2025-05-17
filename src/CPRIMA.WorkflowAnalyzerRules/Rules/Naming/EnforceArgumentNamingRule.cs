using System;
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
        // TODO: Move regex patterns and all user-facing strings to localization resources.
        private static readonly Regex ValidNamePattern = new Regex(@"^(in|out|io)_[a-z][a-zA-Z0-9]*$", RegexOptions.Compiled);
        private static readonly Regex IgnorePattern = new Regex(@"@ignore\s+CPRIMA-NMG-001", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public void Initialize(IAnalyzerConfigurationService config) =>
            config.AddRule<IWorkflowModel>(Get());

        public Rule<IWorkflowModel> Get() =>
            new Rule<IWorkflowModel>("Enforce XAML Argument Naming", "CPRIMA-NMG-001", InspectXamlForNaming)
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
        private InspectionResult InspectXamlForNaming(IWorkflowModel workflow, Rule rule)
        {
            var result = new InspectionResult();
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
                    if (IgnorePattern.IsMatch(arg.Annotation ?? ""))
                    {
                        // TODO: Move this log message to localization resources.
                        RuleLogger.LogAndReturn("IgnoredArgument", $"Name={arg.Name} skipped due to ignore tag.");
                        continue;
                    }

                    // If name does not match the valid pattern, log and set error.
                    if (!ValidNamePattern.IsMatch(arg.Name))
                    {
                        // TODO: Move this log message to localization resources.
                        RuleLogger.LogAndReturn("InvalidArgumentName", $"Name={arg.Name}, Type={arg.Type}, Annotation={arg.Annotation}");
                        result.HasErrors = true;
                        result.ErrorLevel = rule.DefaultErrorLevel;
                        result.RecommendationMessage = rule.RecommendationMessage;
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
