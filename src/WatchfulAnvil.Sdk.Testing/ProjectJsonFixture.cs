// <copyright file="ProjectJsonFixture.cs" company="Christian Prior-Mamulyan">
// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WatchfulAnvil.Sdk.Testing
{
    /// <summary>
    /// Writes a throwaway <c>project.json</c> on disk so rules that read project metadata
    /// can be tested.
    /// </summary>
    /// <remarks>
    /// The analyzer API does not expose project version, declared dependencies or output
    /// type, so <c>ProjectContext</c> reads them straight from <c>project.json</c>. Testing
    /// anything built on that needs a real file at a real path — hence a fixture rather
    /// than a mock.
    ///
    /// Dispose deletes the directory. Use with <c>using</c>, or the temp directory leaks.
    /// </remarks>
    public sealed class ProjectJsonFixture : IDisposable
    {
        private ProjectJsonFixture(string directory, string path)
        {
            Directory = directory;
            Path = path;
        }

        /// <summary>Directory holding the file. Hand this to a fake project's <c>ProjectFilePath</c> parent.</summary>
        public string Directory { get; }

        /// <summary>Absolute path to the written <c>project.json</c>.</summary>
        public string Path { get; }

        /// <summary>Writes the given JSON verbatim, for malformed or unusual content.</summary>
        public static ProjectJsonFixture FromJson(string json)
        {
            var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "wa-test-" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(dir);
            var path = System.IO.Path.Combine(dir, "project.json");
            File.WriteAllText(path, json, Encoding.UTF8);
            return new ProjectJsonFixture(dir, path);
        }

        /// <summary>
        /// Writes a minimal but well-formed <c>project.json</c>.
        /// </summary>
        /// <param name="dependencies">
        /// Package id to declared version, written verbatim — pass the bracketed form
        /// (<c>"[1.2.3]"</c>) when the exact-pin shape matters, since that is how UiPath
        /// writes pinned dependencies and callers may parse it.
        /// </param>
        public static ProjectJsonFixture Create(
            string name = "TestProject",
            string projectVersion = "1.0.0",
            string description = null,
            string outputType = "Process",
            string expressionLanguage = "VisualBasic",
            IReadOnlyDictionary<string, string> dependencies = null)
        {
            var deps = dependencies ?? new Dictionary<string, string>();
            var depJson = string.Join(
                ",\n",
                deps.Select(kv => $"    {Quote(kv.Key)}: {Quote(kv.Value)}"));

            var json =
                "{\n" +
                $"  \"name\": {Quote(name)},\n" +
                $"  \"description\": {Quote(description)},\n" +
                $"  \"projectVersion\": {Quote(projectVersion)},\n" +
                "  \"dependencies\": {" + (depJson.Length > 0 ? "\n" + depJson + "\n  " : string.Empty) + "},\n" +
                $"  \"expressionLanguage\": {Quote(expressionLanguage)},\n" +
                "  \"designOptions\": {\n" +
                $"    \"outputType\": {Quote(outputType)},\n" +
                "    \"projectProfile\": \"Developement\"\n" +
                "  },\n" +
                "  \"schemaVersion\": \"4.0\",\n" +
                "  \"targetFramework\": \"Windows\"\n" +
                "}\n";

            return FromJson(json);
        }

        public void Dispose()
        {
            try
            {
                if (System.IO.Directory.Exists(Directory))
                {
                    System.IO.Directory.Delete(Directory, recursive: true);
                }
            }
            catch (IOException)
            {
                // A leaked temp directory is not worth failing a test over.
            }
        }

        private static string Quote(string value)
            => value == null ? "null" : "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }
}
