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
    /// Provides methods for writing transfer request information.
    /// </summary>
    public class TransferRequestWriter : EntityWriter<TransferRequest>, ITransferRequestWriter
    {
        private readonly IDataOwnerReader dataOwnerReader;
        private readonly IAssetGroupReader assetGroupReader;
        private readonly AuthenticatedPrincipal authenticatedPrincipal;
        private readonly IDateFactory dateFactory;
        private readonly IEventWriterFactory eventWriterFactory;

        private readonly string componentName = nameof(TransferRequestWriter);

        /// <summary>
        /// Initializes a new instance of the <see cref="TransferRequestWriter" /> class.
        /// </summary>
        /// <param name="storageWriter">The storage writer instance.</param>
        /// <param name="entityReader">The entity reader instance.</param>
        /// <param name="authenticatedPrincipal">The principal object created for this specific request.</param>
        /// <param name="authorizationProvider">The authorization provider.</param>
        /// <param name="dateFactory">The date factory instance.</param>
        /// <param name="mapper">A utility for mapping data types by convention.</param>
        /// <param name="dataOwnerReader">The reader for data owners.</param>
        /// <param name="assetGroupReader">The reader for asset groups.</param>
        /// <param name="eventWriterFactory">The event writer factory instance.</param>
        public TransferRequestWriter(
            IPrivacyDataStorageWriter storageWriter,
            ITransferRequestReader entityReader,
            AuthenticatedPrincipal authenticatedPrincipal,
            IAuthorizationProvider authorizationProvider,
            IDateFactory dateFactory,
            IMapper mapper,
            IDataOwnerReader dataOwnerReader,
            IAssetGroupReader assetGroupReader,
            IEventWriterFactory eventWriterFactory)
            : base(
                  storageWriter,
                  entityReader,
                  authenticatedPrincipal,
                  authorizationProvider,
                  dateFactory,
                  mapper)
        {
            this.dataOwnerReader = dataOwnerReader;
            this.assetGroupReader = assetGroupReader;
            this.authenticatedPrincipal = authenticatedPrincipal;
            this.dateFactory = dateFactory;
            this.eventWriterFactory = eventWriterFactory;
            this.AuthorizationRoles = AuthorizationRole.ServiceEditor;
        }

        /// <summary>
        /// Validate the properties of the incoming entity.
        /// This function is called by entity writer (ValidateAsync) during Create and Update operations. 
        /// This is the opportunity for validation of TransferRequest specific properties.
        /// </summary>
        /// <param name="action">The write action for the request.</param>
        /// <param name="incomingEntity">The incoming entity.</param>
        public override void ValidateProperties(WriteAction action, TransferRequest incomingEntity)
        {
            // Call base class property validation - it validates id, ETag and tracking details.
            base.ValidateProperties(action, incomingEntity);

            if (action == WriteAction.Create)
            {
                // Validate TransferRequest object specific properties.
                ValidationModule.PropertyRequired(incomingEntity.SourceOwnerId, "sourceOwnerId");
                ValidationModule.PropertyRequired(incomingEntity.TargetOwnerId, "targetOwnerId");
                ValidationModule.PropertyRequiredAndNotEmpty(incomingEntity.AssetGroups, "assetGroups");

                // The State and AssetGroupId properties are for internal use - it should not be set during create.
                ValidationModule.PropertyShouldNotBeSet(incomingEntity.RequestState, "state", false);
            }
            else if (action == WriteAction.Update)
            {
                // Update should not really get invoked for TransferRequest. Log a message if that happens.
                // But add consistency checks anyway to ensure that the transfer request does not accidentally get messed up.
                this.eventWriterFactory.Trace(
                    componentName,
                    $"ValidateProperties (TransferRequest) invoked for UPDATE operation, Source Owner: [{incomingEntity.SourceOwnerId}], Target Owner: [{incomingEntity.TargetOwnerId}], State: [{incomingEntity.RequestState}].");
                throw new ConflictException(ConflictType.InvalidValue_StateTransition, "ValidateConsistencyAsync (TransferRequest) invoked for UPDATE operation", "ValidateConsistencyAsync");
            }
        }

        /// <summary>
        /// Ensure consistency between the incoming entity and any existing entities.
        /// This function is called by entity writer (ValidateAsync) during Create and Update operations.
        /// This is the opportunity for consistency check of TransferRequest specific properties.
        /// </summary>
        /// <param name="action">The write action for the request.</param>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>A task that performs the checks.</returns>
        public override async Task ValidateConsistencyAsync(WriteAction action, TransferRequest incomingEntity)
        {
            // Call base class property consistency checks.
            await base.ValidateConsistencyAsync(action, incomingEntity).ConfigureAwait(false);

            // This function should be called only for 'Create', as we do not update the transfer request.
            if (action == WriteAction.Create)
            {
                await this.OwnerShouldExist(incomingEntity.SourceOwnerId).ConfigureAwait(false);
                await this.OwnerShouldExist(incomingEntity.TargetOwnerId).ConfigureAwait(false);
                await this.ValidateAssetGroups(incomingEntity.AssetGroups, incomingEntity.SourceOwnerId).ConfigureAwait(false);
            }
            else if (action == WriteAction.Update)
            {
                // Update should not really get invoked for TransferRequest. Log a message if that happens.
                // But add consistency checks anyway to ensure that the transfer request does not accidentally get messed up.
                this.eventWriterFactory.Trace(
                    componentName,
                    $"ValidateConsistencyAsync (TransferRequest) invoked for UPDATE operation, Source Owner: [{incomingEntity.SourceOwnerId}], Target Owner: [{incomingEntity.TargetOwnerId}], State: [{incomingEntity.RequestState}].");
                throw new ConflictException(ConflictType.InvalidValue_StateTransition, "ValidateConsistencyAsync (TransferRequest) invoked for UPDATE operation", "ValidateConsistencyAsync");
            }
        }

        /// <summary>
        /// Write the entity in storage.
        /// This function is called by entity writer during Create, Update and Delete operations. 
        /// </summary>
        /// <param name="action">The write action for the request.</param>
        /// <param name="entity">The incoming entity.</param>
        /// <returns>A task that performs the checks.</returns>
        public override async Task<TransferRequest> WriteAsync(WriteAction action, TransferRequest entity)
        {
            if (action == WriteAction.Create)
            {
                this.eventWriterFactory.Trace(
                    componentName,
                    $"Creating TransferRequest - Source Owner: [{entity.SourceOwnerId}], Target Owner: [{entity.TargetOwnerId}], State: [{entity.RequestState}].");
                return await this.TransferRequestCreateHelper(entity).ConfigureAwait(false);
            }
            else if (action == WriteAction.SoftDelete)
            {
                this.eventWriterFactory.Trace(
                    componentName,
                    $"Deleting TransferRequest - Source Owner: [{entity.SourceOwnerId}], Target Owner: [{entity.TargetOwnerId}], State: [{entity.RequestState}].");
                await this.TransferRequestDeleteHelper(entity, action).ConfigureAwait(false);

                // Delete does not return a value, so no need to extract the correct entity from the storage results.
                return null;
            }
            else
            {
                // WriteAsync function should not really get invoked for any other operation. Log a trace if it does.
                this.eventWriterFactory.Trace(
                    componentName,
                    $"WriteAsync (TransferRequest) invoked for [{action}] operation, Source Owner: [{entity.SourceOwnerId}], Target Owner: [{entity.TargetOwnerId}], State: [{entity.RequestState}].");
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Approves the transfer request.
        /// Approval results in the deletion of the request and an update on all associated asset groups.
        /// </summary>
        /// <param name="id">The id of the transfer request to approve.</param>
        /// <param name="etag">The ETag of the transfer request.</param>
        /// <returns>A task that performs the asynchronous execution.</returns>
        public async Task ApproveAsync(Guid id, string etag)
        {
            var transferRequest = await this.GetExistingEntityWithConsistencyChecks(id, etag).ConfigureAwait(false);
            this.eventWriterFactory.Trace(
                    componentName,
                    $"Approving TransferRequest - Source Owner: [{transferRequest.SourceOwnerId}], Target Owner: [{transferRequest.TargetOwnerId}], State: [{transferRequest.RequestState}].");
            await this.TransferRequestDeleteHelper(transferRequest, WriteAction.Update).ConfigureAwait(false);
        }

        /// <summary>
        /// Get the source data owner associated with the incoming entity.
        /// </summary>
        /// <param name="action">The write action for the request.</param>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>The data owner list.</returns>
        public async Task<IEnumerable<DataOwner>> GetSourceDataOwnersAsync(WriteAction action, TransferRequest incomingEntity)
        {
            var owner = await this.GetExistingOwnerAsync(incomingEntity.SourceOwnerId).ConfigureAwait(false);
            if (owner == null)
            {
                return null;
            }

            return new[] { owner };
        }

        /// <summary>
        /// Get the target data owner associated with the incoming entity.
        /// This function is called by entity writer to get the data owner for authorization and validating consistency. 
        /// </summary>
        /// <param name="action">The write action for the request.</param>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>The data owner list.</returns>
        public async Task<IEnumerable<DataOwner>> GetTargetDataOwnersAsync(WriteAction action, TransferRequest incomingEntity)
        {
            var owner = await this.GetExistingOwnerAsync(incomingEntity.TargetOwnerId).ConfigureAwait(false);
            if (owner == null)
            {
                return null;
            }

            return new[] { owner };
        }

        /// <summary>
        /// Get the data owners linked to the incoming entity.
        /// </summary>
        /// <param name="action">The write action for the request.</param>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>The data owner list.</returns>
        public override async Task<IEnumerable<DataOwner>> GetDataOwnersAsync(WriteAction action, TransferRequest incomingEntity)
        {
            // This method is called by ValidateConsistencyAsync function, which checks if the write security
            // group of the source owner is still valid.
            if (action == WriteAction.Create)
            {
                // For create operation, we should return source owner as data owner.
                var owner = await this.GetExistingOwnerAsync(incomingEntity.SourceOwnerId).ConfigureAwait(false);
                if (owner == null)
                {
                    return null;
                }

                return new[] { owner };
            }
            else
            {
                // GetDataOwnersAsync function should not really get invoked for any other operation. Log a trace if it does.
                this.eventWriterFactory.Trace(
                    componentName,
                    $"GetDataOwnersAsync (TransferRequest) invoked for [{action}] operation, Source Owner: [{incomingEntity.SourceOwnerId}], Target Owner: [{incomingEntity.TargetOwnerId}], State: [{incomingEntity.RequestState}].");
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Authorize the given write action.
        /// The AuthorizeAsync method is overridden because the base authorization method does not provide ability to authorize
        /// either source owner or target owner to delete the transfer request. We implement that in this method.
        /// </summary>
        /// <param name="action">The write action.</param>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>A task for authorization.</returns>
        protected override async Task AuthorizeAsync(WriteAction action, TransferRequest incomingEntity)
        {
            if (action == WriteAction.Create)
            {
                // For create operation, we should return source owner as data owner.
                await this.AuthorizationProvider.AuthorizeAsync(this.AuthorizationRoles, () => this.GetSourceDataOwnersAsync(action, incomingEntity)).ConfigureAwait(false);
                return;
            }
            else if (action == WriteAction.Update)
            {
                // Action is set to 'Update' internally when request is approved.
                // So, we should return target owner as data owner.
                await this.AuthorizationProvider.AuthorizeAsync(this.AuthorizationRoles, () => this.GetTargetDataOwnersAsync(action, incomingEntity)).ConfigureAwait(false);
                return;
            }
            else if (action == WriteAction.SoftDelete)
            {
                // For soft delete operation we should return both source and target owners. The source owner should be able to
                // cancel the request and target owner should be able to deny the request, both of which result in soft delete.
                var isAuthorized = await this.AuthorizationProvider.TryAuthorizeAsync(AuthorizationRole.ServiceEditor, () => this.GetSourceDataOwnersAsync(action, incomingEntity)).ConfigureAwait(false);
                if (!isAuthorized)
                {
                    await this.AuthorizationProvider.TryAuthorizeAsync(this.AuthorizationRoles, () => this.GetTargetDataOwnersAsync(action, incomingEntity)).ConfigureAwait(false);
                }

                return;
            }
            else
            {
                // AuthorizeAsync function should not really get invoked for any other operation. Log a trace if it does.
                this.eventWriterFactory.Trace(
                    componentName,
                    $"AuthorizeAsync (TransferRequest) invoked for [{action}] operation, Source Owner: [{incomingEntity.SourceOwnerId}], Target Owner: [{incomingEntity.TargetOwnerId}], State: [{incomingEntity.RequestState}].");
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Helper function to create a new transfer request.
        /// </summary>
        /// <param name="entity">The transfer request object.</param>
        /// <returns>Void task.</returns>
        private async Task<TransferRequest> TransferRequestCreateHelper(TransferRequest entity)
        {
            await this.AuthorizeAsync(WriteAction.Create, entity).ConfigureAwait(false);

            // Update all the asset groups in the request to indicate that there is a pending transfer request.
            // TODO: The function below may filter the asset groups in the request that might have been deleted.
            // Is that really a problem??
            var assetGroups = await this.GetExistingAssetGroupsAsync(entity.AssetGroups).ConfigureAwait(false);
            foreach (var assetGroup in assetGroups)
            {
                assetGroup.HasPendingTransferRequest = true;
            }

            // Update the data owner object for SourceOwnerId to indicate that it has initiated transfer requests.
            // TODO: We need to probably sync with the approval of other transfer request for the same owner.
            // Probably ETag might help with that. But then we might need to retry if this operation fails due to 
            // ETag mismatch!
            var sourceOwner = await this.GetExistingOwnerAsync(entity.SourceOwnerId).ConfigureAwait(false);
            if (sourceOwner == null)
            {
                // This should not really happen, except for extreme edge case.
                throw new ConflictException(ConflictType.DoesNotExist, "The source owner id in the request does not exist anymore.", "sourceOwnerId", entity.SourceOwnerId.ToString());
            }

            sourceOwner.HasInitiatedTransferRequests = true;

            // Update the data owner object for TargetOwnerId to indicate that it has pendingtransfer requests.
            // TODO: We need to probably sync with the cancellation of other pending transfer request for 
            // the same owner. Probably ETag might help with that. But then we might need to retry if this 
            // operation fails due to ETag mismatch!
            var targetOwner = await this.GetExistingOwnerAsync(entity.TargetOwnerId).ConfigureAwait(false);
            if (targetOwner == null)
            {
                // This should not really happen, except for extreme edge case.
                throw new ConflictException(ConflictType.DoesNotExist, "The target owner id in the request does not exist anymore.", "targetOwnerId", entity.TargetOwnerId.ToString());
            }

            targetOwner.HasPendingTransferRequests = true;

            // Set the state to 'Pending' to begin with before creating the object.
            // TODO: Do we need to populate tracking details for transfer request? Probably not!
            entity.RequestState = TransferRequestStates.Pending;

            var updatedEntities =
                new[] { (Entity)sourceOwner, targetOwner }.Concat(assetGroups)
                .Select(x =>
                {
                    this.PopulateProperties(WriteAction.Update, x);
                    return x;
                })
                .ToList();

            return await this.StorageWriter.CreateTransferRequestAsync(entity, updatedEntities).ConfigureAwait(false);
        }

        /// <summary>
        /// Helper function to delete the specified transfer request.
        /// </summary>
        /// <param name="entity">The transfer request object.</param>
        /// <param name="action">The write action - Write or soft delete.</param>
        /// <returns>Void task.</returns>
        private async Task TransferRequestDeleteHelper(TransferRequest entity, WriteAction action)
        {
            await this.AuthorizeAsync(action, entity).ConfigureAwait(false);

            // Update all the asset groups in the request.
            var assetGroups = await this.GetExistingAssetGroupsAsync(entity.AssetGroups).ConfigureAwait(false);
            foreach (var assetGroup in assetGroups)
            {
                // Indicate that there is no pending request anymore.
                assetGroup.HasPendingTransferRequest = false;

                // If the request is apporved, change the owner id of the asset group to target owner.
                if (action == WriteAction.Update)
                {
                    assetGroup.OwnerId = entity.TargetOwnerId;
                }
            }

            // Find out of data owner object for SourceOwnerId has other initiated transfer requests.
            // If not, set the HasInitiatedTransferRequests to false.
            var sourceOwner = await this.GetExistingOwnerAsync(entity.SourceOwnerId).ConfigureAwait(false);
            if (sourceOwner != null)
            {
                // Source owner being null should not really happen, except for extreme edge case.
                // But we are deleting the request, so there is no point in throwing exception here.
                sourceOwner.HasInitiatedTransferRequests = await this.HasOtherInitiatedTransferRequests(entity.SourceOwnerId).ConfigureAwait(false);
                sourceOwner.HasPendingTransferRequests = await this.HasOtherPendingTransferRequests(entity.SourceOwnerId).ConfigureAwait(false);
            }

            // Find out of data owner object for TargetOwnerId has other initiated transfer requests.
            // If not, set the HasInitiatedTransferRequests to false.
            var targetOwner = await this.GetExistingOwnerAsync(entity.TargetOwnerId).ConfigureAwait(false);
            if (targetOwner != null)
            {
                // Target owner being null should not really happen, except for extreme edge case.
                // But we are deleting the request, so there is no point in throwing exception here.
                targetOwner.HasInitiatedTransferRequests = await this.HasOtherInitiatedTransferRequests(entity.TargetOwnerId).ConfigureAwait(false);
                targetOwner.HasPendingTransferRequests = await this.HasOtherPendingTransferRequests(entity.TargetOwnerId).ConfigureAwait(false);
            }

            // Set the state to 'Cancelled' before deleting the object.
            TransferRequestStates deleteState = (action == WriteAction.Update) ? TransferRequestStates.Approved : TransferRequestStates.Cancelled;
            entity.RequestState = deleteState;
            entity.IsDeleted = true;

            // Create a list of updated entries and update their tracking details.
            var updatedEntities =
                new[] { (Entity)entity, sourceOwner, targetOwner }.Concat(assetGroups)
                .Select(x =>
                {
                    this.PopulateProperties(WriteAction.Update, x);
                    return x;
                })
                .ToList();

            await this.StorageWriter.UpdateEntitiesAsync(updatedEntities).ConfigureAwait(false);
        }

        /// <summary>
        /// Helper function to check if the linked data owner exists.
        /// </summary>
        /// <param name="ownerId">The data owner id.</param>
        /// <returns>Void task.</returns>
        private async Task OwnerShouldExist(Guid ownerId)
        {
            var existingDataOwner = await this.GetExistingOwnerAsync(ownerId).ConfigureAwait(false);

            if (existingDataOwner == null)
            {
                throw new ConflictException(ConflictType.DoesNotExist, "The referenced entity does not exist", "ownerId", ownerId.ToString());
            }
        }

        /// <summary>
        /// Helper function to validate the asset groups specified in the transfer request.
        /// </summary>
        /// <param name="assetGroupIds">The asset group ids.</param>
        /// <param name="ownerId">The owner id of the request.</param>
        /// <returns>Void task.</returns>
        private async Task ValidateAssetGroups(IEnumerable<Guid> assetGroupIds, Guid ownerId)
        {
            // First get all the asset group objects based on the asset group ids.
            var assetGroups = await this.GetExistingAssetGroupsAsync(assetGroupIds).ConfigureAwait(false);

            // Find out if any of them are missing.
            var missingAssetGroups = assetGroupIds.Except(assetGroups.Select(x => x.Id));
            if (missingAssetGroups.Any())
            {
                var missingId = missingAssetGroups.First().ToString();
                throw new ConflictException(ConflictType.DoesNotExist, "Asset group does not exist in storage.", "assetGroup", missingId);
            }

            // Now for each asset group, ensure it has the same owner id as specified as the source owner 
            // in the request and it does not have any pending transfer request.
            foreach (var assetGroup in assetGroups)
            {
                if (assetGroup.OwnerId == Guid.Empty)
                {
                    throw new ConflictException(ConflictType.DoesNotExist, "The asset group does not have any owner associated with it.", "assetGroupOwner", assetGroup.Id.ToString());
                }

                if (assetGroup.OwnerId != ownerId)
                {
                    throw new ConflictException(ConflictType.InvalidValue, "The asset group does not have the same owner id as the request.", "assetGroupOwner", assetGroup.Id.ToString());
                }

                if (assetGroup.HasPendingTransferRequest)
                {
                    throw new ConflictException(ConflictType.LinkedEntityExists, "The asset group already has a pending transfer request.", "assetGroup", assetGroup.Id.ToString());
                }
            }
        }

        private Task<DataOwner> GetExistingOwnerAsync(Guid ownerId)
        {
            // TODO: Consider using MemoizeAsync and investigate why unit test fails if we do so. 
            return this.dataOwnerReader.ReadByIdAsync(ownerId, ExpandOptions.WriteProperties);
        }

        private Task<IEnumerable<AssetGroup>> GetExistingAssetGroupsAsync(IEnumerable<Guid> assetGroupIds)
        {
            // TODO: Consider using MemoizeAsync and investigate why feature test (CreateATransferRequest) fails if we do so. 
            return this.assetGroupReader.ReadByIdsAsync(assetGroupIds, ExpandOptions.WriteProperties);
        }

        private async Task<bool> HasOtherInitiatedTransferRequests(Guid ownerId)
        {
            var transferRequestFilterCriteria = new TransferRequestFilterCriteria
            {
                SourceOwnerId = ownerId,
                Count = 0
            };

            var transferRequests = await this.EntityReader.ReadByFiltersAsync(transferRequestFilterCriteria, ExpandOptions.None).ConfigureAwait(false);

            return transferRequests.Total > 1;
        }

        private async Task<bool> HasOtherPendingTransferRequests(Guid ownerId)
        {
            var transferRequestFilterCriteria = new TransferRequestFilterCriteria
            {
                TargetOwnerId = ownerId
            };

            var transferRequests = await this.EntityReader.ReadByFiltersAsync(transferRequestFilterCriteria, ExpandOptions.None).ConfigureAwait(false);

            return transferRequests.Total > 1;
        }
    }
}