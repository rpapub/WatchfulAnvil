using System;
using System.Diagnostics;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using CPRIMA.WorkflowAnalyzerRules.Common;
using WatchfulAnvil.Sdk.Core;

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
    public class TapSdkBypassRule : WorkflowRule
    {
        protected override string Id => "CPRIMA-TAP-004";
        protected override string Name => "Tap SDK Bypass Rule";
        protected override string Recommendation => "Inspects raw XAML for x:Members annotations.";
        protected override TraceLevel DefaultSeverity => TraceLevel.Info;

        /// <summary>
        /// Inspects the XAML file for argument annotations in the x:Members section.
        /// </summary>
        protected override InspectionResult Inspect(IWorkflowModel workflow, Rule rule)
        {
            try
            {
                var fullPath = workflow.RelativePath;
                if (!System.IO.File.Exists(fullPath))
                {
                    RuleLogger.LogAndReturn("XamlLoadError", $"File not found: {fullPath}");
                    return new InspectionResult { HasErrors = false };
                }

                var arguments = XamlParser.ParseArguments(fullPath);
                foreach (var arg in arguments)
                {
                    RuleLogger.LogAndReturn("XamlArgumentAnnotation", $"Name={arg.Name}, Type={arg.Type}, Annotation={arg.Annotation}");
                }
            }
            catch (Exception ex)
            {
                RuleLogger.LogAndReturn("XamlReadException", ex.Message);
            }

            return new InspectionResult { HasErrors = false };
        }
    }
}
