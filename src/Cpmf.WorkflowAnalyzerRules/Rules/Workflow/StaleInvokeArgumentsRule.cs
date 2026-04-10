using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using WatchfulAnvil.Sdk.Core;

namespace Cpmf.Rules.Workflow
{
    public class StaleInvokeArgumentsRule : WorkflowRule
    {
        private static readonly XNamespace UiNs =
            XNamespace.Get("http://schemas.uipath.com/workflow/activities");
        private static readonly XNamespace Sap2010Ns =
            XNamespace.Get("http://schemas.microsoft.com/netfx/2010/xaml/activities/presentation");

        protected override string Id => "CPMF-U004";
        protected override string Name => "InvokeWorkflowFile Argument Count Mismatch";
        protected override string? RequiredFeature => DesignFeatureKeys.WorkflowAnalyzerV9;
        protected override string Recommendation =>
            "The number of arguments bound on an Invoke Workflow File activity does not match " +
            "the number declared in the target workflow. Update the argument mappings.";
        protected override string? DocumentationLink =>
            "https://github.com/rpapub/WatchfulAnvil/wiki/Rule-Documentation-CPMF-U004";

        protected override InspectionResult Inspect(IWorkflowModel workflow, Rule rule)
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
                var fileNameArg = invoke.Arguments == null ? null :
                    System.Linq.Enumerable.FirstOrDefault(invoke.Arguments,
                        a => a.DisplayName == "Workflow file name");

                if (fileNameArg == null || !(fileNameArg.HasLiteralExpression ?? false))
                    return;

                var targetRelPath = fileNameArg.DefinedExpression?.Trim('"');
                if (string.IsNullOrWhiteSpace(targetRelPath))
                    return;

                var projectModel = workflow.Project as IProjectModel;
                var target = projectModel?.Workflows == null ? null :
                    System.Linq.Enumerable.FirstOrDefault(projectModel.Workflows,
                        w => string.Equals(w.RelativePath, targetRelPath,
                            StringComparison.OrdinalIgnoreCase));

                if (target == null)
                    return;

                var expectedCount = target.Arguments?.Count ?? 0;
                var actualCount = CountBoundArguments(doc, invoke.Id);
                if (actualCount < 0)
                    return;

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

            return System.Linq.Enumerable.Count(argsEl.Elements(), e =>
                e.Name.LocalName == "InArgument" ||
                e.Name.LocalName == "OutArgument" ||
                e.Name.LocalName == "InOutArgument");
        }
    }
}
