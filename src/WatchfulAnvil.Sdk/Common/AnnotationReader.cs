// <copyright file="AnnotationReader.cs" company="Christian Prior-Mamulyan">
// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;

using UiPath.Studio.Analyzer.Models;

namespace WatchfulAnvil.Sdk.Common
{
    /// <summary>
    /// Parses <c>@tag</c> and <c>@tag:VALUE</c> directives from UiPath activity/workflow annotation text.
    /// Tokenizes on whitespace and newlines; all tag name and value matching is case-insensitive.
    /// </summary>
    /// <remarks>
    /// Annotation sources:
    /// <list type="bullet">
    ///   <item><c>IActivityModel.AnnotationText</c> — activity-level annotations (requires WorkflowAnalyzerV9 feature gate)</item>
    ///   <item><c>IWorkflowModel.Root.AnnotationText</c> — workflow-wide annotations on the root Sequence/Flowchart</item>
    ///   <item><c>XamlArgumentInfo.Annotation</c> — argument annotations from x:Members (not exposed by IArgumentModel; requires XamlArgumentParser)</item>
    /// </list>
    /// </remarks>
    public static class AnnotationReader
    {
        // ── Core string-level methods ──────────────────────────────────────────

        /// <summary>True if the annotation contains <c>@suppress:RULE-ID</c> for the given rule ID.</summary>
        public static bool IsSuppressed(string? annotation, string ruleId)
            => HasTagValue(annotation, "@suppress", ruleId);

        /// <summary>True if the annotation contains <c>@nocheck</c> (suppresses all rules).</summary>
        public static bool IsNoCheck(string? annotation)
            => HasTag(annotation, "@nocheck");

        /// <summary>True if the annotation contains <c>@violates:RULE-ID</c> for the given rule ID.
        /// Used to mark intentional violations in a rule-development test corpus.</summary>
        public static bool IsViolates(string? annotation, string ruleId)
            => HasTagValue(annotation, "@violates", ruleId);

        /// <summary>True if the annotation contains the given tag (e.g. <c>@unit</c>, <c>@nocheck</c>).</summary>
        public static bool HasTag(string? annotation, string tag)
            => Tokenize(annotation).Any(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// True if the annotation carries <c>@tag:VALUE</c> for the given value, checking
        /// <b>every</b> occurrence of the tag rather than only the first.
        /// </summary>
        /// <remarks>
        /// <see cref="GetTagValue"/> returns the first match and stops, so testing a value
        /// through it silently ignores every later occurrence: given
        /// <c>@suppress:RULE-A @suppress:RULE-B</c>, only RULE-A was ever detected and
        /// RULE-B was unreachable. Annotations carrying more than one directive of the same
        /// kind are the normal case, so the value test scans all of them.
        /// Accepts both <c>@tag:VALUE</c> and the space-separated <c>@tag: VALUE</c> form,
        /// matching <see cref="GetTagValue"/>.
        /// </remarks>
        public static bool HasTagValue(string? annotation, string tag, string value)
        {
            var prefix = tag.TrimEnd(':') + ":";
            var tokens = Tokenize(annotation).ToList();
            for (var i = 0; i < tokens.Count; i++)
            {
                if (!tokens[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var candidate = tokens[i].Substring(prefix.Length);
                if (candidate.Length == 0 && i + 1 < tokens.Count)
                {
                    candidate = tokens[i + 1];
                }

                if (candidate.Equals(value, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the value after the colon for <c>@tag:VALUE</c>, or <c>null</c> if the tag is absent or has no value.
        /// Returns the <b>first</b> occurrence only; use <see cref="HasTagValue"/> to test a specific value.
        /// </summary>
        public static string? GetTagValue(string? annotation, string tag)
        {
            var prefix = tag.TrimEnd(':') + ":";
            var tokens = Tokenize(annotation).ToList();
            for (var i = 0; i < tokens.Count; i++)
            {
                if (tokens[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    var value = tokens[i].Substring(prefix.Length);
                    if (value.Length == 0 && i + 1 < tokens.Count)
                    {
                        return tokens[i + 1];
                    }

                    return value;
                }
            }

            return null;
        }

        // ── Model-aware convenience overloads ──────────────────────────────────

        /// <summary>True if the activity's annotation contains <c>@suppress:RULE-ID</c>.</summary>
        public static bool IsSuppressed(IActivityModel? activity, string ruleId)
            => IsSuppressed(activity?.AnnotationText, ruleId);

        /// <summary>True if the workflow root annotation contains <c>@suppress:RULE-ID</c>.</summary>
        public static bool IsSuppressed(IWorkflowModel? workflow, string ruleId)
            => IsSuppressed(workflow?.Root?.AnnotationText, ruleId);

        /// <summary>True if the XAML argument annotation contains <c>@suppress:RULE-ID</c>.</summary>
        public static bool IsSuppressed(XamlArgumentInfo? arg, string ruleId)
            => IsSuppressed(arg?.Annotation, ruleId);

        /// <summary>True if the activity's annotation contains <c>@nocheck</c>.</summary>
        public static bool IsNoCheck(IActivityModel? activity)
            => IsNoCheck(activity?.AnnotationText);

        /// <summary>True if the workflow root annotation contains <c>@nocheck</c>.</summary>
        public static bool IsNoCheck(IWorkflowModel? workflow)
            => IsNoCheck(workflow?.Root?.AnnotationText);

        // ── Internal ──────────────────────────────────────────────────────────
        private static IEnumerable<string> Tokenize(string? annotation)
            => annotation?.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
               ?? Enumerable.Empty<string>();
    }
}
