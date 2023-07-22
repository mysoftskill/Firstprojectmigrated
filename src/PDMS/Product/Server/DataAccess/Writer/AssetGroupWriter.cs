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
    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;
    using Microsoft.PrivacyServices.Identity.Metadata;
    using Microsoft.PrivacyServices.Policy;

    /// <summary>
    /// Provides methods for writing asset group information.
    /// </summary>
    public class AssetGroupWriter : EntityWriter<AssetGroup>, IAssetGroupWriter
    {
        private readonly IDataOwnerReader dataOwnerReader;
        private readonly IDeleteAgentReader deleteAgentReader;
        private readonly ISharingRequestReader sharingRequestReader;
        private readonly IVariantDefinitionReader variantDefinitionReader;
        private readonly IManifest identityManifest;
        private readonly Policy policy;
        private readonly IValidator validator;

        private IEnumerable<DeleteAgent> agentUpdates;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetGroupWriter" /> class.
        /// </summary>
        /// <param name="storageWriter">The storage writer instance.</param>
        /// <param name="entityReader">The entity reader instance.</param>
        /// <param name="authenticatedPrincipal">The principal object created for this specific request.</param>
        /// <param name="authorizationProvider">The authorization provider.</param>
        /// <param name="dateFactory">The date factory instance.</param>
        /// <param name="mapper">A utility for mapping data types by convention.</param>
        /// <param name="dataOwnerReader">The reader for data owners.</param>
        /// <param name="deleteAgentReader">The reader for delete agents.</param>
        /// <param name="sharingRequestReader">The reader for sharing requests.</param>
        /// <param name="variantDefinitionReader">The reader for variant definitions.</param>
        /// <param name="identityManifest">The identify manifest instance.</param>
        /// <param name="policy">The current policy data.</param>
        /// <param name="validator">The validator instance.</param>
        public AssetGroupWriter(
            IPrivacyDataStorageWriter storageWriter,
            IAssetGroupReader entityReader,
            AuthenticatedPrincipal authenticatedPrincipal,
            IAuthorizationProvider authorizationProvider,
            IDateFactory dateFactory,
            IMapper mapper,
            IDataOwnerReader dataOwnerReader,
            IDeleteAgentReader deleteAgentReader,
            ISharingRequestReader sharingRequestReader,
            IVariantDefinitionReader variantDefinitionReader,
            IManifest identityManifest,
            Policy policy,
            IValidator validator)
            : base(
                  storageWriter,
                  entityReader,
                  authenticatedPrincipal,
                  authorizationProvider,
                  dateFactory,
                  mapper)
        {
            this.dataOwnerReader = dataOwnerReader;
            this.deleteAgentReader = deleteAgentReader;
            this.sharingRequestReader = sharingRequestReader;
            this.variantDefinitionReader = variantDefinitionReader;
            this.identityManifest = identityManifest;
            this.policy = policy;
            this.validator = validator;

            this.AuthorizationRoles = AuthorizationRole.ServiceEditor;
        }

        /// <summary>
        /// Validate the properties of the incoming entity.
        /// </summary>
        /// <param name="action">The write action for the request.</param>
        /// <param name="incomingEntity">The incoming entity.</param>
        public override void ValidateProperties(WriteAction action, AssetGroup incomingEntity)
        {
            base.ValidateProperties(action, incomingEntity);

            ValidationModule.PropertyRequired(
                () => this.IsNullOrEmpty(incomingEntity.OwnerId) && this.IsNullOrEmpty(incomingEntity.DeleteAgentId),
                "ownerId,deleteAgentId",
                "Owner or DeleteAgent must be set.");

            ValidationModule.PropertyRequired(incomingEntity.Qualifier, "qualifier");

            if (incomingEntity.Variants != null && incomingEntity.Variants.Any())
            {
                foreach (var variant in incomingEntity.Variants)
                {
                    ValidationModule.PropertyRequired(variant.VariantId, "variantId");
                }
            }

            // Referenced entities should not be provided.
            ValidationModule.PropertyShouldNotBeSet(incomingEntity.Owner, "owner", false);
            ValidationModule.PropertyShouldNotBeSet(incomingEntity.DeleteAgent, "deleteAgent", false);
            ValidationModule.PropertyShouldNotBeSet(incomingEntity.ExportAgent, "exportAgent", false);
            ValidationModule.PropertyShouldNotBeSet(incomingEntity.AccountCloseAgent, "accountCloseAgent", false);
            ValidationModule.PropertyShouldNotBeSet(incomingEntity.Inventory, "inventory", false);

            if (action == WriteAction.Create)
            {
                ValidationModule.PropertyShouldNotBeSet(incomingEntity.DeleteSharingRequestId, "deleteSharingRequestId", false);
                ValidationModule.PropertyShouldNotBeSet(incomingEntity.ExportSharingRequestId, "exportSharingRequestId", false);
                ValidationModule.PropertyShouldNotBeSet(incomingEntity.Variants, "variants", false);
                ValidationModule.PropertyShouldNotBeSet(incomingEntity.HasPendingTransferRequest, "hasPendingTransferRequest", false);

                // Always set the compliance state to the default value when creating a new asset group.
                this.SetDefaultComplianceState(incomingEntity);
            }
            else if (action == WriteAction.Update)
            {
                if (incomingEntity.Variants != null && incomingEntity.Variants.Any())
                {
                    this.AssetGroupVariantsCannotHaveDuplicateValues(incomingEntity.Variants.Select(x => x.VariantId));
                }

                if (incomingEntity.DeleteSharingRequestId.HasValue)
                {
                    ValidationModule.MutuallyExclusivePropertyShouldNotBeSet(
                        "deleteSharingRequestId",
                        incomingEntity.DeleteAgentId,
                        "deleteAgentId");
                }

                if (incomingEntity.ExportSharingRequestId.HasValue)
                {
                    ValidationModule.MutuallyExclusivePropertyShouldNotBeSet(
                        "exportSharingRequestId",
                        incomingEntity.ExportAgentId,
                        "exportAgentId");
                }
            }
        }

        /// <summary>
        /// Ensure consistency between the incoming entity and any existing entities.
        /// </summary>
        /// <param name="action">The write action for the request.</param>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>A task that performs the checks.</returns>
        public override async Task ValidateConsistencyAsync(WriteAction action, AssetGroup incomingEntity)
        {
            await base.ValidateConsistencyAsync(action, incomingEntity).ConfigureAwait(false);

            if (action == WriteAction.Create)
            {
                if (incomingEntity.OwnerId != Guid.Empty)
                {
                    await this.OwnerShouldExist(incomingEntity).ConfigureAwait(false);
                }

                if (incomingEntity.DeleteAgentId != null && incomingEntity.DeleteAgentId != Guid.Empty)
                {
                    await this.DeleteAgentShouldExist(incomingEntity).ConfigureAwait(false);

                    await this.DeleteAgentShouldHaveValidProtocols(incomingEntity).ConfigureAwait(false);
                }

                if (incomingEntity.ExportAgentId != null && incomingEntity.ExportAgentId != Guid.Empty)
                {
                    await this.ExportAgentShouldExist(incomingEntity).ConfigureAwait(false);

                    await this.ExportAgentShouldHaveValidProtocols(incomingEntity).ConfigureAwait(false);
                }

                if (incomingEntity.AccountCloseAgentId != null && incomingEntity.AccountCloseAgentId != Guid.Empty)
                {
                    await this.AccountCloseAgentShouldExist(incomingEntity).ConfigureAwait(false);

                    await this.AccountCloseAgentShouldHaveValidProtocols(incomingEntity).ConfigureAwait(false);
                }

                if (incomingEntity.InventoryId != null && incomingEntity.InventoryId != Guid.Empty)
                {
                    // todo: inventoryId is not being used and should be stripped out safely
                    incomingEntity.InventoryId = null;
                }

                await this.QualifierShouldBeUnique(incomingEntity).ConfigureAwait(false);

                // Identify any data agent changes that need to be made.
                this.agentUpdates = await this.CalculateAgentUpdatesAsync(action, incomingEntity, null).ConfigureAwait(false);
            }
            else if (action == WriteAction.Update)
            {
                var existingAssetGroup = await this.GetExistingEntityAsync(incomingEntity).ConfigureAwait(false);

                // Must be a ServiceAdmin to alter variants and asset qualifiers
                var isAuthorized = await this.AuthorizationProvider.TryAuthorizeAsync(AuthorizationRole.ServiceAdmin, null).ConfigureAwait(false);

                // For non-service admins retain the existing asset group qualifer
                if (!isAuthorized)
                {
                    if (!incomingEntity.Qualifier.IsEquivalentTo(existingAssetGroup.Qualifier))
                    {
                        throw new ConflictException(ConflictType.InvalidValue_Immutable, "Asset qualifiers are immutable. Delete the asset group and recreate it to change it.", "qualifier");
                    }
                    else
                    {
                        // Even if the asset qualifiers were equivalent, they may not have been identical. So, we preserve the original before making an update.
                        var qualifierPartsClone = existingAssetGroup.QualifierParts.ToDictionary(entry => entry.Key, entry => entry.Value);
                        incomingEntity.QualifierParts = qualifierPartsClone;
                    }
                }

                if (incomingEntity.OwnerId != existingAssetGroup.OwnerId && incomingEntity.OwnerId != Guid.Empty)
                {
                    await this.OwnerShouldExist(incomingEntity).ConfigureAwait(false);
                }

                if (incomingEntity.DeleteAgentId != existingAssetGroup.DeleteAgentId && incomingEntity.DeleteAgentId != null && incomingEntity.DeleteAgentId != Guid.Empty)
                {
                    await this.DeleteAgentShouldExist(incomingEntity).ConfigureAwait(false);

                    await this.DeleteAgentShouldHaveValidProtocols(incomingEntity).ConfigureAwait(false);
                }

                if (incomingEntity.ExportAgentId != existingAssetGroup.ExportAgentId && incomingEntity.ExportAgentId != null && incomingEntity.ExportAgentId != Guid.Empty)
                {
                    await this.ExportAgentShouldExist(incomingEntity).ConfigureAwait(false);

                    await this.ExportAgentShouldHaveValidProtocols(incomingEntity).ConfigureAwait(false);
                }

                if (incomingEntity.AccountCloseAgentId != existingAssetGroup.AccountCloseAgentId && incomingEntity.AccountCloseAgentId != null && incomingEntity.AccountCloseAgentId != Guid.Empty)
                {
                    await this.AccountCloseAgentShouldExist(incomingEntity).ConfigureAwait(false);

                    await this.AccountCloseAgentShouldHaveValidProtocols(incomingEntity).ConfigureAwait(false);
                }

                if (incomingEntity.InventoryId != null && incomingEntity.InventoryId != Guid.Empty)
                {
                    // todo: inventoryId is not being used and should be stripped out safely
                    incomingEntity.InventoryId = null;
                }

                if (!isAuthorized)
                {
                    this.VariantsAreImmutableIfNotAdmin(existingAssetGroup.Variants, incomingEntity.Variants);
                }
                else
                {
                    if (incomingEntity.Variants != null && incomingEntity.Variants.Any())
                    {
                        var newVariantIds = incomingEntity.Variants.Select(x => x.VariantId);

                        if (existingAssetGroup.Variants != null && existingAssetGroup.Variants.Any())
                        {
                            newVariantIds = newVariantIds.Except(existingAssetGroup.Variants.Select(x => x.VariantId));
                        }

                        foreach (var variantId in newVariantIds)
                        {
                            await this.VariantDefinitionShouldExist(variantId).ConfigureAwait(false);
                        }
                    }
                }

                // Sharing request values are set by the service, so they must be immutable.
                if (incomingEntity.DeleteSharingRequestId != existingAssetGroup.DeleteSharingRequestId)
                {
                    throw new ConflictException(ConflictType.InvalidValue_Immutable, "Sharing request values must be set by the service.", "deleteSharingRequestId", incomingEntity.DeleteSharingRequestId?.ToString());
                }

                if (incomingEntity.ExportSharingRequestId != existingAssetGroup.ExportSharingRequestId)
                {
                    throw new ConflictException(ConflictType.InvalidValue_Immutable, "Sharing request values must be set by the service.", "exportSharingRequestId", incomingEntity.ExportSharingRequestId?.ToString());
                }

                if (incomingEntity.HasPendingTransferRequest != existingAssetGroup.HasPendingTransferRequest)
                {
                    throw new ConflictException(ConflictType.InvalidValue_Immutable, "Transfer request related values must be set by the service.", "HasPendingTransferRequest", incomingEntity.HasPendingTransferRequest.ToString());
                }

                if (existingAssetGroup.HasPendingTransferRequest && incomingEntity.OwnerId != existingAssetGroup.OwnerId)
                {
                    throw new ConflictException(ConflictType.InvalidValue_Immutable, "OwnerId cannot be changed when a transfer request exists.", "HasPendingTransferRequest", incomingEntity.OwnerId.ToString());
                }

                // Identify any data agent changes that need to be made.
                this.agentUpdates = await this.CalculateAgentUpdatesAsync(action, incomingEntity, existingAssetGroup).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Create the entity in storage.
        /// </summary>
        /// <param name="action">The write action for the request.</param>
        /// <param name="entity">The incoming entity.</param>
        /// <returns>A task that performs the checks.</returns>
        public override async Task<AssetGroup> WriteAsync(WriteAction action, AssetGroup entity)
        {
            // If data agent changes are needed, then we must update both the asset group and the agents in a single transaction.
            if (this.agentUpdates != null && this.agentUpdates.Any())
            {
                // Update the agent tracking information.
                foreach (var agent in this.agentUpdates)
                {
                    this.PopulateProperties(WriteAction.Update, agent);
                }

                return await this.StorageWriter.UpsertAssetGroupWithSideEffectsAsync(entity, action, this.agentUpdates).ConfigureAwait(false);
            }
            else if (action == WriteAction.Create)
            {
                return await this.StorageWriter.CreateAssetGroupAsync(entity).ConfigureAwait(false);
            }
            else if (action == WriteAction.Update || action == WriteAction.SoftDelete)
            {
                return await this.StorageWriter.UpdateAssetGroupAsync(entity).ConfigureAwait(false);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Remove the given variants attached to an asset group.
        /// </summary>
        /// <param name="assetGroupId">The id for the asset group.</param>
        /// <param name="variantIds">The list of variant ids to remove from the asset group.</param>
        /// <param name="etag">The e-tag of the asset group.</param>
        /// <returns>The updated asset group.</returns>
        public async Task<AssetGroup> RemoveVariantsAsync(Guid assetGroupId, IEnumerable<Guid> variantIds, string etag)
        {
            if (variantIds == null || !variantIds.Any())
            {
                throw new MissingPropertyException("variantIds", "The provided list is null or empty");
            }

            var assetGroup = await this.EntityReader.ReadByIdAsync(assetGroupId, ExpandOptions.WriteProperties).ConfigureAwait(false);

            if (assetGroup == null)
            {
                throw new EntityNotFoundException(assetGroupId, "AssetGroup");
            }

            await this.AuthorizeAsync(WriteAction.Update, assetGroup).ConfigureAwait(false);

            if (assetGroup.ETag == null || !assetGroup.ETag.Equals(etag, StringComparison.OrdinalIgnoreCase))
            {
                throw new ETagMismatchException("ETag mismatch.", null, etag);
            }

            if (assetGroup.Variants == null || !assetGroup.Variants.Any())
            {
                throw new ConflictException(ConflictType.NullValue, "The asset group does not contain any variants.", "variants");
            }

            var variantsNotFound = variantIds.Where(id => !assetGroup.Variants.Select(v => v.VariantId).Any(existingIds => existingIds == id));
            if (variantsNotFound.Any())
            {
                throw new ConflictException(ConflictType.DoesNotExist, "The variant requested for removal does not exist", "variantIds", string.Join(";", variantsNotFound));
            }

            assetGroup.Variants = assetGroup.Variants.Where(v => !variantIds.Any(id => id == v.VariantId));
            this.PopulateProperties(WriteAction.Update, assetGroup);

            return await this.StorageWriter.UpdateAssetGroupAsync(assetGroup).ConfigureAwait(false);
        }

        /// <summary>
        /// Get the data owners linked to the incoming entity.
        /// </summary>
        /// <param name="action">The write action for the request.</param>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>The data owner list.</returns>
        public override async Task<IEnumerable<DataOwner>> GetDataOwnersAsync(WriteAction action, AssetGroup incomingEntity)
        {
            var incomingDataOwner = await this.GetDataOwnerForEntityAsync(incomingEntity).ConfigureAwait(false);

            if (incomingDataOwner == null)
            {
                return null; // Exit early. Other validations will fail if no security groups are set.
            }
            else if (action == WriteAction.Update)
            {
                var existingEntity = await this.GetExistingEntityAsync(incomingEntity).ConfigureAwait(false);

                if (existingEntity != null)
                {
                    var existingDataOwner = await this.GetDataOwnerForEntityAsync(existingEntity).ConfigureAwait(false);

                    return new[] { existingDataOwner, incomingDataOwner };
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return new[] { incomingDataOwner };
            }
        }

        /// <summary>
        /// Set agent relationships in bulk. Create requests as needed.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The response.</returns>
        public async Task<SetAgentRelationshipResponse> SetAgentRelationshipsAsync(SetAgentRelationshipParameters parameters)
        {
            var relationshipManager = new AssetGroupRelationshipManager(
                this.StorageWriter,
                this.EntityReader,
                this.AuthenticatedPrincipal,
                this.AuthorizationProvider,
                this.DateFactory,
                this.dataOwnerReader,
                this.deleteAgentReader,
                this.sharingRequestReader,
                this.policy);

            return await relationshipManager.ApplyChanges(parameters).ConfigureAwait(false);
        }

        #region Private Methods
        private async Task<IEnumerable<DeleteAgent>> CalculateAgentUpdatesAsync(WriteAction action, AssetGroup incomingEntity, AssetGroup existingEntity)
        {
            var newAgentIds = new Dictionary<Guid, List<CapabilityId>>();
            var removedAgentIds = new Dictionary<Guid, List<CapabilityId>>();

            // For create, all ids must be new, so we can skip checking for removed ids.
            if (action == WriteAction.Create)
            {
                this.AddAgentId(newAgentIds, incomingEntity.DeleteAgentId, this.policy.Capabilities.Ids.Delete);
                this.AddAgentId(newAgentIds, incomingEntity.ExportAgentId, this.policy.Capabilities.Ids.Export);
                this.AddAgentId(newAgentIds, incomingEntity.AccountCloseAgentId, this.policy.Capabilities.Ids.AccountClose);
            }
            else
            {
                if (incomingEntity.DeleteAgentId != existingEntity.DeleteAgentId)
                {
                    this.AddAgentId(newAgentIds, incomingEntity.DeleteAgentId, this.policy.Capabilities.Ids.Delete);
                    this.AddAgentId(removedAgentIds, existingEntity.DeleteAgentId, this.policy.Capabilities.Ids.Delete);
                }

                if (incomingEntity.ExportAgentId != existingEntity.ExportAgentId)
                {
                    this.AddAgentId(newAgentIds, incomingEntity.ExportAgentId, this.policy.Capabilities.Ids.Export);
                    this.AddAgentId(removedAgentIds, existingEntity.ExportAgentId, this.policy.Capabilities.Ids.Export);
                }

                if (incomingEntity.AccountCloseAgentId != existingEntity.AccountCloseAgentId)
                {
                    this.AddAgentId(newAgentIds, incomingEntity.AccountCloseAgentId, this.policy.Capabilities.Ids.AccountClose);
                    this.AddAgentId(removedAgentIds, existingEntity.AccountCloseAgentId, this.policy.Capabilities.Ids.AccountClose);
                }
            }

            var allDataAgentIds = newAgentIds.Keys.Concat(removedAgentIds.Keys).Distinct();

            var dataAgents = await this.GetExistingDataAgentsAsync(allDataAgentIds).ConfigureAwait(false);

            var modifiedAgents = new Dictionary<Guid, DeleteAgent>();

            // For the removed agents, we need to refresh their capabilities based on the other asset groups in the system.
            // We must handle removals first, because if an agent is both removed and new, then we want the agent information
            // to be accurate based on the refreshed values before applying the add logic.
            foreach (var agent in dataAgents)
            {
                if (removedAgentIds.ContainsKey(agent.Id))
                {
                    // Determine if any asset groups are linked to this agent for specific capabilities.
                    var filterCriteria =
                    new AssetGroupFilterCriteria { DeleteAgentId = agent.Id }
                    .Or(new AssetGroupFilterCriteria { ExportAgentId = agent.Id })
                    .Or(new AssetGroupFilterCriteria { AccountCloseAgentId = agent.Id });

                    var results = await this.EntityReader.ReadByFiltersAsync(filterCriteria, ExpandOptions.None).ConfigureAwait(false);
                    results.Values = results.Values.Where(x => x.Id != incomingEntity.Id); // Filter out the incoming entity.

                    // Identify the set of capabilities that are appropriate for this agent.
                    var capabilities = new List<CapabilityId>();

                    if (results.Values.Any(x => x.DeleteAgentId == agent.Id))
                    {
                        capabilities.Add(this.policy.Capabilities.Ids.Delete);
                    }

                    if (results.Values.Any(x => x.ExportAgentId == agent.Id))
                    {
                        capabilities.Add(this.policy.Capabilities.Ids.Export);
                    }

                    if (results.Values.Any(x => x.AccountCloseAgentId == agent.Id))
                    {
                        capabilities.Add(this.policy.Capabilities.Ids.AccountClose);
                    }

                    // Determine if the capabilities have changed,
                    // and apply the updated values.
                    if (!capabilities.OrderBy(x => x.Value).SequenceEqual(agent.Capabilities?.OrderBy(x => x.Value) ?? Enumerable.Empty<CapabilityId>()))
                    {
                        agent.Capabilities = capabilities;
                        modifiedAgents[agent.Id] = agent;
                    }
                }
            }

            // For the new agents, we can just add the new capabilities.
            foreach (var agent in dataAgents)
            {
                if (newAgentIds.ContainsKey(agent.Id))
                {
                    agent.Capabilities = agent.Capabilities?.Concat(newAgentIds[agent.Id])?.Distinct() ?? newAgentIds[agent.Id];
                    modifiedAgents[agent.Id] = agent;
                }
            }

            // Return all data agents that need to be saved.
            return modifiedAgents.Values;
        }

        // Adds the id to the given dictionary if the value is not null.
        private void AddAgentId(Dictionary<Guid, List<CapabilityId>> values, Guid? agentId, CapabilityId capability)
        {
            if (agentId != null && agentId != Guid.Empty)
            {
                if (!values.ContainsKey(agentId.Value))
                {
                    values[agentId.Value] = new List<CapabilityId>();
                }

                values[agentId.Value].Add(capability);
            }
        }

        private async Task<DataOwner> GetDataOwnerForEntityAsync(AssetGroup entity)
        {
            if (entity.OwnerId != Guid.Empty)
            {
                var existingOwner = await this.GetExistingOwnerAsync(entity.OwnerId).ConfigureAwait(false);

                return existingOwner;
            }
            else
            {
                if (entity.DeleteAgentId == null || entity.DeleteAgentId == Guid.Empty)
                {
                    return null;
                }

                var existingDeleteAgent = await this.GetExistingDataAgentAsync(entity.DeleteAgentId.Value).ConfigureAwait(false);

                if (existingDeleteAgent != null)
                {
                    var deleteAgentOwner = await this.GetExistingOwnerAsync(existingDeleteAgent.OwnerId).ConfigureAwait(false);

                    return deleteAgentOwner;
                }
                else
                {
                    return null;
                }
            }
        }

        private Task<DataOwner> GetExistingOwnerAsync(Guid ownerId)
        {
            return this.MemoizeAsync(ownerId, () => this.dataOwnerReader.ReadByIdAsync(ownerId, ExpandOptions.None));
        }

        private Task<DeleteAgent> GetExistingDataAgentAsync(Guid agentId)
        {
            // We need tracking details in order to perform updates on the data agents.
            return this.MemoizeAsync(agentId, () => this.deleteAgentReader.ReadByIdAsync(agentId, ExpandOptions.WriteProperties));
        }

        private Task<IEnumerable<DeleteAgent>> GetExistingDataAgentsAsync(IEnumerable<Guid> agentIds)
        {
            // We need tracking details in order to perform updates on the data agents.
            return this.MemoizeEntitiesAsync(agentIds, ids => this.deleteAgentReader.ReadByIdsAsync(ids, ExpandOptions.WriteProperties));
        }

        private Task<VariantDefinition> GetExistingVariantDefinitionAsync(Guid variantId)
        {
            return this.MemoizeAsync(variantId, () => this.variantDefinitionReader.ReadByIdAsync(variantId, ExpandOptions.None));
        }

        /// <summary>
        /// Helper function to check if the qualifier of the incomingEntity does not exist in PDMS collection.
        /// </summary>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>Void task.</returns>
        private async Task QualifierShouldBeUnique(AssetGroup incomingEntity)
        {
            // Create a filter for asset qualifier.
            // This filter will take all of the provided property names and values from the incoming entity
            // and create a query that matches those exact values in DocDB, using the proper case-sensitivity 
            // based on the property definition in the manifest document.
            // Any properties that are not provided, will not be included in the search.
            // As such, we may get some false positives, so we also need to do a post comparison check.
            var typeDefinition = this.identityManifest.AssetTypes.Single(x => x.Id == incomingEntity.Qualifier.AssetType);

            var filterPropertyNames =
                from property in typeDefinition.Properties
                where incomingEntity.QualifierParts.ContainsKey(property.Id)
                select property.Id;

            Func<string, StringComparisonType> getComparision = (propName) =>
            {
                var propDefinition = typeDefinition.Properties.Single(x => x.Id == propName);

                return propDefinition.CaseSensitive ? StringComparisonType.EqualsCaseSensitive : StringComparisonType.Equals;
            };

            var filterCriteria = new AssetGroupFilterCriteria
            {
                Qualifier = filterPropertyNames.ToDictionary(
                    v => v,
                    v => new StringFilter(incomingEntity.Qualifier.Properties[v], getComparision(v)))
            };

            // Asset type is not a property in the manifest, so we add it manually.
            filterCriteria.Qualifier.Add("AssetType", new StringFilter(incomingEntity.Qualifier.AssetType.ToString(), StringComparisonType.EqualsCaseSensitive));

            var existingAssetGroups = await this.EntityReader.ReadByFiltersAsync(filterCriteria, ExpandOptions.None).ConfigureAwait(false);

            // Do a post comparison check to exclude false positives.
            var assetGroupMatches = existingAssetGroups.Values.Where(sg => sg.Qualifier.Equals(incomingEntity.Qualifier)).ToList();
            if (assetGroupMatches?.Count > 0)
            {
                throw new AlreadyOwnedException(ConflictType.AlreadyExists_ClaimedByOwner, "The qualifier is already in use.", "qualifier", incomingEntity.Qualifier.ToString(), assetGroupMatches.First().OwnerId.ToString());
            }
        }

        /// <summary>
        /// Helper function to check if the linked data owner exists.
        /// </summary>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>Void task.</returns>
        private async Task OwnerShouldExist(AssetGroup incomingEntity)
        {
            var existingOwner = await this.GetExistingOwnerAsync(incomingEntity.OwnerId).ConfigureAwait(false);

            if (existingOwner == null)
            {
                throw new ConflictException(ConflictType.DoesNotExist, "The referenced entity does not exist", "ownerId", incomingEntity.OwnerId.ToString());
            }
        }

        /// <summary>
        /// Helper function to check if the linked delete agent exists.
        /// </summary>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>Void task.</returns>
        private async Task DeleteAgentShouldExist(AssetGroup incomingEntity)
        {
            var existingDeleteAgent = await this.GetExistingDataAgentAsync(incomingEntity.DeleteAgentId.Value).ConfigureAwait(false);

            if (existingDeleteAgent == null)
            {
                throw new ConflictException(ConflictType.DoesNotExist, "The referenced entity does not exist", "deleteAgentId", incomingEntity.DeleteAgentId.ToString());
            }
        }

        /// <summary>
        /// Helper function to check if the linked export agent exists.
        /// </summary>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>Void task.</returns>
        private async Task ExportAgentShouldExist(AssetGroup incomingEntity)
        {
            var existingExportAgent = await this.GetExistingDataAgentAsync(incomingEntity.ExportAgentId.Value).ConfigureAwait(false);

            if (existingExportAgent == null)
            {
                throw new ConflictException(ConflictType.DoesNotExist, "The referenced entity does not exist", "exportAgentId", incomingEntity.ExportAgentId.ToString());
            }
        }

        /// <summary>
        /// Helper function to check if the linked account close agent exists.
        /// </summary>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>Void task.</returns>
        private async Task AccountCloseAgentShouldExist(AssetGroup incomingEntity)
        {
            var existingAccountCloseAgent = await this.GetExistingDataAgentAsync(incomingEntity.AccountCloseAgentId.Value).ConfigureAwait(false);

            if (existingAccountCloseAgent == null)
            {
                throw new ConflictException(ConflictType.DoesNotExist, "The referenced entity does not exist", "accountCloseAgentId", incomingEntity.AccountCloseAgentId.ToString());
            }
        }

        /// <summary>
        /// Helper function to check if the linked variant definition exists.
        /// </summary>
        /// <param name="variantId">The variant id.</param>
        /// <returns>Void task.</returns>
        private async Task VariantDefinitionShouldExist(Guid variantId)
        {
            var existingVariantDefinition = await this.GetExistingVariantDefinitionAsync(variantId).ConfigureAwait(false);

            if (existingVariantDefinition == null)
            {
                throw new ConflictException(ConflictType.DoesNotExist, "The referenced entity does not exist", "variantId", variantId.ToString());
            }
        }

        /// <summary>
        /// Helper function to check if the linked delete agent has valid protocols.
        /// </summary>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>Void task.</returns>
        private async Task DeleteAgentShouldHaveValidProtocols(AssetGroup incomingEntity)
        {
            var existingDeleteAgent = await this.GetExistingDataAgentAsync(incomingEntity.DeleteAgentId.Value).ConfigureAwait(false);

            this.AgentConnectionDetailsShouldBeValid(existingDeleteAgent, this.policy.Capabilities.Ids.Delete, "Unsupported protocol type for the linked delete agent.");
        }

        /// <summary>
        /// Helper function to check if the linked export agent has valid protocols.
        /// </summary>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>Void task.</returns>
        private async Task ExportAgentShouldHaveValidProtocols(AssetGroup incomingEntity)
        {
            var existingExportAgent = await this.GetExistingDataAgentAsync(incomingEntity.ExportAgentId.Value).ConfigureAwait(false);

            this.AgentConnectionDetailsShouldBeValid(existingExportAgent, this.policy.Capabilities.Ids.Export, "Unsupported protocol type for the linked export agent.");
        }

        /// <summary>
        /// Helper function to check if the linked account close agent has valid protocols.
        /// </summary>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>Void task.</returns>
        private async Task AccountCloseAgentShouldHaveValidProtocols(AssetGroup incomingEntity)
        {
            var existingAccountCloseAgent = await this.GetExistingDataAgentAsync(incomingEntity.AccountCloseAgentId.Value).ConfigureAwait(false);

            this.AgentConnectionDetailsShouldBeValid(existingAccountCloseAgent, this.policy.Capabilities.Ids.AccountClose, "Unsupported protocol type for the linked account close agent.");
        }

        /// <summary>
        /// Helper function to check if the data agent has valid protocols.
        /// </summary>
        /// <param name="dataAgent">The agent to check.</param>
        /// <param name="capabilityId">The capability to check on.</param>
        /// <param name="message">The error message to display.</param>
        private void AgentConnectionDetailsShouldBeValid(DeleteAgent dataAgent, CapabilityId capabilityId, string message)
        {
            if (dataAgent.ConnectionDetails != null && dataAgent.ConnectionDetails.Any())
            {
                foreach (var connectionDetail in dataAgent.ConnectionDetails.Values)
                {
                    if (!this.policy.Capabilities.IsSupportedProtocol(capabilityId, connectionDetail.Protocol))
                    {
                        throw new InvalidPropertyException($"connectionDetails[{connectionDetail.ReleaseState}].protocol", connectionDetail.Protocol?.Value, message);
                    }
                }
            }
        }

        /// <summary>
        /// Helper function to check if the asset group variants has any duplicates.
        /// </summary>
        /// <param name="assetGroupVariantIds">The asset group variants.</param>
        private void AssetGroupVariantsCannotHaveDuplicateValues(IEnumerable<Guid> assetGroupVariantIds)
        {
            if (assetGroupVariantIds.Count() != assetGroupVariantIds.Distinct().Count())
            {
                throw new InvalidPropertyException("variants", assetGroupVariantIds.ToString(), "The variant request cannot have duplicate variants.");
            }
        }

        /// <summary>
        /// Helper function to check if the variants are immutable.
        /// </summary>
        /// <param name="existingVariants">Existing variants.</param>
        /// <param name="incomingVariants">Incoming variants.</param>
        private void VariantsAreImmutableIfNotAdmin(IEnumerable<AssetGroupVariant> existingVariants, IEnumerable<AssetGroupVariant> incomingVariants)
        {
            if (existingVariants == null || incomingVariants == null)
            {
                var existingCount = existingVariants == null ? 0 : existingVariants.Count();
                var incomingCount = incomingVariants == null ? 0 : incomingVariants.Count();

                if (existingCount != incomingCount)
                {
                    throw new ConflictException(ConflictType.InvalidValue_Immutable, "Variants are immutable if the user is not service admin.", "variants");
                }
            }
            else
            {
                IDictionary<Guid, AssetGroupVariant> existingVariantsDict = existingVariants.ToDictionary(x => x.VariantId);
                IDictionary<Guid, AssetGroupVariant> incomingVariantsDict = incomingVariants.ToDictionary(x => x.VariantId);

                Action<IDictionary<Guid, AssetGroupVariant>, IDictionary<Guid, AssetGroupVariant>> check = (a, b) =>
                {
                    foreach (var keyValuePair in a)
                    {
                        if (!b.ContainsKey(keyValuePair.Key))
                        {
                            throw new ConflictException(ConflictType.InvalidValue_Immutable, "Variants are immutable if the user is not service admin.", $"variants[{keyValuePair.Key}]");
                        }

                        this.validator.Immutable(
                            b[keyValuePair.Key],
                            keyValuePair.Value,
                            (target, value, message) => Validator.ConflictInvalidValueImmutable($"variants[{keyValuePair.Key}].{target}", value, $"Variants are immutable if the user is not service admin. {message}"),
                            nameof(AssetGroupVariant.TfsTrackingUris));

                        this.VariantTrackingUrisAreImmutable(keyValuePair.Key, b[keyValuePair.Key].TfsTrackingUris, keyValuePair.Value.TfsTrackingUris);
                    }
                };

                // Check both directions to catch any adds/removes.
                check(incomingVariantsDict, existingVariantsDict);
                check(existingVariantsDict, incomingVariantsDict);
            }
        }

        /// <summary>
        /// Helper function to check if the variant tracking URIs are immutable.
        /// </summary>
        /// <param name="variantId">The variant to check.</param>
        /// <param name="existingTrackingUris">Existing tracking URIs.</param>
        /// <param name="incomingTrackingUris">Incoming tracking URIs.</param>
        private void VariantTrackingUrisAreImmutable(Guid variantId, IEnumerable<Uri> existingTrackingUris, IEnumerable<Uri> incomingTrackingUris)
        {
            if (existingTrackingUris == null || incomingTrackingUris == null)
            {
                var existingCount = existingTrackingUris == null ? 0 : existingTrackingUris.Count();
                var incomingCount = incomingTrackingUris == null ? 0 : incomingTrackingUris.Count();

                if (existingCount != incomingCount)
                {
                    throw new ConflictException(ConflictType.InvalidValue_Immutable, "Variants are immutable if the user is not service admin. Property cannot be changed.", $"variants[{variantId}].tfsTrackingUris");
                }
            }
            else
            {
                if (!existingTrackingUris.Select(x => x.ToString()).OrderBy(e => e).SequenceEqual(incomingTrackingUris.Select(x => x.ToString()).OrderBy(i => i)))
                {
                    throw new ConflictException(ConflictType.InvalidValue_Immutable, "Variants are immutable if the user is not service admin. Property cannot be changed.", $"variants[{variantId}].tfsTrackingUris");
                }
            }
        }

        /// <summary>
        /// Set the default compliance state when a new asset group is crated.
        /// The default value is IsCompliant = true, IncompliantReason = null.
        /// </summary>
        /// <param name="incomingEntity">The new asset group to be created.</param>
        private void SetDefaultComplianceState(AssetGroup incomingEntity)
        {
            incomingEntity.ComplianceState = new ComplianceState
            {
                IsCompliant = true,
                IncompliantReason = null
            };
        }

        private bool IsNullOrEmpty(Guid? guid)
        {
            return guid == null || guid == Guid.Empty;
        }
        #endregion
    }
}
