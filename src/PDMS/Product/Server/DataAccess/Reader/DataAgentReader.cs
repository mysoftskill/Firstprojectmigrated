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
    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;

    /// <summary>
    /// Provides methods for reading data agent information.
    /// </summary>
    public class DataAgentReader : EntityReader<DataAgent>, IDataAgentReader
    {
        private readonly ISessionFactory sessionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataAgentReader" /> class.
        /// </summary>
        /// <param name="storageReader">The storage reader instance.</param>
        /// <param name="coreConfiguration">The core configuration.</param>
        /// <param name="authorizationProvider">The authorization provider instance.</param>
        /// <param name="sessionFactory">Session factory enable instrumentation</param>
        public DataAgentReader(
            IPrivacyDataStorageReader storageReader, 
            ICoreConfiguration coreConfiguration, 
            IAuthorizationProvider authorizationProvider,
            ISessionFactory sessionFactory)
            : base(storageReader, coreConfiguration, authorizationProvider)
        {
            this.AuthorizationRoles = AuthorizationRole.ApplicationAccess;
            this.sessionFactory = sessionFactory;
        }

        /// <summary>
        /// Get entity for given id. Returns null if not found.
        /// </summary>
        /// <param name="id">Entity id.</param>
        /// <param name="expandOptions">Expand options for the entity.</param>
        /// <returns>Entity for given id.</returns>
        public async Task<DataAgent> ReadByIdAsync(Guid id, ExpandOptions expandOptions)
        {
            (DataAccessResult logInfo, DataAgent dataAgent) result = await this.sessionFactory.InstrumentAsync(
                "DataAgentReader.ReadByIdAsync",
                SessionType.Outgoing,
                async () =>
                {
                    await this.AuthorizationProvider.AuthorizeAsync(this.AuthorizationRoles).ConfigureAwait(false);

                    var includeTrackingDetails = expandOptions.HasFlag(ExpandOptions.TrackingDetails);

                    var dataAgent = await this.StorageReader.GetDataAgentAsync(id, includeTrackingDetails);

                    return (new DataAccessResult()
                    {
                        AccessKey = id.ToString(),
                        TotalHits = dataAgent != null ? 1 : 0
                    }, dataAgent);
                }).ConfigureAwait(false);

            return result.dataAgent;
        }

        /// <summary>
        /// Get entities based on filter criteria.
        /// </summary>
        /// <param name="filterCriteria">Filter criteria for the entity.</param>
        /// <param name="expandOptions">Expand options for the entity.</param>
        /// <returns>Entities matching filter criteria.</returns>
        public async Task<FilterResult<DataAgent>> ReadByFiltersAsync(IFilterCriteria<DataAgent> filterCriteria,
            ExpandOptions expandOptions)
        {
            (DataAccessResult logInfo, FilterResult<DataAgent> dataAgents) result = await this.sessionFactory.InstrumentAsync(
                "DataAgentReader.ReadByFiltersAsync",
                SessionType.Outgoing,
                async () =>
                {
                    await this.AuthorizationProvider.AuthorizeAsync(this.AuthorizationRoles).ConfigureAwait(false);

                    filterCriteria.Initialize(this.MaxPageSize);

                    var includeTrackingDetails = expandOptions.HasFlag(ExpandOptions.TrackingDetails);

                    var dataAgents = await this.StorageReader.GetDataAgentsAsync(filterCriteria, includeTrackingDetails)
                        .ConfigureAwait(false);

                    return (new DataAccessResult()
                    {
                        AccessKey = filterCriteria.BuildExpression().ToString(),
                        TotalHits = dataAgents?.Count ?? 0,
                    }, dataAgents);
                }).ConfigureAwait(false);

            return result.dataAgents;
        }

        /// <summary>
        /// Determines if there are any other entities linked to this data agent.
        /// </summary>
        /// <param name="id">The id of the data agent.</param>
        /// <returns>True if the data agent is linked to any other entities, False otherwise.</returns>
        public async Task<bool> IsLinkedToAnyOtherEntities(Guid id)
        {
            (DataAccessResult logInfo, bool isLinked) result = await this.sessionFactory.InstrumentAsync(
                "DataAgentReader.IsLinkedToAnyOtherEntities",
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
                "DataAgentReader.HasPendingCommands",
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
        /// Get entities based on a set of ids.
        /// </summary>
        /// <param name="ids">The set if ids to retrieve.</param>
        /// <param name="expandOptions">The set of expand options.</param>
        /// <returns>Entities matching the set of ids. If some ids don't have matches, then those entities are omitted.</returns>
        protected override async Task<IEnumerable<DataAgent>> ReadByIdsFromStorageAsync(IEnumerable<Guid> ids,
            ExpandOptions expandOptions)
        {
            (DataAccessResult logInfo, IEnumerable<DataAgent> dataAgents) result = await this.sessionFactory.InstrumentAsync(
                "DataAgentReader.ReadByIdsFromStorageAsync",
                SessionType.Outgoing,
                async () =>
                {
                    await this.AuthorizationProvider.AuthorizeAsync(this.AuthorizationRoles).ConfigureAwait(false);

                    var includeTrackingDetails = expandOptions.HasFlag(ExpandOptions.TrackingDetails);

                    var dataAgents = await this.StorageReader.GetDataAgentsAsync<DataAgent>(ids,
                                        expandOptions.HasFlag(ExpandOptions.TrackingDetails));

                    return (new DataAccessResult()
                    {
                        AccessKey = ids.ToArray().ToString(),
                        TotalHits = dataAgents?.Count() ?? 0
                    }, dataAgents);
                }).ConfigureAwait(false);

            return result.dataAgents;
        }
    }
}