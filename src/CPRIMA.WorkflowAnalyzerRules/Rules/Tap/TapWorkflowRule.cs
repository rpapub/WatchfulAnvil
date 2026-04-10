using System.Diagnostics;
using System.Linq;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using CPRIMA.WorkflowAnalyzerRules.Common;
using WatchfulAnvil.Sdk.Core;

namespace CPRIMA.WorkflowAnalyzerRules.Rules.Tap
{
    /// <summary>
    /// Probing rule that logs detailed workflow-level metadata for diagnostics and reverse engineering.
    /// This rule does not enforce constraints, but provides a snapshot of the workflow structure and configuration.
    /// </summary>
    /// <remarks>
    /// Useful for understanding the structure of UiPath workflows and for generating workflow-level reports.
    /// </remarks>
    public class TapWorkflowRule : WorkflowRule
    {
        protected override string Id => "CPRIMA-TAP-001";
        protected override string Name => "Tap Workflow Rule";
        protected override string Recommendation => "You are in Workflow mode.";
        protected override TraceLevel DefaultSeverity => TraceLevel.Info;

        /// <summary>
        /// Logs workflow metadata, root activity details, and information about invoked workflows.
        /// </summary>
        protected override InspectionResult Inspect(IWorkflowModel workflow, Rule rule)
        {
            RuleLogger.LogAndReturn("WorkflowRule", workflow?.DisplayName ?? "<null>");
            RuleLogger.LogAndReturn("WorkflowArguments", RuleLogger.FormatArguments(workflow?.Arguments ?? Enumerable.Empty<IArgumentModel>()));
            RuleLogger.LogAndReturn("WorkflowTap",
                $"DisplayName={workflow?.DisplayName ?? "<null>"}, " +
                $"RelativePath={workflow?.RelativePath ?? "<null>"}, " +
                $"Args={workflow?.Arguments?.Count ?? -1}, " +
                $"ImportedNamespaces={workflow?.ImportedNamespaces?.Count ?? -1}, " +
                $"Assemblies={workflow?.Assemblies?.Count ?? -1}, " +
                $"HasRoot={(workflow?.Root != null)}, " +
                $"RootType={workflow?.Root?.GetType().Name ?? "<null>"}"
            );

            if (workflow?.Root is IActivityModel root)
            {
                RuleLogger.LogAndReturn("RootActivity",
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
                    $"Id={(string.IsNullOrWhiteSpace(root.Id) ? "<none>" : root.Id)}"
                );
            }

            if (workflow?.Root?.Children != null && workflow.Root.Children.Any())
            {
                var children = workflow.Root.Children.ToList();

                var first = children.FirstOrDefault();
                var last = children.LastOrDefault();

                if (first != null)
                    RuleLogger.LogAndReturn("RootFirstChild",
                        $"DisplayName={first.DisplayName}, ToolboxName={first.ToolboxName}, Type={first.Type}, Id={first.Id}");

                if (last != null && !ReferenceEquals(last, first))
                    RuleLogger.LogAndReturn("RootLastChild",
                        $"DisplayName={last.DisplayName}, ToolboxName={last.ToolboxName}, Type={last.Type}, Id={last.Id}");
            }

            void LogInvokeWorkflowCalls(IActivityModel node)
            {
                if (node.Type?.Contains("InvokeWorkflowFile") == true)
                {
                    var wfFileProp = node.Properties.FirstOrDefault(p => p.DisplayName == "WorkflowFileName");
                    var wfFileName = wfFileProp?.DefinedExpression ?? "<not set>";

                    RuleLogger.LogAndReturn("InvokeWorkflowCall",
                        $"DisplayName={node.DisplayName}, WorkflowFileName={wfFileName}, Id={node.Id}");
                }

                foreach (var child in node.Children ?? Enumerable.Empty<IActivityModel>())
                {
                    LogInvokeWorkflowCalls(child);
                }
            }

            if (workflow?.Root is IActivityModel rootForInvoke)
            {
                LogInvokeWorkflowCalls(rootForInvoke);
            }

            return new InspectionResult { HasErrors = false };
        }
    }
}
