namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Reader
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authorization;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Readers;
    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;

    /// <summary>
    /// Provides methods for reading sharing request information.
    /// </summary>
    public class SharingRequestReader : EntityReader<SharingRequest>, ISharingRequestReader
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SharingRequestReader" /> class.
        /// </summary>
        /// <param name="storageReader">The storage reader instance.</param>
        /// <param name="coreConfiguration">The core configuration.</param>
        /// <param name="authorizationProvider">The authorization provider.</param>
        public SharingRequestReader(IPrivacyDataStorageReader storageReader, ICoreConfiguration coreConfiguration, IAuthorizationProvider authorizationProvider) : base(storageReader, coreConfiguration, authorizationProvider)
        {
            this.AuthorizationRoles = AuthorizationRole.ApplicationAccess;
        }

        /// <summary>
        /// Get sharing request for given id.
        /// </summary>
        /// <param name="id">Sharing request id.</param>
        /// <param name="expandOptions">Expand options for sharing request.</param>
        /// <returns>Sharing request for given id.</returns>
        public async Task<SharingRequest> ReadByIdAsync(Guid id, ExpandOptions expandOptions)
        {
            await this.AuthorizationProvider.AuthorizeAsync(this.AuthorizationRoles).ConfigureAwait(false);

            var includeTrackingDetails = expandOptions.HasFlag(ExpandOptions.TrackingDetails);

            return await this.StorageReader.GetSharingRequestAsync(id, includeTrackingDetails).ConfigureAwait(false);
        }

        /// <summary>
        /// Get sharing request based on filter criteria.
        /// </summary>
        /// <param name="filterCriteria">Filter criteria for sharing requests.</param>
        /// <param name="expandOptions">Expand options for sharing request.</param>
        /// <returns>Sharing requests matching filter criteria.</returns>
        public async Task<FilterResult<SharingRequest>> ReadByFiltersAsync(IFilterCriteria<SharingRequest> filterCriteria, ExpandOptions expandOptions)
        {
            await this.AuthorizationProvider.AuthorizeAsync(this.AuthorizationRoles).ConfigureAwait(false);

            filterCriteria.Initialize(this.MaxPageSize);

            var includeTrackingDetails = expandOptions.HasFlag(ExpandOptions.TrackingDetails);

            var sharingRequests = await this.StorageReader.GetSharingRequestsAsync(filterCriteria, includeTrackingDetails).ConfigureAwait(false);

            return sharingRequests;
        }

        /// <summary>
        /// Determines if there are any other entities linked to this sharing request.
        /// </summary>
        /// <param name="id">The id of the sharing request.</param>
        /// <returns>True if the sharing request is linked to any other entities, False otherwise.</returns>
        public Task<bool> IsLinkedToAnyOtherEntities(Guid id)
        {
            return Task.FromResult(false);
        }

        /// <summary>
        /// Get entities based on a set of ids.
        /// </summary>
        /// <param name="ids">The set if ids to retrieve.</param>
        /// <param name="expandOptions">The set of expand options.</param>
        /// <returns>Entities matching the set of ids. If some ids don't have matches, then those entities are omitted.</returns>
        protected override Task<IEnumerable<SharingRequest>> ReadByIdsFromStorageAsync(IEnumerable<Guid> ids, ExpandOptions expandOptions)
        {
            return this.StorageReader.GetSharingRequestsAsync(ids, expandOptions.HasFlag(ExpandOptions.TrackingDetails));
        }
    }
}