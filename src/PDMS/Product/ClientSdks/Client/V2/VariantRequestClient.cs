namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Exposes the available APIs for the service that target the VariantRequest controller.
    /// </summary>
    internal class VariantRequestClient : IVariantRequestClient
    {
        private readonly IHttpServiceProxy httpServiceProxy;

        /// <summary>
        /// Initializes a new instance of the <see cref="VariantRequestClient" /> class.
        /// </summary>
        /// <param name="httpServiceProxy">The <see cref="IHttpServiceProxy" /> to use for interacting with the service.</param>
        public VariantRequestClient(IHttpServiceProxy httpServiceProxy)
        {
            this.httpServiceProxy = httpServiceProxy;
        }

        /// <summary>
        /// Issues a create call for the given variant request
        /// and returns the newly created variant request.
        /// </summary>
        /// <param name="variantRequest">The variant request to create.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The service issued variant request.</returns>
        public async Task<IHttpResult<VariantRequest>> CreateAsync(VariantRequest variantRequest, RequestContext requestContext)
        {
            var result =
                await this.httpServiceProxy.PostAsync<VariantRequest, VariantRequest>(
                    "/api/v2/variantRequests",
                    variantRequest,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }

        /// <summary>
        /// Issues a read call for the given variant request id.
        /// </summary>
        /// <param name="id">The id of the variant request to retrieve.</param>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <returns>The corresponding variant request.</returns>
        public async Task<IHttpResult<VariantRequest>> ReadAsync(string id, RequestContext requestContext, VariantRequestExpandOptions expandOptions = VariantRequestExpandOptions.None)
        {
            string url = $"/api/v2/variantRequests('{id}'){GetExpandOptions(expandOptions)}";

            var result =
                await this.httpServiceProxy.GetAsync<VariantRequest>(
                    url,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }

        /// <summary>
        /// Issues a read call that retrieves all known variant requests.
        /// If the number of existing variant requests is larger than the configured server-side max page size,
        /// then only the first page variant requests are returned.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <param name="filterCriteria">The variant request filter criteria.</param>
        /// <returns>A collection result with all the returned variant requests, total number of existing variant requests and possible next page link.</returns>
        public async Task<IHttpResult<Collection<VariantRequest>>> ReadByFiltersAsync(RequestContext requestContext, VariantRequestExpandOptions expandOptions = VariantRequestExpandOptions.None, VariantRequestFilterCriteria filterCriteria = null)
        {
            string url = $"/api/v2/variantRequests{GetExpandOptions(expandOptions)}{GetFilterCriteria(filterCriteria)}";

            var result =
                await this.httpServiceProxy.GetAsync<Collection<VariantRequest>>(
                    url,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }

        /// <summary>
        /// Issues a read call that retrieves all known variant requests. Automatically handles any service side paging.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <param name="filterCriteria">The variant request filter criteria.</param>
        /// <returns>All available variant requests.</returns>
        public Task<IHttpResult<IEnumerable<VariantRequest>>> ReadAllByFiltersAsync(RequestContext requestContext, VariantRequestExpandOptions expandOptions = VariantRequestExpandOptions.None, VariantRequestFilterCriteria filterCriteria = null)
        {
            return DataManagementClient.ReadMany<VariantRequest>(
                $"/api/v2/variantRequests{GetExpandOptions(expandOptions)}{GetFilterCriteria(filterCriteria)}",
                requestContext,
                this.httpServiceProxy);
        }

        /// <summary>
        /// Issues an update call for the given variant request
        /// and returns the updated variant request.
        /// </summary>
        /// <param name="variantRequest">The variant request to create.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The service issued variant request.</returns>
        public async Task<IHttpResult<VariantRequest>> UpdateAsync(VariantRequest variantRequest, RequestContext requestContext)
        {
            var result =
                await this.httpServiceProxy.PutAsync<VariantRequest, VariantRequest>(
                    $"/api/v2/variantRequests('{variantRequest.Id}')",
                    variantRequest,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }

        /// <summary>
        /// Issues a call to approve the specified variant request.
        /// </summary>
        /// <param name="id">The id of the variant request to approve.</param>
        /// <param name="etag">The ETag of the variant request.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The task to perform the operation.</returns>
        public async Task<IHttpResult> ApproveAsync(string id, string etag, RequestContext requestContext)
        {
            string url = $"/api/v2/variantRequests('{id}')/v2.approve";

            var headers = requestContext.GetHeaders();
            if (!string.IsNullOrWhiteSpace(etag))
            {
                headers.Add("If-Match", () => Task.FromResult(etag));
            }

            var result =
                await this.httpServiceProxy.PostAsync<object, object>(
                    url,
                    null,
                    headers,
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }

        /// <summary>
        /// Issues a delete call with the given variant request id.
        /// </summary>
        /// <param name="id">The id of the variant request to delete.</param>
        /// <param name="etag">The ETag of the variant request.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The task to perform the operation.</returns>
        public async Task<IHttpResult> DeleteAsync(string id, string etag, RequestContext requestContext)
        {
            string url = $"/api/v2/variantRequests('{id}')";

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
        /// Get expand options to be used in url from VariantRequestExpandOptions.
        /// </summary>
        /// <param name="expandOptions">Variant request expand options.</param>
        /// <returns>Variant request expand options in string format.</returns>
        private static string GetExpandOptions(VariantRequestExpandOptions expandOptions)
        {
            var queryString = "?$select=id,eTag,ownerId,ownerName,requesterAlias,generalContractorAlias,celaContactAlias,workItemUri,requestedVariants,variantRelationships,additionalInformation";

            if (expandOptions == VariantRequestExpandOptions.None)
            {
                return queryString;
            }
            else
            {
                if (expandOptions.HasFlag(VariantRequestExpandOptions.TrackingDetails))
                {
                    queryString += ",trackingDetails";
                }

                return queryString;
            }
        }

        /// <summary>
        /// Get filter criteria to be used in url from VariantRequestFilterCriteria.
        /// </summary>
        /// <param name="filterCriteria">Variant request filter criteria.</param>
        /// <returns>Variant request filter criteria in string format.</returns>
        private static string GetFilterCriteria(VariantRequestFilterCriteria filterCriteria)
        {
            return filterCriteria == null ? string.Empty : "&" + filterCriteria.BuildRequestString();
        }
    }
}