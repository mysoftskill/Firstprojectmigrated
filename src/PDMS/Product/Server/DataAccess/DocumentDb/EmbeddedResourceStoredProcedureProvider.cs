namespace Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb
{
    using Microsoft.PrivacyServices.DataManagement.Common.Factory;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Serialization;

    /// <summary>
    /// Retrieves the stored procedure information from the embedded resources of an assembly.
    /// </summary>
    public class EmbeddedResourceStoredProcedureProvider : IStoredProcedureProvider
    {
        private readonly string embeddedResourceQualifiedName;
        private readonly string installationXmlFileName;
        private readonly Assembly resourceAssembly;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmbeddedResourceStoredProcedureProvider" /> class.
        /// </summary>
        /// <param name="embeddedResourceQualifiedName">The fully qualified prefix for the resource names.</param>
        /// <param name="installationXmlFileName">The non-qualified name of the installation xml file.</param>
        /// <param name="resourceAssembly">The assembly that contains the embedded resources.</param>
        public EmbeddedResourceStoredProcedureProvider(
            string embeddedResourceQualifiedName,
            string installationXmlFileName,
            Assembly resourceAssembly)
        {
            this.embeddedResourceQualifiedName = embeddedResourceQualifiedName;
            this.installationXmlFileName = installationXmlFileName;
            this.resourceAssembly = resourceAssembly;
        }

        /// <summary>
        /// Retrieves the stored procedure data for database initialization.
        /// </summary>
        /// <returns>The stored procedure information.</returns>
        public IEnumerable<StoredProcedure> GetStoredProcedures()
        {
            var installationData = this.LoadXml<StoredProcedures>(this.installationXmlFileName);

            return installationData.Sprocs.Select(s => new StoredProcedure
            {
                Name = Path.GetFileNameWithoutExtension(s.Name),
                Action = s.Action,
                Value = s.Action == StoredProcedure.Actions.Install ? this.LoadFile(s.Name) : null
            });
        }

        private string GetFileName(string embeddedResource)
        {
            return $"{this.embeddedResourceQualifiedName}.{embeddedResource}";
        }

        private Stream LoadEmbeddedResources(string embeddedResource)
        {
            embeddedResource = this.GetFileName(embeddedResource);
            return this.resourceAssembly.GetManifestResourceStream(embeddedResource);
        }

        private T LoadXml<T>(string embeddedResource) where T : class
        {
            using (var resourceStream = this.LoadEmbeddedResources(embeddedResource))
            {
                // Deserialize the XML data using the XmlSerializer and secure XmlReader
                var xmlSerializer = new XmlSerializer(typeof(T));
                using (var reader = SecureXmlReaderFactory.CreateReader(resourceStream))
                {
                    return xmlSerializer.Deserialize(reader) as T;
                }
            }
        }

        private string LoadFile(string embeddedResource)
        {
            using (var streamReader = new StreamReader(this.LoadEmbeddedResources(embeddedResource)))
            {
                return streamReader.ReadToEnd();
            }
        }

        #region Serialization types
        /// <summary>
        /// A collection of operation data objects.
        /// </summary>
        [XmlType(AnonymousType = true)]
        [XmlRoot(Namespace = "", ElementName = "storedProcedures", IsNullable = false)]
        public class StoredProcedures
        {
            /// <summary>
            /// Gets or sets the operation data values.
            /// </summary>
            [XmlElement("sproc")]
            public Sproc[] Sprocs { get; set; }
        }

        /// <summary>
        /// The individual stored procedure data.
        /// </summary>
        [XmlType(AnonymousType = true)]
        public class Sproc
        {
            /// <summary>
            /// Gets or sets the name value.
            /// </summary>
            [XmlAttribute("name")]
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets the name value.
            /// </summary>
            [XmlAttribute("action")]
            public StoredProcedure.Actions Action { get; set; }
        }
        #endregion
    }
}