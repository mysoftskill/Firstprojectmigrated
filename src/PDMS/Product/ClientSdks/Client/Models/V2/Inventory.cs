[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:PropertySummaryDocumentationMustMatchAccessors", Justification = "Swagger documentation.")]

namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using Newtonsoft.Json;

    /// <summary>
    /// The asset group inventory which can contain arbitrary number of asset groups.
    /// </summary>
    public class Inventory : NamedEntity
    {
        /// <summary>
        /// The inventory data category.
        /// </summary>
        [JsonProperty(PropertyName = "dataCategory")]
        public DataCategory DataCategory { get; set; }

        /// <summary>
        /// The inventory retention policy.
        /// </summary>
        [JsonProperty(PropertyName = "retentionPolicy")]
        public RetentionPolicy RetentionPolicy { get; set; }

        /// <summary>
        /// The inventory retention policy detail if other is selected for retention policy.
        /// </summary>
        [JsonProperty(PropertyName = "retentionPolicyDetail")]
        public string RetentionPolicyDetail { get; set; }

        /// <summary>
        /// The inventory disposal method.
        /// </summary>
        [JsonProperty(PropertyName = "disposalMethod")]
        public DisposalMethod DisposalMethod { get; set; }

        /// <summary>
        /// The inventory disposal method detail if other is selected for disposal method.
        /// </summary>
        [JsonProperty(PropertyName = "disposalMethodDetail")]
        public string DisposalMethodDetail { get; set; }

        /// <summary>
        /// The inventory documentation type.
        /// </summary>
        [JsonProperty(PropertyName = "documentationType")]
        public DocumentationType DocumentationType { get; set; }

        /// <summary>
        /// The inventory documentation link.
        /// </summary>
        [JsonProperty(PropertyName = "documentationLink")]
        public string DocumentationLink { get; set; }

        /// <summary>
        /// The inventory third party relation.
        /// </summary>
        [JsonProperty(PropertyName = "thirdPartyRelation")]
        public ThirdPartyRelation ThirdPartyRelation { get; set; }

        /// <summary>
        /// The inventory third party details if it's not internal only.
        /// </summary>
        [JsonProperty(PropertyName = "thirdPartyDetails")]
        public string ThirdPartyDetails { get; set; }

        /// <summary>
        /// The id of the associated data owner.
        /// </summary>
        [JsonProperty(PropertyName = "ownerId")]
        public string OwnerId { get; set; }

        /// <summary>
        /// The associated data owner. Must use $expand to retrieve these.
        /// </summary>
        [JsonProperty(PropertyName = "owner")]
        public DataOwner Owner { get; set; }
    }
}