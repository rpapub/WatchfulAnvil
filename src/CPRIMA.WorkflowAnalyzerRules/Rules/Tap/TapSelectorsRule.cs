using System.Diagnostics;
using System.Linq;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using System.Collections.Generic;
using CPRIMA.WorkflowAnalyzerRules.Common;

namespace CPRIMA.WorkflowAnalyzerRules.Rules.Tap
{
    public class TapSelectorsRule : IRegisterAnalyzerConfiguration
    {
        private const string RuleId = "CPRIMA-TAP-SELECTORS-001";

        public void Initialize(IAnalyzerConfigurationService config) =>
            config.AddRule<IActivityModel>(Get());

        public Rule<IActivityModel> Get() =>
            new Rule<IActivityModel>("Tap Selectors Rule", RuleId, InspectActivity)
            {
                RecommendationMessage = "Logs all structured selector data under 'Target' for diagnostic analysis.",
                DefaultErrorLevel = TraceLevel.Info
            };

        private InspectionResult InspectActivity(IActivityModel activity, Rule rule)
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

                // Dive deeper into nested structures
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
