namespace WatchfulAnvil.Sdk.Common
{
    /// <summary>
    /// Represents an argument defined in a XAML <c>x:Members</c> section,
    /// including its annotation text as stored by UiPath Studio.
    /// </summary>
    public class XamlArgumentInfo
    {
        /// <summary>Initializes a new instance of <see cref="XamlArgumentInfo"/>.</summary>
        public XamlArgumentInfo(string name, string type, string annotation)
        {
            Name = name;
            Type = type;
            Annotation = annotation;
        }

        /// <summary>Gets the argument name.</summary>
        public string Name { get; }

        /// <summary>Gets the argument type.</summary>
        public string Type { get; }

        /// <summary>Gets the annotation text from <c>sap2010:Annotation.AnnotationText</c>.</summary>
        public string Annotation { get; }
    }
}
