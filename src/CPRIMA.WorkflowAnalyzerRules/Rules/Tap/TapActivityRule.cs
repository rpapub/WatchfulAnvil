using System.Diagnostics;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using CPRIMA.WorkflowAnalyzerRules.LocalizationResources;
using CPRIMA.WorkflowAnalyzerRules.Common;
using System.Linq;
using System;
using UiPath.Studio.Activities.Api.PackageBindings;

namespace CPRIMA.WorkflowAnalyzerRules.Rules.Tap
{
    /// <summary>
    /// Probing rule that logs detailed activity-level metadata for diagnostics and reverse engineering.
    /// This rule does not enforce constraints, but provides a snapshot of the activity structure and configuration.
    /// </summary>
    /// <remarks>
    /// Useful for understanding the structure of UiPath activities and for generating activity-level reports.
    /// </remarks>
    public class TapActivityRule : IRegisterAnalyzerConfiguration
    {
        private const string RuleId = "CPRIMA-TAP-001";

        public void Initialize(IAnalyzerConfigurationService config) =>
            config.AddRule<IActivityModel>(Get());

        public Rule<IActivityModel> Get() =>
            new Rule<IActivityModel>("Tap Activity Rule", RuleId, InspectActivity)
            {
                // TODO: Move this recommendation message to localization resources.
                RecommendationMessage = "You are in Activity mode.",
                DefaultErrorLevel = TraceLevel.Info
            };

        /// <summary>
        /// Logs activity metadata, arguments, properties, internal elements, package bindings, and parent info.
        /// </summary>
        /// <param name="activity">The activity model.</param>
        /// <param name="_">The rule metadata (unused).</param>
        /// <returns>InspectionResult indicating if any errors were found (always false for this probing rule).</returns>
        private InspectionResult InspectActivity(IActivityModel activity, Rule _)
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
        /// Assumes <paramref name="activity"/> is non-null; throws if not.
        /// </summary>
        /// <param name="activity">The activity to start from.</param>
        /// <returns>The root activity in the hierarchy.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="activity"/> is null.</exception>
        /// <remarks>
        /// TODO: Evaluate whether this method should support nullable input for broader compatibility scenarios,
        /// especially if invoked in contexts where null activities are legitimate (e.g., defensive logging).
        /// </remarks>
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



        /// <summary>
        /// Logs basic information about the activity.
        /// </summary>
        private void LogBasicInfo(IActivityModel activity)
        {
            // TODO: Move all user-facing log messages to localization resources.
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

        /// <summary>
        /// Logs all arguments, internal arguments, and delegate arguments of the activity.
        /// </summary>
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

        /// <summary>
        /// Logs all properties of the activity.
        /// </summary>
        private void LogProperties(IActivityModel activity)
        {
            foreach (var prop in activity?.Properties ?? Enumerable.Empty<IPropertyModel>())
            {
                RuleLogger.LogAndReturn("ActivityPropertyExpr",
                    $"DisplayName={prop.DisplayName}, Expression={(string.IsNullOrWhiteSpace(prop.DefinedExpression) ? "<unset>" : prop.DefinedExpression)}");
            }
        }

        /// <summary>
        /// Logs all internal properties and object references of the activity.
        /// </summary>
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

        /// <summary>
        /// Logs all package bindings of the activity.
        /// </summary>
        private void LogPackageBindings(IActivityModel activity)
        {
            foreach (var binding in activity?.PackageBindings ?? Enumerable.Empty<IPackageBindingModel>())
            {
                RuleLogger.LogAndReturn("PackageBindingItem", binding?.ToString() ?? "<null>");
            }
        }

        /// <summary>
        /// Logs the parent activity, if any.
        /// </summary>
        private void LogParent(IActivityModel activity)
        {
            RuleLogger.LogAndReturn("ParentActivity", activity?.Parent?.DisplayName ?? "<null>");
        }

        // TODO: Move all user-facing log messages to localization resources.
        // TODO: Write and publish documentation for this rule at a public URL.
        //       Add the documentation URL to the rule metadata or as a comment here.
    }
}
