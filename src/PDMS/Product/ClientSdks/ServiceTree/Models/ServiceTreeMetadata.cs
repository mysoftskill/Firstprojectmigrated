using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PrivacyServices.DataManagement.Client.ServiceTree.Models
{

    /// <summary>
    /// The model for servicetree metadata. There are more properties in service tree than what is listed below.
    /// </summary>
    public class ServiceTreeMetadata
    {
        /// <summary>
        /// Gets or sets the service id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the service name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the service metadata.
        /// </summary>
        public ServiceTreeMetadataValue Value { get; set; }
    }

    public class ServiceTreeMetadataValue
    {
        /// <summary>
        /// Gets or sets the NGPPowerBIUrl.
        /// </summary>
        [JsonProperty("NGP_PowerbI_URL", NamingStrategyType = typeof(DefaultNamingStrategy))]
        public string NGPPowerBIUrl { get; set; }


        /// <summary>
        /// Gets or sets the PrivacyComplianceDashboard.
        /// </summary>
        [JsonProperty("Privacy_Compliance_Dashboard", NamingStrategyType = typeof(DefaultNamingStrategy)    )]
        public string PrivacyComplianceDashboard { get; set; }

    }

    public class ServiceTreeMetadataGetResults
    {
        /// <summary>
        /// Gets or sets the Metadata Id in Get result.
        /// </summary>
        [JsonProperty("value", NamingStrategyType = typeof(DefaultNamingStrategy))]
        public IEnumerable<ServiceMetadata> Values { get; set; }


    }

    public class ServiceMetadataPostBody
    {
        /// <summary>
        /// Gets or sets the ServiceMetadata in Get body.
        /// </summary>
        [JsonProperty("ServiceMetadata", NamingStrategyType = typeof(DefaultNamingStrategy))]
        public ServiceMetadata ServiceMetadata { get; set; }
    }

    public class ServiceMetadata
    {
        /// <summary>
        /// Gets or sets the Metadata Id in Get result.
        /// </summary>
        [JsonProperty("Id", NamingStrategyType = typeof(DefaultNamingStrategy))]
        public string MetadataId { get; set; }

        /// <summary>
        /// Gets or sets the AzureCloud in Get result.
        /// </summary>
        [JsonProperty("AzureCloud", NamingStrategyType = typeof(DefaultNamingStrategy))]
        public string AzureCloud { get; set; }

        /// <summary>
        /// Gets or sets the ServiceHierarchyId in Get result.
        /// </summary>
        [JsonProperty("ServiceHierarchyId", NamingStrategyType = typeof(DefaultNamingStrategy))]
        public string ServiceHierarchyId { get; set; }

        /// <summary>
        /// Gets or sets the MetadataDefinitionId in Get result.
        /// </summary>
        [JsonProperty("MetadataDefinitionId", NamingStrategyType = typeof(DefaultNamingStrategy))]
        public string MetadataDefinitionId { get; set; }

        // <summary>
        /// Gets or sets the NGP_PowerbI_URL in Get result.
        /// </summary>
        [JsonProperty("NGP_PowerbI_URL", NamingStrategyType = typeof(DefaultNamingStrategy))]
        public string NGP_PowerBI_URL { get; set; }

        // <summary>
        /// Gets or sets the Privacy_Compliance_Dashboard in Get result.
        /// </summary>
        [JsonProperty("Privacy_Compliance_Dashboard", NamingStrategyType = typeof(DefaultNamingStrategy))]
        public string Privacy_Compliance_Dashboard { get; set; }


        // <summary>
        /// Gets or sets the EntityState in Get result.
        /// </summary>
        [JsonProperty("EntityState", NamingStrategyType = typeof(DefaultNamingStrategy))]
        public string EntityState { get; set; }
    }
}
