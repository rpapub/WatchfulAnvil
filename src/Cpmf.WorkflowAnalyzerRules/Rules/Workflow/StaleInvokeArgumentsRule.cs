// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
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
        private static readonly XNamespace XNs =
            XNamespace.Get("http://schemas.microsoft.com/winfx/2006/xaml");

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
            {
                return new InspectionResult { HasErrors = false };
            }

            // Workflows collection is on IProjectModel (subtype of IProjectSummary).
            var projectModel = workflow.Project as IProjectModel;

            var callingPath = Path.Combine(projectDir, workflow.RelativePath);
            if (!File.Exists(callingPath))
            {
                return new InspectionResult { HasErrors = false };
            }

            XDocument doc;
            try
            {
                doc = XDocument.Load(callingPath);
            }
            catch
            {
                return new InspectionResult { HasErrors = false };
            }

            var messages = new List<string>();

            foreach (var invokeEl in doc.Descendants(UiNs + "InvokeWorkflowFile"))
            {
                var targetRelPath = invokeEl.Attribute("WorkflowFileName")?.Value;
                if (string.IsNullOrWhiteSpace(targetRelPath))
                {
                    continue;
                }

                // Resolve target argument count from the project model when available,
                // falling back to reading the target XAML from disk.
                int expectedCount;
                var targetWorkflow = projectModel?.Workflows != null
                    ? FindWorkflow(projectModel.Workflows, targetRelPath)
                    : null;

                if (targetWorkflow != null)
                {
                    expectedCount = targetWorkflow.Arguments?.Count ?? 0;
                }
                else
                {
                    var targetAbsPath = Path.GetFullPath(Path.Combine(projectDir, targetRelPath));
                    if (!File.Exists(targetAbsPath))
                        continue;

                    XDocument targetDoc;
                    try { targetDoc = XDocument.Load(targetAbsPath); }
                    catch { continue; }

                    expectedCount = CountDeclaredArgumentsFromXaml(targetDoc);
                }

                var displayName = invokeEl.Attribute("DisplayName")?.Value ?? "InvokeWorkflowFile";
                var actualCount = CountBoundArguments(invokeEl);

                if (actualCount != expectedCount)
                {
                    messages.Add(
                        $"'{displayName}' invokes '{targetRelPath}' which declares " +
                        $"{expectedCount} argument(s), but {actualCount} are bound here.");
                }
            }

            if (messages.Count == 0)
            {
                return new InspectionResult { HasErrors = false };
            }

            return new InspectionResult
            {
                HasErrors = true,
                RecommendationMessage = rule.RecommendationMessage,
                Messages = messages,
                ErrorLevel = rule.DefaultErrorLevel,
            };
        }

        private static IWorkflowModel FindWorkflow(
            System.Collections.Generic.IEnumerable<IWorkflowModel> workflows,
            string relPath)
        {
            foreach (var wf in workflows)
            {
                if (string.Equals(wf.RelativePath, relPath, StringComparison.OrdinalIgnoreCase))
                    return wf;
            }
            return null;
        }

        /// <summary>Counts arguments declared in x:Members of a target XAML file (disk fallback).</summary>
        private static int CountDeclaredArgumentsFromXaml(XDocument doc)
        {
            var members = doc.Root?.Element(XNs + "Members");
            if (members == null)
                return 0;

            return System.Linq.Enumerable.Count(members.Elements(XNs + "Property"));
        }

        private static int CountBoundArguments(XElement invokeEl)
        {
            var argsEl = invokeEl.Element(UiNs + "InvokeWorkflowFile.Arguments");
            if (argsEl == null)
            {
                return 0;
            }

            return System.Linq.Enumerable.Count(argsEl.Elements(), e =>
                e.Name.LocalName == "InArgument" ||
                e.Name.LocalName == "OutArgument" ||
                e.Name.LocalName == "InOutArgument");
        }
    }
}
