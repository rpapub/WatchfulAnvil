using System;
using System.Diagnostics;
using System.Linq;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using UiPath.Studio.Activities.Api.PackageBindings;
using WatchfulAnvil.Sdk.Common;
using WatchfulAnvil.Sdk.Core;

namespace WatchfulAnvil.Sdk.Diagnostics
{
    /// <summary>
    /// Diagnostic tap rule that logs detailed activity-level metadata to a configurable log file.
    /// Disabled by default. Enable per-project in Workflow Analyzer settings.
    /// </summary>
    public class TapActivityRule : ActivityRule
    {
        private const string LogFileKey = "LogFile";
        private const string DefaultLogFile = @"%TEMP%\wa-tap-activity.log";

        protected override string Id => "WA-TAP-ACT-001";
        protected override string Name => "Tap Activity (Diagnostics)";
        protected override string Recommendation => "Diagnostic tap — logs activity metadata to the configured log file.";
        protected override TraceLevel DefaultSeverity => TraceLevel.Info;

        protected override void ConfigureParameters(Rule<IActivityModel> rule)
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

        protected override InspectionResult Inspect(IActivityModel activity, Rule rule)
        {
            var logFile = rule.Parameters[LogFileKey]?.Value ?? DefaultLogFile;

            LogBasicInfo(activity, logFile);
            LogArguments(activity, logFile);
            LogProperties(activity, logFile);
            LogInternalElements(activity, logFile);
            LogPackageBindings(activity, logFile);
            LogParent(activity, logFile);

            var root = FindRootContainer(activity);
            RuleLogger.Log("RootContainer", root?.DisplayName ?? "<null>", logFile);

            foreach (var prop in activity.InternalProperties ?? Enumerable.Empty<IPropertyModel>())
            {
                if (ContainsIgnoreCase(prop.DisplayName, "target"))
                    RuleLogger.Log("SelectorTargetMatch", $"{prop.DisplayName} = {prop.DefinedExpression}", logFile);
            }

            return new InspectionResult { HasErrors = false };
        }

        private static bool ContainsIgnoreCase(string source, string toCheck)
            => source?.IndexOf(toCheck, StringComparison.OrdinalIgnoreCase) >= 0;

        private static IActivityModel FindRootContainer(IActivityModel activity)
        {
            var current = activity ?? throw new ArgumentNullException(nameof(activity));
            while (current.Parent != null)
                current = current.Parent;
            return current;
        }

        private static void LogBasicInfo(IActivityModel activity, string logFile)
        {
            RuleLogger.Log("ActivityRule", activity?.DisplayName ?? "<null>", logFile);
            RuleLogger.Log("ActivityTap",
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
                $"UiPathActivityTypeId={activity?.UiPathActivityTypeId ?? "<null>"}",
                logFile);
        }

        private static void LogArguments(IActivityModel activity, string logFile)
        {
            RuleLogger.Log("ActivityArguments", RuleLogger.FormatArguments(activity?.Arguments ?? Enumerable.Empty<IArgumentModel>()), logFile);

            foreach (var arg in activity?.InternalArguments ?? Enumerable.Empty<IArgumentModel>())
                RuleLogger.Log("InternalArgument",
                    $"{arg.Direction}:{arg.DisplayName}:{arg.Type?.ToString() ?? "?"}" +
                    ((arg.HasLiteralExpression ?? false) ? $" = {arg.DefinedExpression}" : ""),
                    logFile);

            foreach (var arg in activity?.DelegateArguments ?? Enumerable.Empty<IVariableModel>())
                RuleLogger.Log("DelegateArgument",
                    $"{arg.DisplayName}:{arg.Type?.ToString() ?? "?"}" +
                    ((arg.HasLiteralExpression ?? false) ? $" = {arg.DefinedExpression}" : ""),
                    logFile);
        }

        private static void LogProperties(IActivityModel activity, string logFile)
        {
            foreach (var prop in activity?.Properties ?? Enumerable.Empty<IPropertyModel>())
                RuleLogger.Log("ActivityPropertyExpr",
                    $"DisplayName={prop.DisplayName}, Expression={(string.IsNullOrWhiteSpace(prop.DefinedExpression) ? "<unset>" : prop.DefinedExpression)}",
                    logFile);
        }

        private static void LogInternalElements(IActivityModel activity, string logFile)
        {
            foreach (var prop in activity?.InternalProperties ?? Enumerable.Empty<IPropertyModel>())
                RuleLogger.Log("InternalPropertyExpr",
                    $"DisplayName={prop.DisplayName}, Expression={(string.IsNullOrWhiteSpace(prop.DefinedExpression) ? "<unset>" : prop.DefinedExpression)}",
                    logFile);

            foreach (var reference in activity?.ObjectReferences ?? Enumerable.Empty<string>())
                RuleLogger.Log("ObjectReference", reference ?? "<null>", logFile);
        }

        private static void LogPackageBindings(IActivityModel activity, string logFile)
        {
            foreach (var binding in activity?.PackageBindings ?? Enumerable.Empty<IPackageBindingModel>())
                RuleLogger.Log("PackageBindingItem", binding?.ToString() ?? "<null>", logFile);
        }

        private static void LogParent(IActivityModel activity, string logFile)
            => RuleLogger.Log("ParentActivity", activity?.Parent?.DisplayName ?? "<null>", logFile);
    }
}
