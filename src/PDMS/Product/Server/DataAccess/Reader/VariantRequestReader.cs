namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Reader
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authorization;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Readers;
    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;

    /// <summary>
    /// Provides methods for reading Variant request information.
    /// </summary>
    public class VariantRequestReader : EntityReader<VariantRequest>, IVariantRequestReader
    {
        private readonly IVariantDefinitionReader variantDefinitionReader;

        /// <summary>
        /// Initializes a new instance of the <see cref="VariantRequestReader" /> class.
        /// </summary>
        /// <param name="storageReader">The storage reader instance.</param>
        /// <param name="variantDefinitionReader">Variant definition reader</param>
        /// <param name="coreConfiguration">The core configuration.</param>
        /// <param name="authorizationProvider">The authorization provider.</param>
        public VariantRequestReader(
            IPrivacyDataStorageReader storageReader,
            IVariantDefinitionReader variantDefinitionReader,
            ICoreConfiguration coreConfiguration, 
            IAuthorizationProvider authorizationProvider) : base(storageReader, coreConfiguration, authorizationProvider)
        {
            this.variantDefinitionReader = variantDefinitionReader ?? throw new ArgumentException(nameof(variantDefinitionReader));

            this.AuthorizationRoles = AuthorizationRole.ApplicationAccess;
        }

        /// <summary>
        /// Get variant request for given id.
        /// </summary>
        /// <param name="id">Variant request id.</param>
        /// <param name="expandOptions">Expand options for Variant request.</param>
        /// <returns>Variant request for given id.</returns>
        public async Task<VariantRequest> ReadByIdAsync(Guid id, ExpandOptions expandOptions)
        {
            await this.AuthorizationProvider.AuthorizeAsync(this.AuthorizationRoles).ConfigureAwait(false);

            var includeTrackingDetails = expandOptions.HasFlag(ExpandOptions.TrackingDetails);

            var variantRequest = await this.StorageReader.GetVariantRequestAsync(id, includeTrackingDetails).ConfigureAwait(false);

            if (variantRequest != null)
            {
                var variantIds = variantRequest.RequestedVariants.Select(vr => vr.VariantId).ToList();

                var variantDetails = await this.variantDefinitionReader.ReadByIdsAsync(variantIds, ExpandOptions.None).ConfigureAwait(false);

                foreach (var variant in variantRequest.RequestedVariants)
                {
                    var variantDefinition = variantDetails.Where(v => v.Id.Equals(variant.VariantId)).FirstOrDefault();
                    if (variantDefinition != null)
                    {
                        variant.VariantName = variantDefinition.Name;
                        variant.EgrcId = variantDefinition.EgrcId;
                        variant.EgrcName = variantDefinition.EgrcName;
                    }
                }
            }

            return variantRequest;
        }

        /// <summary>
        /// Get variant request based on filter criteria.
        /// </summary>
        /// <param name="filterCriteria">Filter criteria for variant requests.</param>
        /// <param name="expandOptions">Expand options for variant request.</param>
        /// <returns>Variant requests matching filter criteria.</returns>
        public async Task<FilterResult<VariantRequest>> ReadByFiltersAsync(IFilterCriteria<VariantRequest> filterCriteria, ExpandOptions expandOptions)
        {
            await this.AuthorizationProvider.AuthorizeAsync(this.AuthorizationRoles).ConfigureAwait(false);

            filterCriteria.Initialize(this.MaxPageSize);

            var includeTrackingDetails = expandOptions.HasFlag(ExpandOptions.TrackingDetails);

            var variantRequests = await this.StorageReader.GetVariantRequestsAsync(filterCriteria, includeTrackingDetails).ConfigureAwait(false);

            return variantRequests;
        }

        /// <summary>
        /// Determines if there are any other entities linked to this variant request.
        /// </summary>
        /// <param name="id">The id of the variant request.</param>
        /// <returns>True if the variant request is linked to any other entities, False otherwise.</returns>
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
        protected override Task<IEnumerable<VariantRequest>> ReadByIdsFromStorageAsync(IEnumerable<Guid> ids, ExpandOptions expandOptions)
        {
            return this.StorageReader.GetVariantRequestsAsync(ids, expandOptions.HasFlag(ExpandOptions.TrackingDetails));
        }
    }
}