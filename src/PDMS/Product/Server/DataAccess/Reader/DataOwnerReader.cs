namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Reader
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authentication;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authorization;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Readers;
    using Microsoft.PrivacyServices.DataManagement.Models.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;

    /// <summary>
    /// Provides methods for reading data owner information.
    /// </summary>
    public class DataOwnerReader : EntityReader<DataOwner>, IDataOwnerReader
    {
        private static readonly IEqualityComparer<DataOwner> Comparer = new EntityEqualityComparer<DataOwner>();
        private readonly AuthenticatedPrincipal authenticatedPrincipal;
        private readonly IActiveDirectory activeDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataOwnerReader" /> class.
        /// </summary>
        /// <param name="storageReader">The storage reader instance.</param>
        /// <param name="coreConfiguration">The core configuration.</param>
        /// <param name="authorizationProvider">The authorization provider instance.</param>
        /// <param name="authenticatedPrincipal">The authenticated user.</param>
        /// <param name="activeDirectory">The active directory instance.</param>
        public DataOwnerReader(
            IPrivacyDataStorageReader storageReader, 
            ICoreConfiguration coreConfiguration,
            IAuthorizationProvider authorizationProvider,
            AuthenticatedPrincipal authenticatedPrincipal,
            IActiveDirectory activeDirectory)
            : base(storageReader, coreConfiguration, authorizationProvider)
        {
            this.authenticatedPrincipal = authenticatedPrincipal;
            this.activeDirectory = activeDirectory;
            this.AuthorizationRoles = AuthorizationRole.ApplicationAccess;
        }

        /// <summary>
        /// Get data owner for given id.
        /// </summary>
        /// <param name="id">Data owner id.</param>
        /// <param name="expandOptions">Expand options for data owner.</param>
        /// <returns>Data owner for given id.</returns>
        public async Task<DataOwner> ReadByIdAsync(Guid id, ExpandOptions expandOptions)
        {
            await this.AuthorizationProvider.AuthorizeAsync(this.AuthorizationRoles).ConfigureAwait(false);

            var includeTrackingDetails = expandOptions.HasFlag(ExpandOptions.TrackingDetails);

            // We always want the service tree data back for data owners, so that we can use the service admin list to authorize users.
            var dataOwner = await this.StorageReader.GetDataOwnerAsync(id, includeTrackingDetails, true).ConfigureAwait(false);

            if (dataOwner != null)
            {
                if (expandOptions.HasFlag(ExpandOptions.DataAgents))
                {
                    var dataAgentFilter = new DataAgentFilterCriteria { OwnerId = dataOwner.Id }.Initialize(this.MaxPageSize);

                    var dataAgents = await this.StorageReader.GetDataAgentsAsync(dataAgentFilter, false).ConfigureAwait(false);

                    if (dataAgents.Total > this.MaxPageSize)
                    {
                        throw new ConflictException(ConflictType.MaxExpansionSizeExceeded, "Number of expanded values exceeds the maximum allowed paging size.", "dataAgents", dataAgents.Total.ToString());
                    }

                    dataOwner.DataAgents = dataAgents.Values;
                }

                if (expandOptions.HasFlag(ExpandOptions.AssetGroups))
                {
                    var assetGroupFilter = new AssetGroupFilterCriteria { OwnerId = dataOwner.Id }.Initialize(this.MaxPageSize);

                    var assetGroups = await this.StorageReader.GetAssetGroupsAsync(assetGroupFilter, false).ConfigureAwait(false);

                    if (assetGroups.Total > this.MaxPageSize)
                    {
                        throw new ConflictException(ConflictType.MaxExpansionSizeExceeded, "Number of expanded values exceeds the maximum allowed paging size.", "assetGroups", assetGroups.Total.ToString());
                    }

                    dataOwner.AssetGroups = assetGroups.Values;
                }
            }

            return dataOwner;
        }

        /// <summary>
        /// Get data owner based on filter criteria.
        /// </summary>
        /// <param name="filterCriteria">Filter criteria for data owners.</param>
        /// <param name="expandOptions">Expand options for data owner.</param>
        /// <returns>Data owners matching filter criteria.</returns>
        public async Task<FilterResult<DataOwner>> ReadByFiltersAsync(IFilterCriteria<DataOwner> filterCriteria, ExpandOptions expandOptions)
        {
            await this.AuthorizationProvider.AuthorizeAsync(this.AuthorizationRoles).ConfigureAwait(false);

            filterCriteria.Initialize(this.MaxPageSize);

            var includeTrackingDetails = expandOptions.HasFlag(ExpandOptions.TrackingDetails);
            var includeServiceTree = expandOptions.HasFlag(ExpandOptions.ServiceTree);

            var dataOwners = await this.StorageReader.GetDataOwnersAsync(filterCriteria, includeTrackingDetails, includeServiceTree).ConfigureAwait(false);

            if (expandOptions.HasFlag(ExpandOptions.DataAgents) || 
                expandOptions.HasFlag(ExpandOptions.AssetGroups))
            {
                throw new NotImplementedException(); // I'm not sure if we'll need this ability or not.
            }

            return dataOwners;
        }

        /// <summary>
        /// Finds all data owners that contain a write security group based on the authenticated user's write security groups.
        /// </summary>
        /// <param name="expandOptions">Expand options for the call.</param>
        /// <returns>The data owners or an empty collection.</returns>
        public async Task<IEnumerable<DataOwner>> FindByAuthenticatedUserAsync(ExpandOptions expandOptions)
        {
            await this.AuthorizationProvider.AuthorizeAsync(this.AuthorizationRoles).ConfigureAwait(false);

            var securityGroupIds = await this.activeDirectory.GetSecurityGroupIdsAsync(this.authenticatedPrincipal).ConfigureAwait(false);
            securityGroupIds = securityGroupIds?.Distinct() ?? Enumerable.Empty<Guid>();
            
            var includeTrackingDetails = expandOptions.HasFlag(ExpandOptions.TrackingDetails);
            var includeServiceTree = expandOptions.HasFlag(ExpandOptions.ServiceTree);

            var securityGroupOwners = await this.StorageReader.GetDataOwnersBySecurityGroupsAsync(securityGroupIds, includeTrackingDetails, includeServiceTree).ConfigureAwait(false);            

            var serviceTreeOwners = await this.StorageReader.GetDataOwnersByServiceAdminAsync(this.authenticatedPrincipal.UserAlias, includeTrackingDetails, includeServiceTree).ConfigureAwait(false);

            return securityGroupOwners.Concat(serviceTreeOwners).Distinct(Comparer);
        }

        /// <summary>
        /// Determines if there are any other entities linked to the data owner entity.
        /// </summary>
        /// <param name="id">The id of the data owner entity.</param>
        /// <returns>True if the data owner entity is linked to any other entities, False otherwise.</returns>
        public Task<bool> IsLinkedToAnyOtherEntities(Guid id)
        {
            return this.StorageReader.IsDataOwnerLinkedToAnyOtherEntities(id);
        }

        /// <summary>
        /// Get entities based on a set of ids.
        /// </summary>
        /// <param name="ids">The set if ids to retrieve.</param>
        /// <param name="expandOptions">The set of expand options.</param>
        /// <returns>Entities matching the set of ids. If some ids don't have matches, then those entities are omitted.</returns>
        protected override Task<IEnumerable<DataOwner>> ReadByIdsFromStorageAsync(IEnumerable<Guid> ids, ExpandOptions expandOptions)
        {
            // We always want the service tree data back for data owners, so that we can use the service admin list to authorize users.
            return this.StorageReader.GetDataOwnersAsync(
                ids, 
                expandOptions.HasFlag(ExpandOptions.TrackingDetails),
                true);
        }
    }
}