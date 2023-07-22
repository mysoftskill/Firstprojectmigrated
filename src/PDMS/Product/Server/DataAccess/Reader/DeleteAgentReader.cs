namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Reader
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Client;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authorization;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac.Sll;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Readers;
    using Microsoft.PrivacyServices.DataManagement.Models.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;
    using Microsoft.PrivacyServices.Policy;

    /// <summary>
    /// Provides methods for reading delete agents.
    /// </summary>
    public class DeleteAgentReader : EntityReader<DeleteAgent>, IDeleteAgentReader
    {
        private readonly IAssetGroupReader assetGroupReader;
        private readonly ISharingRequestReader sharingRequestReader;
        private readonly int assetGroupCapForAgentHealth;
        private readonly ISessionFactory sessionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteAgentReader" /> class.
        /// </summary>
        /// <param name="assetGroupReader">The asset group reader instance.</param>
        /// <param name="sharingRequestReader">The sharing request reader instance.</param>
        /// <param name="storageReader">The storage reader instance.</param>
        /// <param name="coreConfiguration">The core configuration.</param>
        /// <param name="authorizationProvider">The authorization provider instance.</param>
        /// <param name="sessionFactory">Session factory enable instrumentation</param>
        public DeleteAgentReader(
            IAssetGroupReader assetGroupReader,
            ISharingRequestReader sharingRequestReader,
            IPrivacyDataStorageReader storageReader,
            ICoreConfiguration coreConfiguration,
            IAuthorizationProvider authorizationProvider,
            ISessionFactory sessionFactory)
            : base(storageReader, coreConfiguration, authorizationProvider)
        {
            this.assetGroupReader = assetGroupReader;
            this.sharingRequestReader = sharingRequestReader;
            this.assetGroupCapForAgentHealth = coreConfiguration.AssetGroupCapForAgentHealth;

            this.AuthorizationRoles = AuthorizationRole.ApplicationAccess;
            this.sessionFactory = sessionFactory;   
        }

        /// <summary>
        /// Get entity for given id. Returns null if not found.
        /// </summary>
        /// <param name="id">Entity id.</param>
        /// <param name="expandOptions">Expand options for the entity.</param>
        /// <returns>Entity for given id.</returns>
        public async Task<DeleteAgent> ReadByIdAsync(Guid id, ExpandOptions expandOptions)
        {
            await this.AuthorizationProvider.AuthorizeAsync(this.AuthorizationRoles).ConfigureAwait(false);

            var includeTrackingDetails = expandOptions.HasFlag(ExpandOptions.TrackingDetails);

            var deleteAgent = await this.StorageReader.GetDataAgentAsync(id, includeTrackingDetails).ConfigureAwait(false) as DeleteAgent;

            if (deleteAgent != null)
            {
                await this.ExpandHasSharingRequests(deleteAgent, expandOptions).ConfigureAwait(false);
            }

            return deleteAgent;
        }

        /// <summary>
        /// Get entities based on filter criteria.
        /// </summary>
        /// <param name="filterCriteria">Filter criteria for the entity.</param>
        /// <param name="expandOptions">Expand options for the entity.</param>
        /// <returns>Entities matching filter criteria.</returns>
        public async Task<FilterResult<DeleteAgent>> ReadByFiltersAsync(IFilterCriteria<DeleteAgent> filterCriteria, ExpandOptions expandOptions)
        {
            await this.AuthorizationProvider.AuthorizeAsync(this.AuthorizationRoles).ConfigureAwait(false);

            filterCriteria.Initialize(this.MaxPageSize);

            var includeTrackingDetails = expandOptions.HasFlag(ExpandOptions.TrackingDetails);

            var dataAgents = await this.StorageReader.GetDataAgentsAsync(filterCriteria, includeTrackingDetails).ConfigureAwait(false);

            foreach (var deleteAgent in dataAgents.Values)
            {
                await this.ExpandHasSharingRequests(deleteAgent, expandOptions).ConfigureAwait(false);
            }

            return dataAgents;
        }

        /// <summary>
        /// Determines if there are any other entities linked to the data owner entity.
        /// </summary>
        /// <param name="id">The id of the data owner entity.</param>
        /// <returns>True if the data owner entity is linked to any other entities, False otherwise.</returns>
        public async Task<bool> IsLinkedToAnyOtherEntities(Guid id)
        {
            (DataAccessResult logInfo, bool isLinked) result = await this.sessionFactory.InstrumentAsync(
                "DeleteAgentReader.IsLinkedToAnyOtherEntities",
                SessionType.Outgoing,
                async () =>
                {
                    bool isLinked = await this.StorageReader.IsDataAgentLinkedToAnyOtherEntities(id);

                    return (new DataAccessResult()
                    {
                        AccessKey = id.ToString(),
                        TotalHits = isLinked ? 1 : 0
                    }, isLinked);
                }).ConfigureAwait(false);

            return result.isLinked;
        }

        /// <summary>
        /// Determines if there are any pending commands for the entity.
        /// </summary>
        /// <param name="id">The id of the entity.</param>
        /// <returns>True if pending commands found, False otherwise.</returns>
        public override async Task<bool> HasPendingCommands(Guid id)
        {
            (DataAccessResult logInfo, bool hasPending) result = await this.sessionFactory.InstrumentAsync<(DataAccessResult logInfo, bool hasPending), BaseException>(
                "DeleteAgentReader.HasPendingCommands",
                SessionType.Outgoing,
                async () =>
                {
                    bool hasPending = await this.StorageReader.DataAgentHasPendingCommands(id);

                    return (new DataAccessResult()
                    {
                        AccessKey = id.ToString(),
                        TotalHits = hasPending ? 1 : 0
                    }, hasPending);
                }).ConfigureAwait(false);

            return result.hasPending;
        }

        /// <summary>
        /// Calculate the agent registration status.
        /// </summary>
        /// <param name="id">The id of the agent.</param>
        /// <returns>The registration status.</returns>
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public async Task<AgentRegistrationStatus> CalculateRegistrationStatus(Guid id)
        {
            var agent = await this.ReadByIdAsync(id, ExpandOptions.None).ConfigureAwait(false);

            if (agent == null)
            {
                throw new EntityNotFoundException(id, "DeleteAgent");
            }
            else
            {
                var assetGroupFilter =
                    new AssetGroupFilterCriteria { DeleteAgentId = id }
                    .Or(new AssetGroupFilterCriteria { ExportAgentId = id });

                // Load all asset groups.
                var assetGroups = await this.Recurse(assetGroupFilter, (filter) => this.assetGroupReader.ReadByFiltersAsync(filter, ExpandOptions.None)).ConfigureAwait(false);

                var assetGroupsStatus = new List<AssetGroupRegistrationStatus>();

                if (assetGroups.Total > 0)
                {
                    int assetGroupIndex = 0;

                    foreach (var assetGroup in assetGroups.Values)
                    {
                        if (assetGroupIndex++ >= this.assetGroupCapForAgentHealth)
                        {
                            // Set it as truncated.
                            var assetGroupStatus = new AssetGroupRegistrationStatus();
                            assetGroupsStatus.Add(assetGroupStatus);

                            assetGroupStatus.Id = assetGroup.Id;
                            assetGroupStatus.OwnerId = assetGroup.OwnerId;
                            assetGroupStatus.Qualifier = assetGroup.Qualifier;
                            assetGroupStatus.Assets = Enumerable.Empty<AssetRegistrationStatus>();
                            assetGroupStatus.AssetsStatus = RegistrationState.ValidButTruncated;
                            assetGroupStatus.IsComplete = false;
                        }
                        else
                        {
                            var status = await this.assetGroupReader.CalculateRegistrationStatus(assetGroup).ConfigureAwait(false);
                            assetGroupsStatus.Add(status);
                        }
                    }
                }

                // Calculate status based on the loaded data.
                var agentRegistrationStatus = new AgentRegistrationStatus { Id = id, OwnerId = agent.OwnerId };

                // Calculate protocol status.
                agentRegistrationStatus.Protocols = agent.ConnectionDetails.Values.Select(x => x.Protocol).Distinct();
                agentRegistrationStatus.ProtocolStatus = agentRegistrationStatus.Protocols.Count() > 1 ? RegistrationState.Invalid : RegistrationState.Valid;
                
                // Calculate environment status.
                agentRegistrationStatus.Environments = agent.ConnectionDetails.Keys;
                agentRegistrationStatus.EnvironmentStatus = agent.ConnectionDetails.ContainsKey(ReleaseState.Prod) ? RegistrationState.Valid : RegistrationState.Partial;

                // Calculate capability status.
                var linkedForDelete = assetGroups.Values.Where(x => x.DeleteAgentId == id).Any();
                var linkedForExport = assetGroups.Values.Where(x => x.ExportAgentId == id).Any();
                var capabilities = new List<CapabilityId>();

                if (linkedForDelete)
                {
                    capabilities.Add(Policies.Current.Capabilities.Ids.Delete);
                    capabilities.Add(Policies.Current.Capabilities.Ids.AccountClose);
                }

                if (linkedForExport)
                {
                    capabilities.Add(Policies.Current.Capabilities.Ids.Export);
                }

                agentRegistrationStatus.Capabilities = capabilities;
                agentRegistrationStatus.CapabilityStatus = agentRegistrationStatus.Capabilities.Any() ? RegistrationState.Valid : RegistrationState.Missing;

                // Calculate asset groups.
                agentRegistrationStatus.AssetGroups = assetGroupsStatus;

                if (agentRegistrationStatus.AssetGroups.Any())
                {
                    if (agentRegistrationStatus.AssetGroups.All(x => x.IsComplete))
                    {
                        agentRegistrationStatus.AssetGroupsStatus = RegistrationState.Valid;
                    }
                    else if (agentRegistrationStatus.AssetGroups.All(x => x.IsComplete || x.AssetsStatus == RegistrationState.ValidButTruncated))
                    {
                        agentRegistrationStatus.AssetGroupsStatus = RegistrationState.ValidButTruncated;
                    }
                    else if (agentRegistrationStatus.AssetGroups.All(x => !x.IsComplete))
                    {
                        agentRegistrationStatus.AssetGroupsStatus = RegistrationState.Invalid;
                    }
                    else
                    {
                        agentRegistrationStatus.AssetGroupsStatus = RegistrationState.Partial;
                    }
                }
                else
                {
                    agentRegistrationStatus.AssetGroupsStatus = RegistrationState.Missing;
                }

                // Calculate summary.
                agentRegistrationStatus.IsComplete =
                    agentRegistrationStatus.ProtocolStatus == RegistrationState.Valid &&
                    agentRegistrationStatus.EnvironmentStatus == RegistrationState.Valid &&
                    agentRegistrationStatus.CapabilityStatus == RegistrationState.Valid &&
                    agentRegistrationStatus.AssetGroupsStatus == RegistrationState.Valid;

                return agentRegistrationStatus;
            }
        }

        /// <summary>
        /// Get entities based on a set of ids.
        /// </summary>
        /// <param name="ids">The set if ids to retrieve.</param>
        /// <param name="expandOptions">The set of expand options.</param>
        /// <returns>Entities matching the set of ids. If some ids don't have matches, then those entities are omitted.</returns>
        protected override async Task<IEnumerable<DeleteAgent>> ReadByIdsFromStorageAsync(IEnumerable<Guid> ids, ExpandOptions expandOptions)
        {
            var dataAgents = await this.StorageReader.GetDataAgentsAsync<DeleteAgent>(ids, expandOptions.HasFlag(ExpandOptions.TrackingDetails)).ConfigureAwait(false);

            foreach (var deleteAgent in dataAgents)
            {
                await this.ExpandHasSharingRequests(deleteAgent, expandOptions).ConfigureAwait(false);
            }

            return dataAgents;
        }

        private async Task ExpandHasSharingRequests(DeleteAgent deleteAgent, ExpandOptions expandOptions)
        {
            if (expandOptions.HasFlag(ExpandOptions.HasSharingRequests))
            {
                var sharingRequestFilter = new SharingRequestFilterCriteria
                {
                    DeleteAgentId = deleteAgent.Id,
                    Count = 0
                };

                var results = await this.sharingRequestReader.ReadByFiltersAsync(sharingRequestFilter, ExpandOptions.None).ConfigureAwait(false);

                deleteAgent.HasSharingRequests = results.Total > 0;
            }
        }

        private async Task<FilterResult<V>> Recurse<T, V>(T filterCriteria, Func<T, Task<FilterResult<V>>> action) where T : IFilterCriteria
        {
            IEnumerable<V> data = Enumerable.Empty<V>();
            FilterResult<V> result;

            do
            {
                result = await action(filterCriteria).ConfigureAwait(false);

                filterCriteria.Index += filterCriteria.Count;

                if (result.Values != null)
                {
                    data = data.Concat(result.Values);
                }
            }
            while (result.Total > filterCriteria.Index);

            result.Values = data;
            return result;
        }
    }
}