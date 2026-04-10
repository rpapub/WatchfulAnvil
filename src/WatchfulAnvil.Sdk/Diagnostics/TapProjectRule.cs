// <copyright file="TapProjectRule.cs" company="Christian Prior-Mamulyan">
// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// </copyright>

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

using WatchfulAnvil.Sdk.Common;
using WatchfulAnvil.Sdk.Core;

namespace WatchfulAnvil.Sdk.Diagnostics
{
    /// <summary>
    /// Diagnostic tap rule that logs detailed project-level metadata to a configurable log file.
    /// Disabled by default. Enable per-project in Workflow Analyzer settings.
    /// </summary>
    public class TapProjectRule : ProjectRule
    {
        private const string LogFileKey = "LogFile";
        private const string DefaultLogFile = @"%TEMP%\wa-tap-project.log";

        protected override string Id => "WA-TAP-PRJ-001";

        protected override string Name => "Tap Project (Diagnostics)";

        protected override string Recommendation => "Diagnostic tap — logs project metadata to the configured log file.";

        protected override TraceLevel DefaultSeverity => TraceLevel.Info;

        protected override bool IsEnabledByDefault => false;

        protected override void ConfigureParameters(Rule<IProjectModel> rule)
        {
            rule.Parameters.Add(LogFileKey, new Parameter
            {
                Key = LogFileKey,
                DefaultValue = DefaultLogFile,
                Value = DefaultLogFile,
                LocalizedDisplayName = "Log file path",
            });
        }

        protected override InspectionResult Inspect(IProjectModel project, Rule rule)
        {
            var logFile = rule.Parameters[LogFileKey]?.Value ?? DefaultLogFile;

            RuleLogger.Log("ProjectRule triggered", project?.DisplayName ?? "<null>", logFile);
            RuleLogger.Log("ProjectRule Count", project?.Workflows?.Count.ToString() ?? "<null>", logFile);
            RuleLogger.Log("ProjectTap",
                $"Name={project?.DisplayName ?? "<null>"}, " +
                $"Path={project?.ProjectFilePath ?? "<null>"}, " +
                $"Directory={project?.Directory ?? "<null>"}, " +
                $"EntryPointName={project?.EntryPointName ?? "<null>"}, " +
                $"ExceptionHandlerWorkflowName={project?.ExceptionHandlerWorkflowName ?? "<null>"}, " +
                $"ExpressionLanguage={project?.ExpressionLanguage ?? "<null>"}, " +
                $"RequiresUserInteraction={project?.RequiresUserInteraction}, " +
                $"SupportsPersistence={project?.SupportsPersistence}, " +
                $"OutputType={project?.ProjectOutputType}, " +
                $"ProfileType={project?.ProjectProfileType}, " +
                $"EntryPoints={project?.EntryPoints?.Count ?? -1}, " +
                $"TestCases={project?.TestCases?.Count ?? -1}, " +
                $"IgnoredFiles={project?.IgnoredFiles?.Count ?? -1}, " +
                $"Tags=[{string.Join(";", project?.Tags ?? new[] { "<none>" })}], " +
                $"FileNames=[{string.Join(";", project?.FileNames ?? new[] { "<none>" })}], " +
                $"Dependencies=[{string.Join(";", project?.Dependencies?.Select(d => d.Name) ?? new[] { "<none>" })}], " +
                $"Templates=[{string.Join(";", project?.Templates?.Select(t => t.ToString()) ?? new[] { "<none>" })}], " +
                $"AutomationHubIdeaUrl={project?.AutomationHubIdeaUrl ?? "<null>"}, " +
                $"ObjectBrowserSummary={(project?.ObjectBrowserSummary != null ? "present" : "null")}, " +
                $"Workflows={project?.Workflows?.Count ?? -1}",
                logFile);

            try
            {
                var projectRoot = project?.Directory;
                if (!string.IsNullOrWhiteSpace(projectRoot) && Directory.Exists(projectRoot))
                {
                    var todoPath = Path.Combine(projectRoot, "Todo.md");
                    File.WriteAllText(todoPath, $"# TODO Report\n\nGenerated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n");
                    RuleLogger.Log("TodoFile", $"Created or cleared: {todoPath}", logFile);
                }
                else
                {
                    RuleLogger.Log("TodoFile", "Project root could not be determined.", logFile);
                }
            }
            catch (Exception ex)
            {
                RuleLogger.Log("TodoFileError", ex.Message, logFile);
            }

            return new InspectionResult { HasErrors = false };
        }
    }
}
