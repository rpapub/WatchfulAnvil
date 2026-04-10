// <copyright file="TapSelectorsRule.cs" company="Christian Prior-Mamulyan">
// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// </copyright>

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

using WatchfulAnvil.Sdk.Common;
using WatchfulAnvil.Sdk.Core;

namespace WatchfulAnvil.Sdk.Diagnostics
{
    /// <summary>
    /// Diagnostic tap rule that logs structured selector data for all activities containing a Target property.
    /// Disabled by default. Enable per-project in Workflow Analyzer settings.
    /// </summary>
    public class TapSelectorsRule : ActivityRule
    {
        private const string LogFileKey = "LogFile";
        private const string DefaultLogFile = @"%TEMP%\wa-tap-selectors.log";

        protected override string Id => "WA-TAP-SEL-001";

        protected override string Name => "Tap Selectors (Diagnostics)";

        protected override string Recommendation => "Diagnostic tap — logs selector/Target data to the configured log file.";

        protected override TraceLevel DefaultSeverity => TraceLevel.Info;

        protected override bool IsEnabledByDefault => false;

        protected override void ConfigureParameters(Rule<IActivityModel> rule)
        {
            rule.Parameters.Add(LogFileKey, new Parameter
            {
                Key = LogFileKey,
                DefaultValue = DefaultLogFile,
                Value = DefaultLogFile,
                LocalizedDisplayName = "Log file path",
            });
        }

        protected override InspectionResult Inspect(IActivityModel activity, Rule rule)
        {
            var logFile = rule.Parameters[LogFileKey]?.Value ?? DefaultLogFile;

            var targetProp = activity.Properties.FirstOrDefault(p => p.DisplayName == "Target");
            if (targetProp != null)
            {
                RuleLogger.Log("TargetDetected", $"Activity '{activity.DisplayName}' contains a structured Target.", logFile);
                LogMembers("Target.Arguments", targetProp.Arguments, logFile);
                LogMembers("Target.InternalArguments", targetProp.InternalArguments, logFile);
                LogProperties("Target.Properties", targetProp.Properties, logFile);
                LogProperties("Target.InternalProperties", targetProp.InternalProperties, logFile);
            }

            return new InspectionResult { HasErrors = false };
        }

        private static void LogMembers(string label, IEnumerable<IArgumentModel> args, string logFile)
        {
            foreach (var arg in args ?? Enumerable.Empty<IArgumentModel>())
            {
                RuleLogger.Log(label, $"{arg.DisplayName} => {arg.DefinedExpression ?? "<null>"}", logFile);
            }
        }

        private static void LogProperties(string label, IEnumerable<IPropertyModel> props, string logFile, string prefix = "")
        {
            foreach (var prop in props ?? Enumerable.Empty<IPropertyModel>())
            {
                var path = $"{prefix}{prop.DisplayName}";
                RuleLogger.Log(label, $"{path} => {prop.DefinedExpression ?? "<null>"}", logFile);

                LogProperties(label, prop.Properties, logFile, path + ".");
                LogProperties(label, prop.InternalProperties, logFile, path + ".");

                foreach (var arg in prop.Arguments ?? Enumerable.Empty<IArgumentModel>())
                {
                    RuleLogger.Log($"{label}.Argument", $"{path}.{arg.DisplayName} => {arg.DefinedExpression ?? "<null>"}", logFile);
                }

                foreach (var arg in prop.InternalArguments ?? Enumerable.Empty<IArgumentModel>())
                {
                    RuleLogger.Log($"{label}.InternalArgument", $"{path}.{arg.DisplayName} => {arg.DefinedExpression ?? "<null>"}", logFile);
                }
            }
        }
    }
}
