// <copyright file="TapTargetRule.cs" company="Christian Prior-Mamulyan">
// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// </copyright>

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

using WatchfulAnvil.Sdk.Common;
using WatchfulAnvil.Sdk.Core;

namespace WatchfulAnvil.Sdk.Diagnostics
{
    /// <summary>
    /// Answers "how do I target this activity?" for a rule author.
    /// </summary>
    /// <remarks>
    /// Annotate any activity with <c>@tap</c>, run the analyzer, and this reports — at Info,
    /// against that activity — what the analyzer model actually exposes for it, plus
    /// paste-ready predicates for matching it in a rule. Add <c>@tap:dump</c> to also write
    /// the full JSONL record to the run directory.
    ///
    /// Enabled by default, unlike the other tap rules, and deliberately so: the annotation
    /// IS the opt-in. With no <c>@tap</c> anywhere the rule is silent, so there is nothing
    /// to switch off — and requiring a governance policy would defeat the point, since the
    /// developer would have to configure the tool before it could tell them anything.
    ///
    /// Writing a rule means guessing which of ToolboxName, Type or a property expression
    /// discriminates the activity you care about, and the model only tells you once you
    /// look. This is that look, taken from inside a real project rather than from prose
    /// that goes stale a release later.
    /// </remarks>
    public class TapTargetRule : ActivityRule
    {
        private const string Tag = "@tap";
        private const string DumpValue = "dump";

        protected override string Id => "CPMF-TAP-TGT-001";

        protected override string Name => "Tap Target (Diagnostics)";

        protected override string Recommendation =>
            "Diagnostic aid for rule authors. Annotate an activity with @tap to see how to target it, " +
            "or @tap:dump to also write the full record to the run directory. Remove the annotation when done.";

        protected override TraceLevel DefaultSeverity => TraceLevel.Info;

        /// <summary>Enabled by default: the <c>@tap</c> annotation is the opt-in, so it is silent without one.</summary>
        protected override bool IsEnabledByDefault => true;

        protected override string[] RequiresAnyTag => new[] { Tag };

        protected override InspectionResult Inspect(IActivityModel activity, Rule rule)
        {
            if (activity == null)
            {
                return Pass();
            }

            if (AnnotationReader.HasTagValue(activity.AnnotationText, Tag, DumpValue))
            {
                DumpRecord(activity);
            }

            return Fail(rule, BuildRecipe(activity));
        }

        private static void DumpRecord(IActivityModel activity)
        {
            try
            {
                var run = RunContext.Instance;
                JsonlWriter.AppendActivity(ActivityRecordBuilder.Build(activity, run), run, null, null);
            }
            catch (System.Exception ex)
            {
                // A diagnostic aid must never break the analysis run it is helping with.
                RuleLogger.LogAndReturn("TapTargetDumpFailed", ex.Message);
            }
        }

        private static string BuildRecipe(IActivityModel activity)
        {
            var sb = new StringBuilder();
            var toolbox = activity.ToolboxName;
            var type = activity.Type;

            sb.Append("@tap '").Append(activity.DisplayName ?? "(no display name)").Append("' — ");

            // Facts first: what the model actually holds.
            sb.Append("ToolboxName=").Append(Quote(toolbox));
            sb.Append(" Type=").Append(Quote(type));

            var parent = activity.Parent;
            if (parent != null)
            {
                sb.Append(" Parent=").Append(Quote(parent.DisplayName));
            }

            var props = SafeList(activity.Properties)
                .Where(p => p != null && !string.IsNullOrWhiteSpace(p.DefinedExpression))
                .Select(p => $"{p.DisplayName}={Truncate(p.DefinedExpression)}")
                .ToList();
            sb.Append(" Properties=").Append(props.Count == 0 ? "(none set)" : string.Join(" | ", props));

            var args = SafeList(activity.Arguments)
                .Where(a => a != null)
                .Select(a => $"{a.Direction} {a.DisplayName}:{a.Type}")
                .ToList();
            if (args.Count > 0)
            {
                sb.Append(" Arguments=").Append(string.Join(" | ", args));
            }

            // Then the paste-ready part, which is the actual question being asked.
            sb.Append("  >> Match: ");
            if (!string.IsNullOrWhiteSpace(toolbox))
            {
                sb.Append($"activity.ToolboxName == \"{toolbox}\"");
            }
            else if (!string.IsNullOrWhiteSpace(type))
            {
                sb.Append($"(activity.Type ?? \"\").StartsWith(\"{FirstSegment(type)}\")");
            }
            else
            {
                // Say so rather than emit a predicate that cannot work. Where the model is
                // blind, the answer is XamlArgumentParser, not a better guess.
                sb.Append("neither ToolboxName nor Type is exposed for this activity — "
                        + "read the XAML directly (see XamlArgumentParser)");
            }

            if (props.Count > 0)
            {
                var first = SafeList(activity.Properties)
                    .First(p => p != null && !string.IsNullOrWhiteSpace(p.DefinedExpression));
                sb.Append($"  >> Read a property: activity.Properties.FirstOrDefault(p => p.DisplayName == \"{first.DisplayName}\")?.DefinedExpression");
            }

            return sb.ToString();
        }

        private static IReadOnlyList<T> SafeList<T>(IReadOnlyCollection<T>? source)
            => source == null ? new List<T>() : source.ToList();

        private static string Quote(string? value)
            => string.IsNullOrWhiteSpace(value) ? "(null)" : "\"" + value + "\"";

        /// <summary>Namespace-qualified type minus assembly details, which are noise when matching.</summary>
        private static string FirstSegment(string type)
        {
            var comma = type.IndexOf(',');
            return comma > 0 ? type.Substring(0, comma).Trim() : type;
        }

        private static string Truncate(string? value, int max = 60)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "(empty)";
            }

            var flat = value!.Replace("\r", " ").Replace("\n", " ").Trim();
            return flat.Length <= max ? flat : flat.Substring(0, max) + "…";
        }
    }
}
