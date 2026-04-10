using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using CPRIMA.WorkflowAnalyzerRules.Common;
using WatchfulAnvil.Sdk.Core;

namespace CPRIMA.WorkflowAnalyzerRules.Rules.Tap
{
    /// <summary>
    /// Probing rule that logs detailed project-level metadata for diagnostics and reverse engineering.
    /// This rule does not enforce constraints, but provides a snapshot of the project structure and configuration.
    /// </summary>
    /// <remarks>
    /// Useful for understanding the structure of UiPath projects and for generating project-level reports.
    /// </remarks>
    public class TapProjectRule : ProjectRule
    {
        protected override string Id => "CPRIMA-TAP-003";
        protected override string Name => "Tap Project Rule";
        protected override string Recommendation => "You are in Project mode.";
        protected override TraceLevel DefaultSeverity => TraceLevel.Info;

        /// <summary>
        /// Logs project metadata and attempts to create or clear a Todo.md file in the project root.
        /// </summary>
        protected override InspectionResult Inspect(IProjectModel project, Rule rule)
        {
            RuleLogger.LogAndReturn("ProjectRule triggered", project?.DisplayName ?? "<null>");
            RuleLogger.LogAndReturn("ProjectRule Count", project?.Workflows?.Count.ToString() ?? "<null>");
            RuleLogger.LogAndReturn("ProjectTap",
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
                $"Workflows={project?.Workflows?.Count ?? -1}"
            );

            try
            {
                var projectRoot = project?.Directory;

                if (!string.IsNullOrWhiteSpace(projectRoot) && Directory.Exists(projectRoot))
                {
                    var todoPath = Path.Combine(projectRoot, "Todo.md");
                    File.WriteAllText(todoPath, $"# TODO Report\n\nGenerated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n");
                    RuleLogger.LogAndReturn("TodoFile", $"Created or cleared: {todoPath}");
                }
                else
                {
                    RuleLogger.LogAndReturn("TodoFile", "Project root could not be determined.");
                }
            }
            catch (Exception ex)
            {
                RuleLogger.LogAndReturn("TodoFileError", ex.Message);
            }

            return new InspectionResult { HasErrors = false };
        }
    }
}
