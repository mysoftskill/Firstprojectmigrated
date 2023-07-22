namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Reader
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authorization;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Readers;
    using Microsoft.PrivacyServices.DataManagement.Models.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;
    using Microsoft.PrivacyServices.Identity;
    using Microsoft.PrivacyServices.Identity.Metadata;

    /// <summary>
    /// Provides methods for reading asset group information.
    /// </summary>
    public class AssetGroupReader : EntityReader<AssetGroup>, IAssetGroupReader
    {
        private readonly IManifest identityManifest;
        private readonly ISharingRequestReader sharingRequestReader;
        private readonly IVariantRequestReader variantRequestReader;
        private readonly ITransferRequestReader transferRequestReader;
        private readonly IDataOwnerReader dataOwnerReader;
        private readonly IDataAssetReader assetReader;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetGroupReader" /> class.
        /// </summary>
        /// <param name="storageReader">The storage reader instance.</param>
        /// <param name="coreConfiguration">The core configuration.</param>
        /// <param name="authorizationProvider">The authorization provider instance.</param>
        /// <param name="identityManifest">The identity manifest instance.</param>
        /// <param name="sharingRequestReader">The sharing request reader instance.</param>
        /// <param name="variantRequestReader">The variant request reader instance.</param>
        /// <param name="transferRequestReader">The transfer request reader instance.</param>
        /// <param name="dataOwnerReader">The data owner reader instance.</param>
        /// <param name="assetReader">The asset reader instance.</param>
        public AssetGroupReader(
            IPrivacyDataStorageReader storageReader,
            ICoreConfiguration coreConfiguration,
            IAuthorizationProvider authorizationProvider,
            IManifest identityManifest,
            ISharingRequestReader sharingRequestReader,
            IVariantRequestReader variantRequestReader,
            ITransferRequestReader transferRequestReader,
            IDataOwnerReader dataOwnerReader,
            IDataAssetReader assetReader)
            : base(storageReader, coreConfiguration, authorizationProvider)
        {
            this.identityManifest = identityManifest;
            this.sharingRequestReader = sharingRequestReader;
            this.variantRequestReader = variantRequestReader;
            this.transferRequestReader = transferRequestReader;
            this.dataOwnerReader = dataOwnerReader;
            this.assetReader = assetReader;
            this.AuthorizationRoles = AuthorizationRole.ApplicationAccess;
        }

        /// <summary>
        /// Get asset group for given id.
        /// </summary>
        /// <param name="id">Asset group id.</param>
        /// <param name="expandOptions">Expand options for asset group.</param>
        /// <returns>Asset group for given id.</returns>
        public async Task<AssetGroup> ReadByIdAsync(Guid id, ExpandOptions expandOptions)
        {
            await this.AuthorizationProvider.AuthorizeAsync(this.AuthorizationRoles).ConfigureAwait(false);

            var includeTrackingDetails = expandOptions.HasFlag(ExpandOptions.TrackingDetails);

            var assetGroup = await this.StorageReader.GetAssetGroupAsync(id, includeTrackingDetails).ConfigureAwait(false);

            if (assetGroup != null)
            {
                if (expandOptions.HasFlag(ExpandOptions.DeleteAgent) ||
                    expandOptions.HasFlag(ExpandOptions.DataAssets))
                {
                    throw new NotImplementedException();
                }
            }

            if (assetGroup != null && assetGroup.HasPendingTransferRequest)
            {
                await this.GetPendingTransferRequestForAssetGroups(assetGroup.OwnerId, new[] { assetGroup }).ConfigureAwait(false);
            }

            return assetGroup;
        }

        /// <summary>
        /// Get asset group based on filter criteria.
        /// </summary>
        /// <param name="filterCriteria">Filter criteria for asset groups.</param>
        /// <param name="expandOptions">Expand options for asset group.</param>
        /// <returns>Asset groups matching filter criteria.</returns>
        public async Task<FilterResult<AssetGroup>> ReadByFiltersAsync(IFilterCriteria<AssetGroup> filterCriteria, ExpandOptions expandOptions)
        {
            await this.AuthorizationProvider.AuthorizeAsync(this.AuthorizationRoles).ConfigureAwait(false);

            filterCriteria.Initialize(this.MaxPageSize);

            var includeTrackingDetails = expandOptions.HasFlag(ExpandOptions.TrackingDetails);

            var assetGroups = await this.StorageReader.GetAssetGroupsAsync(filterCriteria, includeTrackingDetails).ConfigureAwait(false);

            if (expandOptions.HasFlag(ExpandOptions.DeleteAgent) ||
                expandOptions.HasFlag(ExpandOptions.DataAssets))
            {
                throw new NotImplementedException();
            }

            var assetGroupFilterCriteria = filterCriteria as AssetGroupFilterCriteria;

            if (assetGroupFilterCriteria != null && assetGroupFilterCriteria.OwnerId != null)
            {
                var assetGroupsWithPendingTransferRequest = assetGroups.Values?.Where(a => a.HasPendingTransferRequest == true);
                await this.GetPendingTransferRequestForAssetGroups(assetGroupFilterCriteria.OwnerId.Value, assetGroupsWithPendingTransferRequest).ConfigureAwait(false);
            }

            return assetGroups;
        }

        /// <summary>
        /// Get the most specific asset group based on the provided asset qualifier.
        /// </summary>
        /// <param name="qualifier">The asset qualifier to check.</param>
        /// <returns>The most specific asset group.</returns>
        public async Task<AssetGroup> FindByAssetQualifierAsync(AssetQualifier qualifier)
        {
            await this.AuthorizationProvider.AuthorizeAsync(this.AuthorizationRoles).ConfigureAwait(false);

            var typeDefinition = this.identityManifest.AssetTypes.Single(x => x.Id == qualifier.AssetType);

            this.QualifierNeedsToBeFullySpecified(qualifier, typeDefinition);

            var requiredProperties = this.GetRequiredProperties(typeDefinition);

            Func<string, StringComparisonType> getComparision = (propName) =>
            {
                var propDefinition = typeDefinition.Properties.Single(x => x.Id == propName);

                return propDefinition.CaseSensitive ? StringComparisonType.EqualsCaseSensitive : StringComparisonType.Equals;
            };

            var filterCriteria = new AssetGroupFilterCriteria
            {
                Qualifier = requiredProperties.ToDictionary(
                    v => v,
                    v => new StringFilter(qualifier.Properties[v], getComparision(v)))
            };

            // Asset type is not a property in the manifest, so we add it manually.
            filterCriteria.Qualifier.Add("AssetType", new StringFilter(qualifier.AssetType.ToString(), StringComparisonType.EqualsCaseSensitive));

            // Find all possible matches
            var qualifiedAssetGroups = await this.FindAllByFiltersAsync(filterCriteria, ExpandOptions.None).ConfigureAwait(false);
 
            var mostSpecificAssetGruop = this.GetMostSpecificAssetGroup(qualifiedAssetGroups, qualifier);

            return mostSpecificAssetGruop;
        }

        /// <summary>
        /// Determines if there are any other entities linked to this asset group.
        /// </summary>
        /// <param name="id">The id of the asset group.</param>
        /// <returns>True if the asset group is linked to any other entities, False otherwise.</returns>
        public async Task<bool> IsLinkedToAnyOtherEntities(Guid id)
        {
            var sharingRequestFilterCriteria = new SharingRequestFilterCriteria { AssetGroupId = id, Index = 0, Count = 0 };
            var variantRequestFilterCriteria = new VariantRequestFilterCriteria { AssetGroupId = id, Index = 0, Count = 0 };
            var transferRequestFilterCriteria = new TransferRequestFilterCriteria { AssetGroupId = id, Index = 0, Count = 0 };

            var sharingRequestResult = await this.sharingRequestReader.ReadByFiltersAsync(sharingRequestFilterCriteria, ExpandOptions.None).ConfigureAwait(false);
            var variantRequestResult = await this.variantRequestReader.ReadByFiltersAsync(variantRequestFilterCriteria, ExpandOptions.None).ConfigureAwait(false);
            var transferRequestResult = await this.transferRequestReader.ReadByFiltersAsync(transferRequestFilterCriteria, ExpandOptions.None).ConfigureAwait(false);

            return sharingRequestResult.Total > 0 || variantRequestResult.Total > 0 || transferRequestResult.Total > 0;
        }

        /// <summary>
        /// Calculate the asset group registration status.
        /// </summary>
        /// <param name="id">The id of the asset group.</param>
        /// <returns>The registration status.</returns>
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public async Task<AssetGroupRegistrationStatus> CalculateRegistrationStatus(Guid id)
        {
            var assetGroup = await this.ReadByIdAsync(id, ExpandOptions.None).ConfigureAwait(false);

            if (assetGroup == null)
            {
                throw new EntityNotFoundException(id, "AssetGroup");
            }

            return await this.CalculateRegistrationStatus(assetGroup).ConfigureAwait(false);
        }

        /// <summary>
        /// Calculate the asset group registration status.
        /// </summary>
        /// <param name="assetGroup">The the asset group.</param>
        /// <returns>The registration status.</returns>
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public async Task<AssetGroupRegistrationStatus> CalculateRegistrationStatus(AssetGroup assetGroup)
        {
            // Calculate asset status;
            var assetFilter = new DataAssetFilterCriteria();
            assetFilter.Count = int.MaxValue; // A number large enough that we use the max page size in the data asset reader config.

            var assets = await this.assetReader.FindByQualifierAsync(assetFilter, assetGroup.Qualifier, true).ConfigureAwait(false);

            var assetsStatus = new List<AssetRegistrationStatus>();

            if (assets.Total > 0)
            {
                foreach (var asset in assets.Values)
                {
                    var assetStatus = new AssetRegistrationStatus();
                    assetsStatus.Add(assetStatus);

                    assetStatus.Id = asset.Id;
                    assetStatus.Qualifier = asset.Qualifier;
                    assetStatus.IsNonPersonal = asset.Tags.IsNonPersonal;
                    assetStatus.IsLongTailOrCustomNonUse = asset.Tags.IsLongTailOrCustomNonUse;
                    assetStatus.SubjectTypeTags = asset.Tags.SubjectTypes;
                    assetStatus.DataTypeTags = asset.Tags.DataTypes;

                    if (assetStatus.IsNonPersonal)
                    {
                        assetStatus.SubjectTypeTagsStatus = assetStatus.SubjectTypeTags.Any() ? RegistrationState.Invalid : RegistrationState.NotApplicable;
                        assetStatus.DataTypeTagsStatus = assetStatus.DataTypeTags.Any() ? RegistrationState.Invalid : RegistrationState.NotApplicable;
                    }
                    else if (assetStatus.IsLongTailOrCustomNonUse)
                    {
                        assetStatus.SubjectTypeTagsStatus = RegistrationState.NotApplicable;
                        assetStatus.DataTypeTagsStatus = assetStatus.DataTypeTags.Any() ? RegistrationState.NotApplicable : RegistrationState.Missing;
                    }
                    else
                    {
                        assetStatus.SubjectTypeTagsStatus = assetStatus.SubjectTypeTags.Any() ? RegistrationState.Valid : RegistrationState.Missing;
                        assetStatus.DataTypeTagsStatus = assetStatus.DataTypeTags.Any() ? RegistrationState.Valid : RegistrationState.Missing;
                    }

                    assetStatus.IsComplete =
                        assetStatus.SubjectTypeTagsStatus == assetStatus.DataTypeTagsStatus &&
                        (assetStatus.SubjectTypeTagsStatus == RegistrationState.Valid || assetStatus.SubjectTypeTagsStatus == RegistrationState.NotApplicable);
                }
            }

            // Calculate the group status.
            var assetGroupStatus = new AssetGroupRegistrationStatus();

            assetGroupStatus.Id = assetGroup.Id;
            assetGroupStatus.OwnerId = assetGroup.OwnerId;
            assetGroupStatus.Qualifier = assetGroup.Qualifier;
            assetGroupStatus.Assets = assetsStatus;

            if (assetGroupStatus.Assets.Any())
            {
                if (assetGroupStatus.Assets.All(x => !x.IsComplete))
                {
                    assetGroupStatus.AssetsStatus = RegistrationState.Invalid;
                }
                else if (assetGroupStatus.Assets.All(x => x.IsComplete && x.SubjectTypeTagsStatus == RegistrationState.NotApplicable))
                {
                    assetGroupStatus.AssetsStatus = RegistrationState.NotApplicable;
                }
                else if (assetGroupStatus.Assets.All(x => x.IsComplete))
                {
                    assetGroupStatus.AssetsStatus = assets.Count < assets.Total ? RegistrationState.ValidButTruncated : RegistrationState.Valid;
                }
                else
                {
                    assetGroupStatus.AssetsStatus = RegistrationState.Partial;
                }
            }
            else
            {
                assetGroupStatus.AssetsStatus = RegistrationState.Missing;
            }

            assetGroupStatus.IsComplete =
                assetGroupStatus.AssetsStatus == RegistrationState.Valid ||
                assetGroupStatus.AssetsStatus == RegistrationState.NotApplicable;

            return assetGroupStatus;
        }

        /// <summary>
        /// Get entities based on a set of ids.
        /// </summary>
        /// <param name="ids">The set if ids to retrieve.</param>
        /// <param name="expandOptions">The set of expand options.</param>
        /// <returns>Entities matching the set of ids. If some ids don't have matches, then those entities are omitted.</returns>
        protected override Task<IEnumerable<AssetGroup>> ReadByIdsFromStorageAsync(IEnumerable<Guid> ids, ExpandOptions expandOptions)
        {
            return this.StorageReader.GetAssetGroupsAsync(ids, expandOptions.HasFlag(ExpandOptions.TrackingDetails));
        }

        /// <summary>
        /// Checks if the provided qualifier is fully specific for its corresponding asset type.
        /// </summary>
        /// <param name="qualifier">The qualifier to check.</param>
        /// <param name="typeDefinition">The type definition of the qualifier's asset type.</param>
        private void QualifierNeedsToBeFullySpecified(AssetQualifier qualifier, AssetTypeDefinition typeDefinition)
        {
            foreach (var propDefinition in typeDefinition.Properties)
            {
                if (!qualifier.Properties.ContainsKey(propDefinition.Id) || string.IsNullOrEmpty(qualifier.Properties[propDefinition.Id]))
                {
                    throw new MissingPropertyException($"qualifier[{propDefinition.Id}]", $"The provided qualifier is not fully specific, missing {propDefinition.Id}.");
                }
            }
        }

        /// <summary>
        /// Helper function to get the list of required properties of the asset type.
        /// </summary>
        /// <param name="typeDefinition">The asset type definition.</param>
        /// <returns>The list of required properties.</returns>
        private IEnumerable<string> GetRequiredProperties(AssetTypeDefinition typeDefinition)
        {
            var requiredProperties = new List<string>();

            foreach (var propDefinition in typeDefinition.Properties)
            {
                if (propDefinition.Level <= typeDefinition.MinimumRequiredLevel)
                {
                    requiredProperties.Add(propDefinition.Id);
                }
            }

            return requiredProperties;
        }

        /// <summary>
        /// Find the most specific asset group from all the qualified ones.
        /// </summary>
        /// <param name="qualifiedAssetGroups">All the qualified asset groups.</param>
        /// <param name="qualifier">The provided asset qualifier.</param>
        /// <returns>The most specific asset group.</returns>
        private AssetGroup GetMostSpecificAssetGroup(IEnumerable<AssetGroup> qualifiedAssetGroups, AssetQualifier qualifier)
        {
            var parentAssetGroups = qualifiedAssetGroups?.Where(ag => ag.Qualifier.Contains(qualifier));

            if (parentAssetGroups == null || !parentAssetGroups.Any())
            {
                return null;
            }
            else
            {
                int maxNumOfProperties = parentAssetGroups.Max(ag => ag.Qualifier.Properties.Count);
                return parentAssetGroups.First(ag => ag.Qualifier.Properties.Count == maxNumOfProperties);
            }
        }

        private async Task GetPendingTransferRequestForAssetGroups(Guid ownerId, IEnumerable<AssetGroup> assetGroups)
        {
            if (assetGroups != null && assetGroups.Any())
            {
                var transferRequestFilterCriteria = new TransferRequestFilterCriteria { SourceOwnerId = ownerId, AssetGroupId = assetGroups.Count() == 1 ? assetGroups.First().Id : (Guid?)null };
                var transferReqestResult = await this.transferRequestReader.ReadByFiltersAsync(transferRequestFilterCriteria, ExpandOptions.None).ConfigureAwait(false);

                var pendingTransferRequests = transferReqestResult.Values.Where(t => t.RequestState == TransferRequestStates.Pending);
                var pendingTransferRequestsTargetOwners = await this.GetPendingTranferRequestsTargetOwners(pendingTransferRequests).ConfigureAwait(false);

                if (pendingTransferRequestsTargetOwners != null && pendingTransferRequestsTargetOwners.Any())
                {
                    foreach (var assetGroup in assetGroups)
                    {
                        var tranferRequests = pendingTransferRequests.Where(t => t.AssetGroups.Contains(assetGroup.Id));
                        this.SetAssetGroupPendingTransferRequestValues(assetGroup, tranferRequests, pendingTransferRequestsTargetOwners);
                    }
                }
            }
        }

        private void SetAssetGroupPendingTransferRequestValues(AssetGroup assetGroup, IEnumerable<TransferRequest> transferRequestsLinked, IEnumerable<DataOwner> pendingTranferRequestsTargetOwners)
        {
            if (transferRequestsLinked.Count() > 1)
            {
                throw new ConflictException(ConflictType.InvalidValue, "There should be only one pending transfer request linked to an AssetGroup", "transferRequests");
            }
            else if (transferRequestsLinked.Count() == 1)
            {
                var targetOwner = pendingTranferRequestsTargetOwners.SingleOrDefault(t => t.Id == transferRequestsLinked.First().TargetOwnerId);

                if (targetOwner == null)
                {
                    throw new ConflictException(ConflictType.InvalidValue, "Could not find the target owner of the linked pending transfer request.", "targetOwnerId");
                }

                assetGroup.PendingTransferRequestTargetOwnerId = targetOwner.Id;
                assetGroup.PendingTransferRequestTargetOwnerName = targetOwner.Name;
            }
            else
            {
                throw new ConflictException(ConflictType.InvalidValue, "AssetGroup has PendingTransferRequest flag on, but no actual transfer request can be found linked to it", "hasPendingTransferRequest");
            }
        }

        private async Task<IEnumerable<DataOwner>> GetPendingTranferRequestsTargetOwners(IEnumerable<TransferRequest> pendingTransferRequests)
        {
            if (pendingTransferRequests != null && pendingTransferRequests.Any())
            {
                return await this.dataOwnerReader.ReadByIdsAsync(pendingTransferRequests.Select(t => t.TargetOwnerId).Distinct(), ExpandOptions.None).ConfigureAwait(false);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Get all asset groups based on filter criteria.
        /// </summary>
        /// <param name="filterCriteria">Filter criteria for asset groups.</param>
        /// <param name="expandOptions">Expand options for asset group.</param>
        /// <returns>Asset groups matching filter criteria.</returns>
        private async Task<IEnumerable<AssetGroup>> FindAllByFiltersAsync(IFilterCriteria<AssetGroup> filterCriteria, ExpandOptions expandOptions)
        {
            // Find all possible matches
            var filterResults = await this.ReadByFiltersAsync(filterCriteria, ExpandOptions.None).ConfigureAwait(false);
            var assetGroups = filterResults.Values;

            //// Bug 955109: [PDMS] AssetGroupReader.FindByAssetQualifierAsync doesn't find the asset group if the matching asset group is not on the first page of results
            //// This fix works, but it is causing unrelated unit tests to fail with an infinite exception in the build env.

            //int matchCount = assetGroups?.Count<AssetGroup>() ?? 0;

            //// Reading by filters only returns the results from one page at a time.
            //// If the current number of match is less than Total, then we didn't get all the matches;
            //// Update the index and continue reading until we get all matches.
            //while (matchCount < filterResults.Total)
            //{
            //    filterCriteria.Index += filterCriteria.Count;
            //    filterResults = await this.ReadByFiltersAsync(filterCriteria, ExpandOptions.None).ConfigureAwait(false);
            //    if (filterResults.Values != null)
            //    {
            //        assetGroups = assetGroups?.Concat<AssetGroup>(filterResults.Values) ?? filterResults.Values;
            //        matchCount = assetGroups.Count<AssetGroup>();
            //    }
            //}

            return assetGroups;
        }
    }
}
