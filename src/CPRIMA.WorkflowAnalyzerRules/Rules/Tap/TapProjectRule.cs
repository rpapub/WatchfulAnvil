using System.Diagnostics;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using CPRIMA.WorkflowAnalyzerRules.LocalizationResources;
using CPRIMA.WorkflowAnalyzerRules.Common;
using System.Linq;
using System.IO;
using System;

namespace CPRIMA.WorkflowAnalyzerRules.Rules.Tap
{
    /// <summary>
    /// Probing rule that logs detailed project-level metadata for diagnostics and reverse engineering.
    /// This rule does not enforce constraints, but provides a snapshot of the project structure and configuration.
    /// </summary>
    /// <remarks>
    /// Useful for understanding the structure of UiPath projects and for generating project-level reports.
    /// </remarks>
    public class TapProjectRule : IRegisterAnalyzerConfiguration
    {
        public void Initialize(IAnalyzerConfigurationService config) =>
            config.AddRule<IProjectModel>(Get());

        public Rule<IProjectModel> Get() =>
            new Rule<IProjectModel>("Tap Project Rule", "CPRIMA-TAP-003", InspectProject)
            {
                // TODO: Move this recommendation message to localization resources.
                RecommendationMessage = "You are in Project mode.",
                DefaultErrorLevel = TraceLevel.Info
            };

        /// <summary>
        /// Logs project metadata and attempts to create or clear a Todo.md file in the project root.
        /// </summary>
        /// <param name="project">The project model.</param>
        /// <param name="_">The rule metadata (unused).</param>
        /// <returns>InspectionResult indicating if any errors were found (always false for this probing rule).</returns>
        private InspectionResult InspectProject(IProjectModel project, Rule _)
        {
            // Log basic project information
            RuleLogger.LogAndReturn("ProjectRule triggered", project?.DisplayName ?? "<null>");
            RuleLogger.LogAndReturn("ProjectRule Count", project?.Workflows?.Count.ToString() ?? "<null>");
            RuleLogger.LogAndReturn("ProjectTap",
                $"Name={project?.DisplayName ?? "<null>"}, " +
                $"Path={project?.ProjectFilePath ?? "<null>"}, " +
                $"Directory={project?.Directory ?? "<null>"}, " +
                $"EntryPointName={project?.EntryPointName ?? "<null>"}, " +
                $"ExceptionHandlerWorkflowName={project?.ExceptionHandlerWorkflowName ?? "<null>"}, " +
                $"ExpressionLanguage={project?.ExpressionLanguage ?? "<null>"}, " +
                // $"HasModernBehavior={project?.HasModernBehavior}, " +
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

                // Attempt to create or clear a Todo.md file in the project root
                if (!string.IsNullOrWhiteSpace(projectRoot) && Directory.Exists(projectRoot))
                {
                    var todoPath = Path.Combine(projectRoot, "Todo.md");
                    File.WriteAllText(todoPath, $"# TODO Report\n\nGenerated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n");

                    // TODO: Move this log message to localization resources.
                    RuleLogger.LogAndReturn("TodoFile", $"Created or cleared: {todoPath}");
                }
                else
                {
                    // TODO: Move this log message to localization resources.
                    RuleLogger.LogAndReturn("TodoFile", "Project root could not be determined.");
                }
            }
            catch (Exception ex)
            {
                // TODO: Move this log message to localization resources.
                RuleLogger.LogAndReturn("TodoFileError", ex.Message);
            }

            return new InspectionResult { HasErrors = false };
        }

        // TODO: Write and publish documentation for this rule at a public URL.
        //       Add the documentation URL to the rule metadata or as a comment here.
    }
}
