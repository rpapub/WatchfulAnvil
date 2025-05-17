using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace CPRIMA.WorkflowAnalyzerRules.Common
{
    public static class RuleLogger
    {
        public static InspectionResult LogAndReturn(string label, object data)
        {
            var safeData = data?.ToString() ?? "<null>";
            var message = $"{DateTime.Now:HH:mm:ss} | {label}: {safeData}{Environment.NewLine}";
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "analyzer_log.txt");

            using (var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(message);
            }

            return new InspectionResult { HasErrors = false };
        }
        public static string FormatArguments(IEnumerable<IArgumentModel> arguments)
        {
            if (!(arguments?.Any() ?? false))
                return "<no arguments>";

            string SimplifyType(string rawType)
            {
                if (string.IsNullOrWhiteSpace(rawType))
                    return "?";

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
