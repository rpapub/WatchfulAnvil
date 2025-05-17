using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

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

        // TODO: Support parsing arguments from other possible XAML structures (not just x:Members).
        // TODO: Make namespace handling more flexible (detect or allow override for custom XAML namespaces).

        return xdoc.Descendants(x + "Members")
                   .Descendants(x + "Property")
                   .Select(prop => new XamlArgumentInfo(
                       prop.Attribute("Name")?.Value ?? "<unknown>",
                       prop.Attribute("Type")?.Value ?? "<unknown>",
                       prop.Attribute(sap2010 + "Annotation.AnnotationText")?.Value ?? ""
                   ));
    }

    // TODO: Add method to parse variables (look for <Variable> elements in the XAML).
    // public static IEnumerable<XamlVariableInfo> ParseVariables(string xamlPath) { ... }

    // TODO: Add method to parse activities (look for root activity and child activities).
    // public static IEnumerable<XamlActivityInfo> ParseActivities(string xamlPath) { ... }

    // TODO: Add method to extract workflow metadata (e.g., display name, annotations, version).
    // public static XamlWorkflowMetadata ParseWorkflowMetadata(string xamlPath) { ... }

    // TODO: Add overloads to accept XDocument or XElement for in-memory parsing and testing.
    // public static IEnumerable<XamlArgumentInfo> ParseArguments(XDocument xdoc) { ... }
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
