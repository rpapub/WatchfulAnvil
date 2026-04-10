// <copyright file="XamlArgumentParser.cs" company="Christian Prior-Mamulyan">
// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace WatchfulAnvil.Sdk.Common
{
    /// <summary>
    /// SDK bypass utility for reading argument metadata from the <c>x:Members</c> section of a UiPath XAML file.
    /// Use this when <c>IArgumentModel</c> is insufficient — it does not expose <c>sap2010:Annotation.AnnotationText</c>.
    /// </summary>
    public static class XamlArgumentParser
    {
        /// <summary>
        /// Parses the <c>x:Members</c> section of a UiPath XAML file and returns argument metadata
        /// including <c>sap2010:Annotation.AnnotationText</c>, which is not exposed by <c>IArgumentModel</c> in the UiPath SDK.
        /// </summary>
        /// <param name="xamlPath">The full file path to the XAML file.</param>
        /// <returns>
        /// An enumerable of <see cref="XamlArgumentInfo"/> containing the name, type, and annotation
        /// for each argument found in the <c>x:Members</c> section.
        /// </returns>
        public static IEnumerable<XamlArgumentInfo> ParseArguments(string xamlPath)
        {
            var xdoc = XDocument.Load(xamlPath);
            XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
            XNamespace sap2010 = "http://schemas.microsoft.com/netfx/2010/xaml/activities/presentation";

            return xdoc.Descendants(x + "Members")
                       .Descendants(x + "Property")
                       .Select(prop => new XamlArgumentInfo(
                           prop.Attribute("Name")?.Value ?? "<unknown>",
                           prop.Attribute("Type")?.Value ?? "<unknown>",
                           prop.Attribute(sap2010 + "Annotation.AnnotationText")?.Value ?? ""
                       ));
        }
    }
}
