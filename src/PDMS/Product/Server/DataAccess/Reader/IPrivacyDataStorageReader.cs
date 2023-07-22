namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Reader
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;

    /// <summary>
    /// Interface for read operations of PDMS entities.
    /// </summary>
    public interface IPrivacyDataStorageReader
    {
        /// <summary>
        /// Get the data owner for a given id. Returns null if not found.
        /// </summary>
        /// <param name="dataOwnerId">Id of the data owner.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <param name="includeServiceTree">Whether or not service trees should be retrieved.</param>
        /// <returns>Data owner for the given id.</returns>
        Task<DataOwner> GetDataOwnerAsync(Guid dataOwnerId, bool includeTrackingDetails, bool includeServiceTree);

        /// <summary>
        /// Get all data owners based on filter criteria.
        /// </summary>
        /// <param name="filterCriteria">Filter criteria for data owner.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <param name="includeServiceTree">Whether or not service tree should be retrieved.</param>
        /// <returns>Data owners matching the filter criteria.</returns>
        Task<FilterResult<DataOwner>> GetDataOwnersAsync(IFilterCriteria<DataOwner> filterCriteria, bool includeTrackingDetails, bool includeServiceTree);

        /// <summary>
        /// Get all data owners based on a list of owner ids.
        /// </summary>
        /// <param name="ownerIds">The set of owner ids to retrieve.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <param name="includeServiceTree">Whether or not service tree should be retrieved.</param>
        /// <returns>Data owners matching the given owner ids.</returns>
        Task<IEnumerable<DataOwner>> GetDataOwnersAsync(IEnumerable<Guid> ownerIds, bool includeTrackingDetails, bool includeServiceTree);

        /// <summary>
        /// Get all data owners based on a list of security groups.
        /// </summary>
        /// <param name="securityGroups">The set of security groups to retrieve.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <param name="includeServiceTree">Whether or not service tree should be retrieved.</param>
        /// <returns>Data owners matching the given security groups.</returns>
        Task<IEnumerable<DataOwner>> GetDataOwnersBySecurityGroupsAsync(IEnumerable<Guid> securityGroups, bool includeTrackingDetails, bool includeServiceTree);

        /// <summary>
        /// Get all data owners who have the given user alias as a service admin.
        /// </summary>
        /// <param name="userAlias">The user alias to search for.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <param name="includeServiceTree">Whether or not service tree should be retrieved.</param>
        /// <returns>Data owners matching the given user alias.</returns>
        Task<IEnumerable<DataOwner>> GetDataOwnersByServiceAdminAsync(string userAlias, bool includeTrackingDetails, bool includeServiceTree);

        /// <summary>
        /// Get the data agent for a given id. Returns null if not found.
        /// </summary>
        /// <param name="dataAgentId">Id of the data agent.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <returns>Data agent for the given id.</returns>
        Task<DataAgent> GetDataAgentAsync(Guid dataAgentId, bool includeTrackingDetails);

        /// <summary>
        /// Get all data agents based on filter criteria.
        /// </summary>
        /// <typeparam name="TDataAgent">The data agent type.</typeparam>
        /// <param name="filterCriteria">Filter criteria for data agent.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <returns>Data agents matching the filter criteria.</returns>
        Task<FilterResult<TDataAgent>> GetDataAgentsAsync<TDataAgent>(IFilterCriteria<TDataAgent> filterCriteria, bool includeTrackingDetails) where TDataAgent : DataAgent;

        /// <summary>
        /// Get all data agents based on a list of agent ids.
        /// </summary>
        /// <typeparam name="TDataAgent">The data agent type.</typeparam>
        /// <param name="agentIds">The set of agent ids to retrieve.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <returns>Data agents matching the given agent ids.</returns>
        Task<IEnumerable<TDataAgent>> GetDataAgentsAsync<TDataAgent>(IEnumerable<Guid> agentIds, bool includeTrackingDetails) where TDataAgent : DataAgent;

        /// <summary>
        /// Get the asset group for a given id. Returns null if not found.
        /// </summary>
        /// <param name="assetGroupId">Id of the asset group.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <returns>Asset group for the given id.</returns>
        Task<AssetGroup> GetAssetGroupAsync(Guid assetGroupId, bool includeTrackingDetails);

        /// <summary>
        /// Get all asset groups based on filter criteria.
        /// </summary>
        /// <param name="filterCriteria">Filter criteria for asset group.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <returns>Asset groups matching the filter criteria.</returns>
        Task<FilterResult<AssetGroup>> GetAssetGroupsAsync(IFilterCriteria<AssetGroup> filterCriteria, bool includeTrackingDetails);

        /// <summary>
        /// Get all asset groups based on a list of asset group ids.
        /// </summary>
        /// <param name="assetGroupIds">The set of asset group ids to retrieve.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <returns>Asset groups matching the given asset group ids.</returns>
        Task<IEnumerable<AssetGroup>> GetAssetGroupsAsync(IEnumerable<Guid> assetGroupIds, bool includeTrackingDetails);

        /// <summary>
        /// Get the variant definition for a given id. Returns null if not found.
        /// </summary>
        /// <param name="variantDefinitionId">Id of the variant definition.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <returns>Variant definition for the given id.</returns>
        Task<VariantDefinition> GetVariantDefinitionAsync(Guid variantDefinitionId, bool includeTrackingDetails);

        /// <summary>
        /// Get all variant definitions based on filter criteria.
        /// </summary>
        /// <param name="filterCriteria">Filter criteria for variant definition.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <returns>Variant definitions matching the filter criteria.</returns>
        Task<FilterResult<VariantDefinition>> GetVariantDefinitionsAsync(IFilterCriteria<VariantDefinition> filterCriteria, bool includeTrackingDetails);

        /// <summary>
        /// Get all variant definitions based on a list of variant definition ids.
        /// </summary>
        /// <param name="variantDefinitionIds">The set of variant definition ids to retrieve.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <returns>Variant definitions matching the given variant definition ids.</returns>
        Task<IEnumerable<VariantDefinition>> GetVariantDefinitionsAsync(IEnumerable<Guid> variantDefinitionIds, bool includeTrackingDetails);

        /// <summary>
        /// Get all history items based on filter criteria.
        /// </summary>
        /// <param name="filterCriteria">Filter criteria for history item.</param>
        /// <returns>History items matching the filter criteria.</returns>
        Task<FilterResult<HistoryItem>> GetHistoryItemsAsync(IFilterCriteria<HistoryItem> filterCriteria);

        /// <summary>
        /// Determines if there are any other entities linked to the data agent entity.
        /// </summary>
        /// <param name="dataAgentId">Id of the data agent.</param>
        /// <returns>True if the data agent entity is linked to any other entities, False otherwise.</returns>
        Task<bool> IsDataAgentLinkedToAnyOtherEntities(Guid dataAgentId);

        /// <summary>
        /// Determines if there are any pending commands linked to the data agent entity.
        /// </summary>
        /// <param name="dataAgentId">Id of the data agent.</param>
        /// <returns>True if the data agent entity has pending commands, False otherwise.</returns>
        Task<bool> DataAgentHasPendingCommands(Guid dataAgentId);

        /// <summary>
        /// Determines if there are any other entities linked to the data owner entity.
        /// </summary>
        /// <param name="dataOwnerId">Id of the data owner.</param>
        /// <returns>True if the data owner entity is linked to any other entities, False otherwise.</returns>
        Task<bool> IsDataOwnerLinkedToAnyOtherEntities(Guid dataOwnerId);

        /// <summary>
        /// Get the sharing request for a given id. Returns null if not found.
        /// </summary>
        /// <param name="sharingRequestId">Id of the sharing request.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <returns>Sharing request for the given id.</returns>
        Task<SharingRequest> GetSharingRequestAsync(Guid sharingRequestId, bool includeTrackingDetails);

        /// <summary>
        /// Get all sharing requests based on filter criteria.
        /// </summary>
        /// <param name="filterCriteria">Filter criteria for sharing request.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <returns>Sharing requests matching the filter criteria.</returns>
        Task<FilterResult<SharingRequest>> GetSharingRequestsAsync(IFilterCriteria<SharingRequest> filterCriteria, bool includeTrackingDetails);

        /// <summary>
        /// Get all sharing requests based on a list of sharing request ids.
        /// </summary>
        /// <param name="sharingRequestIds">The set of sharing request ids to retrieve.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <returns>Sharing requests matching the given view source ids.</returns>
        Task<IEnumerable<SharingRequest>> GetSharingRequestsAsync(IEnumerable<Guid> sharingRequestIds, bool includeTrackingDetails);

        /// <summary>
        /// Get the variant request for a given id. Returns null if not found.
        /// </summary>
        /// <param name="variantRequestId">Id of the variant request.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <returns>Variant request for the given id.</returns>
        Task<VariantRequest> GetVariantRequestAsync(Guid variantRequestId, bool includeTrackingDetails);

        /// <summary>
        /// Get all variant requests based on filter criteria.
        /// </summary>
        /// <param name="filterCriteria">Filter criteria for variant request.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <returns>Variant requests matching the filter criteria.</returns>
        Task<FilterResult<VariantRequest>> GetVariantRequestsAsync(IFilterCriteria<VariantRequest> filterCriteria, bool includeTrackingDetails);

        /// <summary>
        /// Get all variant requests based on a list of variant request ids.
        /// </summary>
        /// <param name="variantRequestIds">The set of variant request ids to retrieve.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <returns>Variant requests matching the given view source ids.</returns>
        Task<IEnumerable<VariantRequest>> GetVariantRequestsAsync(IEnumerable<Guid> variantRequestIds, bool includeTrackingDetails);

        /// <summary>
        /// Get the transfer request for a given id. Returns null if not found.
        /// </summary>
        /// <param name="transferRequestId">Id of the transfer request.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <returns>Transfer request for the given id.</returns>
        Task<TransferRequest> GetTransferRequestAsync(Guid transferRequestId, bool includeTrackingDetails);

        /// <summary>
        /// Get all transfer requests based on filter criteria.
        /// </summary>
        /// <param name="filterCriteria">Filter criteria for transfer request.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <returns>Transfer requests matching the filter criteria.</returns>
        Task<FilterResult<TransferRequest>> GetTransferRequestsAsync(IFilterCriteria<TransferRequest> filterCriteria, bool includeTrackingDetails);

        /// <summary>
        /// Get all transfer requests based on a list of variant request ids.
        /// </summary>
        /// <param name="transferRequestIds">The set of transfer request ids to retrieve.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <returns>Transfer requests matching the given view source ids.</returns>
        Task<IEnumerable<TransferRequest>> GetTransferRequestsAsync(IEnumerable<Guid> transferRequestIds, bool includeTrackingDetails);

        /// <summary>
        /// Determines if there are any other entities linked to the give variant definition.
        /// </summary>
        /// <param name="variantId">Id of the variant definition.</param>
        /// <returns>True if the variant definition entity is linked to any other entities, False otherwise.</returns>
        Task<bool> IsVariantDefinitionLinkedToAnyOtherEntities(Guid variantId);

        /// <summary>
        /// Get all AssetGroup linked to the given VariantDefinition.
        /// </summary>
        /// <param name="variantId">The variant definition id.</param>
        /// <returns>All linked AssetGroup.</returns>
        Task<IEnumerable<AssetGroup>> GetLinkedAssetGroups(Guid variantId);
    }
}
