namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2
{
    using System.Collections.Generic;
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
        public string AssetQualifier { get; set; }

        /// <summary>
        /// The set of capabilities to set for this relationship.
        /// </summary>
        [JsonProperty(PropertyName = "capabilities")]
        public IEnumerable<string> Capabilities { get; set; }
    }
}