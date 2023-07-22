namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.Identity;

    /// <summary>
    /// Exposes the available APIs for the service that target the AssetGroup controller.
    /// </summary>
    internal class AssetGroupClient : IAssetGroupClient
    {
        private readonly IHttpServiceProxy httpServiceProxy;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetGroupClient" /> class.
        /// </summary>
        /// <param name="httpServiceProxy">The <see cref="IHttpServiceProxy" /> to use for interacting with the service.</param>
        public AssetGroupClient(IHttpServiceProxy httpServiceProxy)
        {
            this.httpServiceProxy = httpServiceProxy;
        }

        /// <summary>
        /// Determines if there is a compliant asset group for the given asset qualifier.
        /// </summary>
        /// <exception cref="ServiceFault">
        /// Thrown for unknown service responses.
        /// </exception>
        /// <exception cref="BadArgumentError.InvalidArgument">
        /// Thrown when the qualifier is not fully specified.
        /// </exception>
        /// <exception cref="NotFoundError">
        /// Thrown when no asset group can be found for the given qualifier.
        /// </exception>
        /// <param name="qualifier">The asset qualifier upon which to search.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The compliant state.</returns>
        public async Task<IHttpResult<ComplianceState>> GetComplianceStateByAssetQualifierAsync(AssetQualifier qualifier, RequestContext requestContext)
        {
            string url = $"/api/v2/assetGroups/v2.findByAssetQualifier(qualifier=@value)/complianceState?@value='{SerializerSettings.EscapeForODataQuery(qualifier.Value)}'";

            var result =
                await this.httpServiceProxy.GetAsync<ComplianceState>(
                    url,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }

        /// <summary>
        /// Issues a create call for the given asset group
        /// and returns the newly created asset group.
        /// </summary>
        /// <param name="assetGroup">The asset group to create.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The service issued asset group.</returns>
        public async Task<IHttpResult<AssetGroup>> CreateAsync(AssetGroup assetGroup, RequestContext requestContext)
        {
            var result =
                await this.httpServiceProxy.PostAsync<AssetGroup, AssetGroup>(
                    "/api/v2/assetGroups",
                    assetGroup,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }

        /// <summary>
        /// Issues a read call for the given asset group id.
        /// </summary>
        /// <param name="id">The id of the asset group to retrieve.</param>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <returns>The corresponding asset group.</returns>
        public async Task<IHttpResult<AssetGroup>> ReadAsync(string id, RequestContext requestContext, AssetGroupExpandOptions expandOptions = AssetGroupExpandOptions.None)
        {
            string url = $"/api/v2/assetGroups('{id}'){GetAssetGroupExpandOptions(expandOptions)}";

            var result =
                await this.httpServiceProxy.GetAsync<AssetGroup>(
                    url,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }

        /// <summary>
        /// Issues a delete call with the given asset group id and ETag.
        /// </summary>
        /// <param name="id">The id of the asset group to delete.</param>
        /// <param name="etag">The ETag of the asset group.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The task to perform the operation.</returns>
        public async Task<IHttpResult> DeleteAsync(string id, string etag, RequestContext requestContext)
        {
            string url = $"/api/v2/assetGroups('{id}')";

            var headers = requestContext.GetHeaders();
            if (!string.IsNullOrWhiteSpace(etag))
            {
                headers.Add("If-Match", () => Task.FromResult(etag));
            }

            var result =
                await this.httpServiceProxy.DeleteAsync(
                    url,
                    headers,
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }

        /// <summary>
        /// Issues a read call that retrieves all known asset groups.
        /// If the number of existing asset groups is larger than the configured server-side max page size,
        /// then only the first page asset groups are returned.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <param name="filterCriteria">The asset group filter criteria.</param>
        /// <returns>A collection result with all the returned asset groups, total number of existing asset groups and possible next page link.</returns>
        public async Task<IHttpResult<Collection<AssetGroup>>> ReadByFiltersAsync(RequestContext requestContext, AssetGroupExpandOptions expandOptions = AssetGroupExpandOptions.None, AssetGroupFilterCriteria filterCriteria = null)
        {
            string url = $"/api/v2/assetGroups{GetAssetGroupExpandOptions(expandOptions)}{GetAssetGroupFilterCriteria(filterCriteria)}";

            var result =
                await this.httpServiceProxy.GetAsync<Collection<AssetGroup>>(
                    url,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }

        /// <summary>
        /// Issues a read call that retrieves all known asset groups. Automatically handles any service side paging.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <param name="filterCriteria">The asset group filter criteria.</param>
        /// <returns>All available asset groups.</returns>
        public Task<IHttpResult<IEnumerable<AssetGroup>>> ReadAllByFiltersAsync(RequestContext requestContext, AssetGroupExpandOptions expandOptions = AssetGroupExpandOptions.None, AssetGroupFilterCriteria filterCriteria = null)
        {
            return DataManagementClient.ReadMany<AssetGroup>(
                $"/api/v2/assetGroups{GetAssetGroupExpandOptions(expandOptions)}{GetAssetGroupFilterCriteria(filterCriteria)}",
                requestContext,
                this.httpServiceProxy);
        }

        /// <summary>
        /// Issues an update call for the given asset group
        /// and returns the updated asset group.
        /// </summary>
        /// <param name="assetGroup">The asset group to create.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The service issued asset group.</returns>
        public async Task<IHttpResult<AssetGroup>> UpdateAsync(AssetGroup assetGroup, RequestContext requestContext)
        {
            var result =
                await this.httpServiceProxy.PutAsync<AssetGroup, AssetGroup>(
                    $"/api/v2/assetGroups('{assetGroup.Id}')",
                    assetGroup,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }

        /// <summary>
        /// Set agent relationships in bulk. Creates requests as needed.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The response.</returns>
        public async Task<IHttpResult<SetAgentRelationshipResponse>> SetAgentRelationshipsAsync(SetAgentRelationshipParameters parameters, RequestContext requestContext)
        {
            var result =
                await this.httpServiceProxy.PostAsync<SetAgentRelationshipParameters, SetAgentRelationshipResponse>(
                    $"/api/v2/assetGroups/v2.setAgentRelationships",
                    parameters,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }
        
        /// <summary>
        /// Invokes the calculate agent registration status API for asset groups.
        /// </summary>
        /// <param name="id">The id of the asset group to use in the query.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The registration status.</returns>
        public async Task<IHttpResult<AssetGroupRegistrationStatus>> CalculateRegistrationStatus(string id, RequestContext requestContext)
        {
            string url = $"/api/v2/assetGroups('{id}')/v2.calculateRegistrationStatus";

            var result =
                await this.httpServiceProxy.GetAsync<AssetGroupRegistrationStatus>(
                    url,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }

        /// <summary>
        /// Issues a post call to remove variants with the given asset group id, list of
        /// variant ids and ETag.
        /// </summary>
        /// <param name="id">The id of the asset group to delete.</param>
        /// <param name="variantIds">The list of variant ids to remove from the asset group.</param>
        /// <param name="etag">The ETag of the asset group.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The task to perform the operation.</returns>
        public async Task<IHttpResult<AssetGroup>> RemoveVariantsAsync(string id, string[] variantIds, string etag, RequestContext requestContext)
        {
            string url = $"/api/v2/assetGroups('{id}')/v2.removeVariants";

            var headers = requestContext.GetHeaders();
            if (!string.IsNullOrWhiteSpace(etag))
            {
                headers.Add("If-Match", () => Task.FromResult(etag));
            }

            var parameters = new RemoveVariantsParameters
            {
                VariantIds = variantIds
            };

            var result =
                await this.httpServiceProxy.PostAsync<RemoveVariantsParameters, AssetGroup>(
                    url,
                    parameters,
                    headers,
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }

        /// <summary>
        /// Get expand options to be used in url from AssetGroupExpandOptions.
        /// </summary>
        /// <param name="expandOptions">Asset group expand options.</param>
        /// <returns>Asset group expand options in string format.</returns>
        private static string GetAssetGroupExpandOptions(AssetGroupExpandOptions expandOptions)
        {
            var queryString = "?$select=id,eTag,qualifier,variants,complianceState,deleteAgentId,exportAgentId,accountCloseAgentId,inventoryId,isRealTimeStore,isVariantsInheritanceBlocked,isDeleteAgentInheritanceBlocked,isExportAgentInheritanceBlocked,hasPendingVariantRequests,optionalFeatures,ownerId,deleteSharingRequestId,exportSharingRequestId,hasPendingTransferRequest,pendingTransferRequestTargetOwnerId,pendingTransferRequestTargetOwnerName";

            if (expandOptions == AssetGroupExpandOptions.None)
            {
                return queryString;
            }
            else
            {
                if (expandOptions.HasFlag(AssetGroupExpandOptions.TrackingDetails))
                {
                    queryString += ",trackingDetails";
                }

                return queryString;
            }
        }

        /// <summary>
        /// Get filter criteria to be used in url from AssetGroupFilterCriteria.
        /// </summary>
        /// <param name="filterCriteria">Asset group filter criteria.</param>
        /// <returns>Asset group filter criteria in string format.</returns>
        private static string GetAssetGroupFilterCriteria(AssetGroupFilterCriteria filterCriteria)
        {
            return filterCriteria == null ? string.Empty : "&" + filterCriteria.BuildRequestString();
        }
    }
}