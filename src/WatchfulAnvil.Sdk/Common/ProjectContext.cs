// <copyright file="ProjectContext.cs" company="Christian Prior-Mamulyan">
// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// </copyright>

using System;
using System.Collections.Generic;

using UiPath.Studio.Analyzer.Models;

namespace WatchfulAnvil.Sdk.Common
{
    /// <summary>
    /// Unified project metadata for rule authors.
    /// Combines properties available on <see cref="IProjectSummary"/> with fields that the
    /// UiPath Analyzer SDK does not expose (project version, dependency versions), which are
    /// read directly from <c>project.json</c>.
    /// </summary>
    /// <remarks>
    /// Construct via <see cref="From"/>. All project.json reads are performed once at
    /// construction time; repeated property access is cheap.
    /// </remarks>
    public sealed class ProjectContext
    {
        private readonly IReadOnlyDictionary<string, string> _dependencies;

        private ProjectContext(
            string name,
            string? outputType,
            string? profileType,
            string? version,
            string? expressionLanguage,
            string filePath,
            IReadOnlyDictionary<string, string> dependencies)
        {
            Name = name;
            OutputType = outputType;
            ProfileType = profileType;
            Version = version;
            ExpressionLanguage = expressionLanguage;
            FilePath = filePath;
            _dependencies = dependencies;
        }

        // ── SDK-sourced properties ────────────────────────────────────────────

        /// <summary>Project display name (<c>IInspectionObject.DisplayName</c>).</summary>
        public string Name { get; }

        /// <summary>
        /// Project output type: <c>"Process"</c>, <c>"Library"</c>, <c>"Tests"</c>, or <c>"Framework"</c>.
        /// <c>null</c> if not available.
        /// </summary>
        public string? OutputType { get; }

        /// <summary>Project profile type (e.g. <c>"Developement"</c>). <c>null</c> if not available.</summary>
        public string? ProfileType { get; }

        /// <summary>Expression language: <c>"VisualBasic"</c> or <c>"CSharp"</c>.</summary>
        public string? ExpressionLanguage { get; }

        /// <summary>Absolute path to <c>project.json</c>.</summary>
        public string FilePath { get; }

        // ── project.json-sourced properties ──────────────────────────────────

        /// <summary>
        /// Version declared in <c>project.json</c> as <c>projectVersion</c>.
        /// <c>null</c> if the field is absent or the file cannot be read.
        /// </summary>
        public string? Version { get; }

        /// <summary>
        /// All NuGet dependencies and their declared version strings, keyed case-insensitively.
        /// Values are returned as declared (e.g. <c>"[26.2.4]"</c> or <c>"26.2.4"</c>).
        /// Empty when <c>project.json</c> cannot be read.
        /// </summary>
        public IReadOnlyDictionary<string, string> Dependencies => _dependencies;

        // ── Convenience predicates ────────────────────────────────────────────

        /// <summary><c>true</c> when <see cref="OutputType"/> is <c>"Library"</c> (case-insensitive).</summary>
        public bool IsLibrary =>
            "Library".Equals(OutputType, StringComparison.OrdinalIgnoreCase);

        /// <summary><c>true</c> when <see cref="OutputType"/> is <c>"Process"</c> (case-insensitive).</summary>
        public bool IsProcess =>
            "Process".Equals(OutputType, StringComparison.OrdinalIgnoreCase);

        // ── Factory ───────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a <see cref="ProjectContext"/> from <paramref name="summary"/>.
        /// Reads <c>project.json</c> for fields not available on the model.
        /// </summary>
        public static ProjectContext From(IProjectSummary summary)
        {
            if (summary == null) throw new ArgumentNullException(nameof(summary));

            var filePath = summary.ProjectFilePath ?? string.Empty;
            return new ProjectContext(
                name: summary.DisplayName ?? string.Empty,
                outputType: summary.ProjectOutputType,
                profileType: summary.ProjectProfileType,
                version: ProjectJsonReader.ReadProjectVersion(filePath),
                expressionLanguage: summary.ExpressionLanguage,
                filePath: filePath,
                dependencies: ProjectJsonReader.ReadAllDependencies(filePath));
        }

        // ── Lookup ────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the declared version string for <paramref name="packageId"/>, or <c>null</c>
        /// if the package is not a direct dependency. Matching is case-insensitive.
        /// </summary>
        public string? GetDependencyVersion(string packageId)
        {
            if (string.IsNullOrEmpty(packageId))
                return null;
            return _dependencies.TryGetValue(packageId, out var v) ? v : null;
        }
    }
}
