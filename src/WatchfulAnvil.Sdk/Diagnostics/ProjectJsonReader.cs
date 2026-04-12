// <copyright file="ProjectJsonReader.cs" company="Christian Prior-Mamulyan">
// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// </copyright>

using System;
using System.IO;
using System.Text.Json;

namespace WatchfulAnvil.Sdk.Diagnostics;

/// <summary>
/// Reads <c>project.json</c> to resolve the declared dependency version for a given package ID.
/// </summary>
/// <remarks>
/// UiPath project.json stores direct dependencies in a structure like:
/// <code>
/// {
///   "dependencies": {
///     "UiPath.System.Activities": { "version": "26.2.4" },
///     ...
///   }
/// }
/// </code>
/// The version string is returned exactly as declared; no semver normalisation is applied.
/// All matching is case-insensitive on the package key.
/// </remarks>
public static class ProjectJsonReader
{
    /// <summary>
    /// Returns the declared version string for <paramref name="packageId"/> from
    /// the <c>project.json</c> at <paramref name="projectFilePath"/>.
    /// Returns <c>null</c> when:
    /// <list type="bullet">
    ///   <item><paramref name="projectFilePath"/> is null or empty</item>
    ///   <item>The file does not exist</item>
    ///   <item>The file cannot be parsed as JSON</item>
    ///   <item>The <c>dependencies</c> section is absent</item>
    ///   <item>The <paramref name="packageId"/> key is not present (case-insensitive)</item>
    ///   <item>The <c>version</c> field within the entry is absent or null</item>
    /// </list>
    /// </summary>
    /// <param name="projectFilePath">Absolute path to <c>project.json</c>.</param>
    /// <param name="packageId">NuGet package ID to look up (case-insensitive).</param>
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

            // Case-insensitive key search across the dependencies object.
            foreach (var dep in deps.EnumerateObject())
            {
                if (string.Equals(dep.Name, packageId, StringComparison.OrdinalIgnoreCase))
                {
                    // The value may be an object { "version": "x.y.z" } or a plain string.
                    if (dep.Value.ValueKind == JsonValueKind.Object)
                    {
                        if (dep.Value.TryGetProperty("version", out var ver))
                            return ver.GetString();
                    }
                    else if (dep.Value.ValueKind == JsonValueKind.String)
                    {
                        return dep.Value.GetString();
                    }

                    return null;
                }
            }

            return null; // package key not found
        }
        catch
        {
            return null;
        }
    }
}
