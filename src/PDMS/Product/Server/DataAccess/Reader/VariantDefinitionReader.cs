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
    /// Provides methods for reading variant definition information.
    /// </summary>
    public class VariantDefinitionReader : EntityReader<VariantDefinition>, IVariantDefinitionReader
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VariantDefinitionReader" /> class.
        /// </summary>
        /// <param name="storageReader">The storage reader instance.</param>
        /// <param name="coreConfiguration">The core configuration.</param>
        /// <param name="authorizationProvider">The authorization provider instance.</param>
        public VariantDefinitionReader(IPrivacyDataStorageReader storageReader, ICoreConfiguration coreConfiguration, IAuthorizationProvider authorizationProvider)
            : base(storageReader, coreConfiguration, authorizationProvider)
        {
            this.AuthorizationRoles = AuthorizationRole.ApplicationAccess;
        }

        /// <summary>
        /// Get variant definition for given id.
        /// </summary>
        /// <param name="id">Variant definition id.</param>
        /// <param name="expandOptions">Expand options for variant definition.</param>
        /// <returns>Variant definition for given id.</returns>
        public async Task<VariantDefinition> ReadByIdAsync(Guid id, ExpandOptions expandOptions)
        {
            await this.AuthorizationProvider.AuthorizeAsync(this.AuthorizationRoles).ConfigureAwait(false);

            var includeTrackingDetails = expandOptions.HasFlag(ExpandOptions.TrackingDetails);

            var variantDefinition = await this.StorageReader.GetVariantDefinitionAsync(id, includeTrackingDetails).ConfigureAwait(false);

            return variantDefinition;
        }

        /// <summary>
        /// Get variant definition based on filter criteria.
        /// </summary>
        /// <param name="filterCriteria">Filter criteria for variant definitions.</param>
        /// <param name="expandOptions">Expand options for variant definitions.</param>
        /// <returns>Variant definitions matching filter criteria.</returns>
        public async Task<FilterResult<VariantDefinition>> ReadByFiltersAsync(IFilterCriteria<VariantDefinition> filterCriteria, ExpandOptions expandOptions)
        {
            await this.AuthorizationProvider.AuthorizeAsync(this.AuthorizationRoles).ConfigureAwait(false);

            filterCriteria.Initialize(this.MaxPageSize);

            var includeTrackingDetails = expandOptions.HasFlag(ExpandOptions.TrackingDetails);

            // return only the Active definitions if no filter was specified
            if (filterCriteria?.GetType() != typeof(CompositeFilterCriteria<VariantDefinition>))
            {
                var variantDefinitionFilterCriteria = filterCriteria as VariantDefinitionFilterCriteria;
                if (variantDefinitionFilterCriteria?.State == null)
                {
                    variantDefinitionFilterCriteria.State = VariantDefinitionState.Active;
                    filterCriteria = variantDefinitionFilterCriteria;
                }
            }

            var variantDefinitions = await this.StorageReader.GetVariantDefinitionsAsync(filterCriteria, includeTrackingDetails).ConfigureAwait(false);

            return variantDefinitions;
        }

        /// <summary>
        /// Determines if there are any other entities linked to the variant definition entity.
        /// </summary>
        /// <param name="id">The id of the variant definition entity.</param>
        /// <returns>True if the variant definition entity is linked to any other entities, False otherwise.</returns>
        public Task<bool> IsLinkedToAnyOtherEntities(Guid id)
        {
            return this.StorageReader.IsVariantDefinitionLinkedToAnyOtherEntities(id);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<AssetGroup>> GetLinkedAssetGroups(Guid variantId)
        {
            return this.StorageReader.GetLinkedAssetGroups(variantId);
        }

        /// <summary>
        /// Get entities based on a set of ids.
        /// </summary>
        /// <param name="ids">The set of ids to retrieve.</param>
        /// <param name="expandOptions">The set of expand options.</param>
        /// <returns>Entities matching the set of ids. If some ids don't have matches, then those entities are omitted.</returns>
        protected override Task<IEnumerable<VariantDefinition>> ReadByIdsFromStorageAsync(IEnumerable<Guid> ids, ExpandOptions expandOptions)
        {
            return this.StorageReader.GetVariantDefinitionsAsync(ids, expandOptions.HasFlag(ExpandOptions.TrackingDetails));
        }
    }
}