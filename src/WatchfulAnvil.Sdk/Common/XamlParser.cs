using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace WatchfulAnvil.Sdk.Common
{
    /// <summary>
    /// Provides utility methods for parsing XAML files used in UiPath and Workflow Foundation workflows.
    /// </summary>
    public static class XamlParser
    {
        /// <summary>
        /// Parses the arguments defined in the x:Members section of a XAML file.
        /// </summary>
        /// <param name="xamlPath">The full file path to the XAML file.</param>
        /// <returns>
        /// An enumerable of <see cref="XamlArgumentInfo"/> containing the name, type, and annotation
        /// for each argument found in the XAML file.
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

    /// <summary>
    /// Represents information about an argument defined in a XAML file.
    /// </summary>
    public class XamlArgumentInfo
    {
        public string Name { get; }
        public string Type { get; }
        public string Annotation { get; }

        public XamlArgumentInfo(string name, string type, string annotation)
        {
            Name = name;
            Type = type;
            Annotation = annotation;
        }
    }
}
