[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:PropertySummaryDocumentationMustMatchAccessors", Justification = "Swagger documentation.")]

namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.Identity;
    using Microsoft.PrivacyServices.Policy;

    using Newtonsoft.Json;

    /// <summary>
    /// Represents the relationship that is requested in a sharing request.
    /// </summary>
    public class SharingRelationship
    {
        /// <summary>
        /// The id of the asset group used in this relationship.
        /// </summary>
        [JsonProperty(PropertyName = "assetGroupId")]
        public string AssetGroupId { get; set; }

        /// <summary>
        /// The qualifier of the asset group. This is static metadata and not kept in sync by the service.
        /// However, the value is immutable on asset groups, so this should not have any impact.
        /// </summary>
        [JsonProperty(PropertyName = "assetQualifier")]
        [JsonConverter(typeof(AssetQualifierConverter))]
        public AssetQualifier AssetQualifier { get; set; }

        /// <summary>
        /// The set of capabilities to set for this relationship.
        /// </summary>
        [JsonProperty(PropertyName = "capabilities")]
        public IEnumerable<CapabilityId> Capabilities { get; set; }
    }
}