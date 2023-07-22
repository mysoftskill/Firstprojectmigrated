namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Writer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using AutoMapper;
    using Microsoft.PrivacyServices.DataManagement.Common;
    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authorization;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.AzureQueue;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Reader;
    using Microsoft.PrivacyServices.DataManagement.Models;
    using Microsoft.PrivacyServices.DataManagement.Models.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;
    using Microsoft.PrivacyServices.Policy;

    using Microsoft.Azure.Storage.Queue;
    using Microsoft.PrivacyServices.DataManagement.Common.FileSystem.KeyVault;

    /// <summary>
    /// Provides methods for writing variant request information.
    /// </summary>
    public class VariantRequestWriter : EntityWriter<VariantRequest>, IVariantRequestWriter
    {
        private readonly IDataOwnerReader dataOwnerReader;
        private readonly IAssetGroupReader assetGroupReader;
        private readonly IVariantDefinitionReader variantDefinitionReader;
        private readonly AuthenticatedPrincipal authenticatedPrincipal;
        private readonly IDateFactory dateFactory;
        private readonly Policy policy;
        private readonly ICloudQueue variantRequestsQueue;
        private readonly ICloudQueueConfig cloudQueueConfig;
        private readonly IEventWriterFactory eventWriterFactory;
        private readonly IAzureKeyVaultReader keyVaultReader;        

        private IEnumerable<AssetGroup> assetGroupUpdates; // Asset group updates for request create or update.

        /// <summary>
        /// Initializes a new instance of the <see cref="VariantRequestWriter" /> class.
        /// </summary>
        /// <param name="storageWriter">The storage writer instance.</param>
        /// <param name="entityReader">The entity reader instance.</param>
        /// <param name="authenticatedPrincipal">The principal object created for this specific request.</param>
        /// <param name="authorizationProvider">The authorization provider.</param>
        /// <param name="dateFactory">The date factory instance.</param>
        /// <param name="mapper">A utility for mapping data types by convention.</param>
        /// <param name="dataOwnerReader">The reader for data owners.</param>
        /// <param name="assetGroupReader">The reader for asset groups.</param>
        /// <param name="variantDefinitionReader">The reader for variant definitions.</param>
        /// <param name="policy">The policy instance.</param>
        /// <param name="variantRequestsQueue">The azure queue to write new ids to so that we can create a work item.</param>
        /// <param name="cloudQueueConfig">The azure queue configuration information.</param>
        /// <param name="keyVaultReader">The azure key vault reader.</param>
        /// <param name="eventWriterFactory">The event writer factory instance.</param>
        public VariantRequestWriter(
            IPrivacyDataStorageWriter storageWriter,
            IVariantRequestReader entityReader,
            AuthenticatedPrincipal authenticatedPrincipal,
            IAuthorizationProvider authorizationProvider,
            IDateFactory dateFactory,
            IMapper mapper,
            IDataOwnerReader dataOwnerReader,
            IAssetGroupReader assetGroupReader,
            IVariantDefinitionReader variantDefinitionReader,
            Policy policy,
            ICloudQueue variantRequestsQueue,
            ICloudQueueConfig cloudQueueConfig,
            IAzureKeyVaultReader keyVaultReader,
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
            this.variantDefinitionReader = variantDefinitionReader;
            this.authenticatedPrincipal = authenticatedPrincipal;
            this.dateFactory = dateFactory;
            this.policy = policy;
            this.variantRequestsQueue = variantRequestsQueue;
            this.eventWriterFactory = eventWriterFactory;
            this.keyVaultReader = keyVaultReader;
            this.cloudQueueConfig = cloudQueueConfig;
            this.AuthorizationRoles = AuthorizationRole.ServiceEditor | AuthorizationRole.VariantEditor;           
        }

        /// <summary>
        /// Validate the properties of the incoming entity.
        /// </summary>
        /// <param name="action">The write action for the request.</param>
        /// <param name="incomingEntity">The incoming entity.</param>
        public override void ValidateProperties(WriteAction action, VariantRequest incomingEntity)
        {
            base.ValidateProperties(action, incomingEntity);

            ValidationModule.PropertyRequired(incomingEntity.OwnerId, "ownerId");
            ValidationModule.PropertyRequiredAndNotEmpty(incomingEntity.RequestedVariants, "requestedVariants");
            ValidationModule.PropertyRequiredAndNotEmpty(incomingEntity.VariantRelationships, "variantRelationships");

            this.RequestedVariantsCannotHaveDuplicateValues(incomingEntity.RequestedVariants.Select(x => x.VariantId));
            this.VariantRelationshipsShouldHaveValidAssetGroupId(incomingEntity.VariantRelationships);
        }

        /// <summary>
        /// Ensure consistency between the incoming entity and any existing entities.
        /// </summary>
        /// <param name="action">The write action for the request.</param>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>A task that performs the checks.</returns>
        public override async Task ValidateConsistencyAsync(WriteAction action, VariantRequest incomingEntity)
        {
            await base.ValidateConsistencyAsync(action, incomingEntity).ConfigureAwait(false);

            if (action == WriteAction.Create)
            {
                await this.OwnerShouldExist(incomingEntity.OwnerId).ConfigureAwait(false);

                var variantIds = incomingEntity.RequestedVariants.Select(x => x.VariantId);
                await this.VariantDefinitionsShouldExist(variantIds).ConfigureAwait(false);

                var assetGroupIds = incomingEntity.VariantRelationships.Keys;

                this.CheckAssetGroupsCount(assetGroupIds);

                await this.AssetGroupsShouldExist(assetGroupIds).ConfigureAwait(false);                

                await this.AssetGroupsShouldHaveTheSameOwnerAsTheRequest(assetGroupIds, incomingEntity.OwnerId).ConfigureAwait(false);

                await this.VariantRequestShouldNotDuplicateAnExistingRequest(variantIds, assetGroupIds).ConfigureAwait(false);

                this.assetGroupUpdates = await this.CalculateAssetGroupUpdates(action, incomingEntity).ConfigureAwait(false);
            }
            else if (action == WriteAction.Update)
            {
                var existingVariantRequest = await this.GetExistingEntityAsync(incomingEntity).ConfigureAwait(false);

                if (incomingEntity.OwnerId != existingVariantRequest.OwnerId)
                {
                    throw new ConflictException(ConflictType.InvalidValue_Immutable, "Owner id is immutable.", "ownerId");
                }

                if (incomingEntity.OwnerName != existingVariantRequest.OwnerName)
                {
                    throw new ConflictException(ConflictType.InvalidValue_Immutable, "Owner name is immutable.", "ownerName");
                }

                // Compare incoming list of variants with existing list of variants
                bool variantIdsAreEqual = incomingEntity.RequestedVariants.Select(x => x.VariantId).OrderBy(i => i).SequenceEqual(existingVariantRequest.RequestedVariants.Select(x => x.VariantId).OrderBy(i => i));

                // Variant Ids cannot be changed
                if (!variantIdsAreEqual)
                {
                    throw new ConflictException(ConflictType.InvalidValue_Immutable, "RequestedVariants are immutable.", "requestedVariants");
                }

                // Compare incoming list of asset groups with existing list of assetgroups
                bool assetGroupIdsAreEqual = incomingEntity.VariantRelationships.Select(x => x.Value.AssetGroupId).OrderBy(i => i).SequenceEqual(existingVariantRequest.VariantRelationships.Select(x => x.Value.AssetGroupId).OrderBy(i => i));

                // Asset Groups cannot be changed
                if (!assetGroupIdsAreEqual)
                {
                    throw new ConflictException(ConflictType.InvalidValue_Immutable, "VariantRelationships are immutable.", "variantRelationships");
                }
            }
            else
            {
                // We deleting a request... check if any flags need to change
                this.assetGroupUpdates = await this.CalculateAssetGroupUpdates(action, incomingEntity).ConfigureAwait(false);
            }

            await this.PopulateVariantRequestMetaData(action, incomingEntity).ConfigureAwait(false);
        }

        /// <summary>
        /// Write the entity in storage.
        /// </summary>
        /// <param name="action">The write action for the request.</param>
        /// <param name="entity">The incoming entity.</param>
        /// <returns>A task that performs the checks.</returns>
        public override async Task<VariantRequest> WriteAsync(WriteAction action, VariantRequest entity)
        {
            // If there are asset group changes, then we must update both the request and the asset groups
            // in a single transaction.
            if (this.assetGroupUpdates != null && this.assetGroupUpdates.Any())
            {
                // Update the asset group tracking information.
                foreach (var assetGroup in this.assetGroupUpdates)
                {
                    this.PopulateProperties(WriteAction.Update, assetGroup);
                }

                var result = await this.StorageWriter.UpsertVariantRequestWithSideEffectsAsync(entity, this.assetGroupUpdates).ConfigureAwait(false);
                if (result != null)
                {
                    await this.EnqueueVariantRequestWorkItemQueueAsync(result).ConfigureAwait(false);
                }
                return result;
            }
            else if (action == WriteAction.Create)
            {
                // New Variant Requests with no side effects end up here
                var result = await this.StorageWriter.CreateVariantRequestAsync(entity).ConfigureAwait(false);

                if (result != null)
                {
                    await this.EnqueueVariantRequestWorkItemQueueAsync(result).ConfigureAwait(false);
                }
                return result;
            }
            else if (action == WriteAction.Update)
            {
                return await this.StorageWriter.UpdateVariantRequestAsync(entity).ConfigureAwait(false);
            }
            else if (action == WriteAction.SoftDelete)
            {
                // When a request is deleted, we need to remove any necessary HasPendingVariantRequests flag.
                var removedAssetGroups = await this.CalculateAssetGroupsNeedToRemoveFlag(entity.VariantRelationships.Keys).ConfigureAwait(false);

                foreach (var assetGroup in removedAssetGroups)
                {
                    assetGroup.HasPendingVariantRequests = false;

                    this.PopulateProperties(WriteAction.Update, assetGroup);
                }

                await this.StorageWriter.UpdateEntitiesAsync(new[] { (Entity)entity }.Concat(removedAssetGroups)).ConfigureAwait(false);

                return null; // Delete does not return a value, so no need to extract the correct entity from the storage results.
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Approves the variant request.
        /// Approval results in the deletion of the request
        /// and an update on all associated asset groups.
        /// </summary>
        /// <param name="id">The id of the variant request to approve.</param>
        /// <param name="etag">The ETag of the variant request.</param>
        /// <returns>A task that performs the asynchronous execution.</returns>
        public async Task ApproveAsync(Guid id, string etag)
        {
            // The approver needs to be in the variant admin group.
            await this.AuthorizationProvider.AuthorizeAsync(AuthorizationRole.VariantEditor).ConfigureAwait(false);

            var existingEntity = await this.GetExistingEntityWithConsistencyChecks(id, etag).ConfigureAwait(false);

            // Delete the request as part of approving it.
            existingEntity.IsDeleted = true;

            // Link the asset groups to the requested variants.
            var assetGroupIds = existingEntity.VariantRelationships.Keys;
            var assetGroups = await this.GetExistingAssetGroupsAsync(assetGroupIds).ConfigureAwait(false);
            foreach (var assetGroup in assetGroups)
            {
                var variantsDict = assetGroup.Variants != null ? assetGroup.Variants.ToDictionary(x => x.VariantId) : new Dictionary<Guid, AssetGroupVariant>();

                foreach (var variant in existingEntity.RequestedVariants)
                {
                    variant.VariantState = VariantState.Approved;
                    // Add the work item uri if it is non-null and isn't already in the list
                    if (existingEntity.WorkItemUri != null)
                    {
                        if (variant.TfsTrackingUris == null)
                        {
                            variant.TfsTrackingUris = new List<Uri>() { existingEntity.WorkItemUri };
                        }
                        else if (!variant.TfsTrackingUris.Contains(existingEntity.WorkItemUri))
                        {
                            variant.TfsTrackingUris = variant.TfsTrackingUris.Append(existingEntity.WorkItemUri);
                        }
                    }
                    if (variantsDict.ContainsKey(variant.VariantId))
                    {
                        variantsDict[variant.VariantId] = variant;
                    }
                    else
                    {
                        variantsDict.Add(variant.VariantId, variant);
                    }
                }

                assetGroup.Variants = variantsDict.Values;
            }

            // If any asset group no longer has any other pending requests, turn off its HasPendingVariantRequests flag.
            var removedAssetGroups = await this.CalculateAssetGroupsNeedToRemoveFlag(assetGroupIds).ConfigureAwait(false);

            foreach (var assetGroup in removedAssetGroups)
            {
                assetGroup.HasPendingVariantRequests = false;
            }

            // Update all entities with proper tracking details.
            var updatedEntities =
                new[] { (Entity)existingEntity }.Concat(assetGroups)
                .Select(x =>
                {
                    this.PopulateProperties(WriteAction.Update, x);
                    return x;
                })
                .ToList();

            // Store the changes.
            await this.StorageWriter.UpdateEntitiesAsync(updatedEntities).ConfigureAwait(false);
        }

        /// <summary>
        /// Get the data owners linked to the incoming entity.
        /// </summary>
        /// <param name="action">The write action for the request.</param>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>The data owner list.</returns>
        public override async Task<IEnumerable<DataOwner>> GetDataOwnersAsync(WriteAction action, VariantRequest incomingEntity)
        {
            var owner = await this.GetExistingOwnerAsync(incomingEntity.OwnerId).ConfigureAwait(false);

            if (owner == null)
            {
                return null; // Exit early. Other validations will fail if no security groups are set.
            }

            return new[] { owner };
        }

        /// <summary>
        /// Customize the authorization for the given write action.
        /// </summary>
        /// <param name="action">The write action.</param>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>A task for authorization.</returns>
        protected override async Task AuthorizeAsync(WriteAction action, VariantRequest incomingEntity)
        {
            if (action == WriteAction.SoftDelete)
            {
                // This is a temporary fix, so that we unblock the VariantEditor from denying variant requests.
                // In the long run, we should send signals back to the owners indicating that their requests have been denied.
                // The denying operation does not necessarily delete the requests, in which case, we need a separate operation type similar as Approve.
                // Currently, the denying operation from PCD is calling our delete API as well.
                bool isVariantAdmin = await this.AuthorizationProvider.TryAuthorizeAsync(AuthorizationRole.VariantEditor, null).ConfigureAwait(false);

                if (isVariantAdmin)
                {
                    return; // When the user is VariantAdmin, we pass the authorization check for them.
                }
            }
            else if (action == WriteAction.Update)
            {
                // Must be a VariantEditor to change a variant request
                await this.AuthorizationProvider.AuthorizeAsync(AuthorizationRole.VariantEditor).ConfigureAwait(false);
                return;
            }

            await base.AuthorizeAsync(action, incomingEntity).ConfigureAwait(false);
        }

        /// <summary>
        /// Update the impacted asset groups for this request. 
        /// If we are creating a request and the asset group doesn't already have the HasPendingRequest flag set, then set it.
        /// If we are deleting a request and the asset group isn't part of any other request, then turn off HasPendingRequest flag.
        /// </summary>
        /// <param name="action">The write action type.</param>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>The list of asset group to update.</returns>
        private async Task<IEnumerable<AssetGroup>> CalculateAssetGroupUpdates(WriteAction action, VariantRequest incomingEntity)
        {
            IEnumerable<AssetGroup> assetGroupsToUpdate = Enumerable.Empty<AssetGroup>();

            if (action == WriteAction.Create)
            {
                var assetGroups = await this.GetExistingAssetGroupsAsync(incomingEntity.VariantRelationships.Keys).ConfigureAwait(false);

                // Only need to update asset groups that don't already have the HadPendingVariantRequests flag set
                assetGroupsToUpdate = assetGroups.Where(x => x.HasPendingVariantRequests == false).Select(x => { x.HasPendingVariantRequests = true; return x; } ).ToList();
            }
            else if (action == WriteAction.Update)
            {
                // We shouldn't end up here...
                throw new ConflictException(ConflictType.InvalidValue_Immutable, "RequestedVariants are immutable.", "requestedVariants");
            }

            return assetGroupsToUpdate;
        }

        /// <summary>
        /// Find the asset groups that do not have any pending requests any more.
        /// </summary>
        /// <param name="assetGroupIds">Asset group ids.</param>
        /// <returns>The list of updated asset groups.</returns>
        private async Task<IEnumerable<AssetGroup>> CalculateAssetGroupsNeedToRemoveFlag(IEnumerable<Guid> assetGroupIds)
        {
            var neededAssetGroupIds = new List<Guid>();

            foreach (var assetGroupId in assetGroupIds)
            {
                var variantRequestFilterCriteria = new VariantRequestFilterCriteria
                {
                    AssetGroupId = assetGroupId
                };

                var variantRequests = await this.EntityReader.ReadByFiltersAsync(variantRequestFilterCriteria, ExpandOptions.None).ConfigureAwait(false);

                if (variantRequests.Total == 1)
                {
                    neededAssetGroupIds.Add(assetGroupId);
                }
            }

            return await this.GetExistingAssetGroupsAsync(neededAssetGroupIds).ConfigureAwait(false);
        }

        /// <summary>
        /// Populate the variant request metadata: owner name, asset qualifier.
        /// </summary>
        /// <param name="action">The write action type.</param>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>Void task.</returns>
        private async Task PopulateVariantRequestMetaData(WriteAction action, VariantRequest incomingEntity)
        {
            if (action == WriteAction.Create)
            {
                var owner = await this.GetExistingOwnerAsync(incomingEntity.OwnerId).ConfigureAwait(false);

                incomingEntity.OwnerName = owner.Name;

                await this.PopulateAssetQualifier(incomingEntity, incomingEntity.VariantRelationships.Keys).ConfigureAwait(false);
            }
            else if (action == WriteAction.Update)
            {
                var existingEntity = await this.GetExistingEntityAsync(incomingEntity).ConfigureAwait(false);

                var newAssetGroupIds = incomingEntity.VariantRelationships.Keys.Except(existingEntity.VariantRelationships.Keys);

                await this.PopulateAssetQualifier(incomingEntity, newAssetGroupIds).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Populate the asset qualifier to the request.
        /// </summary>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <param name="assetGroupIds">The set of asset groups to populate.</param>
        /// <returns>Void task.</returns>
        private async Task PopulateAssetQualifier(VariantRequest incomingEntity, IEnumerable<Guid> assetGroupIds)
        {
            var assetGroups = await this.GetExistingAssetGroupsAsync(assetGroupIds).ConfigureAwait(false);

            foreach (var assetGroup in assetGroups)
            {
                incomingEntity.VariantRelationships[assetGroup.Id].AssetQualifier = assetGroup.Qualifier;
            }
        }

        /// <summary>
        /// Helper function to check if the requested variants has any duplicates.
        /// </summary>
        /// <param name="requestedVariants">The requested variants.</param>
        private void RequestedVariantsCannotHaveDuplicateValues(IEnumerable<Guid> requestedVariants)
        {
            if (requestedVariants.Count() != requestedVariants.Distinct().Count())
            {
                throw new InvalidPropertyException("requestedVariants", requestedVariants.ToString(), "The variant request cannot have duplicate variants.");
            }
        }

        /// <summary>
        /// Helper function to check that there is no existing or pending request for any of the incoming assetgroup/variant combinations.
        /// </summary>
        /// <param name="variantIds">The new variant ids of the request.</param>
        /// <param name="assetGroupIds">The new asset group ids of the request.</param>
        /// <returns>Void task.</returns>
        private async Task VariantRequestShouldNotDuplicateAnExistingRequest(IEnumerable<Guid> variantIds, IEnumerable<Guid> assetGroupIds)
        {
            // Check for pending duplicate requests:
            // Check if there are any pending requests that already have any of the assetgroup/variant combinations
            foreach (var assetGroupId in assetGroupIds)
            {
                var variantRequestFilterCriteria = new VariantRequestFilterCriteria
                {
                    AssetGroupId = assetGroupId
                };

                var variantRequestFilterResults = await this.EntityReader.ReadByFiltersAsync(variantRequestFilterCriteria, ExpandOptions.None).ConfigureAwait(false);

                if (variantRequestFilterResults.Total > 0)
                {
                    foreach (var pendingRequest in variantRequestFilterResults.Values)
                    {
                        if (pendingRequest.RequestedVariants.Any(x => variantIds.Contains(x.VariantId)))
                        {
                            var matchingVariants = pendingRequest.RequestedVariants.Where(x => variantIds.Contains(x.VariantId)).Select(x => x.VariantId);
                            throw new ConflictException(ConflictType.AlreadyExists,
                                                        "Asset group has a pending variant request for one of the requested variants.",
                                                        $"variantRequest[{assetGroupId}].RequestedVariants",
                                                        string.Join(",", matchingVariants));
                        }
                    }
                }
            }

            // Check for approved duplicate requests:
            // Check that none of the listed asset groups already have a link to one of the requested variants
            var assetGroups = await this.GetExistingAssetGroupsAsync(assetGroupIds).ConfigureAwait(false);
            foreach (var assetGroup in assetGroups)
            {
                // check if the asset group already has a link to one of the variants
                if (assetGroup.Variants != null && assetGroup.Variants.Any(x => variantIds.Contains(x.VariantId)))
                {
                    var matchingVariants = assetGroup.Variants.Where(x => variantIds.Contains(x.VariantId)).Select(x => x.VariantId);
                    throw new ConflictException(ConflictType.AlreadyExists, 
                                                "Asset group has an existing asset group-variant link for one of the requested variants.", 
                                                $"variantRelationships[{assetGroup.Id}].assetGroup.Variants", 
                                                string.Join(",", matchingVariants));
                }
            }

        }

        /// <summary>
        /// Helper function to check if all variant relationships in the request has valid asset group id.
        /// </summary>
        /// <param name="variantRelationships">The variant relationships of the request.</param>
        private void VariantRelationshipsShouldHaveValidAssetGroupId(IDictionary<Guid, VariantRelationship> variantRelationships)
        {
            foreach (var relation in variantRelationships)
            {
                ValidationModule.PropertyRequired(relation.Value.AssetGroupId, "variantRelationships.assetGroupId");
            }
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
        /// Helper function to check if the linked variant definitions exist.
        /// </summary>
        /// <param name="variantIds">The variant ids.</param>
        /// <returns>Void task.</returns>
        private async Task VariantDefinitionsShouldExist(IEnumerable<Guid> variantIds)
        {
            var variants = await this.GetExistingVariantDefinitionsAsync(variantIds).ConfigureAwait(false);

            var missingVariants = variantIds.Except(variants.Select(x => x.Id));

            if (missingVariants.Any())
            {
                var missingId = missingVariants.First().ToString();

                throw new ConflictException(ConflictType.DoesNotExist, "Variant does not exist in storage.", "requestedVariants", missingId);
            }
        }

        /// <summary>
        /// Helper function to check if the linked asset groups exist.
        /// </summary>
        /// <param name="assetGroupIds">The asset group ids.</param>
        /// <returns>Void task.</returns>
        private async Task AssetGroupsShouldExist(IEnumerable<Guid> assetGroupIds)
        {
            var assetGroups = await this.GetExistingAssetGroupsAsync(assetGroupIds).ConfigureAwait(false);

            var missingAssetGroups = assetGroupIds.Except(assetGroups.Select(x => x.Id));

            if (missingAssetGroups.Any())
            {
                var missingId = missingAssetGroups.First().ToString();

                throw new ConflictException(ConflictType.DoesNotExist, "Asset group does not exist in storage.", $"variantRelationships[{missingId}].assetGroup", missingId);
            }
        }

        /// <summary>
        /// Helper function to check if the variant request contain less than 100 asset groups.
        /// </summary>
        /// <param name="assetGroupIds">The asset group ids.</param>
        /// <returns>Void task.</returns>
        private void CheckAssetGroupsCount(IEnumerable<Guid> assetGroupIds)
        {
            var assetGroupsCount = assetGroupIds.Count();
            if (assetGroupsCount > 100)
            {
                throw new ConflictException(ConflictType.InvalidValue_BadCombination, "Variant request must contain less than or equal to 100 asset groups.", null);
            }
        }

        /// <summary>
        /// Helper function to check if all the asset groups have the same owner as the request owner id.
        /// </summary>
        /// <param name="assetGroupIds">The new asset group ids of the request.</param>
        /// <param name="ownerId">The owner id of the request.</param>
        /// <returns>Void task.</returns>
        private async Task AssetGroupsShouldHaveTheSameOwnerAsTheRequest(IEnumerable<Guid> assetGroupIds, Guid ownerId)
        {
            var assetGroups = await this.GetExistingAssetGroupsAsync(assetGroupIds).ConfigureAwait(false);

            foreach (var assetGroup in assetGroups)
            {
                if (assetGroup.OwnerId == Guid.Empty)
                {
                    throw new ConflictException(ConflictType.DoesNotExist, "All asset groups must be associated with an owner.", $"variantRelationships[{assetGroup.Id}].assetGroup.ownerId");
                }

                if (assetGroup.OwnerId != ownerId)
                {
                    throw new ConflictException(ConflictType.InvalidValue, "All asset groups must share the same owner id as the request.", $"variantRelationships[{assetGroup.Id}].assetGroup.ownerId", assetGroup.OwnerId.ToString());
                }
            }
        }
        
        private Task<DataOwner> GetExistingOwnerAsync(Guid ownerId)
        {
            return this.MemoizeAsync(ownerId, () => this.dataOwnerReader.ReadByIdAsync(ownerId, ExpandOptions.None));
        }

        private Task<IEnumerable<AssetGroup>> GetExistingAssetGroupsAsync(IEnumerable<Guid> assetGroupIds)
        {
            // We need write properties in order to perform updates on the asset groups.
            return this.MemoizeEntitiesAsync(assetGroupIds, ids => this.assetGroupReader.ReadByIdsAsync(ids, ExpandOptions.WriteProperties));
        }

        private Task<IEnumerable<VariantDefinition>> GetExistingVariantDefinitionsAsync(IEnumerable<Guid> variantIds)
        {
            return this.MemoizeEntitiesAsync(variantIds, ids => this.variantDefinitionReader.ReadByIdsAsync(ids, ExpandOptions.None));
        }

        // If the request does not contain any excluded variant definitions,
        // write the request id to a queue so that the we can create a work item
        private async Task EnqueueVariantRequestWorkItemQueueAsync(VariantRequest entity)
        {
            if (cloudQueueConfig.EnableWriteToPafVariantRequestsQueue == true)
            {
                // Check key vault for a list of excluded variant definition ids.
                IList<string> variantDefinitionExclusionList = new List<string>();
                try
                {
                    // If this list is not empty, it contains a list of variant definitions
                    // for which we shouldn't create an ADO work item.
                    var exclusionList = this.keyVaultReader.GetSecretByNameAsync("VariantDefinitionExclusionList").GetAwaiter().GetResult();
                    variantDefinitionExclusionList = exclusionList?.Split(',').ToList();
                }
                catch (Exception ex)
                {
                    // Log as SuppressedException so that we can create an alert for it,
                    // but not return an error for the variant request creation.
                    this.eventWriterFactory.SuppressedException(
                        nameof(VariantRequestWriter),
                        new SuppressedException($"VariantRequestWriter.EnqueueVariantRequestWorkItemQueueAsync({entity.Id})", ex));
                }

                // If none of the variant definitions in the list are excluded, then
                // put the request id in the queue for the work item creator.
                var excludedVariantIds = entity.RequestedVariants.Select(x => x.VariantId.ToString()).Intersect(variantDefinitionExclusionList);
                if (excludedVariantIds.Count() == 0)
                {
                    var data = "{ \"variantRequestId\" : \"" + entity.Id.ToString() + "\" }";
                    var message = new CloudQueueMessage(data);

                    try
                    {
                        TimeSpan timeToLive = TimeSpan.FromDays(cloudQueueConfig.PafQueueItemExpiryDurationInDays);
                        await this.variantRequestsQueue.AddMessageAsync(message, timeToLive).ConfigureAwait(false);
                    }
                    catch (CloudQueueException ex)
                    {
                        // Log as SuppressedException so that we can create an alert for it,
                        // but not return an error for the variant request creation.
                        this.eventWriterFactory.SuppressedException(
                            nameof(VariantRequestWriter),
                            new SuppressedException($"VariantRequestWriter.EnqueueVariantRequestWorkItemQueueAsync({entity.Id})", ex));
                    }
                }
            }
        }

        public new async Task<VariantRequest> CreateAsync(VariantRequest incomingEntity)
        {
            DataOwner dataOwner = await this.GetExistingOwnerAsync(incomingEntity.OwnerId);
            if (dataOwner != null)
            {
                string unknown = "Unknown";
                string serviceTreeId = dataOwner.ServiceTree != null ? dataOwner.ServiceTree.ServiceId ?? unknown : unknown;
                string organizationName = dataOwner.ServiceTree != null ? dataOwner.ServiceTree.OrganizationName ?? unknown : unknown;
                string additionalInformation = $"ServiceTree ID: {serviceTreeId }<br/>Organization Name: {organizationName}<br/>";
                incomingEntity.AdditionalInformation = additionalInformation + incomingEntity.AdditionalInformation;
            }
            return await base.CreateAsync(incomingEntity);
        }

    }
}

