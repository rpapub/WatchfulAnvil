// <copyright file="ProjectJsonReader.cs" company="Christian Prior-Mamulyan">
// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace WatchfulAnvil.Sdk.Common
{
    /// <summary>
    /// Reads <c>project.json</c> for project metadata not exposed by the UiPath Analyzer SDK.
    /// </summary>
    /// <remarks>
    /// UiPath project.json stores direct dependencies in a structure like:
    /// <code>
    /// {
    ///   "projectVersion": "1.0.0",
    ///   "dependencies": {
    ///     "UiPath.System.Activities": "[26.2.4]",
    ///     "SomePackage": { "version": "1.2.3" }
    ///   }
    /// }
    /// </code>
    /// All version strings are returned exactly as declared; no semver normalisation is applied.
    /// All package key matching is case-insensitive.
    /// </remarks>
    public static class ProjectJsonReader
    {
        /// <summary>
        /// Returns the declared version string for <paramref name="packageId"/> from the
        /// <c>project.json</c> at <paramref name="projectFilePath"/>.
        /// Returns <c>null</c> on any failure (missing file, parse error, key not found).
        /// </summary>
        public static string? ResolveVersion(string? projectFilePath, string? packageId)
        {
            if (string.IsNullOrEmpty(projectFilePath) || string.IsNullOrEmpty(packageId))
                return null;

            try
            {
                if (!File.Exists(projectFilePath))
                    return null;

                var json = File.ReadAllText(projectFilePath);
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("dependencies", out var deps))
                    return null;

                foreach (var dep in deps.EnumerateObject())
                {
                    if (string.Equals(dep.Name, packageId, StringComparison.OrdinalIgnoreCase))
                        return ExtractVersion(dep.Value);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Reads the <c>projectVersion</c> field from the root of <c>project.json</c>.
        /// Returns <c>null</c> on any failure.
        /// </summary>
        public static string? ReadProjectVersion(string? projectFilePath)
        {
            if (string.IsNullOrEmpty(projectFilePath))
                return null;

            try
            {
                if (!File.Exists(projectFilePath))
                    return null;

                var json = File.ReadAllText(projectFilePath);
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("projectVersion", out var ver))
                    return ver.GetString();

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Returns all dependency name-to-version pairs declared in <c>project.json</c>.
        /// Returns an empty dictionary on any failure.
        /// </summary>
        public static IReadOnlyDictionary<string, string> ReadAllDependencies(string? projectFilePath)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(projectFilePath))
                return result;

            try
            {
                if (!File.Exists(projectFilePath))
                    return result;

                var json = File.ReadAllText(projectFilePath);
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("dependencies", out var deps))
                    return result;

                foreach (var dep in deps.EnumerateObject())
                {
                    var version = ExtractVersion(dep.Value);
                    if (version != null)
                        result[dep.Name] = version;
                }
            }
            catch
            {
                // return whatever was collected
            }

            return result;
        }

        private static string? ExtractVersion(JsonElement value)
        {
            // Plain string: "[26.2.4]" or "26.2.4"
            if (value.ValueKind == JsonValueKind.String)
                return value.GetString();

            // Object: { "version": "26.2.4" }
            if (value.ValueKind == JsonValueKind.Object &&
                value.TryGetProperty("version", out var ver))
                return ver.GetString();

            return null;
        }
    }
}
