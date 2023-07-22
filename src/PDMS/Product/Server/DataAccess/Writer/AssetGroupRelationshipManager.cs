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
    using Microsoft.PrivacyServices.Policy;

    /// <summary>
    /// Performs bulk updates to asset group / data agent relationship mappings.
    /// </summary>
    public class AssetGroupRelationshipManager
    {
        private readonly IDataOwnerReader dataOwnerReader;
        private readonly IDeleteAgentReader deleteAgentReader;
        private readonly ISharingRequestReader sharingRequestReader;
        private readonly Policy policy;
        private readonly IDateFactory dateFactory;
        private readonly IAuthorizationProvider authorizationProvider;
        private readonly AuthenticatedPrincipal authenticatedPrincipal;
        private readonly IPrivacyDataStorageWriter storageWriter;
        private readonly IEntityReader<AssetGroup> entityReader;
        private readonly AuthorizationRole authorizationRoles;

        private Dictionary<Guid, SetAgentRelationshipParameters.Relationship> assetGroupIds = new Dictionary<Guid, SetAgentRelationshipParameters.Relationship>();
        private Guid? agentId = null;
        private SetAgentRelationshipParameters.ActionType? action = null;
        private Guid? ownerId = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetGroupRelationshipManager" /> class.
        /// </summary>
        /// <param name="storageWriter">The storage writer.</param>
        /// <param name="entityReader">The entity reader.</param>
        /// <param name="authenticatedPrincipal">The authentication principal.</param>
        /// <param name="authorizationProvider">The authorization provider.</param>
        /// <param name="dateFactory">The date factory.</param>
        /// <param name="dataOwnerReader">The data owner reader.</param>
        /// <param name="deleteAgentReader">The delete agent reader.</param>
        /// <param name="sharingRequestReader">The sharing request reader.</param>
        /// <param name="policy">The policy instance.</param>
        public AssetGroupRelationshipManager(
            IPrivacyDataStorageWriter storageWriter,
            IEntityReader<AssetGroup> entityReader,
            AuthenticatedPrincipal authenticatedPrincipal,
            IAuthorizationProvider authorizationProvider,
            IDateFactory dateFactory,
            IDataOwnerReader dataOwnerReader,
            IDeleteAgentReader deleteAgentReader,
            ISharingRequestReader sharingRequestReader,
            Policy policy)
        {
            this.dataOwnerReader = dataOwnerReader;
            this.deleteAgentReader = deleteAgentReader;
            this.sharingRequestReader = sharingRequestReader;
            this.policy = policy;
            this.storageWriter = storageWriter;
            this.entityReader = entityReader;
            this.authenticatedPrincipal = authenticatedPrincipal;
            this.authorizationProvider = authorizationProvider;
            this.dateFactory = dateFactory;
            this.authorizationRoles = AuthorizationRole.ServiceEditor;
        }

        /// <summary>
        /// Takes the given values and either updates/requests/clears agent links.
        /// </summary>
        /// <param name="parameters">The set of parameters for the method.</param>
        /// <returns>The response information.</returns>
        public async Task<SetAgentRelationshipResponse> ApplyChanges(SetAgentRelationshipParameters parameters)
        {
            // Validate the incoming data before performing any storage calls.
            this.ValidateIncomingData(parameters);

            // Load the asset groups from storage.
            var assetGroups = await this.entityReader.ReadByIdsAsync(this.assetGroupIds.Keys, ExpandOptions.WriteProperties).ConfigureAwait(false);

            var success = await this.DetectUnlinkingSharedAssetGroups(parameters, assetGroups).ConfigureAwait(false);

            if (success)
            {
                return await this.ExecuteUpdates(assetGroups, () => this.CalculateClearChanges(assetGroups)).ConfigureAwait(false);
            }
            else
            {
                // Perform validation based on the data in storage.
                this.ValidateExistingData(parameters, assetGroups);

                var owner = await this.dataOwnerReader.ReadByIdAsync(this.ownerId.Value, ExpandOptions.WriteProperties).ConfigureAwait(false);

                // Authorize the user before calculating any changes.
                // We only need to authorize one asset group because
                // validation ensures all asset groups have the same owner.
                await this.authorizationProvider.AuthorizeAsync(this.authorizationRoles, () => Task.FromResult(new[] { owner }.AsEnumerable())).ConfigureAwait(false);

                if (this.action == SetAgentRelationshipParameters.ActionType.Set)
                {
                    return await this.ExecuteUpdates(assetGroups, () => this.CalculateSetChanges(assetGroups, owner)).ConfigureAwait(false);
                }
                else if (this.action == SetAgentRelationshipParameters.ActionType.Clear)
                {
                    return await this.ExecuteUpdates(assetGroups, () => this.CalculateClearChanges(assetGroups)).ConfigureAwait(false);
                }
                else
                {
                    throw new InvalidOperationException($"Unrecognized action: {this.action}");
                }
            }
        }

        private void ValidateIncomingData(SetAgentRelationshipParameters parameters)
        {
            ValidationModule.PropertyRequiredAndNotEmpty(parameters.Relationships, "relationships");

            foreach (var param in parameters.Relationships)
            {
                Func<string, string> getPropertyName = x => $"relationships[{param.AssetGroupId}].{x}";

                ValidationModule.PropertyRequiredAndNotEmpty(param.Actions, getPropertyName("actions"));

                // Asset group ids cannot be duplicated.
                if (this.assetGroupIds.ContainsKey(param.AssetGroupId))
                {
                    throw new InvalidPropertyException(
                        getPropertyName("assetGroupId"),
                        param.AssetGroupId.ToString(),
                        "Asset groups must appear only once in the relationship collection.");
                }
                else
                {
                    var capabilityIds = new HashSet<CapabilityId>();

                    foreach (var paramAction in param.Actions)
                    {
                        // Verify that there is exactly 1 action type across all asset groups.
                        if (!this.action.HasValue)
                        {
                            this.action = paramAction.Verb;
                        }
                        else if (this.action.Value != paramAction.Verb)
                        {
                            throw new MutuallyExclusiveException(
                                this.action.ToString(),
                                getPropertyName($"actions[{paramAction.CapabilityId}].verb"),
                                paramAction.Verb.ToString(),
                                $"Only a single verb may be used across all requested relationships.");
                        }

                        // Verify that the capabilities are unique within the asset group.
                        if (capabilityIds.Contains(paramAction.CapabilityId))
                        {
                            throw new InvalidPropertyException(
                                getPropertyName($"actions[{paramAction.CapabilityId}].capabilityId"),
                                paramAction.CapabilityId.ToString(),
                                "Capability ids must be unique within an asset group.");
                        }
                        else
                        {
                            capabilityIds.Add(paramAction.CapabilityId);
                        }

                        // If the action is "Set", then make sure a delete agent id is provided
                        // and that all actions use the same delete agent id.
                        if (this.action == SetAgentRelationshipParameters.ActionType.Set)
                        {
                            if (paramAction.DeleteAgentId == null || paramAction.DeleteAgentId == Guid.Empty)
                            {
                                throw new MissingPropertyException(
                                    getPropertyName($"actions[{paramAction.CapabilityId}].deleteAgentId"),
                                    "SET actions must have a delete agent id set.");
                            }

                            if (!this.agentId.IsSet())
                            {
                                this.agentId = paramAction.DeleteAgentId;
                            }
                            else if (this.agentId.Value != paramAction.DeleteAgentId)
                            {
                                throw new MutuallyExclusiveException(
                                    this.agentId.ToString(),
                                    getPropertyName($"actions[{paramAction.CapabilityId}].deleteAgentId"),
                                    paramAction.DeleteAgentId?.ToString(),
                                    $"Only a single agent id may be used across all requested relationships.");
                            }
                        }
                        else if (this.action == SetAgentRelationshipParameters.ActionType.Clear)
                        {
                            // Make sure that no agent id is set for "Clear" action.
                            if (paramAction.DeleteAgentId != null)
                            {
                                throw new InvalidPropertyException(
                                    getPropertyName($"actions[{paramAction.CapabilityId}].deleteAgentId"),
                                    paramAction.DeleteAgentId.ToString(),
                                    "CLEAR actions cannot have a delete agent id set.");
                            }
                        }
                    }
                }

                this.assetGroupIds.Add(param.AssetGroupId, param);
            }
        }

        private async Task<bool> DetectUnlinkingSharedAssetGroups(SetAgentRelationshipParameters parameters, IEnumerable<AssetGroup> assetGroups)
        {
            // Determine if the context of this API call is to unlink shared agent values.
            // We need to allow agent owners of shared agents to unlink asset groups they do not own.
            // This scenario is different from the others, so it has a special set of validations and authorizations.
            // We identify this scenario when the action is CLEAR, the asset group owner ids do not all match, 
            // but the agent ids for the unlinked capabilities do all match.
            // In that scenario, we authorize against he agent owner (must be singular).            
            List<Guid?> impactedAgentIds = new List<Guid?>();

            // Identify the set of impacted agent ids.
            foreach (var assetGroup in assetGroups)
            {
                var capabilities = this.assetGroupIds[assetGroup.Id].Actions.Select(x => x.CapabilityId);

                if (capabilities.Contains(this.policy.Capabilities.Ids.Delete) && assetGroup.DeleteAgentId.IsSet())
                {
                    impactedAgentIds.Add(assetGroup.DeleteAgentId);
                }

                if (capabilities.Contains(this.policy.Capabilities.Ids.Export) && assetGroup.ExportAgentId.IsSet())
                {
                    impactedAgentIds.Add(assetGroup.ExportAgentId);
                }
            }

            var agentIdsSingular = impactedAgentIds.Distinct().Count() == 1;

            // Get the owner id for authorization.
            // In this scenario, the agent owner is the important security check.
            // If the user is not authorized, then fallback to original behavior 
            // (checking if asset groups all share the same owner).
            if (this.action == SetAgentRelationshipParameters.ActionType.Clear && agentIdsSingular)
            {
                var agent = await this.deleteAgentReader.ReadByIdAsync(impactedAgentIds.Distinct().Single().Value, ExpandOptions.None).ConfigureAwait(false);

                var owner = await this.dataOwnerReader.ReadByIdAsync(agent.OwnerId, ExpandOptions.None).ConfigureAwait(false);

                return await this.authorizationProvider.TryAuthorizeAsync(this.authorizationRoles, () => Task.FromResult(new[] { owner }.AsEnumerable())).ConfigureAwait(false);
            }
            else
            {
                return false;
            }
        }

        private void ValidateExistingData(SetAgentRelationshipParameters parameters, IEnumerable<AssetGroup> assetGroups)
        {
            // Determine if any ids are not found in storage.
            var missing = this.assetGroupIds.Keys.Except(assetGroups.Select(x => x.Id));

            if (missing.Any())
            {
                var missingId = missing.First().ToString();

                throw new ConflictException(
                    ConflictType.DoesNotExist,
                    "Asset group does not exist in storage.",
                    $"relationships[{missingId}].assetGroup",
                    missingId);
            }

            // Ensure all asset groups have the same owner.
            foreach (var assetGroup in assetGroups)
            {
                var param = this.assetGroupIds[assetGroup.Id];

                if (param.ETag != assetGroup.ETag)
                {
                    throw new ConflictException(
                        ConflictType.InvalidValue,
                        $"Provided ETag must match the value in storage. Storage value: {assetGroup.ETag}",
                        $"relationships[{assetGroup.Id}].eTag",
                        param.ETag);
                }

                if (assetGroup.OwnerId == Guid.Empty)
                {
                    throw new ConflictException(
                        ConflictType.DoesNotExist,
                        "All asset groups must be associated with an owner.",
                        $"relationships[{assetGroup.Id}].assetGroup.ownerId");
                }

                if (this.ownerId.IsSet() && assetGroup.OwnerId != this.ownerId.Value)
                {
                    throw new ConflictException(
                        ConflictType.InvalidValue,
                        "All asset groups must share the same owner id.",
                        $"relationships[{assetGroup.Id}].assetGroup.ownerId",
                        assetGroup.OwnerId.ToString());
                }

                this.ownerId = assetGroup.OwnerId;
            }
        }

        private async Task<Tuple<IEnumerable<Entity>, IEnumerable<SetAgentRelationshipResponse.AssetGroupResult>>> CalculateSetChanges(IEnumerable<AssetGroup> assetGroups, DataOwner owner)
        {
            Func<string> getParamName = () => $"relationships[{assetGroupIds.First().Key}].actions[{assetGroupIds.First().Value.Actions.First().CapabilityId}].deleteAgentId";

            var deleteAgent = await this.deleteAgentReader.ReadByIdAsync(this.agentId.Value, ExpandOptions.WriteProperties).ConfigureAwait(false);

            if (deleteAgent == null)
            {
                throw new ConflictException(
                    ConflictType.DoesNotExist,
                    "No data agent could be found for the given agent id.",
                    getParamName(),
                    this.agentId.Value.ToString());
            }

            var isSharingRequest = deleteAgent.OwnerId != this.ownerId.Value;

            if (isSharingRequest && !deleteAgent.SharingEnabled)
            {
                throw new ConflictException(
                    ConflictType.InvalidValue,
                    "The owner id of the associated agent is not identical to the asset group owner, nor is the agent enabled for sharing.",
                    getParamName(),
                    this.agentId.Value.ToString());
            }
            else if (isSharingRequest)
            {
                return await this.CalculateSetChangesForSharingRequests(assetGroups, owner, deleteAgent).ConfigureAwait(false);
            }
            else
            {
                return await this.CalculateSetChangesForAssetGroups(assetGroups, deleteAgent).ConfigureAwait(false);
            }
        }

        private async Task<Tuple<IEnumerable<Entity>, IEnumerable<SetAgentRelationshipResponse.AssetGroupResult>>> CalculateSetChangesForAssetGroups(IEnumerable<AssetGroup> assetGroups, DeleteAgent deleteAgent)
        {
            bool hasDelete = false;
            bool hasExport = false;

            List<Guid> clearedDeleteAssetGroupIds = new List<Guid>();
            List<Guid> clearedExportAssetGroupIds = new List<Guid>();
            List<Guid?> impactedSharingRequestIds = new List<Guid?>();

            var results = new List<SetAgentRelationshipResponse.AssetGroupResult>();

            // Update the asset group properties.
            foreach (var assetGroup in assetGroups)
            {
                var capabilities = this.assetGroupIds[assetGroup.Id].Actions.Select(x => x.CapabilityId);

                if (capabilities.Contains(this.policy.Capabilities.Ids.Delete))
                {
                    assetGroup.DeleteAgentId = deleteAgent.Id;
                    impactedSharingRequestIds.Add(assetGroup.DeleteSharingRequestId);
                    clearedDeleteAssetGroupIds.Add(assetGroup.Id);
                    assetGroup.DeleteSharingRequestId = null;
                    hasDelete = true;
                }

                if (capabilities.Contains(this.policy.Capabilities.Ids.Export))
                {
                    assetGroup.ExportAgentId = deleteAgent.Id;
                    impactedSharingRequestIds.Add(assetGroup.ExportSharingRequestId);
                    clearedExportAssetGroupIds.Add(assetGroup.Id);
                    assetGroup.ExportSharingRequestId = null;
                    hasExport = true;
                }

                results.Add(new SetAgentRelationshipResponse.AssetGroupResult
                {
                    AssetGroupId = assetGroup.Id,
                    Capabilities = capabilities.Select(x => new SetAgentRelationshipResponse.CapabilityResult
                    {
                        CapabilityId = x,
                        Status = SetAgentRelationshipResponse.StatusType.Updated
                    })
                });
            }

            var sharingRequests = await this.ClearSharingRequests(clearedDeleteAssetGroupIds, clearedExportAssetGroupIds, impactedSharingRequestIds).ConfigureAwait(false);

            // Update the capabilities on the agent.
            List<CapabilityId> agentCapabilityChanges = new List<CapabilityId>();

            if (hasDelete)
            {
                agentCapabilityChanges.Add(this.policy.Capabilities.Ids.Delete);
            }

            if (hasExport)
            {
                agentCapabilityChanges.Add(this.policy.Capabilities.Ids.Export);
            }

            deleteAgent.Capabilities = deleteAgent.Capabilities ?? Enumerable.Empty<CapabilityId>();
            deleteAgent.Capabilities = deleteAgent.Capabilities.Concat(agentCapabilityChanges).Distinct();

            return Tuple.Create<IEnumerable<Entity>, IEnumerable<SetAgentRelationshipResponse.AssetGroupResult>>(sharingRequests.Concat(new[] { deleteAgent }), results);
        }

        private async Task<Tuple<IEnumerable<Entity>, IEnumerable<SetAgentRelationshipResponse.AssetGroupResult>>> CalculateSetChangesForSharingRequests(IEnumerable<AssetGroup> assetGroups, DataOwner owner, DeleteAgent deleteAgent)
        {
            var entitiesToSave = new List<Entity>();

            var filterCriteria = new SharingRequestFilterCriteria
            {
                DeleteAgentId = this.agentId.Value,
                OwnerId = this.ownerId.Value
            };

            var sharingRequests = await this.sharingRequestReader.ReadByFiltersAsync(filterCriteria, ExpandOptions.WriteProperties).ConfigureAwait(false);
            SharingRequest sharingRequest;

            var results = new List<SetAgentRelationshipResponse.AssetGroupResult>();

            // There should only ever be 1 request in storage
            // for any dataowner/dataagent combination.
            // To ensure this, we 'update' the owner
            // so that if another tries to save at the same time
            // the ETag check will fail and avoid duplicate requests.
            if (sharingRequests.Total == 0)
            {
                sharingRequest = new SharingRequest
                {
                    Id = Guid.NewGuid(),
                    OwnerId = this.ownerId.Value,
                    DeleteAgentId = this.agentId.Value,
                    Relationships = new Dictionary<Guid, SharingRelationship>()
                };

                entitiesToSave.Add(owner); // We must save the owner to ensure consistency.
            }
            else
            {
                sharingRequest = sharingRequests.Values.Single();
            }

            sharingRequest.OwnerName = owner.Name; // Refresh to be safe.
            entitiesToSave.Add(sharingRequest);

            foreach (var assetGroup in assetGroups)
            {
                // Find or create the sharing relationship.
                if (!sharingRequest.Relationships.ContainsKey(assetGroup.Id))
                {
                    sharingRequest.Relationships[assetGroup.Id] = new SharingRelationship
                    {
                        AssetGroupId = assetGroup.Id,
                        AssetQualifier = assetGroup.Qualifier,
                        Capabilities = new CapabilityId[0]
                    };
                }

                var relationship = sharingRequest.Relationships[assetGroup.Id];

                // Set the capabilities on the relationship.
                var capabilities = this.assetGroupIds[assetGroup.Id].Actions.Select(x => x.CapabilityId);
                relationship.Capabilities = relationship.Capabilities.Concat(capabilities).Distinct();

                results.Add(new SetAgentRelationshipResponse.AssetGroupResult
                {
                    AssetGroupId = assetGroup.Id,
                    Capabilities = capabilities.Select(x => new SetAgentRelationshipResponse.CapabilityResult
                    {
                        CapabilityId = x,
                        SharingRequestId = sharingRequest.Id,
                        Status = SetAgentRelationshipResponse.StatusType.Requested
                    })
                });
            }

            // Update the asset group values.
            this.UpdateAssetGroupSharingRequestIds(assetGroups, sharingRequest);

            return Tuple.Create<IEnumerable<Entity>, IEnumerable<SetAgentRelationshipResponse.AssetGroupResult>>(entitiesToSave, results);
        }

        private void UpdateAssetGroupSharingRequestIds(IEnumerable<AssetGroup> assetGroups, SharingRequest sharingRequest)
        {
            foreach (var assetGroup in assetGroups)
            {
                var relationship = sharingRequest.Relationships[assetGroup.Id];

                if (relationship.Capabilities.Contains(this.policy.Capabilities.Ids.Delete))
                {
                    assetGroup.DeleteSharingRequestId = sharingRequest.Id;
                    assetGroup.DeleteAgentId = null;
                }

                if (relationship.Capabilities.Contains(this.policy.Capabilities.Ids.Export))
                {
                    assetGroup.ExportSharingRequestId = sharingRequest.Id;
                    assetGroup.ExportAgentId = null;
                }
            }
        }

        private async Task<Tuple<IEnumerable<Entity>, IEnumerable<SetAgentRelationshipResponse.AssetGroupResult>>> CalculateClearChanges(IEnumerable<AssetGroup> assetGroups)
        {
            List<Guid> clearedDeleteAssetGroupIds = new List<Guid>();
            List<Guid> clearedExportAssetGroupIds = new List<Guid>();
            List<Guid?> impactedSharingRequestIds = new List<Guid?>();
            List<Guid?> impactedAgentIds = new List<Guid?>();

            var results = new List<SetAgentRelationshipResponse.AssetGroupResult>();

            // Update the asset group ids.
            foreach (var assetGroup in assetGroups)
            {
                var capabilities = this.assetGroupIds[assetGroup.Id].Actions.Select(x => x.CapabilityId);

                if (capabilities.Contains(this.policy.Capabilities.Ids.Delete))
                {
                    impactedSharingRequestIds.Add(assetGroup.DeleteSharingRequestId);
                    impactedAgentIds.Add(assetGroup.DeleteAgentId);
                    clearedDeleteAssetGroupIds.Add(assetGroup.Id);

                    assetGroup.DeleteAgentId = null;
                    assetGroup.DeleteSharingRequestId = null;

                    if (assetGroup.OwnerId == Guid.Empty)
                    {
                        // We must delete the asset group
                        // if we are removing the agent link and there is no owner.
                        // Otherwise, the asset group becomes orphanded.
                        assetGroup.IsDeleted = true;
                    }
                }

                if (capabilities.Contains(this.policy.Capabilities.Ids.Export))
                {
                    impactedSharingRequestIds.Add(assetGroup.ExportSharingRequestId);
                    impactedAgentIds.Add(assetGroup.ExportAgentId);
                    clearedExportAssetGroupIds.Add(assetGroup.Id);

                    assetGroup.ExportAgentId = null;
                    assetGroup.ExportSharingRequestId = null;
                }

                results.Add(new SetAgentRelationshipResponse.AssetGroupResult
                {
                    AssetGroupId = assetGroup.Id,
                    Capabilities = capabilities.Select(x => new SetAgentRelationshipResponse.CapabilityResult
                    {
                        CapabilityId = x,
                        Status = SetAgentRelationshipResponse.StatusType.Removed
                    })
                });
            }

            var sharingRequests = await this.ClearSharingRequests(clearedDeleteAssetGroupIds, clearedExportAssetGroupIds, impactedSharingRequestIds).ConfigureAwait(false);

            var agentIds = impactedAgentIds.Distinct().Where(x => x.IsSet()).Select(x => x.Value);
            var modifiedAgents = new List<DeleteAgent>();

            if (agentIds.Any())
            {
                var agents = await this.deleteAgentReader.ReadByIdsAsync(agentIds, ExpandOptions.WriteProperties).ConfigureAwait(false);

                foreach (var agent in agents)
                {
                    // Determine if any asset groups are linked to this agent for specific capabilities.
                    var filterCriteria =
                    new AssetGroupFilterCriteria { DeleteAgentId = agent.Id }
                    .Or(new AssetGroupFilterCriteria { ExportAgentId = agent.Id })
                    .Or(new AssetGroupFilterCriteria { AccountCloseAgentId = agent.Id });

                    var remainingAssetGroups = await this.entityReader.ReadByFiltersAsync(filterCriteria, ExpandOptions.None).ConfigureAwait(false);

                    // Identify the set of capabilities that are appropriate for this agent.
                    var capabilities = new List<CapabilityId>();

                    if (remainingAssetGroups.Values.Any(x => x.DeleteAgentId == agent.Id && !clearedDeleteAssetGroupIds.Any(y => y == x.Id)))
                    {
                        capabilities.Add(this.policy.Capabilities.Ids.Delete);
                    }

                    if (remainingAssetGroups.Values.Any(x => x.ExportAgentId == agent.Id && !clearedExportAssetGroupIds.Any(y => y == x.Id)))
                    {
                        capabilities.Add(this.policy.Capabilities.Ids.Export);
                    }

                    if (remainingAssetGroups.Values.Any(x => x.AccountCloseAgentId == agent.Id))
                    {
                        capabilities.Add(this.policy.Capabilities.Ids.AccountClose);
                    }

                    // Determine if the capabilities have changed,
                    // and apply the updated values.
                    if (!capabilities.OrderBy(x => x.Value).SequenceEqual(agent.Capabilities?.OrderBy(x => x.Value) ?? Enumerable.Empty<CapabilityId>()))
                    {
                        agent.Capabilities = capabilities;
                        modifiedAgents.Add(agent);
                    }
                }
            }

            return Tuple.Create<IEnumerable<Entity>, IEnumerable<SetAgentRelationshipResponse.AssetGroupResult>>(sharingRequests.Cast<Entity>().Concat(modifiedAgents), results);
        }

        private async Task<IEnumerable<Entity>> ClearSharingRequests(
            List<Guid> clearedDeleteAssetGroupIds,
            List<Guid> clearedExportAssetGroupIds,
            List<Guid?> impactedSharingRequestIds)
        {
            var requestIds = impactedSharingRequestIds.Distinct().Where(x => x.IsSet()).Select(x => x.Value);
            var sharingRequests = Enumerable.Empty<SharingRequest>();

            if (requestIds.Any())
            {
                sharingRequests = await this.sharingRequestReader.ReadByIdsAsync(requestIds, ExpandOptions.WriteProperties).ConfigureAwait(false);

                foreach (var sharingRequest in sharingRequests)
                {
                    foreach (var assetId in clearedDeleteAssetGroupIds)
                    {
                        if (sharingRequest.Relationships.ContainsKey(assetId))
                        {
                            sharingRequest.Relationships[assetId].Capabilities = sharingRequest.Relationships[assetId].Capabilities.Where(x => x != this.policy.Capabilities.Ids.Delete);
                        }
                    }

                    foreach (var assetId in clearedExportAssetGroupIds)
                    {
                        if (sharingRequest.Relationships.ContainsKey(assetId))
                        {
                            sharingRequest.Relationships[assetId].Capabilities = sharingRequest.Relationships[assetId].Capabilities.Where(x => x != this.policy.Capabilities.Ids.Export);
                        }
                    }

                    sharingRequest.Relationships = sharingRequest.Relationships.Where(x => x.Value.Capabilities.Any()).ToDictionary(x => x.Key, x => x.Value);

                    // Delete requests that no longer have any asset groups in them.
                    if (!sharingRequest.Relationships.Any())
                    {
                        sharingRequest.IsDeleted = true;
                    }
                }
            }

            return sharingRequests;
        }

        private Entity UpdateTrackingDetails(Entity entity)
        {
            var now = this.dateFactory.GetCurrentTime();

            if (entity.TrackingDetails == null)
            {
                if (!string.IsNullOrEmpty(entity.ETag))
                {
                    // Put in a guard to ensure we do not clear data on accident.
                    // Any entity that is updated must retrieve all properties from storage.
                    throw new InvalidOperationException("Cannot update an entity without first retrieving tracking details.");
                }

                entity.TrackingDetails = new TrackingDetails
                {
                    CreatedBy = this.authenticatedPrincipal.UserId,
                    CreatedOn = now,
                    Version = 0 // Use 0 so that it is incremented properly below.
                };
            }

            entity.TrackingDetails.Version += 1;
            entity.TrackingDetails.UpdatedBy = this.authenticatedPrincipal.UserId;
            entity.TrackingDetails.UpdatedOn = now;

            // Perform a consistency check to ensure mapping logic is correct.
            var assetGroup = entity as AssetGroup;
            if (assetGroup != null)
            {
                if (assetGroup.DeleteAgentId != null && assetGroup.DeleteSharingRequestId != null)
                {
                    throw new InvalidOperationException($"Asset group has become inconsistent for Delete capability. AgentId: {assetGroup.DeleteAgentId}, RequestId: {assetGroup.DeleteSharingRequestId}");
                }
                else if (assetGroup.ExportAgentId != null && assetGroup.ExportSharingRequestId != null)
                {
                    throw new InvalidOperationException($"Asset group has become inconsistent for Export capability. AgentId: {assetGroup.ExportAgentId}, RequestId: {assetGroup.ExportSharingRequestId}");
                }
            }

            return entity;
        }

        private async Task<SetAgentRelationshipResponse> ExecuteUpdates(
            IEnumerable<AssetGroup> assetGroups,
            Func<Task<Tuple<IEnumerable<Entity>, IEnumerable<SetAgentRelationshipResponse.AssetGroupResult>>>> calculateChanges)
        {
            // Identify any changes to other entities,
            // and apply updates to the asset groups.
            var entityChanges = await calculateChanges().ConfigureAwait(false);

            var finalChanges = entityChanges.Item1.Concat(assetGroups).Select(this.UpdateTrackingDetails).ToArray(); // Force evaluation early to cache the results.

            // Write the entity changes to storage.
            var assetGroupUpdates = await this.storageWriter.UpdateEntitiesAsync(finalChanges).ConfigureAwait(false);

            // Update the etags in the response based on the returned storage data.
            var responseValues = entityChanges.Item2.ToDictionary(x => x.AssetGroupId);

            foreach (var update in assetGroupUpdates)
            {
                if (update.EntityType == EntityType.AssetGroup)
                {
                    responseValues[update.Id].ETag = update.ETag;
                }
            }

            // Return the final payload.
            return new SetAgentRelationshipResponse { Results = responseValues.Values };
        }
    }
}