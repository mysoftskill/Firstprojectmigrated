namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Exposes the available APIs for the service that target the TransferRequest controller.
    /// </summary>
    public interface ITransferRequestClient
    {
        /// <summary>
        /// Issues a create call for the given Transfer request and returns the newly created Transfer request.
        /// </summary>
        /// <param name="transferRequest">The Transfer request to create.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The service issued Transfer request.</returns>
        Task<IHttpResult<TransferRequest>> CreateAsync(TransferRequest transferRequest, RequestContext requestContext);

        /// <summary>
        /// Issues a read call for the given Transfer request id.
        /// </summary>
        /// <param name="id">The id of the Transfer request to retrieve.</param>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <returns>The corresponding Transfer request.</returns>
        Task<IHttpResult<TransferRequest>> ReadAsync(string id, RequestContext requestContext, TransferRequestExpandOptions expandOptions = TransferRequestExpandOptions.None);

        /// <summary>
        /// Issues a read call that retrieves all known Transfer requests.
        /// If the number of existing Transfer requests is larger than the configured server-side max page size,
        /// then only the first page Transfer requests are returned.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <param name="filterCriteria">The Transfer request filter criteria.</param>
        /// <returns>A collection result with all the returned Transfer requests, total number of existing Transfer requests and possible next page link.</returns>
        Task<IHttpResult<Collection<TransferRequest>>> ReadByFiltersAsync(RequestContext requestContext, TransferRequestExpandOptions expandOptions = TransferRequestExpandOptions.None, TransferRequestFilterCriteria filterCriteria = null);

        /// <summary>
        /// Issues a read call that retrieves all known Transfer requests. Automatically handles any service side paging.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <param name="filterCriteria">The Transfer request filter criteria.</param>
        /// <returns>All available Transfer requests.</returns>
        Task<IHttpResult<IEnumerable<TransferRequest>>> ReadAllByFiltersAsync(RequestContext requestContext, TransferRequestExpandOptions expandOptions = TransferRequestExpandOptions.None, TransferRequestFilterCriteria filterCriteria = null);

        /// <summary>
        /// Issues a call to approve the specified Transfer request.
        /// </summary>
        /// <param name="id">The id of the Transfer request to approve.</param>
        /// <param name="etag">The ETag of the Transfer request.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The task to perform the operation.</returns>
        Task<IHttpResult> ApproveAsync(string id, string etag, RequestContext requestContext);

        /// <summary>
        /// Issues a delete call with the given Transfer request id.
        /// </summary>
        /// <param name="id">The id of the Transfer request to delete.</param>
        /// <param name="etag">The ETag of the Transfer request.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The task to perform the operation.</returns>
        Task<IHttpResult> DeleteAsync(string id, string etag, RequestContext requestContext);
    }
}