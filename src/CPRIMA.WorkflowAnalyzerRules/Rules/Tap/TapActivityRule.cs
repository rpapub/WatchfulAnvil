using System.Diagnostics;
using System.Linq;
using System;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using UiPath.Studio.Activities.Api.PackageBindings;
using WatchfulAnvil.Sdk.Common;
using WatchfulAnvil.Sdk.Core;

namespace CPRIMA.WorkflowAnalyzerRules.Rules.Tap
{
    /// <summary>
    /// Probing rule that logs detailed activity-level metadata for diagnostics and reverse engineering.
    /// This rule does not enforce constraints, but provides a snapshot of the activity structure and configuration.
    /// </summary>
    /// <remarks>
    /// Useful for understanding the structure of UiPath activities and for generating activity-level reports.
    /// </remarks>
    public class TapActivityRule : ActivityRule
    {
        protected override string Id => "CPRIMA-TAP-005";
        protected override string Name => "Tap Activity Rule";
        protected override string Recommendation => "You are in Activity mode.";
        protected override TraceLevel DefaultSeverity => TraceLevel.Info;

        /// <summary>
        /// Logs activity metadata, arguments, properties, internal elements, package bindings, and parent info.
        /// </summary>
        protected override InspectionResult Inspect(IActivityModel activity, Rule rule)
        {
            LogBasicInfo(activity);
            LogArguments(activity);
            LogProperties(activity);
            LogInternalElements(activity);
            LogPackageBindings(activity);
            LogParent(activity);

            IActivityModel root = FindRootContainer(activity);
            if (root == null)
            {
                RuleLogger.LogAndReturn("RootContainer", "<null>");
            }
            else
            {
                RuleLogger.LogAndReturn("RootContainer", root.DisplayName);
            }

            foreach (var prop in activity.InternalProperties ?? Enumerable.Empty<IPropertyModel>())
            {
                bool ContainsIgnoreCase(string source, string toCheck)
                {
                    return source?.IndexOf(toCheck, StringComparison.OrdinalIgnoreCase) >= 0;
                }

                if (ContainsIgnoreCase(prop.DisplayName, "target"))
                {
                    RuleLogger.LogAndReturn("SelectorTargetMatch", $"{prop.DisplayName} = {prop.DefinedExpression}");
                }
            }

            return new InspectionResult { HasErrors = false };
        }

        /// <summary>
        /// Traverses the activity tree to find the top-level (root) container.
        /// </summary>
        private IActivityModel FindRootContainer(IActivityModel activity)
        {
            if (activity == null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            var current = activity;
            while (current.Parent != null)
            {
                current = current.Parent;
            }

            return current;
        }

        private void LogBasicInfo(IActivityModel activity)
        {
            RuleLogger.LogAndReturn("ActivityRule", activity?.DisplayName ?? "<null>");
            RuleLogger.LogAndReturn("ActivityTap",
                $"DisplayName={activity?.DisplayName ?? "<null>"}, " +
                $"Type={activity?.GetType().Name ?? "<null>"}, " +
                $"Children={activity?.Children?.Count ?? -1}, " +
                $"Vars={activity?.Variables?.Count ?? -1}, " +
                $"Props={activity?.Properties?.Count ?? -1}, " +
                $"Args={activity?.Arguments?.Count ?? -1}, " +
                $"Toolbox={activity?.ToolboxName ?? "<null>"}, " +
                $"SupportsRefs={activity?.SupportsObjectReferences}, " +
                $"HasId={!string.IsNullOrWhiteSpace(activity?.Id)}, " +
                $"Annotation={(string.IsNullOrWhiteSpace(activity?.AnnotationText) ? "<none>" : activity.AnnotationText.Trim().Replace(Environment.NewLine, " "))}, " +
                $"UiPathActivityTypeId={activity?.UiPathActivityTypeId ?? "<null>"}");
        }

        private void LogArguments(IActivityModel activity)
        {
            RuleLogger.LogAndReturn("ActivityArguments", RuleLogger.FormatArguments(activity?.Arguments ?? Enumerable.Empty<IArgumentModel>()));

            foreach (var arg in activity?.InternalArguments ?? Enumerable.Empty<IArgumentModel>())
            {
                RuleLogger.LogAndReturn("InternalArgument",
                    $"{arg.Direction}:{arg.DisplayName}:{arg.Type?.ToString() ?? "?"}" +
                    ((arg.HasLiteralExpression ?? false) ? $" = {arg.DefinedExpression}" : ""));
            }

            foreach (var arg in activity?.DelegateArguments ?? Enumerable.Empty<IVariableModel>())
            {
                RuleLogger.LogAndReturn("DelegateArgument",
                    $"{arg.DisplayName}:{arg.Type?.ToString() ?? "?"}" +
                    ((arg.HasLiteralExpression ?? false) ? $" = {arg.DefinedExpression}" : ""));
            }
        }

        private void LogProperties(IActivityModel activity)
        {
            foreach (var prop in activity?.Properties ?? Enumerable.Empty<IPropertyModel>())
            {
                RuleLogger.LogAndReturn("ActivityPropertyExpr",
                    $"DisplayName={prop.DisplayName}, Expression={(string.IsNullOrWhiteSpace(prop.DefinedExpression) ? "<unset>" : prop.DefinedExpression)}");
            }
        }

        private void LogInternalElements(IActivityModel activity)
        {
            foreach (var prop in activity?.InternalProperties ?? Enumerable.Empty<IPropertyModel>())
            {
                RuleLogger.LogAndReturn("InternalPropertyExpr",
                    $"DisplayName={prop.DisplayName}, Expression={(string.IsNullOrWhiteSpace(prop.DefinedExpression) ? "<unset>" : prop.DefinedExpression)}");
            }

            foreach (var reference in activity?.ObjectReferences ?? Enumerable.Empty<string>())
            {
                RuleLogger.LogAndReturn("ObjectReference", reference ?? "<null>");
            }
        }

        private void LogPackageBindings(IActivityModel activity)
        {
            foreach (var binding in activity?.PackageBindings ?? Enumerable.Empty<IPackageBindingModel>())
            {
                RuleLogger.LogAndReturn("PackageBindingItem", binding?.ToString() ?? "<null>");
            }
        }

        private void LogParent(IActivityModel activity)
        {
            RuleLogger.LogAndReturn("ParentActivity", activity?.Parent?.DisplayName ?? "<null>");
        }
    }
}
