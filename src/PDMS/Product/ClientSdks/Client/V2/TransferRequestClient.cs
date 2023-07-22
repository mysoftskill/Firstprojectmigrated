namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Exposes the available APIs for the service that target the TransferRequest controller.
    /// </summary>
    internal class TransferRequestClient : ITransferRequestClient
    {
        private readonly IHttpServiceProxy httpServiceProxy;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransferRequestClient" /> class.
        /// </summary>
        /// <param name="httpServiceProxy">The <see cref="IHttpServiceProxy" /> to use for interacting with the service.</param>
        public TransferRequestClient(IHttpServiceProxy httpServiceProxy)
        {
            this.httpServiceProxy = httpServiceProxy;
        }

        /// <summary>
        /// Issues a create call for the given transfer request and returns the newly created transfer request.
        /// </summary>
        /// <param name="transferRequest">The transfer request to create.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The service issued transfer request.</returns>
        public async Task<IHttpResult<TransferRequest>> CreateAsync(TransferRequest transferRequest, RequestContext requestContext)
        {
            var result =
                await this.httpServiceProxy.PostAsync<TransferRequest, TransferRequest>(
                    "/api/v2/transferRequests",
                    transferRequest,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }

        /// <summary>
        /// Issues a read call for the given transfer request id.
        /// </summary>
        /// <param name="id">The id of the transfer request to retrieve.</param>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <returns>The corresponding transfer request.</returns>
        public async Task<IHttpResult<TransferRequest>> ReadAsync(string id, RequestContext requestContext, TransferRequestExpandOptions expandOptions = TransferRequestExpandOptions.None)
        {
            string url = $"/api/v2/transferRequests('{id}'){GetExpandOptions(expandOptions)}";

            var result =
                await this.httpServiceProxy.GetAsync<TransferRequest>(
                    url,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }

        /// <summary>
        /// Issues a read call that retrieves all known transfer requests.
        /// If the number of existing transfer requests is larger than the configured server-side max page size,
        /// then only the first page transfer requests are returned.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <param name="filterCriteria">The transfer request filter criteria.</param>
        /// <returns>A collection result with all the returned transfer requests, total number of existing transfer requests and possible next page link.</returns>
        public async Task<IHttpResult<Collection<TransferRequest>>> ReadByFiltersAsync(RequestContext requestContext, TransferRequestExpandOptions expandOptions = TransferRequestExpandOptions.None, TransferRequestFilterCriteria filterCriteria = null)
        {
            string url = $"/api/v2/transferRequests{GetExpandOptions(expandOptions)}{GetFilterCriteria(filterCriteria)}";

            var result =
                await this.httpServiceProxy.GetAsync<Collection<TransferRequest>>(
                    url,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }

        /// <summary>
        /// Issues a read call that retrieves all known transfer requests. Automatically handles any service side paging.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <param name="filterCriteria">The transfer request filter criteria.</param>
        /// <returns>All available transfer requests.</returns>
        public Task<IHttpResult<IEnumerable<TransferRequest>>> ReadAllByFiltersAsync(RequestContext requestContext, TransferRequestExpandOptions expandOptions = TransferRequestExpandOptions.None, TransferRequestFilterCriteria filterCriteria = null)
        {
            return DataManagementClient.ReadMany<TransferRequest>(
                $"/api/v2/transferRequests{GetExpandOptions(expandOptions)}{GetFilterCriteria(filterCriteria)}",
                requestContext,
                this.httpServiceProxy);
        }

        /// <summary>
        /// Issues a call to approve the specified transfer request.
        /// </summary>
        /// <param name="id">The id of the transfer request to approve.</param>
        /// <param name="etag">The ETag of the transfer request.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The task to perform the operation.</returns>
        public async Task<IHttpResult> ApproveAsync(string id, string etag, RequestContext requestContext)
        {
            string url = $"/api/v2/transferRequests('{id}')/v2.approve";

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
        /// Issues a delete call with the given transfer request id.
        /// </summary>
        /// <param name="id">The id of the transfer request to delete.</param>
        /// <param name="etag">The ETag of the transfer request.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The task to perform the operation.</returns>
        public async Task<IHttpResult> DeleteAsync(string id, string etag, RequestContext requestContext)
        {
            string url = $"/api/v2/transferRequests('{id}')";

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
        /// Get expand options to be used in url from TransferRequestExpandOptions.
        /// </summary>
        /// <param name="expandOptions">Transfer request expand options.</param>
        /// <returns>Transfer request expand options in string format.</returns>
        private static string GetExpandOptions(TransferRequestExpandOptions expandOptions)
        {
            var queryString = "?$select=id,eTag,sourceOwnerId,targetOwnerId,requestState,assetGroups";

            if (expandOptions == TransferRequestExpandOptions.None)
            {
                return queryString;
            }
            else
            {
                if (expandOptions.HasFlag(TransferRequestExpandOptions.TrackingDetails))
                {
                    queryString += ",trackingDetails";
                }

                return queryString;
            }
        }

        /// <summary>
        /// Get filter criteria to be used in url from TransferRequestFilterCriteria.
        /// </summary>
        /// <param name="filterCriteria">Transfer request filter criteria.</param>
        /// <returns>Transfer request filter criteria in string format.</returns>
        private static string GetFilterCriteria(TransferRequestFilterCriteria filterCriteria)
        {
            return filterCriteria == null ? string.Empty : "&" + filterCriteria.BuildRequestString();
        }
    }
}