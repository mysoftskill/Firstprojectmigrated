namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Writer
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Models.V2;

    /// <summary>
    /// Interface for create/update/delete operations of PDMS entities.
    /// </summary>
    public interface IPrivacyDataStorageWriter
    {
        /// <summary>
        /// Update a set of entities in bulk.
        /// </summary>
        /// <param name="entities">The set of entities to update.</param>
        /// <returns>The updated entities.</returns>
        Task<IEnumerable<Entity>> UpdateEntitiesAsync(IEnumerable<Entity> entities);

        /// <summary>
        /// Create a data owner.
        /// </summary>
        /// <param name="dataOwner">The data owner to be persisted.</param>
        /// <returns>The data owner that was stored.</returns>
        Task<DataOwner> CreateDataOwnerAsync(DataOwner dataOwner);

        /// <summary>
        /// Update a data owner.
        /// </summary>
        /// <param name="dataOwner">The data owner to update.</param>
        /// <returns>The data owner that was stored.</returns>
        Task<DataOwner> UpdateDataOwnerAsync(DataOwner dataOwner);

        /// <summary>
        /// Create a asset group.
        /// </summary>
        /// <param name="assetGroup">The asset group to be persisted.</param>
        /// <returns>The data that was stored.</returns>
        Task<AssetGroup> CreateAssetGroupAsync(AssetGroup assetGroup);

        /// <summary>
        /// Update a asset group. Fails if the ETag does not match.
        /// </summary>
        /// <param name="assetGroup">The asset group with updated properties.</param>
        /// <returns>The data that was stored.</returns>
        Task<AssetGroup> UpdateAssetGroupAsync(AssetGroup assetGroup);

        /// <summary>
        /// Create or replace an asset group and additionally create or replace any entities that were changed as a side effect
        /// of the write action. Returns the modified asset group.
        /// </summary>
        /// <param name="assetGroup">The asset group with updated properties.</param>
        /// <param name="writeAction">The specific write action: create/update.</param>
        /// <param name="additionalEntities">Any additional entities that need to be created or replaced.</param>
        /// <returns>The data that was stored.</returns>
        Task<AssetGroup> UpsertAssetGroupWithSideEffectsAsync(AssetGroup assetGroup, WriteAction writeAction, IEnumerable<Entity> additionalEntities);

        /// <summary>
        /// Create a variant definition.
        /// </summary>
        /// <param name="variantDefinition">The variant definition to be persisted.</param>
        /// <returns>The data that was stored.</returns>
        Task<VariantDefinition> CreateVariantDefinitionAsync(VariantDefinition variantDefinition);

        /// <summary>
        /// Update a variant definition. Fails if the ETag does not match.
        /// </summary>
        /// <param name="variantDefinition">The variant definition with updated properties.</param>
        /// <returns>The data that was stored.</returns>
        Task<VariantDefinition> UpdateVariantDefinitionAsync(VariantDefinition variantDefinition);

        /// <summary>
        /// Create a data agent.
        /// </summary>
        /// <typeparam name="TDataAgent">The data agent type.</typeparam>
        /// <param name="dataAgent">The data agent to be persisted.</param>
        /// <returns>The data that was stored.</returns>
        Task<TDataAgent> CreateDataAgentAsync<TDataAgent>(TDataAgent dataAgent) where TDataAgent : DataAgent;

        /// <summary>
        /// Update a data agent. Fails if the ETag does not match.
        /// </summary>
        /// <typeparam name="TDataAgent">The data agent type.</typeparam>
        /// <param name="dataAgent">The data agent with updated properties.</param>
        /// <returns>The data that was stored.</returns>
        Task<TDataAgent> UpdateDataAgentAsync<TDataAgent>(TDataAgent dataAgent) where TDataAgent : DataAgent;

        /// <summary>
        /// Create a variant request.
        /// </summary>
        /// <param name="variantRequest">The variant request to be persisted.</param>
        /// <returns>The variant request that was stored.</returns>
        Task<VariantRequest> CreateVariantRequestAsync(VariantRequest variantRequest);

        /// <summary>
        /// Update a variant request.
        /// </summary>
        /// <param name="variantRequest">The variant request to update.</param>
        /// <returns>The variant request that was stored.</returns>
        Task<VariantRequest> UpdateVariantRequestAsync(VariantRequest variantRequest);

        /// <summary>
        /// Create or replace a variant request and additionally create or replace any entities that were changed as a side effect
        /// of the write action. Returns the modified variant request.
        /// </summary>
        /// <param name="variantRequest">The variant request with updated properties.</param>
        /// <param name="additionalEntities">Any additional entities that need to be created or replaced.</param>
        /// <returns>The data that was stored.</returns>
        Task<VariantRequest> UpsertVariantRequestWithSideEffectsAsync(VariantRequest variantRequest, IEnumerable<Entity> additionalEntities);

        /// <summary>
        /// Create a transfer request.
        /// </summary>
        /// <param name="transferRequest">The transfer request to be created.</param>
        /// <param name="additionalEntities">Any additional entities that need to be created or replaced.</param>
        /// <returns>The transfer request that was created.</returns>
        Task<TransferRequest> CreateTransferRequestAsync(TransferRequest transferRequest, IEnumerable<Entity> additionalEntities);
    }
}
