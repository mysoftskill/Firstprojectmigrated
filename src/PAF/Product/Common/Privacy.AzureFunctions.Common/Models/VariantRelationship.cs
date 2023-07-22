namespace Microsoft.PrivacyServices.AzureFunctions.Common.Models
{
    using Newtonsoft.Json;

    /// <summary>
    /// Represents the relationship that is requested in a variant request.
    /// </summary>
    public class VariantRelationship
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
    }
}
