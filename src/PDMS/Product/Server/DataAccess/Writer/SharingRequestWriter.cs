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
    using Microsoft.PrivacyServices.DataManagement.Models.V2;
    using Microsoft.PrivacyServices.Policy;

    /// <summary>
    /// Provides methods for writing sharing request information.
    /// </summary>
    public class SharingRequestWriter : EntityWriter<SharingRequest>, ISharingRequestWriter
    {
        private readonly IDataOwnerReader dataOwnerReader;
        private readonly IDeleteAgentReader deleteAgentReader;
        private readonly IAssetGroupReader assetGroupReader;
        private readonly Policy policy;

        /// <summary>
        /// Initializes a new instance of the <see cref="SharingRequestWriter" /> class.
        /// </summary>
        /// <param name="storageWriter">The storage writer instance.</param>
        /// <param name="entityReader">The entity reader instance.</param>
        /// <param name="authenticatedPrincipal">The principal object created for this specific request.</param>
        /// <param name="authorizationProvider">The authorization provider.</param>
        /// <param name="dateFactory">The date factory instance.</param>
        /// <param name="mapper">A utility for mapping data types by convention.</param>
        /// <param name="dataOwnerReader">The reader for data owners.</param>
        /// <param name="deleteAgentReader">The reader for delete agents.</param>
        /// <param name="assetGroupReader">The reader for asset groups.</param>
        /// <param name="policy">The policy instance.</param>
        public SharingRequestWriter(
            IPrivacyDataStorageWriter storageWriter,
            ISharingRequestReader entityReader,
            AuthenticatedPrincipal authenticatedPrincipal,
            IAuthorizationProvider authorizationProvider,
            IDateFactory dateFactory,
            IMapper mapper,
            IDataOwnerReader dataOwnerReader,
            IDeleteAgentReader deleteAgentReader,
            IAssetGroupReader assetGroupReader,
            Policy policy)
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
            this.assetGroupReader = assetGroupReader;
            this.policy = policy;
            
            this.AuthorizationRoles = AuthorizationRole.ServiceEditor;
        }

        /// <summary>
        /// Approves the sharing request.
        /// Approval results in the deletion of the request
        /// and an update on all associated asset groups.
        /// </summary>
        /// <param name="id">The id of the sharing request to approve.</param>
        /// <param name="etag">The ETag of the sharing request.</param>
        /// <returns>A task that performs the asynchronous execution.</returns>
        public async Task ApproveAsync(Guid id, string etag)
        {            
            var action = WriteAction.Update;

            var existingEntity = await this.GetExistingEntityWithConsistencyChecks(id, etag).ConfigureAwait(false);
            
            await this.AuthorizeAsync(action, existingEntity).ConfigureAwait(false);
            
            // Delete the request as part of approving it.
            existingEntity.IsDeleted = true;
            
            // Clear the sharing request ids from all linked asset groups.
            // And update the agent id with the correct value.
            var assetGroupIds = existingEntity.Relationships.Keys;

            var assetGroups = await this.assetGroupReader.ReadByIdsAsync(assetGroupIds, ExpandOptions.WriteProperties).ConfigureAwait(false);

            var agent = await this.GetExistingDataAgentAsync(existingEntity.DeleteAgentId).ConfigureAwait(false);

            var capabilities = agent.Capabilities?.ToList() ?? new List<CapabilityId>();

            foreach (var assetGroup in assetGroups)
            {
                if (assetGroup.DeleteSharingRequestId == existingEntity.Id)
                {
                    assetGroup.DeleteSharingRequestId = null;
                    assetGroup.DeleteAgentId = existingEntity.DeleteAgentId;
                    capabilities.Add(this.policy.Capabilities.Ids.Delete);
                }

                if (assetGroup.ExportSharingRequestId == existingEntity.Id)
                {
                    assetGroup.ExportSharingRequestId = null;
                    assetGroup.ExportAgentId = existingEntity.DeleteAgentId;
                    capabilities.Add(this.policy.Capabilities.Ids.Export);
                }
            }

            // Update the agent capabilities with the changes.
            agent.Capabilities = capabilities.Distinct();

            // Update all entities with proper tracking details.
            var updatedEntities = 
                new[] { (Entity)existingEntity, agent }.Concat(assetGroups)
                .Select(x =>
                {
                    this.PopulateProperties(action, x);
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
        public override async Task<IEnumerable<DataOwner>> GetDataOwnersAsync(WriteAction action, SharingRequest incomingEntity)
        {
            // At the moment, all APIs are assumed to be called from the perspective of the data agent owner, not the sharing request owner.
            var deleteAgent = await this.GetExistingDataAgentAsync(incomingEntity.DeleteAgentId).ConfigureAwait(false);

            var owner = await this.GetExistingOwnerAsync(deleteAgent.OwnerId).ConfigureAwait(false);

            return new[] { owner };
        }

        /// <summary>
        /// Create the entity in storage.
        /// </summary>
        /// <param name="action">The write action for the request.</param>
        /// <param name="entity">The incoming entity.</param>
        /// <returns>A task that performs the checks.</returns>
        public override async Task<SharingRequest> WriteAsync(WriteAction action, SharingRequest entity)
        {
            if (action == WriteAction.SoftDelete)
            {
                // Clear the sharing request ids from all linked asset groups.
                var assetGroupIds = entity.Relationships.Keys;

                var assetGroups = await this.assetGroupReader.ReadByIdsAsync(assetGroupIds, ExpandOptions.WriteProperties).ConfigureAwait(false);

                foreach (var assetGroup in assetGroups)
                {
                    if (assetGroup.DeleteSharingRequestId == entity.Id)
                    {
                        assetGroup.DeleteSharingRequestId = null;
                    }

                    if (assetGroup.ExportSharingRequestId == entity.Id)
                    {
                        assetGroup.ExportSharingRequestId = null;
                    }

                    this.PopulateProperties(WriteAction.Update, assetGroup);
                }

                await this.StorageWriter.UpdateEntitiesAsync(new[] { (Entity)entity }.Concat(assetGroups)).ConfigureAwait(false);

                return null; // Delete does not return a value, so no need to extract the correct entity from the storage results.
            }
            else
            {
                throw new NotImplementedException($"Invalid write action: {action}");
            }
        }

        private Task<DeleteAgent> GetExistingDataAgentAsync(Guid agentId)
        {
            // We need tracking details in order to perform updates on the data agents.
            return this.MemoizeAsync(agentId, () => this.deleteAgentReader.ReadByIdAsync(agentId, ExpandOptions.WriteProperties));
        }

        private Task<DataOwner> GetExistingOwnerAsync(Guid ownerId)
        {
            return this.MemoizeAsync(ownerId, () => this.dataOwnerReader.ReadByIdAsync(ownerId, ExpandOptions.None));
        }
    }
}