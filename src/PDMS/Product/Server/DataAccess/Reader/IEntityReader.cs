namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Reader
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;
    using Microsoft.PrivacyServices.Identity;

    /// <summary>
    /// Provides methods for reading entity information.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    public interface IEntityReader<TEntity> where TEntity : Entity
    {
        /// <summary>
        /// Get entity for given id. Returns null if not found.
        /// </summary>
        /// <param name="id">Entity id.</param>
        /// <param name="expandOptions">Expand options for the entity.</param>
        /// <returns>Entity for given id.</returns>
        Task<TEntity> ReadByIdAsync(Guid id, ExpandOptions expandOptions);

        /// <summary>
        /// Get entities based on filter criteria.
        /// </summary>
        /// <param name="filterCriteria">Filter criteria for the entity.</param>
        /// <param name="expandOptions">Expand options for the entity.</param>
        /// <returns>Entities matching filter criteria.</returns>
        Task<FilterResult<TEntity>> ReadByFiltersAsync(IFilterCriteria<TEntity> filterCriteria, ExpandOptions expandOptions);

        /// <summary>
        /// Get entities based on a set of ids.
        /// </summary>
        /// <param name="ids">The set if ids to retrieve.</param>
        /// <param name="expandOptions">The set of expand options.</param>
        /// <returns>Entities matching the set of ids. If some ids don't have matches, then those entities are omitted.</returns>
        Task<IEnumerable<TEntity>> ReadByIdsAsync(IEnumerable<Guid> ids, ExpandOptions expandOptions);

        /// <summary>
        /// Determines if there are any other entities linked to this entity.
        /// </summary>
        /// <param name="id">The id of the entity.</param>
        /// <returns>True if the entity is linked to any other entities, False otherwise.</returns>
        Task<bool> IsLinkedToAnyOtherEntities(Guid id);

        /// <summary>
        /// Determines if there are any pending commands linked to this entity.
        /// </summary>
        /// <param name="id">The id of the entity.</param>
        /// <returns>True if the entity has pending commands, False otherwise.</returns>
        Task<bool> HasPendingCommands(Guid id);
    }

    /// <summary>
    /// Provides methods for reading data owner information.
    /// </summary>
    public interface IDataOwnerReader : IEntityReader<DataOwner>
    {
        /// <summary>
        /// Finds all data owners that contain a write security group based on the authenticated user's write security groups.
        /// </summary>
        /// <param name="expandOptions">Expand options for the call.</param>
        /// <returns>The data owners or an empty collection.</returns>
        Task<IEnumerable<DataOwner>> FindByAuthenticatedUserAsync(ExpandOptions expandOptions);
    }

    /// <summary>
    /// Provides methods for reading asset group information.
    /// </summary>
    public interface IAssetGroupReader : IEntityReader<AssetGroup>
    {
        /// <summary>
        /// Get the most specific asset group based on the provided asset qualifier.
        /// </summary>
        /// <param name="qualifier">The asset qualifier to check.</param>
        /// <returns>The most specific asset group.</returns>
        Task<AssetGroup> FindByAssetQualifierAsync(AssetQualifier qualifier);

        /// <summary>
        /// Calculate the asset group registration status.
        /// </summary>
        /// <param name="id">The id of the asset group.</param>
        /// <returns>The registration status.</returns>
        Task<AssetGroupRegistrationStatus> CalculateRegistrationStatus(Guid id);

        /// <summary>
        /// Calculate the asset group registration status.
        /// </summary>
        /// <param name="assetGroup">The the asset group.</param>
        /// <returns>The registration status.</returns>
        Task<AssetGroupRegistrationStatus> CalculateRegistrationStatus(AssetGroup assetGroup);
    }

    /// <summary>
    /// Provides methods for reading sharing request information.
    /// </summary>
    public interface ISharingRequestReader : IEntityReader<SharingRequest>
    {
    }

    /// <summary>
    /// Provides methods for reading variant request information.
    /// </summary>
    public interface IVariantRequestReader : IEntityReader<VariantRequest>
    {
    }

    /// <summary>
    /// Provides methods for reading variant definition information.
    /// </summary>
    public interface IVariantDefinitionReader : IEntityReader<VariantDefinition>
    {
        /// <summary>
        /// Get all AssetGroup linked to the given VariantDefinition.
        /// </summary>
        /// <param name="variantId">The variant definition id.</param>
        /// <returns>All linked AssetGroup.</returns>
        Task<IEnumerable<AssetGroup>> GetLinkedAssetGroups(Guid variantId);
    }

    /// <summary>
    /// Provides methods for reading data agents generically.
    /// </summary>
    /// <typeparam name="TDataAgent">The data agent type.</typeparam>
    public interface IDataAgentReader<TDataAgent> : IEntityReader<TDataAgent> where TDataAgent : DataAgent
    {
    }

    /// <summary>
    /// Provides methods for reading delete agents.
    /// </summary>
    public interface IDeleteAgentReader : IDataAgentReader<DeleteAgent>
    {
        /// <summary>
        /// Calculate the agent registration status.
        /// </summary>
        /// <param name="id">The id of the agent.</param>
        /// <returns>The registration status.</returns>
        Task<AgentRegistrationStatus> CalculateRegistrationStatus(Guid id);
    }

    /// <summary>
    /// Provides methods for reading data agents as abstract types.
    /// </summary>
    public interface IDataAgentReader : IDataAgentReader<DataAgent>
    {
    }

    /// <summary>
    /// Provides methods for reading transfer request information.
    /// </summary>
    public interface ITransferRequestReader : IEntityReader<TransferRequest>
    {
    }
}