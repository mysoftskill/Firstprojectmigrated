namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Writer
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Models.V2;

    /// <summary>
    /// Provides methods for writing entity.
    /// </summary>
    /// <typeparam name="TEntity">Entity type.</typeparam>
    public interface IEntityWriter<TEntity> where TEntity : Entity
    {
        /// <summary>
        /// Creates an entity.
        /// </summary>
        /// <param name="entity">Entity to be persisted.</param>
        /// <returns>The Task object for asynchronous execution.</returns>
        Task<TEntity> CreateAsync(TEntity entity);

        /// <summary>
        /// Updates an entity.
        /// </summary>
        /// <param name="entity">Entity with updated properties.</param>
        /// <returns>The Task object for asynchronous execution.</returns>
        Task<TEntity> UpdateAsync(TEntity entity);

        /// <summary>
        /// Deletes an entity.
        /// </summary>
        /// <param name="id">The id of the entity to delete.</param>
        /// <param name="etag">The ETag of the entity.</param>
        /// <returns>A task that performs the asynchronous execution.</returns>
        Task DeleteAsync(Guid id, string etag);

        /// <summary>
        /// Deletes an entity, with override checks.
        /// </summary>
        /// <param name="id">The id of the entity to delete.</param>
        /// <param name="etag">The ETag of the entity.</param>
        /// <param name="overrideChecks">The flag to override checks.</param>
        /// <param name="force">The flag to force delete.</param>
        /// <returns>A task that performs the asynchronous execution.</returns>
        Task DeleteAsync(Guid id, string etag, bool overrideChecks, bool force);
    }

    /// <summary>
    /// Provides methods for writing data owner information.
    /// </summary>
    public interface IDataOwnerWriter : IEntityWriter<DataOwner>
    {
        /// <summary>
        /// This method finds the existing data owner that contains the provided service tree id and delete it. 
        /// It then updates the provided data owner with the given service tree id.
        /// </summary>
        /// <param name="dataOwner">The target data owner for the migration.</param>
        /// <returns>The updated data owner.</returns>
        Task<DataOwner> ReplaceServiceIdAsync(DataOwner dataOwner);
    }

    /// <summary>
    /// Provides methods for writing asset group information.
    /// </summary>
    public interface IAssetGroupWriter : IEntityWriter<AssetGroup>
    {
        /// <summary>
        /// Set agent relationships in bulk. Create requests as needed.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The response.</returns>
        Task<SetAgentRelationshipResponse> SetAgentRelationshipsAsync(SetAgentRelationshipParameters parameters);

        /// <summary>
        /// Remove the given variants attached to an asset group.
        /// </summary>
        /// <param name="assetGroupId">The id for the asset group.</param>
        /// <param name="variantIds">The list of variant ids to remove from the asset group.</param>
        /// <param name="etag">The e-tag of the asset group.</param>
        /// <returns>The updated asset group.</returns>
        Task<AssetGroup> RemoveVariantsAsync(Guid assetGroupId, IEnumerable<Guid> variantIds, string etag);
    }

    /// <summary>
    /// Provides methods for writing variant definition information.
    /// </summary>
    public interface IVariantDefinitionWriter : IEntityWriter<VariantDefinition>
    {
    }

    /// <summary>
    /// Provides methods for writing data agents generically.
    /// </summary>
    /// <typeparam name="TDataAgent">The data agent type.</typeparam>
    public interface IDataAgentWriter<TDataAgent> : IEntityWriter<TDataAgent> where TDataAgent : DataAgent
    {
    }

    /// <summary>
    /// Provides methods for writing delete agents.
    /// </summary>
    public interface IDeleteAgentWriter : IDataAgentWriter<DeleteAgent>
    {
    }

    /// <summary>
    /// Provides methods for writing data agents generically.
    /// </summary>
    public interface IDataAgentWriter : IEntityWriter<DataAgent>
    {
    }

    /// <summary>
    /// Provides methods for writing sharing requests.
    /// </summary>
    public interface ISharingRequestWriter : IEntityWriter<SharingRequest>
    {
        /// <summary>
        /// Approves the sharing request.
        /// Approval results in the deletion of the request
        /// and an update on all associated asset groups.
        /// </summary>
        /// <param name="id">The id of the sharing request to approve.</param>
        /// <param name="etag">The ETag of the sharing request.</param>
        /// <returns>A task that performs the asynchronous execution.</returns>
        Task ApproveAsync(Guid id, string etag);
    }

    /// <summary>
    /// Provides methods for writing variant requests.
    /// </summary>
    public interface IVariantRequestWriter : IEntityWriter<VariantRequest>
    {
        /// <summary>
        /// Approves the variant request.
        /// Approval results in the deletion of the request
        /// and an update on all associated asset groups.
        /// </summary>
        /// <param name="id">The id of the variant request to approve.</param>
        /// <param name="etag">The ETag of the variant request.</param>
        /// <returns>A task that performs the asynchronous execution.</returns>
        Task ApproveAsync(Guid id, string etag);
    }

    /// <summary>
    /// Provides methods for writing transfer requests.
    /// </summary>
    public interface ITransferRequestWriter : IEntityWriter<TransferRequest>
    {
        /// <summary>
        /// Approves the transfer request.
        /// </summary>
        /// <param name="id">The id of the transfer request to approve.</param>
        /// <param name="etag">The ETag of the transfer request.</param>
        /// <returns>A task that performs the asynchronous execution.</returns>
        Task ApproveAsync(Guid id, string etag);
    }
}