using System.Collections.Generic;
using System.Diagnostics;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using WatchfulAnvil.Sdk.Core;

namespace Cpmf.Rules.Workflow
{
    public class LogMessageBookendsRule : WorkflowRule
    {
        private const string StartPrefixKey = "StartPrefix";
        private const string EndPrefixKey = "EndPrefix";
        private const string DefaultStartPrefix = "Going to";
        private const string DefaultEndPrefix = "Finished";

        protected override string Id => "CPMF-WFL-001";
        protected override string Name => "Log Message Bookends";
        protected override TraceLevel DefaultSeverity => TraceLevel.Warning;
        protected override string? RequiredFeature => DesignFeatureKeys.WorkflowAnalyzerV9;
        protected override string Recommendation =>
            "Workflows annotated @module or @unit must begin with a Log Message whose " +
            "Message starts with the configured StartPrefix (default: \"Going to\") and " +
            "end with a Log Message whose Message starts with the configured EndPrefix (default: \"Finished\").";
        protected override string? DocumentationLink =>
            "https://github.com/rpapub/WatchfulAnvil/wiki/Rule-Documentation-CPMF-WFL-001";

        protected override void ConfigureParameters(Rule<IWorkflowModel> rule)
        {
            rule.Parameters.Add(StartPrefixKey, new Parameter
            {
                Key = StartPrefixKey,
                DefaultValue = DefaultStartPrefix,
                Value = DefaultStartPrefix,
                LocalizedDisplayName = "Start log message prefix"
            });
            rule.Parameters.Add(EndPrefixKey, new Parameter
            {
                Key = EndPrefixKey,
                DefaultValue = DefaultEndPrefix,
                Value = DefaultEndPrefix,
                LocalizedDisplayName = "End log message prefix"
            });
        }

        protected override InspectionResult Inspect(IWorkflowModel workflow, Rule rule)
        {
            if (workflow.Root == null)
                return new InspectionResult { HasErrors = false };

            var annotation = workflow.Root.AnnotationText;
            if (string.IsNullOrWhiteSpace(annotation))
                return new InspectionResult { HasErrors = false };

            var isModule = annotation.Contains("@module");
            var isUnit = annotation.Contains("@unit");
            if (!isModule && !isUnit)
                return new InspectionResult { HasErrors = false };

            var startPrefix = GetParameterValue(rule, StartPrefixKey, DefaultStartPrefix);
            var endPrefix = GetParameterValue(rule, EndPrefixKey, DefaultEndPrefix);

            var children = new List<IActivityModel>(
                workflow.Root.Children ?? (IEnumerable<IActivityModel>)new IActivityModel[0]);

            if (children.Count == 0)
                return new InspectionResult
                {
                    HasErrors = true,
                    RecommendationMessage = rule.RecommendationMessage,
                    Messages = new List<string> { "Workflow has no activities." },
                    ErrorLevel = rule.DefaultErrorLevel
                };

            var messages = new List<string>();
            var first = children[0];
            var last = children[children.Count - 1];

            CheckBookend(first, startPrefix, "first", messages);
            if (children.Count > 1)
                CheckBookend(last, endPrefix, "last", messages);
            else if (!MessageStartsWith(first, startPrefix))
                messages.Add(
                    $"The single activity is not a Log Message whose Message starts with \"{startPrefix}\".");

            if (messages.Count > 0)
                return new InspectionResult
                {
                    HasErrors = true,
                    RecommendationMessage = rule.RecommendationMessage,
                    Messages = messages,
                    ErrorLevel = rule.DefaultErrorLevel
                };

            return new InspectionResult { HasErrors = false };
        }

        private static string GetParameterValue(Rule rule, string key, string fallback)
        {
            var raw = rule.Parameters[key]?.Value;
            return string.IsNullOrWhiteSpace(raw) ? fallback : raw;
        }

        private static void CheckBookend(
            IActivityModel activity, string requiredPrefix, string position, List<string> messages)
        {
            if (!IsLogMessage(activity))
            {
                messages.Add(
                    $"The {position} activity ('{activity.DisplayName}') is not a Log Message. " +
                    $"Expected a Log Message whose Message starts with \"{requiredPrefix}\".");
                return;
            }

            if (!MessageStartsWith(activity, requiredPrefix))
            {
                var actual = GetMessageExpression(activity) ?? "<empty>";
                messages.Add(
                    $"The {position} Log Message ('{activity.DisplayName}') has Message = {actual}, " +
                    $"which does not start with \"{requiredPrefix}\".");
            }
        }

        private static bool IsLogMessage(IActivityModel activity)
            => activity.ToolboxName == "Log Message";

        private static string? GetMessageExpression(IActivityModel activity)
        {
            if (activity.Properties == null)
                return null;
            var messageProp = System.Linq.Enumerable.FirstOrDefault(
                activity.Properties, p => p.DisplayName == "Message");
            return messageProp?.DefinedExpression;
        }

        private static bool MessageStartsWith(IActivityModel activity, string prefix)
        {
            var expr = GetMessageExpression(activity);
            if (expr == null)
                return false;
            var value = expr.TrimStart('"');
            return value.StartsWith(prefix);
        }
    }
}
