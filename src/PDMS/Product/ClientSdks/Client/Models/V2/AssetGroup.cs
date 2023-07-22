[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:PropertySummaryDocumentationMustMatchAccessors", Justification = "Swagger documentation.")]

namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.Identity;
    using Microsoft.PrivacyServices.Policy;

    using Newtonsoft.Json;

    /// <summary>
    /// Determines an asset grouping based on a single storage type. 
    /// </summary>
    public class AssetGroup : Entity
    {
        /// <summary>
        /// The compliance state of the asset group.
        /// </summary>
        [JsonProperty(PropertyName = "complianceState")]
        public ComplianceState ComplianceState { get; set; }

        /// <summary>
        /// The set of properties that are necessary to identify the collection of assets in this group from DataGrid.
        /// </summary>
        [JsonProperty(PropertyName = "qualifier")]
        [JsonConverter(typeof(AssetQualifierConverter))]
        public AssetQualifier Qualifier { get; set; }

        /// <summary>
        /// The data assets in DataGrid that are identified by this group.
        /// This must be included in an $expand parameter to retrieve these values.
        /// </summary>
        [JsonProperty(PropertyName = "dataAssets")]
        public IEnumerable<DataAsset> DataAssets { get; set; }

        /// <summary>
        /// The list of variants on this asset group.
        /// </summary>
        [JsonProperty(PropertyName = "variants")]
        public IEnumerable<AssetGroupVariant> Variants { get; set; }

        /// <summary>
        /// The ID of the data agent that contain this asset group.
        /// </summary>
        [JsonProperty(PropertyName = "deleteAgentId")]
        public string DeleteAgentId { get; set; }

        /// <summary>
        /// The ID of the export delete agent that contains this asset group.
        /// </summary>
        [JsonProperty(PropertyName = "exportAgentId")]
        public string ExportAgentId { get; set; }

        /// <summary>
        /// The ID of the pending sharing request for delete.
        /// </summary>
        [JsonProperty(PropertyName = "deleteSharingRequestId")]
        public string DeleteSharingRequestId { get; set; }

        /// <summary>
        /// The ID of the pending sharing request for export.
        /// </summary>
        [JsonProperty(PropertyName = "exportSharingRequestId")]
        public string ExportSharingRequestId { get; set; }

        /// <summary>
        /// The ID of the account close delete agent that contains this asset group.
        /// </summary>
        [JsonProperty(PropertyName = "accountCloseAgentId")]
        public string AccountCloseAgentId { get; set; }

        /// <summary>
        /// The ID of the inventory that this asset group belongs to.
        /// </summary>
        [JsonProperty(PropertyName = "inventoryId")]
        public string InventoryId { get; set; }

        /// <summary>
        /// Identifies whether or not this asset group is real time store.
        /// </summary>
        [JsonProperty(PropertyName = "isRealTimeStore")]
        public bool IsRealTimeStore { get; set; }

        /// <summary>
        /// Identifies whether or not this asset group is blocked from broader asset groups inheritance if they have variant links.
        /// </summary>
        [JsonProperty(PropertyName = "isVariantsInheritanceBlocked")]
        public bool IsVariantsInheritanceBlocked { get; set; }

        /// <summary>
        /// Identifies whether or not this asset group is blocked from broader asset groups inheritance if they have delete agent links.
        /// </summary>
        [JsonProperty(PropertyName = "isDeleteAgentInheritanceBlocked")]
        public bool IsDeleteAgentInheritanceBlocked { get; set; }

        /// <summary>
        /// Identifies whether or not this asset group is blocked from broader asset groups inheritance if they have export agent links.
        /// </summary>
        [JsonProperty(PropertyName = "isExportAgentInheritanceBlocked")]
        public bool IsExportAgentInheritanceBlocked { get; set; }

        /// <summary>
        /// Identifies whether or not this asset group is linked to any pending variant requests.
        /// </summary>
        [JsonProperty(PropertyName = "hasPendingVariantRequests")]
        public bool HasPendingVariantRequests { get; set; }

        /// <summary>
        /// The delete agent that contain this asset group. Must use $expand to retrieve this value.
        /// </summary>
        [JsonProperty(PropertyName = "deleteAgent")]
        public DeleteAgent DeleteAgent { get; set; }

        /// <summary>
        /// The export delete agent that contain this asset group. Must use $expand to retrieve this value.
        /// </summary>
        [JsonProperty(PropertyName = "exportAgent")]
        public DeleteAgent ExportAgent { get; set; }

        /// <summary>
        /// The account close delete agent that contain this asset group. Must use $expand to retrieve this value.
        /// </summary>
        [JsonProperty(PropertyName = "accountCloseAgent")]
        public DeleteAgent AccountCloseAgent { get; set; }

        /// <summary>
        /// The inventory that this asset group belongs to. Must use $expand to retrieve this value.
        /// </summary>
        [JsonProperty(PropertyName = "inventory")]
        public Inventory Inventory { get; set; }

        /// <summary>
        /// The id of the associated asset group owner.
        /// </summary>
        [JsonProperty(PropertyName = "ownerId")]
        public string OwnerId { get; set; }

        /// <summary>
        /// The associated asset group owner. Must use $expand to retrieve these.
        /// </summary>
        [JsonProperty(PropertyName = "owner")]
        public DataOwner Owner { get; set; }

        /// <summary>
        /// Indicates whether this asset group has any pending transfer request.
        /// </summary>
        [JsonProperty(PropertyName = "hasPendingTransferRequest")]
        public bool HasPendingTransferRequest { get; set; }

        /// <summary>
        /// The Id of the owner that this asset group has been requested to transfer to.
        /// </summary>
        [JsonProperty(PropertyName = "pendingTransferRequestTargetOwnerId")]
        public string PendingTransferRequestTargetOwnerId { get; set; }

        /// <summary>
        /// The name of the owner that this asset group has been requested to transfer to.
        /// </summary>
        [JsonProperty(PropertyName = "pendingTransferRequestTargetOwnerName")]
        public string PendingTransferRequestTargetOwnerName { get; set; }

        /// <summary>
        /// The optional features that associate with the asset group.
        /// </summary>
        [JsonProperty(PropertyName = "optionalFeatures")]
        public IEnumerable<OptionalFeatureId> OptionalFeatures { get; set; }
    }
}