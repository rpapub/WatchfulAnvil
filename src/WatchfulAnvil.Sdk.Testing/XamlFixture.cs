// <copyright file="XamlFixture.cs" company="Christian Prior-Mamulyan">
// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WatchfulAnvil.Sdk.Testing
{
    /// <summary>
    /// Writes a throwaway workflow <c>.xaml</c> carrying an <c>x:Members</c> section, so
    /// rules that parse argument metadata out of the raw file can be tested.
    /// </summary>
    /// <remarks>
    /// This exists because <c>IArgumentModel</c> does not expose
    /// <c>sap2010:Annotation.AnnotationText</c> — argument annotations are invisible to the
    /// analyzer API entirely, which is why <c>XamlArgumentParser</c> reads the file itself.
    /// A mock cannot stand in for that; the parser needs real XAML at a real path.
    ///
    /// Dispose deletes the directory. Use with <c>using</c>.
    /// </remarks>
    public sealed class XamlFixture : IDisposable
    {
        private XamlFixture(string directory, string path)
        {
            Directory = directory;
            Path = path;
        }

        /// <summary>Directory holding the file.</summary>
        public string Directory { get; }

        /// <summary>Absolute path to the written <c>.xaml</c>.</summary>
        public string Path { get; }

        /// <summary>Writes the given XAML verbatim, for malformed or unusual content.</summary>
        public static XamlFixture FromXaml(string xaml, string fileName = "Main.xaml")
        {
            var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "wa-test-" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(dir);
            var path = System.IO.Path.Combine(dir, fileName);
            File.WriteAllText(path, xaml, Encoding.UTF8);
            return new XamlFixture(dir, path);
        }

        /// <summary>
        /// Writes a workflow declaring the given arguments in <c>x:Members</c>.
        /// </summary>
        /// <param name="arguments">
        /// Each argument's name, type and optional annotation. Type is the XAML form, e.g.
        /// <c>InArgument(x:String)</c>.
        /// </param>
        public static XamlFixture WithArguments(
            IEnumerable<XamlArgument> arguments,
            string rootAnnotation = null,
            string fileName = "Main.xaml")
        {
            var sb = new StringBuilder();
            sb.AppendLine("<Activity mc:Ignorable=\"sap sap2010\" x:Class=\"Main\"");
            sb.AppendLine("  xmlns=\"http://schemas.microsoft.com/netfx/2009/xaml/activities\"");
            sb.AppendLine("  xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\"");
            sb.AppendLine("  xmlns:sap=\"http://schemas.microsoft.com/netfx/2009/xaml/activities/presentation\"");
            sb.AppendLine("  xmlns:sap2010=\"http://schemas.microsoft.com/netfx/2010/xaml/activities/presentation\"");
            sb.AppendLine("  xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">");
            sb.AppendLine("  <x:Members>");

            foreach (var arg in arguments ?? new List<XamlArgument>())
            {
                sb.Append("    <x:Property");
                if (arg.Annotation != null)
                {
                    sb.Append($" sap2010:Annotation.AnnotationText={Attr(arg.Annotation)}");
                }

                sb.AppendLine($" Name={Attr(arg.Name)} Type={Attr(arg.Type)} />");
            }

            sb.AppendLine("  </x:Members>");
            sb.Append("  <Sequence DisplayName=\"Main\"");
            if (rootAnnotation != null)
            {
                sb.Append($" sap2010:Annotation.AnnotationText={Attr(rootAnnotation)}");
            }

            sb.AppendLine(" />");
            sb.AppendLine("</Activity>");

            return FromXaml(sb.ToString(), fileName);
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

        private static string Attr(string value)
            => "\"" + value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("\r", "&#xD;")
                .Replace("\n", "&#xA;") + "\"";

        /// <summary>One <c>x:Members</c> entry: an argument name, its XAML type, and an optional annotation.</summary>
        public sealed class XamlArgument
        {
            public XamlArgument(string name, string type = "InArgument(x:String)", string annotation = null)
            {
                Name = name;
                Type = type;
                Annotation = annotation;
            }

            public string Name { get; }

            public string Type { get; }

            /// <summary>Annotation text, e.g. <c>@suppress:RULE-ID</c>. Invisible to IArgumentModel.</summary>
            public string Annotation { get; }
        }
    }
}
