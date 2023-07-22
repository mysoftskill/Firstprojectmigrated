namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Exposes the available APIs for the service that target the SharingRequest controller.
    /// </summary>
    public interface ISharingRequestClient
    {
        /// <summary>
        /// Issues a read call for the given sharing request id.
        /// </summary>
        /// <param name="id">The id of the sharing request to retrieve.</param>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <returns>The corresponding sharing request.</returns>
        Task<IHttpResult<SharingRequest>> ReadAsync(string id, RequestContext requestContext, SharingRequestExpandOptions expandOptions = SharingRequestExpandOptions.None);

        /// <summary>
        /// Issues a read call that retrieves all known sharing requests.
        /// If the number of existing sharing requests is larger than the configured server-side max page size,
        /// then only the first page sharing requests are returned.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <param name="filterCriteria">The sharing request filter criteria.</param>
        /// <returns>A collection result with all the returned sharing requests, total number of existing sharing requests and possible next page link.</returns>
        Task<IHttpResult<Collection<SharingRequest>>> ReadByFiltersAsync(RequestContext requestContext, SharingRequestExpandOptions expandOptions = SharingRequestExpandOptions.None, SharingRequestFilterCriteria filterCriteria = null);

        /// <summary>
        /// Issues a read call that retrieves all known sharing requests. Automatically handles any service side paging.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <param name="filterCriteria">The sharing request filter criteria.</param>
        /// <returns>All available sharing requests.</returns>
        Task<IHttpResult<IEnumerable<SharingRequest>>> ReadAllByFiltersAsync(RequestContext requestContext, SharingRequestExpandOptions expandOptions = SharingRequestExpandOptions.None, SharingRequestFilterCriteria filterCriteria = null);

        /// <summary>
        /// Issues a call to approve the specified sharing request.
        /// </summary>
        /// <param name="id">The id of the sharing request to approve.</param>
        /// <param name="etag">The ETag of the sharing request.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The task to perform the operation.</returns>
        Task<IHttpResult> ApproveAsync(string id, string etag, RequestContext requestContext);

        /// <summary>
        /// Issues a delete call with the given sharing request id.
        /// </summary>
        /// <param name="id">The id of the sharing request to delete.</param>
        /// <param name="etag">The ETag of the sharing request.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The task to perform the operation.</returns>
        Task<IHttpResult> DeleteAsync(string id, string etag, RequestContext requestContext);
    }
}