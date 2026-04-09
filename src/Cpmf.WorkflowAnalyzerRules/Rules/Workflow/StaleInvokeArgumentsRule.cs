using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace Cpmf.Rules.Workflow
{
    public class StaleInvokeArgumentsRule : IRegisterAnalyzerConfiguration
    {
        private const string RuleId = "CPMF-WFL-008";

        // UiPath XAML namespaces — stable across Studio versions.
        private static readonly XNamespace UiNs =
            XNamespace.Get("http://schemas.uipath.com/workflow/activities");
        private static readonly XNamespace Sap2010Ns =
            XNamespace.Get("http://schemas.microsoft.com/netfx/2010/xaml/activities/presentation");

        public void Initialize(IAnalyzerConfigurationService api)
        {
            // project.Directory and activity.Id require WorkflowAnalyzerV9 (sdk-capabilities: 21.4.1+).
            if (!api.HasFeature(DesignFeatureKeys.WorkflowAnalyzerV9))
                return; // Studio < 21.4 — rule cannot function without Directory or activity Id.

            api.AddRule<IWorkflowModel>(Get());
        }

        public Rule<IWorkflowModel> Get() =>
            new Rule<IWorkflowModel>("InvokeWorkflowFile Argument Count Mismatch", RuleId, Inspect)
            {
                RecommendationMessage =
                    "The number of arguments bound on an Invoke Workflow File activity does not match " +
                    "the number declared in the target workflow. Update the argument mappings.",
                DefaultErrorLevel = TraceLevel.Error,
                DocumentationLink = "https://github.com/rpapub/WatchfulAnvil/wiki/Rule-Documentation-CPMF-WFL-008"
            };

        private static InspectionResult Inspect(IWorkflowModel workflow, Rule rule)
        {
            var projectDir = workflow.Project?.Directory;
            if (string.IsNullOrWhiteSpace(projectDir))
                return new InspectionResult { HasErrors = false };

            var callingPath = Path.Combine(projectDir, workflow.RelativePath);
            if (!File.Exists(callingPath))
                return new InspectionResult { HasErrors = false };

            XDocument doc;
            try { doc = XDocument.Load(callingPath); }
            catch { return new InspectionResult { HasErrors = false }; }

            var messages = new List<string>();
            WalkActivities(workflow.Root, invoke =>
            {
                // Get the target file path from the "Workflow file name" argument.
                var fileNameArg = invoke.Arguments == null ? null :
                    System.Linq.Enumerable.FirstOrDefault(invoke.Arguments,
                        a => a.DisplayName == "Workflow file name");

                if (fileNameArg == null || !(fileNameArg.HasLiteralExpression ?? false))
                    return; // dynamic invocation — cannot statically resolve

                var targetRelPath = fileNameArg.DefinedExpression?.Trim('"');
                if (string.IsNullOrWhiteSpace(targetRelPath))
                    return;

                // Find the target IWorkflowModel in the project.
                // workflow.Project is IProjectSummary; Workflows requires IProjectModel.
                var projectModel = workflow.Project as IProjectModel;
                var target = projectModel?.Workflows == null ? null :
                    System.Linq.Enumerable.FirstOrDefault(projectModel.Workflows,
                        w => string.Equals(w.RelativePath, targetRelPath,
                            StringComparison.OrdinalIgnoreCase));

                if (target == null)
                    return; // target not in project (e.g. library workflow) — skip

                var expectedCount = target.Arguments?.Count ?? 0;

                // Count bound arguments in the calling workflow's XML.
                var actualCount = CountBoundArguments(doc, invoke.Id);
                if (actualCount < 0)
                    return; // activity not found in XML — skip

                if (actualCount != expectedCount)
                    messages.Add(
                        $"'{invoke.DisplayName}' invokes '{targetRelPath}' which declares " +
                        $"{expectedCount} argument(s), but {actualCount} are bound here.");
            });

            if (messages.Count == 0)
                return new InspectionResult { HasErrors = false };

            return new InspectionResult
            {
                HasErrors = true,
                RecommendationMessage = rule.RecommendationMessage,
                Messages = messages,
                ErrorLevel = rule.DefaultErrorLevel
            };
        }

        private static void WalkActivities(IActivityModel node, Action<IActivityModel> onInvoke)
        {
            if (node == null) return;

            if (node.ToolboxName == "InvokeWorkflowFile" ||
                (node.Type != null && node.Type.Contains("InvokeWorkflowFile")))
                onInvoke(node);

            foreach (var child in node.Children ?? (IEnumerable<IActivityModel>)new IActivityModel[0])
                WalkActivities(child, onInvoke);
        }

        /// <summary>
        /// Counts the data arguments bound on a specific InvokeWorkflowFile element in the XAML,
        /// identified by its sap2010:WorkflowViewState.IdRef matching <paramref name="activityId"/>.
        /// Returns -1 if the element cannot be located.
        /// </summary>
        private static int CountBoundArguments(XDocument doc, string activityId)
        {
            if (string.IsNullOrWhiteSpace(activityId))
                return -1;

            var invokeEl = System.Linq.Enumerable.FirstOrDefault(
                doc.Descendants(UiNs + "InvokeWorkflowFile"),
                el => el.Attribute(Sap2010Ns + "WorkflowViewState.IdRef")?.Value == activityId);

            if (invokeEl == null)
                return -1;

            var argsEl = invokeEl.Element(UiNs + "InvokeWorkflowFile.Arguments");
            if (argsEl == null)
                return 0;

            // Count InArgument / OutArgument / InOutArgument children.
            // The empty-bindings case uses <scg:Dictionary> which has a different local name.
            return System.Linq.Enumerable.Count(argsEl.Elements(), e =>
                e.Name.LocalName == "InArgument" ||
                e.Name.LocalName == "OutArgument" ||
                e.Name.LocalName == "InOutArgument");
        }
    }
}
