namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Readers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authorization;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Reader;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;

    /// <summary>
    /// Entity read behavior that is common to all entities.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    public abstract class EntityReader<TEntity> where TEntity : Entity
    {
        /// <summary>
        /// The authorization provider.
        /// </summary>
        protected readonly IAuthorizationProvider AuthorizationProvider;

        /// <summary>
        /// The storage reader.
        /// </summary>
        protected readonly IPrivacyDataStorageReader StorageReader;

        /// <summary>
        /// The maximum page size for any query.
        /// </summary>
        protected readonly int MaxPageSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityReader{TEntity}" /> class.
        /// </summary>
        /// <param name="storageReader">The storage reader instance.</param>
        /// <param name="coreConfiguration">The core configuration.</param>
        /// <param name="authorizationProvider">The authorization provider instance.</param>
        public EntityReader(
            IPrivacyDataStorageReader storageReader,
            ICoreConfiguration coreConfiguration,
            IAuthorizationProvider authorizationProvider)
        {
            this.StorageReader = storageReader;
            this.MaxPageSize = coreConfiguration.MaxPageSize;
            this.AuthorizationProvider = authorizationProvider;
        }

        /// <summary>
        /// Gets or sets the set of required roles for accessing this entity.
        /// </summary>
        protected AuthorizationRole AuthorizationRoles { get; set; }

        /// <summary>
        /// Get entities based on a set of ids.
        /// </summary>
        /// <param name="ids">The set if ids to retrieve.</param>
        /// <param name="expandOptions">The set of expand options.</param>
        /// <returns>Entities matching the set of ids. If some ids don't have matches, then those entities are omitted.</returns>
        public async Task<IEnumerable<TEntity>> ReadByIdsAsync(IEnumerable<Guid> ids, ExpandOptions expandOptions)
        {
            await this.AuthorizationProvider.AuthorizeAsync(this.AuthorizationRoles).ConfigureAwait(false);

            return await this.ReadFromStorageUsingCollection(ids, x => this.ReadByIdsFromStorageAsync(x, expandOptions)).ConfigureAwait(false);
        }

        /// <summary>
        /// Determines if there are any pending commands for the entity.
        /// </summary>
        /// <param name="id">The id of the entity.</param>
        /// <returns>True if pending commands found, False otherwise.</returns>
        public virtual async Task<bool> HasPendingCommands(Guid id)
        {
            return await Task.FromResult(false).ConfigureAwait(false);
        }

        /// <summary>
        /// Get entities based on a set of ids.
        /// </summary>
        /// <param name="ids">The set if ids to retrieve.</param>
        /// <param name="expandOptions">The set of expand options.</param>
        /// <returns>Entities matching the set of ids. If some ids don't have matches, then those entities are omitted.</returns>
        protected abstract Task<IEnumerable<TEntity>> ReadByIdsFromStorageAsync(IEnumerable<Guid> ids, ExpandOptions expandOptions);

        /// <summary>
        /// Reads from storage using the provided collection and method. Handles internal paging as needed.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <typeparam name="V">The response value type.</typeparam>
        /// <param name="items">The list of items to search on.</param>
        /// <param name="readFromStorage">The method to use for searching.</param>
        /// <returns>The values from storage.</returns>
        protected async Task<IEnumerable<V>> ReadFromStorageUsingCollection<T, V>(IEnumerable<T> items, Func<IEnumerable<T>, Task<IEnumerable<V>>> readFromStorage)
        {
            await this.AuthorizationProvider.AuthorizeAsync(this.AuthorizationRoles).ConfigureAwait(false);

            var response = Enumerable.Empty<V>();

            while (items.Count() > 0)
            {
                var values = items.Take(this.MaxPageSize);

                var data = await readFromStorage(values).ConfigureAwait(false);

                response = response.Concat(data);

                items = items.Skip(this.MaxPageSize);
            }

            return response;
        }
    }
}