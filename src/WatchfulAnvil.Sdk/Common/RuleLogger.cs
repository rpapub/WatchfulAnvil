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
            Path.Combine(Path.GetTempPath(), "analyzer_log.txt");

        /// <summary>
        /// Appends a timestamped entry to the specified file.
        /// The path may contain environment variable references (e.g. %TEMP%) which are expanded at runtime.
        /// </summary>
        public static void Log(string label, object data, string filePath)
        {
            var resolved = Environment.ExpandEnvironmentVariables(filePath ?? DefaultLogFile);
            var message = $"{DateTime.Now:HH:mm:ss} | {label}: {data?.ToString() ?? "<null>"}{Environment.NewLine}";

            using (var stream = new FileStream(resolved, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(message);
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
