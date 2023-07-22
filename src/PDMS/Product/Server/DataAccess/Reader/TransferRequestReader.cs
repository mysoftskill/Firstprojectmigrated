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
    /// Provides methods for reading Transfer request information.
    /// </summary>
    public class TransferRequestReader : EntityReader<TransferRequest>, ITransferRequestReader
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransferRequestReader" /> class.
        /// </summary>
        /// <param name="storageReader">The storage reader instance.</param>
        /// <param name="coreConfiguration">The core configuration.</param>
        /// <param name="authorizationProvider">The authorization provider.</param>
        public TransferRequestReader(IPrivacyDataStorageReader storageReader, ICoreConfiguration coreConfiguration, IAuthorizationProvider authorizationProvider) : base(storageReader, coreConfiguration, authorizationProvider)
        {
            this.AuthorizationRoles = AuthorizationRole.ApplicationAccess;
        }

        /// <summary>
        /// Get transfer request for given id.
        /// </summary>
        /// <param name="id">Transfer request id.</param>
        /// <param name="expandOptions">Expand options for Transfer request.</param>
        /// <returns>Transfer request for given id.</returns>
        public async Task<TransferRequest> ReadByIdAsync(Guid id, ExpandOptions expandOptions)
        {
            await this.AuthorizationProvider.AuthorizeAsync(this.AuthorizationRoles).ConfigureAwait(false);

            var includeTrackingDetails = expandOptions.HasFlag(ExpandOptions.TrackingDetails);

            return await this.StorageReader.GetTransferRequestAsync(id, includeTrackingDetails).ConfigureAwait(false);
        }

        /// <summary>
        /// Get transfer request based on filter criteria.
        /// </summary>
        /// <param name="filterCriteria">Filter criteria for transfer requests.</param>
        /// <param name="expandOptions">Expand options for transfer request.</param>
        /// <returns>Transfer requests matching filter criteria.</returns>
        public async Task<FilterResult<TransferRequest>> ReadByFiltersAsync(IFilterCriteria<TransferRequest> filterCriteria, ExpandOptions expandOptions)
        {
            await this.AuthorizationProvider.AuthorizeAsync(this.AuthorizationRoles).ConfigureAwait(false);

            filterCriteria.Initialize(this.MaxPageSize);

            var includeTrackingDetails = expandOptions.HasFlag(ExpandOptions.TrackingDetails);

            var transferRequests = await this.StorageReader.GetTransferRequestsAsync(filterCriteria, includeTrackingDetails).ConfigureAwait(false);

            return transferRequests;
        }

        /// <summary>
        /// Determines if there are any other entities linked to this transfer request.
        /// </summary>
        /// <param name="id">The id of the transfer request.</param>
        /// <returns>True if the transfer request is linked to any other entities, False otherwise.</returns>
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
        protected override Task<IEnumerable<TransferRequest>> ReadByIdsFromStorageAsync(IEnumerable<Guid> ids, ExpandOptions expandOptions)
        {
            return this.StorageReader.GetTransferRequestsAsync(ids, expandOptions.HasFlag(ExpandOptions.TrackingDetails));
        }
    }
}