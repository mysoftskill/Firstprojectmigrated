namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Writer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using AutoMapper;

    using Microsoft.PrivacyServices.DataManagement.Common;
    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authorization;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Reader;
    using Microsoft.PrivacyServices.DataManagement.Models;
    using Microsoft.PrivacyServices.DataManagement.Models.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;

    /// <summary>
    /// Provides methods for writing VariantDefinition information.
    /// </summary>
    public class VariantDefinitionWriter : NamedEntityWriter<VariantDefinition, VariantDefinitionFilterCriteria>, IVariantDefinitionWriter
    {
        private readonly IDataOwnerReader dataOwnerReader;

        /// <summary>
        /// Initializes a new instance of the <see cref="VariantDefinitionWriter" /> class.
        /// </summary>
        /// <param name="storageWriter">The storage writer instance.</param>
        /// <param name="entityReader">The entity reader instance.</param>
        /// <param name="authenticatedPrincipal">The principal object created for this specific request.</param>
        /// <param name="authorizationProvider">The authorization provider.</param>
        /// <param name="dateFactory">The date factory instance.</param>
        /// <param name="mapper">A utility for mapping data types by convention.</param>
        /// <param name="dataOwnerReader">The reader for data owners.</param>
        public VariantDefinitionWriter(
            IPrivacyDataStorageWriter storageWriter,
            IVariantDefinitionReader entityReader,
            AuthenticatedPrincipal authenticatedPrincipal,
            IAuthorizationProvider authorizationProvider,
            IDateFactory dateFactory,
            IMapper mapper,
            IDataOwnerReader dataOwnerReader)
            : base(
                  storageWriter,
                  entityReader,
                  authenticatedPrincipal,
                  authorizationProvider,
                  dateFactory,
                  mapper)
        {
            this.dataOwnerReader = dataOwnerReader;

            this.AuthorizationRoles = AuthorizationRole.VariantEditor;
        }

        /// <summary>
        /// Validate the properties of the incoming entity.
        /// </summary>
        /// <param name="action">The write action for the request.</param>
        /// <param name="incomingEntity">The incoming entity.</param>
        public override void ValidateProperties(WriteAction action, VariantDefinition incomingEntity)
        {
            base.ValidateProperties(action, incomingEntity);

            // Referenced entities should not be provided.
            ValidationModule.PropertyShouldNotBeSet(incomingEntity.Owner, "owner", false);
            ValidationModule.PropertyShouldNotBeSet(incomingEntity.IsDeleted, "isDeleted", false);

            if (action == WriteAction.Create)
            {
                // If State is set, it must be Active.
                this.StateShouldBeActive(incomingEntity);

                this.SetDefaultDefinitionState(incomingEntity);
            }
            else
            {
                this.ValidateStateAndReasonConsistency(incomingEntity);
            }
        }

        /// <summary>
        /// Ensure consistency between the incoming entity and any existing entities.
        /// </summary>
        /// <param name="action">The write action for the request.</param>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>A task that performs the checks.</returns>
        public override async Task ValidateConsistencyAsync(WriteAction action, VariantDefinition incomingEntity)
        {
            await base.ValidateConsistencyAsync(action, incomingEntity).ConfigureAwait(false);

            if (action == WriteAction.Create)
            {
                await this.OwnerShouldExist(incomingEntity).ConfigureAwait(false);

            }
            else if (action == WriteAction.Update)
            {
                var existingEntity = await this.GetExistingEntityAsync(incomingEntity).ConfigureAwait(false);

                this.IsDeletedShouldNotBeSet(existingEntity);

                if (incomingEntity.OwnerId != existingEntity.OwnerId)
                {
                    await this.OwnerShouldExist(incomingEntity).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Create the entity in storage.
        /// </summary>
        /// <param name="action">The write action for the request.</param>
        /// <param name="entity">The entity to be written.</param>
        /// <returns>A task that performs the checks.</returns>
        public override async Task<VariantDefinition> WriteAsync(WriteAction action, VariantDefinition entity)
        {
            if (action == WriteAction.Create)
            {
                return await this.StorageWriter.CreateVariantDefinitionAsync(entity).ConfigureAwait(false);
            }
            else if (action == WriteAction.Update)
            {
                return await this.StorageWriter.UpdateVariantDefinitionAsync(entity).ConfigureAwait(false);
            }
            else if (action == WriteAction.SoftDelete)
            {
                // Ideally these two operations should be in the same transaction. But since the delete operation is rare and the chance of a collision 
                // to happen is also rare, it is not worth the effort to create another SPROC for this.
                await this.DelinkAssetGroups(entity.Id).ConfigureAwait(false);
                return await this.StorageWriter.UpdateVariantDefinitionAsync(entity).ConfigureAwait(false);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Get the data owners linked to the incoming entity.
        /// </summary>
        /// <param name="action">The write action for the request.</param>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>The data owner list.</returns>
        public override Task<IEnumerable<DataOwner>> GetDataOwnersAsync(WriteAction action, VariantDefinition incomingEntity)
        {
            // Authorization uses a fixed security group, so no need to get owners.
            return Task.FromResult<IEnumerable<DataOwner>>(null);
        }

        /// <inheritdoc/>
        protected override async Task ValidateDeleteAsync(VariantDefinition entity, bool overrridePendingCommandsChecks, bool force)
        {
            // Definition must be closed before it can be deleted
            this.StateShouldBeClosed(entity);

            // If force flag is specified, no additional check will be performed
            if (!force)
            {
                await base.ValidateDeleteAsync(entity, overrridePendingCommandsChecks, force).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Remove the reference of all AssetGroups to the given Variant id.
        /// </summary>
        /// <param name="variantId">The variant id.</param>
        private async Task DelinkAssetGroups(Guid variantId)
        {
            var assetGroups = (this.EntityReader as IVariantDefinitionReader).GetLinkedAssetGroups(variantId).Result;
            if (assetGroups.Any())
            {
                foreach (var assetGroup in assetGroups)
                {
                    assetGroup.Variants = assetGroup.Variants.Where(v => v.VariantId != variantId).ToList();
                }

                await this.StorageWriter.UpdateEntitiesAsync(assetGroups).ConfigureAwait(false);
            }
        }

        private Task<DataOwner> GetExistingOwnerAsync(Guid ownerId)
        {
            return this.MemoizeAsync(ownerId, () => this.dataOwnerReader.ReadByIdAsync(ownerId, ExpandOptions.None));
        }

        /// <summary>
        /// Helper function to check if the linked data owner exists.
        /// </summary>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>Void task.</returns>
        private async Task OwnerShouldExist(VariantDefinition incomingEntity)
        {
            if (incomingEntity.OwnerId.HasValue)
            {
                var existingOwner = await this.GetExistingOwnerAsync(incomingEntity.OwnerId.Value).ConfigureAwait(false);

                if (existingOwner == null)
                {
                    throw new ConflictException(ConflictType.DoesNotExist, "The referenced entity does not exist", "ownerId", incomingEntity.OwnerId.ToString());
                }
            }
        }

        /// <summary>
        /// Helper function to check if the state is not Active.
        /// </summary>
        /// <param name="incomingEntity">The incoming entity.</param>
        private void StateShouldBeActive(VariantDefinition incomingEntity)
        {
            if (incomingEntity.State != VariantDefinitionState.Active)
            {
                throw new ConflictException(ConflictType.InvalidValue_StateTransition, "VariantDefinition is Not Active", "StateShouldBeActive");
            }
        }

        /// <summary>
        /// Helper function to check if the state is closed.
        /// </summary>
        /// <param name="existingEntity">The existing entity.</param>
        private void StateShouldBeClosed(VariantDefinition existingEntity)
        {
            if (existingEntity.State != VariantDefinitionState.Closed)
            {
                throw new ConflictException(ConflictType.InvalidValue_StateTransition, "VariantDefinition is Active", "StateShouldBeClosed");
            }
        }

        /// <summary>
        /// Set the default state and reason when a new variant definition is crated.
        /// </summary>
        /// <param name="incomingEntity">The new variant definition to be created.</param>
        private void SetDefaultDefinitionState(VariantDefinition incomingEntity)
        {
            incomingEntity.State = VariantDefinitionState.Active;
            incomingEntity.Reason = VariantDefinitionReason.None;
        }

        /// <summary>
        /// Helper function to validate that we are not modifying a deleted definition.
        /// </summary>
        /// <param name="existingEntity">The existing variant definition.</param>
        private void IsDeletedShouldNotBeSet(VariantDefinition existingEntity)
        {
            if (existingEntity.IsDeleted)
            {
                throw new ConflictException(ConflictType.InvalidValue_StateTransition, "VariantDefinition is deleted", "IsDeletedShouldNotBeSet");
            }
        }

        /// <summary>
        /// Helper function to validate that the state and reason are valid combination for the incoming entity.
        /// </summary>
        /// <param name="incomingEntity">The incoming variant definition.</param>
        private void ValidateStateAndReasonConsistency(VariantDefinition incomingEntity)
        {
            if (incomingEntity.State == VariantDefinitionState.Active)
            {
                if (incomingEntity.Reason != VariantDefinitionReason.None)
                {
                    throw new ConflictException(ConflictType.InvalidValue_StateTransition, "ValidateStateAndReasonConsistency (VariantDefinition): invalid Reason code for Active State", "ValidateStateAndReasonConsistency");
                }
            }
            else
            {
                if (incomingEntity.Reason == VariantDefinitionReason.None)
                {
                    throw new ConflictException(ConflictType.InvalidValue_StateTransition, "ValidateStateAndReasonConsistency (VariantDefinition): invalid Reason code for Closed State", "ValidateStateAndReasonConsistency");
                }
            }
        }
    }
}