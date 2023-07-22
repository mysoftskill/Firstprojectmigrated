namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi
{
    using Microsoft.PrivacyServices.DataManagement.Common.Factory;
    using System;
    using System.IO;
    using System.Xml;
    using System.Xml.Serialization;

    /// <summary>
    /// Loads the operation access data from the corresponding AllowedList file
    /// and provides lookup functions for the data given partner id.
    /// This class should be created as a singleton.
    /// </summary>
    public class OperationAccessProvider : IOperationAccessProvider
    {
        private readonly Lazy<AccessList> accessList;

        public const string AccessListFileName = @"WebApi\Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.AccessList.config";

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationAccessProvider" /> class.
        /// Loads the data from the xml resource file.
        /// </summary>
        public OperationAccessProvider() : this(AccessListFileName)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationAccessProvider" /> class.
        /// Loads the data from the xml resource file.
        /// Enables overriding the access list file for testing
        /// </summary>
        /// <param name="sourceFilePath">Source config file path.</param>
        public OperationAccessProvider(string sourceFilePath)
        {
            this.accessList = new Lazy<AccessList>(() => this.LoadResources(sourceFilePath));
        }

        /// <summary>
        /// Finds the corresponding operation access permission or null if not found.
        /// </summary>
        /// <param name="applicationId">The application ID of the request.</param>
        /// <returns>The operation access permission.</returns>
        public OperationAccessPermission GetAccessPermissions(string applicationId)
        {
            if (this.accessList.Value.PartnerPermissions == null)
            {
                return null;
            }

            foreach (var partnerPermission in this.accessList.Value.PartnerPermissions)
            {
                if (partnerPermission.Id.Equals(applicationId))
                {
                    return new OperationAccessPermission
                    {
                        ApplicationId = partnerPermission.Id,
                        FriendlyName = partnerPermission.Name,
                        AllowedOperations = partnerPermission.Apis
                    };
                }
            }

            return null;
        }

        private AccessList LoadResources(string resource)
        {
            var xmlStream = File.OpenRead(resource);
            // Deserialize the XML data using the XmlSerializer and secure XmlReader
            var xmlSerializer = new XmlSerializer(typeof(AccessList));
            using (var xmlReader = SecureXmlReaderFactory.CreateReader(xmlStream))
            {
                return xmlSerializer.Deserialize(xmlReader) as AccessList;
            }
        }

        #region Serialization types
        /// <summary>
        /// A collection of operation access data objects.
        /// </summary>
        [XmlType(AnonymousType = true)]
        [XmlRoot(Namespace = "", ElementName = "accessList", IsNullable = false)]
        public class AccessList
        {
            /// <summary>
            /// Gets or sets the operation access data values.
            /// </summary>
            [XmlElement("partner")]
            public Partner[] PartnerPermissions { get; set; }
        }

        /// <summary>
        /// The individual operation access data.
        /// </summary>
        [XmlType(AnonymousType = true)]
        public class Partner
        {
            /// <summary>
            /// Gets or sets the partner id value.
            /// </summary>
            [XmlAttribute("id")]
            public string Id { get; set; }

            /// <summary>
            /// Gets or sets the name value.
            /// </summary>
            [XmlAttribute("name")]
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets the API array value.
            /// </summary>
            [XmlArray("apis")]
            [XmlArrayItem("api")]
            public string[] Apis { get; set; }
        }
        #endregion
    }
}