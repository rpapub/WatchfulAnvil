using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using CPRIMA.WorkflowAnalyzerRules.Common;

namespace CPRIMA.WorkflowAnalyzerRules.Rules.Tap
{
    /// <summary>
    /// Probing rule that inspects raw XAML for x:Members annotations.
    /// This rule is intended for diagnostics and reverse engineering, not enforcement.
    /// </summary>
    /// <remarks>
    /// This rule parses the XAML file directly to access argument annotations,
    /// bypassing the limitations of the standard UiPath SDK.
    /// </remarks>
    public class TapSdkBypassRule : IRegisterAnalyzerConfiguration
    {
        private const string RuleId = "CPRIMA-TAP-004";

        public void Initialize(IAnalyzerConfigurationService config) =>
            config.AddRule<IWorkflowModel>(Get());

        public Rule<IWorkflowModel> Get() =>
            new Rule<IWorkflowModel>("Tap SDK Bypass Rule", RuleId, InspectRawXaml)
            {
                // TODO: Move this recommendation message to localization resources.
                RecommendationMessage = "Inspects raw XAML for x:Members annotations.",
                DefaultErrorLevel = TraceLevel.Info
            };

        /// <summary>
        /// Inspects the XAML file for argument annotations in the x:Members section.
        /// </summary>
        /// <param name="workflow">The workflow model.</param>
        /// <param name="_">The rule metadata (unused).</param>
        /// <returns>InspectionResult indicating if any errors were found.</returns>
        private InspectionResult InspectRawXaml(IWorkflowModel workflow, Rule _)
        {
            try
            {
                var fullPath = workflow.RelativePath;
                if (!File.Exists(fullPath))
                {
                    // TODO: Move this log message to localization resources.
                    RuleLogger.LogAndReturn("XamlLoadError", $"File not found: {fullPath}");
                    return new InspectionResult { HasErrors = false };
                }

                // Use the shared XamlParser utility to extract argument info.
                var arguments = XamlParser.ParseArguments(fullPath);
                foreach (var arg in arguments)
                {
                    // TODO: Move this log message to localization resources.
                    RuleLogger.LogAndReturn("XamlArgumentAnnotation", $"Name={arg.Name}, Type={arg.Type}, Annotation={arg.Annotation}");
                }
            }
            catch (Exception ex)
            {
                // TODO: Move this log message to localization resources.
                RuleLogger.LogAndReturn("XamlReadException", ex.Message);
            }

            return new InspectionResult { HasErrors = false };
        }

        // TODO: Write and publish documentation for this rule at a public URL.
        //       Add the documentation URL to the rule metadata or as a comment here.
    }
}
