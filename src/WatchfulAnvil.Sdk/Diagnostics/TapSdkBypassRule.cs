using System;
using System.Diagnostics;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using WatchfulAnvil.Sdk.Common;
using WatchfulAnvil.Sdk.Core;

namespace WatchfulAnvil.Sdk.Diagnostics
{
    /// <summary>
    /// Diagnostic tap rule that parses raw XAML to expose x:Members argument annotations,
    /// bypassing the UiPath SDK limitation where IArgumentModel does not expose annotation text.
    /// Disabled by default. Enable per-project in Workflow Analyzer settings.
    /// </summary>
    public class TapSdkBypassRule : WorkflowRule
    {
        private const string LogFileKey = "LogFile";
        private const string DefaultLogFile = @"%TEMP%\wa-tap-xaml.log";

        protected override string Id => "WA-TAP-XAML-001";
        protected override string Name => "Tap XAML SDK Bypass (Diagnostics)";
        protected override string Recommendation => "Diagnostic tap — logs XAML argument annotations to the configured log file.";
        protected override TraceLevel DefaultSeverity => TraceLevel.Info;

        protected override void ConfigureParameters(Rule<IWorkflowModel> rule)
        {
            rule.DefaultIsEnabled = false;
            rule.Parameters.Add(LogFileKey, new Parameter
            {
                Key = LogFileKey,
                DefaultValue = DefaultLogFile,
                Value = DefaultLogFile,
                LocalizedDisplayName = "Log file path"
            });
        }

        protected override InspectionResult Inspect(IWorkflowModel workflow, Rule rule)
        {
            var logFile = rule.Parameters[LogFileKey]?.Value ?? DefaultLogFile;

            try
            {
                var fullPath = workflow.RelativePath;
                if (!System.IO.File.Exists(fullPath))
                {
                    RuleLogger.Log("XamlLoadError", $"File not found: {fullPath}", logFile);
                    return new InspectionResult { HasErrors = false };
                }

                var arguments = XamlParser.ParseArguments(fullPath);
                foreach (var arg in arguments)
                    RuleLogger.Log("XamlArgumentAnnotation", $"Name={arg.Name}, Type={arg.Type}, Annotation={arg.Annotation}", logFile);
            }
            catch (Exception ex)
            {
                RuleLogger.Log("XamlReadException", ex.Message, logFile);
            }

            return new InspectionResult { HasErrors = false };
        }
    }
}
