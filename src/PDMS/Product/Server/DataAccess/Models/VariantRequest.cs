namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// Defines a request to link a set of variants to a collection of asset groups.
    /// </summary>
    public class VariantRequest : Entity
    {
        /// <summary>
        /// The id of the owner pulled from the requested asset groups.
        /// </summary>
        [JsonProperty(PropertyName = "ownerId")]
        public string OwnerId { get; set; }

        /// <summary>
        /// The name of the owner for the asset groups. All asset groups must share the same owner within a single request.
        /// </summary>
        [JsonProperty(PropertyName = "ownerName")]
        public string OwnerName { get; set; }

        /// <summary>
        /// The Microsoft alias of the person requesting the linking.
        /// </summary>
        [JsonProperty(PropertyName = "requesterAlias")]
        public string RequesterAlias { get; set; }

        /// <summary>
        /// The Microsoft alias of the GeneralContractor.
        /// </summary>
        [JsonProperty(PropertyName = "generalContractorAlias")]
        public string GeneralContractorAlias { get; set; }

        /// <summary>
        /// The Microsoft alias of CELA contact.
        /// </summary>
        [JsonProperty(PropertyName = "celaContactAlias")]
        public string CelaContactAlias { get; set; }

        /// <summary>
        /// Link to the ADO WorkItem.
        /// </summary>
        [JsonProperty(PropertyName = "workItemUri")]
        public string WorkItemUri { get; set; }

        /// <summary>
        /// The set of variants that are requested to apply to the asset groups.
        /// </summary>
        [JsonProperty(PropertyName = "requestedVariants")]
        public IEnumerable<AssetGroupVariant> RequestedVariants { get; set; }

        /// <summary>
        /// The set of links that are being requested.
        /// </summary>
        [JsonProperty(PropertyName = "variantRelationships")]
        public IEnumerable<VariantRelationship> VariantRelationships { get; set; }

        /// <summary>
        /// Additional information given by the requestor.
        /// </summary>
        [JsonProperty(PropertyName = "additionalInformation")]
        public string AdditionalInformation { get; set; }

    }
}