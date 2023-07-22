namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Writer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using AutoMapper;

    using Microsoft.PrivacyServices.DataManagement.Common;
    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authorization;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Reader;
    using Microsoft.PrivacyServices.DataManagement.Models;
    using Microsoft.PrivacyServices.DataManagement.Models.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;

    /// <summary>
    /// Base class for all entity write operations.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    public abstract class EntityWriter<TEntity> : IEntityWriter<TEntity> where TEntity : Entity
    {
        private readonly Dictionary<object, object> cache; // Note: This is not thread safe.
        private readonly IEventWriterFactory eventWriterFactory;

        private readonly string componentName = nameof(EntityWriter<TEntity>);
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityWriter{TEntity}" /> class.
        /// </summary>
        /// <param name="storageWriter">The storage writer instance.</param>
        /// <param name="entityReader">The entity reader instance.</param>
        /// <param name="authenticatedPrincipal">The principal object created for this specific request.</param>
        /// <param name="authorizationProvider">The authorization provider.</param>
        /// <param name="dateFactory">The date factory instance.</param>
        /// <param name="mapper">A utility for mapping data types by convention.</param>
        /// <param name="eventWriterFactory">The event writer factory instance.</param>
        protected EntityWriter(
            IPrivacyDataStorageWriter storageWriter,
            IEntityReader<TEntity> entityReader,
            AuthenticatedPrincipal authenticatedPrincipal,
            IAuthorizationProvider authorizationProvider,
            IDateFactory dateFactory,
            IMapper mapper,
            IEventWriterFactory eventWriterFactory = null)
        {
            this.StorageWriter = storageWriter;
            this.EntityReader = entityReader;
            this.AuthenticatedPrincipal = authenticatedPrincipal;
            this.AuthorizationProvider = authorizationProvider;
            this.DateFactory = dateFactory;
            this.Mapper = mapper;
            this.cache = new Dictionary<object, object>();
            this.eventWriterFactory = eventWriterFactory;
        }

        /// <summary>
        /// Gets the mapper.
        /// </summary>
        protected IMapper Mapper { get; private set; }

        /// <summary>
        /// Gets the date factory.
        /// </summary>
        protected IDateFactory DateFactory { get; private set; }

        /// <summary>
        /// Gets the authorization provider.
        /// </summary>
        protected IAuthorizationProvider AuthorizationProvider { get; private set; }

        /// <summary>
        /// Gets the authenticated user information.
        /// </summary>
        protected AuthenticatedPrincipal AuthenticatedPrincipal { get; private set; }

        /// <summary>
        /// Gets the storage writer.
        /// </summary>
        protected IPrivacyDataStorageWriter StorageWriter { get; private set; }

        /// <summary>
        /// Gets the storage reader.
        /// </summary>
        protected IEntityReader<TEntity> EntityReader { get; private set; }

        /// <summary>
        /// Gets or sets the set of required roles for accessing this entity.
        /// </summary>
        protected AuthorizationRole AuthorizationRoles { get; set; }

        #region Base methods
        /// <summary>
        /// Validate the properties of the incoming entity.
        /// </summary>
        /// <param name="action">The write action for the request.</param>
        /// <param name="incomingEntity">The incoming entity.</param>
        public virtual void ValidateProperties(WriteAction action, TEntity incomingEntity)
        {
            this.ValidateEntityProperties(action, incomingEntity);
        }

        /// <summary>
        /// Validate the entity properties of the incoming entity.
        /// </summary>
        /// <param name="action">The write action for the request.</param>
        /// <param name="incomingEntity">The incoming entity.</param>
        public void ValidateEntityProperties(WriteAction action, TEntity incomingEntity)
        {
            ValidationModule.PropertyShouldNotBeSet(incomingEntity.TrackingDetails, "trackingDetails", false);

            if (action == WriteAction.Create)
            {
                ValidationModule.PropertyShouldNotBeSet(incomingEntity.Id, "id");
                ValidationModule.PropertyShouldNotBeSet(incomingEntity.ETag, "eTag");
            }
            else if (action == WriteAction.Update)
            {
                ValidationModule.PropertyRequired(incomingEntity.ETag, "eTag");
                ValidationModule.PropertyRequired(incomingEntity.Id, "id");
            }
        }

        /// <summary>
        /// Ensure consistency between the incoming entity and any existing entities.
        /// </summary>
        /// <param name="action">The write action for the request.</param>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>A task that performs the checks.</returns>
        public virtual async Task ValidateConsistencyAsync(WriteAction action, TEntity incomingEntity)
        {
            await this.ValidateEntityConsistencyAsync(action, incomingEntity).ConfigureAwait(false);

            var dataOwners = await this.GetDataOwnersAsync(action, incomingEntity).ConfigureAwait(false);

            if (dataOwners != null && !dataOwners.All(x => x.WriteSecurityGroups?.Any() == true))
            {
                throw new ConflictException(ConflictType.DoesNotExist, "Linked data owner must have a write security group.", "dataOwner.writeSecurityGroups");
            }
        }

        /// <summary>
        /// Ensure entity consistency between the incoming entity and any existing entities.
        /// </summary>
        /// <param name="action">The write action for the request.</param>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>A task that performs the checks.</returns>
        public async Task ValidateEntityConsistencyAsync(WriteAction action, TEntity incomingEntity)
        {
            if (action == WriteAction.Update)
            {
                await this.GetExistingEntityWithConsistencyChecks(incomingEntity.Id, incomingEntity.ETag).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Get the data owners linked to the incoming entity.
        /// </summary>
        /// <param name="action">The write action for the request.</param>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>The data owner list.</returns>
        public abstract Task<IEnumerable<DataOwner>> GetDataOwnersAsync(WriteAction action, TEntity incomingEntity);

        /// <summary>
        /// Create the entity in storage.
        /// </summary>
        /// <param name="action">The write action for the request.</param>
        /// <param name="entity">The entity to be written.</param>
        /// <returns>A task that performs the checks.</returns>
        public abstract Task<TEntity> WriteAsync(WriteAction action, TEntity entity);
        #endregion

        #region Interface methods
        /// <summary>
        /// Creates an entity with proper validation.
        /// </summary>
        /// <param name="incomingEntity">Entity to be persisted.</param>
        /// <returns>The value that is stored in the system.</returns>
        public async Task<TEntity> CreateAsync(TEntity incomingEntity)
        {
            var action = WriteAction.Create;

            await this.AuthorizeAsync(action, incomingEntity).ConfigureAwait(false);

            await this.ValidateAsync(action, incomingEntity).ConfigureAwait(false);

            this.PopulateProperties(action, incomingEntity);

            var result = await this.WriteAsync(action, incomingEntity).ConfigureAwait(false);

            // We do not return tracking details by default in our service.
            // It must be explicitly requested by read APIs.
            result.TrackingDetails = null;

            return result;
        }

        /// <summary>
        /// Updates an entity with proper validation.
        /// </summary>
        /// <param name="incomingEntity">Entity with updated properties.</param>
        /// <returns>The value that is stored in the system.</returns>
        public async Task<TEntity> UpdateAsync(TEntity incomingEntity)
        {
            var action = WriteAction.Update;

            var existingEntity = await this.GetExistingEntityAsync(incomingEntity).ConfigureAwait(false);

            await this.AuthorizeAsync(action, incomingEntity).ConfigureAwait(false);

            await this.ValidateAsync(action, incomingEntity).ConfigureAwait(false);

            // Copy updatable properties from new entity to existing.
            this.Mapper.Map(incomingEntity, existingEntity);

            this.PopulateProperties(action, existingEntity);

            var result = await this.WriteAsync(action, existingEntity).ConfigureAwait(false);

            // We do not return tracking details by default in our service.
            // It must be explicitly requested by read APIs.
            result.TrackingDetails = null;

            return result;
        }

        /// <summary>
        /// Deletes an entity.
        /// </summary>
        /// <param name="id">The id of the entity to delete.</param>
        /// <param name="etag">The ETag of the entity.</param>
        /// <returns>A task that performs the delete.</returns>
        public async Task DeleteAsync(Guid id, string etag) => await this.DeleteAsync(id, etag, false, false).ConfigureAwait(false);

        /// <summary>
        /// Deletes an entity.
        /// </summary>
        /// <param name="id">The id of the entity to delete.</param>
        /// <param name="etag">The ETag of the entity.</param>
        /// <param name="overrridePendingCommandsChecks">Override checks.</param>
        /// <param name="force">The flag to force delete.</param>
        /// <returns>A task that performs the delete.</returns>
        public async Task DeleteAsync(Guid id, string etag, bool overrridePendingCommandsChecks, bool force)
        {
            var action = WriteAction.SoftDelete;

            var existingEntity = await this.GetExistingEntityWithConsistencyChecks(id, etag).ConfigureAwait(false);

            await this.AuthorizeAsync(action, existingEntity).ConfigureAwait(false);

            await this.ValidateDeleteAsync(existingEntity, overrridePendingCommandsChecks, force).ConfigureAwait(false);

            this.PopulateProperties(action, existingEntity);

            existingEntity.IsDeleted = true;

            await this.WriteAsync(action, existingEntity).ConfigureAwait(false);
        }
        #endregion

        #region Helper methods
        /// <summary>
        /// Retrieve the existing entity and validate consistency by checking for existence and ETag match.
        /// </summary>
        /// <param name="id">The id of the entity to load.</param>
        /// <param name="etag">The ETag to verify.</param>
        /// <returns>The entity if it is consistent. Otherwise, an exception is throw.</returns>
        protected async Task<TEntity> GetExistingEntityWithConsistencyChecks(Guid id, string etag)
        {
            var existingEntity = await this.GetExistingEntityAsync(id).ConfigureAwait(false);

            if (existingEntity == null)
            {
                throw new EntityNotFoundException(id, typeof(TEntity).Name);
            }

            if (!existingEntity.ETag.Equals(etag, StringComparison.OrdinalIgnoreCase))
            {
                throw new ETagMismatchException("ETag mismatch.", null, etag);
            }

            return existingEntity;
        }

        /// <summary>
        /// Calls function to get the existing entity and cache it if called the first time.
        /// </summary>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>The existing entity.</returns>
        protected virtual Task<TEntity> GetExistingEntityAsync(TEntity incomingEntity)
        {
            return this.GetExistingEntityAsync(incomingEntity.Id);
        }

        /// <summary>
        /// Calls function to get the existing entity and cache it if called the first time.
        /// </summary>
        /// <param name="id">The id of the entity to retrieve.</param>
        /// <returns>The existing entity.</returns>
        protected virtual Task<TEntity> GetExistingEntityAsync(Guid id)
        {
            return this.MemoizeAsync(id, () => this.EntityReader.ReadByIdAsync(id, ExpandOptions.WriteProperties));
        }

        /// <summary>
        /// Perform an action and cache the result so that multiple calls are efficient.
        /// This is not thread safe.
        /// </summary>
        /// <typeparam name="T">The return type.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="getter">The method whose result should be cached.</param>
        /// <returns>The cached result.</returns>
        protected async Task<T> MemoizeAsync<T>(object key, Func<Task<T>> getter)
        {
            if (this.cache.ContainsKey(key))
            {
                return (T)this.cache[key];
            }
            else
            {
                var value = await getter().ConfigureAwait(false);
                this.cache[key] = value;
                return value;
            }
        }

        /// <summary>
        /// Given a set of ids, retrieves the corresponding entities. If the entity has previously been retrieved,
        /// then returns the cached value. For any value returned from storage, adds to the cache.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="ids">The set of ids to retrieve.</param>
        /// <param name="getter">A method to get the entities by the filtered ids.</param>
        /// <returns>An entity for each requested id if found.</returns>
        protected async Task<IEnumerable<T>> MemoizeEntitiesAsync<T>(IEnumerable<Guid> ids, Func<IEnumerable<Guid>, Task<IEnumerable<T>>> getter)
            where T : Entity
        {
            var uncachedKeys = new List<Guid>();
            var response = new List<T>();

            foreach (var key in ids)
            {
                if (this.cache.ContainsKey(key))
                {
                    response.Add((T)this.cache[key]);
                }
                else
                {
                    uncachedKeys.Add(key);
                }
            }

            if (uncachedKeys.Count > 0)
            {
                var uncachedResponses = await getter.Invoke(uncachedKeys).ConfigureAwait(false);

                foreach (var entity in uncachedResponses)
                {
                    this.cache[entity.Id] = entity;
                    response.Add(entity);
                }
            }

            return response;
        }

        /// <summary>
        /// Given an entity, applies the server generated properties.
        /// </summary>
        /// <param name="action">The write action.</param>
        /// <param name="entity">The entity.</param>
        protected void PopulateProperties(WriteAction action, Entity entity)
        {
            var currentTime = this.DateFactory.GetCurrentTime();

            if (action == WriteAction.Create)
            {
                entity.Id = Guid.NewGuid();
                entity.TrackingDetails = new TrackingDetails
                {
                    CreatedBy = this.AuthenticatedPrincipal.UserId,
                    CreatedOn = currentTime,
                    Version = 0
                };
            }

            // common updates for all write actions.
            entity.TrackingDetails.Version += 1;
            entity.TrackingDetails.UpdatedOn = currentTime;
            entity.TrackingDetails.UpdatedBy = this.AuthenticatedPrincipal.UserId;
        }
        #endregion

        #region Private and Protected methods
        /// <summary>
        /// Authorize the given write action.
        /// </summary>
        /// <param name="action">The write action.</param>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>A task for authorization.</returns>
        protected virtual Task AuthorizeAsync(WriteAction action, TEntity incomingEntity)
        {
            return this.AuthorizationProvider.AuthorizeAsync(this.AuthorizationRoles, () => this.GetDataOwnersAsync(action, incomingEntity));
        }

        /// <summary>
        /// Ensure if the delete operation is allowed.
        /// </summary>
        /// <param name="entity">The entity to be deleted.</param>
        /// <param name="overrridePendingCommandsChecks">Override checks.</param>
        /// <param name="force">The flag to force delete.</param>
        /// <returns>A task for authorization.</returns>
        protected virtual async Task ValidateDeleteAsync(TEntity entity, bool overrridePendingCommandsChecks, bool force)
        {
            if (await this.EntityReader.IsLinkedToAnyOtherEntities(entity.Id).ConfigureAwait(false))
            {
                throw new ConflictException(ConflictType.LinkedEntityExists, "Unable to perform delete. A dependent entity was found.", null);
            }

            // Log event with user alias and override details -- If overrideChecks=true, user consented to delete from UX
            this.eventWriterFactory?.Trace(
                componentName,
                $"Delete Agent with id: {entity.Id}, User: {this.AuthenticatedPrincipal.UserAlias}, OverrideChecks: {overrridePendingCommandsChecks}.");

            if (!overrridePendingCommandsChecks && await this.EntityReader.HasPendingCommands(entity.Id).ConfigureAwait(false))
            {
                throw new ConflictException(ConflictType.PendingCommandsExists, "Unable to perform delete. Pending commands were found.", null);
            }
        }

        private async Task ValidateAsync(WriteAction action, TEntity incomingEntity)
        {
            this.ValidateProperties(action, incomingEntity);

            await this.ValidateConsistencyAsync(action, incomingEntity).ConfigureAwait(false);
        }
        #endregion
    }
}