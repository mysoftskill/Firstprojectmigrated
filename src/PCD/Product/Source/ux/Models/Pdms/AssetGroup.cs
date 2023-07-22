using System.Collections.Generic;
using Microsoft.PrivacyServices.Policy;

namespace Microsoft.PrivacyServices.UX.Models.Pdms
{
    /// <summary>
    /// Represents asset group.
    /// </summary>
    public class AssetGroup
    {
        /// <summary>
        /// Gets or sets data agent ID.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The id of the associated delete agent for this asset group.
        /// </summary>
        public string DeleteAgentId { get; set; }

        /// <summary>
        /// The id of the associated export agent for this asset group.
        /// </summary>
        public string ExportAgentId { get; set; }

        /// <summary>
        /// The id of the associated asset group owner.
        /// </summary>
        public string OwnerId { get; set; }

        /// <summary>
        /// Gets or sets boolean for if the asset group has pending variant requests.
        /// </summary>
        public bool HasPendingVariantRequests { get; set; }

        /// <summary>
        /// Identifies whether or not this asset group is blocked from broader asset
        /// groups inheritance if they have export agent links.
        /// </summary>
        public bool IsExportAgentInheritanceBlocked { get; set; }

        /// <summary>
        /// Identifies whether or not this asset group is blocked from broader asset
        /// groups inheritance if they have delete agent links.
        /// </summary>
        public bool IsDeleteAgentInheritanceBlocked { get; set; }

        /// <summary>
        /// Identifies whether or not this asset group is blocked from broader asset
        /// groups inheritance if they have variant links.
        /// </summary>
        public bool IsVariantsInheritanceBlocked { get; set; }

        /// <summary>
        /// The set of properties that are necessary to identify the collection of assets in this group from DataGrid.
        /// </summary>
        public AssetGroupQualifier Qualifier { get; set; }

        /// <summary>
        /// The list of variants on this asset group.
        /// </summary>
        public IEnumerable<AssetGroupVariant> Variants { get; set; }

        /// <summary>
        /// ETag
        /// </summary>
        public string ETag { get; set; }

        /// <summary>
        /// Request Id for shared data agent for delete
        /// </summary>        
        public string DeleteSharingRequestId { get; set; }

        /// <summary>
        /// Request Id for shared data agent for export
        /// </summary>        
        public string ExportSharingRequestId { get; set; }

        /// <summary>
        /// Gets or sets boolean for if the asset group has pending transfer requests.
        /// </summary>
        public bool HasPendingTransferRequest { get; set; }

        /// <summary>
        /// The Id of the owner that this asset group has been requested to transfer to.
        /// </summary>
        public string PendingTransferRequestTargetOwnerId { get; set; }

        /// <summary>
        /// The name of the owner that this asset group has been requested to transfer to.
        /// </summary>
        public string PendingTransferRequestTargetOwnerName { get; set; }

        /// <summary>
        /// Gets or sets the optional features that associate with the asset group.
        /// </summary>
        public IEnumerable<OptionalFeatureId> OptionalFeatures { get; set; }
    }
}
