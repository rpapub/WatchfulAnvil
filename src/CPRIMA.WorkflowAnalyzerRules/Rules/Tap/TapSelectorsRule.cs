using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using CPRIMA.WorkflowAnalyzerRules.Common;
using WatchfulAnvil.Sdk.Core;

namespace CPRIMA.WorkflowAnalyzerRules.Rules.Tap
{
    public class TapSelectorsRule : ActivityRule
    {
        protected override string Id => "CPRIMA-TAP-SELECTORS-001";
        protected override string Name => "Tap Selectors Rule";
        protected override string Recommendation => "Logs all structured selector data under 'Target' for diagnostic analysis.";
        protected override TraceLevel DefaultSeverity => TraceLevel.Info;

        protected override InspectionResult Inspect(IActivityModel activity, Rule rule)
        {
            var targetProp = activity.Properties.FirstOrDefault(p => p.DisplayName == "Target");
            if (targetProp != null)
            {
                RuleLogger.LogAndReturn("TargetDetected", $"Activity '{activity.DisplayName}' contains a structured Target.");

                LogMembers("Target.Arguments", targetProp.Arguments);
                LogMembers("Target.InternalArguments", targetProp.InternalArguments);
                LogProperties("Target.Properties", targetProp.Properties);
                LogProperties("Target.InternalProperties", targetProp.InternalProperties);
            }

            return new InspectionResult { HasErrors = false };
        }

        private void LogMembers(string label, IEnumerable<IArgumentModel> args)
        {
            foreach (var arg in args ?? Enumerable.Empty<IArgumentModel>())
            {
                RuleLogger.LogAndReturn(label, $"{arg.DisplayName} => {arg.DefinedExpression ?? "<null>"}");
            }
        }

        private void LogProperties(string label, IEnumerable<IPropertyModel> props, string prefix = "")
        {
            foreach (var prop in props ?? Enumerable.Empty<IPropertyModel>())
            {
                string path = $"{prefix}{prop.DisplayName}";
                RuleLogger.LogAndReturn($"{label}", $"{path} => {prop.DefinedExpression ?? "<null>"}");

                LogProperties(label, prop.Properties, path + ".");
                LogProperties(label, prop.InternalProperties, path + ".");

                foreach (var arg in prop.Arguments ?? Enumerable.Empty<IArgumentModel>())
                {
                    RuleLogger.LogAndReturn($"{label}.Argument", $"{path}.{arg.DisplayName} => {arg.DefinedExpression ?? "<null>"}");
                }

                foreach (var arg in prop.InternalArguments ?? Enumerable.Empty<IArgumentModel>())
                {
                    RuleLogger.LogAndReturn($"{label}.InternalArgument", $"{path}.{arg.DisplayName} => {arg.DefinedExpression ?? "<null>"}");
                }
            }
        }
    }
}
