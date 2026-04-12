// <copyright file="RuleLogger.cs" company="Christian Prior-Mamulyan">
// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace WatchfulAnvil.Sdk.Common
{
    public static class RuleLogger
    {
        private static readonly string DefaultLogFile =
            Path.Combine(Path.GetTempPath(), "cpmf", "wa-tap-diagnostics", "analyzer.log");

        private static readonly object _lock = new object();

        private static string Sanitize(string s)
            => s.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");

        /// <summary>
        /// Appends a timestamped, thread-tagged entry to the specified file.
        /// The path may contain environment variable references (e.g. %TEMP%) which are expanded at runtime.
        /// Newlines in label or data are collapsed to spaces. Write failures are swallowed silently.
        /// </summary>
        public static void Log(string label, object data, string filePath)
        {
            try
            {
                var resolved = Environment.ExpandEnvironmentVariables(filePath ?? DefaultLogFile);
                var tid = Environment.CurrentManagedThreadId;
                var lbl = Sanitize(label ?? "<null>");
                var val = Sanitize(data?.ToString() ?? "<null>");
                var message = $"{DateTime.Now:HH:mm:ss.fff} [T{tid:D2}] | {lbl}: {val}{Environment.NewLine}";

                var dir = Path.GetDirectoryName(resolved);
                if (dir != null) Directory.CreateDirectory(dir);
                lock (_lock)
                {
                    File.AppendAllText(resolved, message);
                }
            }
            catch
            {
                // Diagnostic log — never crash the rule host.
            }
        }

        /// <summary>
        /// Convenience overload that writes to the default log file and returns an empty InspectionResult.
        /// </summary>
        public static InspectionResult LogAndReturn(string label, object data)
        {
            Log(label, data, DefaultLogFile);
            return new InspectionResult { HasErrors = false };
        }

        public static string FormatArguments(IEnumerable<IArgumentModel> arguments)
        {
            if (!(arguments?.Any() ?? false))
            {
                return "<no arguments>";
            }

            string SimplifyType(string rawType)
            {
                if (string.IsNullOrWhiteSpace(rawType))
                {
                    return "?";
                }

                // Handle generic types like Dictionary`2[[System.String,...],[System.Object,...]]
                if (rawType.Contains("`"))
                {
                    try
                    {
                        var start = rawType.IndexOf('[');
                        var args = rawType.Substring(start)
                                          .Split(new[] { "[[" }, StringSplitOptions.None)
                                          .Skip(1)
                                          .Select(part => part.Split(',')[0].Split('.').Last())
                                          .ToList();

                        var genericName = rawType.Split('`')[0].Split('.').Last();
                        return $"{genericName}<{string.Join(", ", args)}>";
                    }
                    catch
                    {
                        return rawType.Split('.').Last(); // fallback
                    }
                }

                return rawType.Split('.').Last(); // non-generic types
            }

            var names = string.Join(", ", arguments.Select(arg => $"{arg.Direction}:{arg.DisplayName}"));

            var details = string.Join("; ", arguments.Select(arg =>
                $"{arg.Direction}:{arg.DisplayName}:{SimplifyType(arg.Type?.ToString() ?? "")}" +
                ((arg.HasLiteralExpression ?? false) ? $" = {arg.DefinedExpression}" : "")
            ));

            return $"Names=[{names}] | Details=[{details}]";
        }
    }
}
