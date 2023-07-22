namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Exposes the available APIs for the service that target the SharingRequest controller.
    /// </summary>
    internal class SharingRequestClient : ISharingRequestClient
    {
        private readonly IHttpServiceProxy httpServiceProxy;

        /// <summary>
        /// Initializes a new instance of the <see cref="SharingRequestClient" /> class.
        /// </summary>
        /// <param name="httpServiceProxy">The <see cref="IHttpServiceProxy" /> to use for interacting with the service.</param>
        public SharingRequestClient(IHttpServiceProxy httpServiceProxy)
        {
            this.httpServiceProxy = httpServiceProxy;
        }

        /// <summary>
        /// Issues a read call for the given sharing request id.
        /// </summary>
        /// <param name="id">The id of the sharing request to retrieve.</param>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <returns>The corresponding sharing request.</returns>
        public async Task<IHttpResult<SharingRequest>> ReadAsync(string id, RequestContext requestContext, SharingRequestExpandOptions expandOptions = SharingRequestExpandOptions.None)
        {
            string url = $"/api/v2/sharingRequests('{id}'){GetExpandOptions(expandOptions)}";

            var result =
                await this.httpServiceProxy.GetAsync<SharingRequest>(
                    url,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }

        /// <summary>
        /// Issues a read call that retrieves all known sharing requests.
        /// If the number of existing sharing requests is larger than the configured server-side max page size,
        /// then only the first page sharing requests are returned.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <param name="filterCriteria">The sharing request filter criteria.</param>
        /// <returns>A collection result with all the returned sharing requests, total number of existing sharing requests and possible next page link.</returns>
        public async Task<IHttpResult<Collection<SharingRequest>>> ReadByFiltersAsync(RequestContext requestContext, SharingRequestExpandOptions expandOptions = SharingRequestExpandOptions.None, SharingRequestFilterCriteria filterCriteria = null)
        {
            string url = $"/api/v2/sharingRequests{GetExpandOptions(expandOptions)}{GetFilterCriteria(filterCriteria)}";

            var result =
                await this.httpServiceProxy.GetAsync<Collection<SharingRequest>>(
                    url,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }

        /// <summary>
        /// Issues a read call that retrieves all known sharing requests. Automatically handles any service side paging.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <param name="filterCriteria">The sharing request filter criteria.</param>
        /// <returns>All available sharing requests.</returns>
        public Task<IHttpResult<IEnumerable<SharingRequest>>> ReadAllByFiltersAsync(RequestContext requestContext, SharingRequestExpandOptions expandOptions = SharingRequestExpandOptions.None, SharingRequestFilterCriteria filterCriteria = null)
        {
            return DataManagementClient.ReadMany<SharingRequest>(
                $"/api/v2/sharingRequests{GetExpandOptions(expandOptions)}{GetFilterCriteria(filterCriteria)}",
                requestContext,
                this.httpServiceProxy);
        }

        /// <summary>
        /// Issues a call to approve the specified sharing request.
        /// </summary>
        /// <param name="id">The id of the sharing request to approve.</param>
        /// <param name="etag">The ETag of the sharing request.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The task to perform the operation.</returns>
        public async Task<IHttpResult> ApproveAsync(string id, string etag, RequestContext requestContext)
        {
            string url = $"/api/v2/sharingRequests('{id}')/v2.approve";

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
        /// Issues a delete call with the given sharing request id.
        /// </summary>
        /// <param name="id">The id of the sharing request to delete.</param>
        /// <param name="etag">The ETag of the sharing request.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The task to perform the operation.</returns>
        public async Task<IHttpResult> DeleteAsync(string id, string etag, RequestContext requestContext)
        {
            string url = $"/api/v2/sharingRequests('{id}')";

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
        /// Get expand options to be used in url from SharingRequestExpandOptions.
        /// </summary>
        /// <param name="expandOptions">Sharing request expand options.</param>
        /// <returns>Sharing request expand options in string format.</returns>
        private static string GetExpandOptions(SharingRequestExpandOptions expandOptions)
        {
            var queryString = "?$select=id,eTag,ownerId,deleteAgentId,ownerName,relationships";

            if (expandOptions == SharingRequestExpandOptions.None)
            {
                return queryString;
            }
            else
            {
                if (expandOptions.HasFlag(SharingRequestExpandOptions.TrackingDetails))
                {
                    queryString += ",trackingDetails";
                }

                return queryString;
            }
        }

        /// <summary>
        /// Get filter criteria to be used in url from SharingRequestFilterCriteria.
        /// </summary>
        /// <param name="filterCriteria">Sharing request filter criteria.</param>
        /// <returns>Sharing request filter criteria in string format.</returns>
        private static string GetFilterCriteria(SharingRequestFilterCriteria filterCriteria)
        {
            return filterCriteria == null ? string.Empty : "&" + filterCriteria.BuildRequestString();
        }
    }
}