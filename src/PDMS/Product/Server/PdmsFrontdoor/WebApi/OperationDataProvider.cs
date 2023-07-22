namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi
{
    using Microsoft.PrivacyServices.DataManagement.Common.Factory;
    using System;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.Serialization;

    /// <summary>
    /// The available data versions embedded in this project.
    /// </summary>
    public enum OperationDataVersion
    {
        /// <summary>
        /// Probe operation data.
        /// </summary>
        Probe,

        /// <summary>
        /// OpenAPI operation data.
        /// </summary>
        OpenApi,

        /// <summary>
        /// V2 operation data.
        /// </summary>
        V2
    }

    /// <summary>
    /// Loads the operation data from the corresponding embedded resource
    /// and provides lookup functions for the data.
    /// This class should be created as a singleton.
    /// </summary>
    public class OperationDataProvider
    {
        private readonly OperationData[] operations;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationDataProvider" /> class.
        /// Loads the data from the embedded resource.
        /// </summary>
        /// <param name="dataVersion">The data version.</param>
        public OperationDataProvider(OperationDataVersion dataVersion)
        {
            this.operations = this.LoadEmbeddedResources(this.GetEmbeddedResourceName(dataVersion));
        }

        /// <summary>
        /// Finds the corresponding operation or null if not found.
        /// </summary>
        /// <param name="httpMethod">The http method of the request.</param>
        /// <param name="pathAndQuery">The path and query of the request.</param>
        /// <returns>The operation.</returns>
        public OperationData GetFromPathAndQuery(string httpMethod, string pathAndQuery)
        {
            foreach (var operationData in this.operations)
            {
                if (operationData.HttpMethod.Equals(httpMethod) && operationData.Regex.IsMatch(pathAndQuery))
                {
                    return operationData;
                }
            }

            return null;
        }

        private string GetEmbeddedResourceName(OperationDataVersion dataVersion)
        {
            switch (dataVersion)
            {
                case OperationDataVersion.Probe:
                    return "Probe.xml";
                case OperationDataVersion.OpenApi:
                    return "OpenApi.xml";
                default:
                    return $"Operations_{dataVersion}.xml";
            }
        }

        private OperationData[] LoadEmbeddedResources(string embeddedResource)
        {
            var type = typeof(OperationDataVersion);
            embeddedResource = $"{type.Namespace}.Metadata.{embeddedResource}";

            // Load the embedded XML file as a stream
            var assembly = Assembly.GetExecutingAssembly();
            var xmlStream = assembly.GetManifestResourceStream(embeddedResource);

            // Deserialize the XML data using the XmlSerializer and secure XmlReader
            var xmlSerializer = new XmlSerializer(typeof(OperationDataCollection));
            using (var reader = SecureXmlReaderFactory.CreateReader(xmlStream))
            {
                var metadata = xmlSerializer.Deserialize(reader) as OperationDataCollection;
                return metadata.Operations;
            }
        }

        #region Serialization types
        /// <summary>
        /// A collection of operation data objects.
        /// </summary>
        [XmlType(AnonymousType = true)]
        [XmlRoot(Namespace = "", ElementName = "operations", IsNullable = false)]
        public class OperationDataCollection
        {
            /// <summary>
            /// Gets or sets the operation data values.
            /// </summary>
            [XmlElement("operation")]
            public OperationData[] Operations { get; set; }
        }

        /// <summary>
        /// The individual operation data.
        /// </summary>
        [XmlType(AnonymousType = true)]
        public class OperationData
        {
            private readonly Lazy<Regex> regex;

            /// <summary>
            /// Initializes a new instance of the <see cref="OperationData" /> class.
            /// </summary>
            public OperationData()
            {
                this.regex = new Lazy<Regex>(() => new Regex(this.Expression, RegexOptions.Compiled | RegexOptions.IgnoreCase));
            }

            /// <summary>
            /// Gets or sets the name value.
            /// </summary>
            [XmlAttribute("name")]
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets the name value.
            /// </summary>
            [XmlAttribute("method")]
            public string HttpMethod { get; set; }

            /// <summary>
            /// Gets or sets the raw regex expression string value.
            /// </summary>
            [XmlAttribute("regex")]
            public string Expression { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether or not this operation should be excluded from telemetry.
            /// </summary>
            [XmlAttribute("excludeFromTelemetry")]
            public bool ExcludeFromTelemetry { get; set; }

            /// <summary>
            /// Gets the regex object.
            /// </summary>
            public Regex Regex
            {
                get
                {
                    return this.regex.Value;
                }
            }
        }
        #endregion
    }
}