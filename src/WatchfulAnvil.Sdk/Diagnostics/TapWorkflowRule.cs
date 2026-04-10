using System.Diagnostics;
using System.Linq;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using WatchfulAnvil.Sdk.Common;
using WatchfulAnvil.Sdk.Core;

namespace WatchfulAnvil.Sdk.Diagnostics
{
    /// <summary>
    /// Diagnostic tap rule that logs detailed workflow-level metadata to a configurable log file.
    /// Disabled by default. Enable per-project in Workflow Analyzer settings.
    /// </summary>
    public class TapWorkflowRule : WorkflowRule
    {
        private const string LogFileKey = "LogFile";
        private const string DefaultLogFile = @"%TEMP%\wa-tap-workflow.log";

        protected override string Id => "WA-TAP-WFL-001";
        protected override string Name => "Tap Workflow (Diagnostics)";
        protected override string Recommendation => "Diagnostic tap — logs workflow metadata to the configured log file.";
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

            RuleLogger.Log("WorkflowRule", workflow?.DisplayName ?? "<null>", logFile);
            RuleLogger.Log("WorkflowArguments", RuleLogger.FormatArguments(workflow?.Arguments ?? Enumerable.Empty<IArgumentModel>()), logFile);
            RuleLogger.Log("WorkflowTap",
                $"DisplayName={workflow?.DisplayName ?? "<null>"}, " +
                $"RelativePath={workflow?.RelativePath ?? "<null>"}, " +
                $"Args={workflow?.Arguments?.Count ?? -1}, " +
                $"ImportedNamespaces={workflow?.ImportedNamespaces?.Count ?? -1}, " +
                $"Assemblies={workflow?.Assemblies?.Count ?? -1}, " +
                $"HasRoot={(workflow?.Root != null)}, " +
                $"RootType={workflow?.Root?.GetType().Name ?? "<null>"}",
                logFile);

            if (workflow?.Root is IActivityModel root)
            {
                RuleLogger.Log("RootActivity",
                    $"DisplayName={root.DisplayName}, " +
                    $"ToolboxName={root.ToolboxName}, " +
                    $"Annotation={(string.IsNullOrWhiteSpace(root.AnnotationText) ? "<none>" : root.AnnotationText.Replace('\n', ' '))}, " +
                    $"Type={root.Type}, " +
                    $"Children={root.Children?.Count ?? 0}, " +
                    $"Variables={root.Variables?.Count ?? 0}, " +
                    $"Properties={root.Properties?.Count ?? 0}, " +
                    $"Arguments={root.Arguments?.Count ?? 0}, " +
                    $"DelegateArgs={root.DelegateArguments?.Count ?? 0}, " +
                    $"InternalArgs={root.InternalArguments?.Count ?? 0}, " +
                    $"SupportsObjectReferences={root.SupportsObjectReferences}, " +
                    $"IsRootOrContainer={root.IsRootOrActivityContainer}, " +
                    $"Id={(string.IsNullOrWhiteSpace(root.Id) ? "<none>" : root.Id)}",
                    logFile);
            }

            if (workflow?.Root?.Children != null && workflow.Root.Children.Any())
            {
                var children = workflow.Root.Children.ToList();
                var first = children.FirstOrDefault();
                var last = children.LastOrDefault();

                if (first != null)
                    RuleLogger.Log("RootFirstChild",
                        $"DisplayName={first.DisplayName}, ToolboxName={first.ToolboxName}, Type={first.Type}, Id={first.Id}",
                        logFile);

                if (last != null && !ReferenceEquals(last, first))
                    RuleLogger.Log("RootLastChild",
                        $"DisplayName={last.DisplayName}, ToolboxName={last.ToolboxName}, Type={last.Type}, Id={last.Id}",
                        logFile);
            }

            if (workflow?.Root is IActivityModel rootForInvoke)
                LogInvokeWorkflowCalls(rootForInvoke, logFile);

            return new InspectionResult { HasErrors = false };
        }

        private static void LogInvokeWorkflowCalls(IActivityModel node, string logFile)
        {
            if (node.Type?.Contains("InvokeWorkflowFile") == true)
            {
                var wfFileProp = node.Properties.FirstOrDefault(p => p.DisplayName == "WorkflowFileName");
                RuleLogger.Log("InvokeWorkflowCall",
                    $"DisplayName={node.DisplayName}, WorkflowFileName={wfFileProp?.DefinedExpression ?? "<not set>"}, Id={node.Id}",
                    logFile);
            }

            foreach (var child in node.Children ?? Enumerable.Empty<IActivityModel>())
                LogInvokeWorkflowCalls(child, logFile);
        }
    }
}
