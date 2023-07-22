namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Exposes the available APIs for the service that target the VariantRequest controller.
    /// </summary>
    public interface IVariantRequestClient
    {
        /// <summary>
        /// Issues a create call for the given variant request
        /// and returns the newly created variant request.
        /// </summary>
        /// <param name="variantRequest">The variant request to create.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The service issued variant request.</returns>
        Task<IHttpResult<VariantRequest>> CreateAsync(VariantRequest variantRequest, RequestContext requestContext);

        /// <summary>
        /// Issues a read call for the given variant request id.
        /// </summary>
        /// <param name="id">The id of the variant request to retrieve.</param>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <returns>The corresponding variant request.</returns>
        Task<IHttpResult<VariantRequest>> ReadAsync(string id, RequestContext requestContext, VariantRequestExpandOptions expandOptions = VariantRequestExpandOptions.None);

        /// <summary>
        /// Issues a read call that retrieves all known variant requests.
        /// If the number of existing variant requests is larger than the configured server-side max page size,
        /// then only the first page variant requests are returned.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <param name="filterCriteria">The variant request filter criteria.</param>
        /// <returns>A collection result with all the returned variant requests, total number of existing variant requests and possible next page link.</returns>
        Task<IHttpResult<Collection<VariantRequest>>> ReadByFiltersAsync(RequestContext requestContext, VariantRequestExpandOptions expandOptions = VariantRequestExpandOptions.None, VariantRequestFilterCriteria filterCriteria = null);

        /// <summary>
        /// Issues a read call that retrieves all known variant requests. Automatically handles any service side paging.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <param name="filterCriteria">The variant request filter criteria.</param>
        /// <returns>All available variant requests.</returns>
        Task<IHttpResult<IEnumerable<VariantRequest>>> ReadAllByFiltersAsync(RequestContext requestContext, VariantRequestExpandOptions expandOptions = VariantRequestExpandOptions.None, VariantRequestFilterCriteria filterCriteria = null);

        /// <summary>
        /// Issues an update call for the given variant request
        /// and returns the updated variant request.
        /// </summary>
        /// <param name="variantRequest">The variant request to create.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The service issued variant request.</returns>
        Task<IHttpResult<VariantRequest>> UpdateAsync(VariantRequest variantRequest, RequestContext requestContext);

        /// <summary>
        /// Issues a call to approve the specified variant request.
        /// </summary>
        /// <param name="id">The id of the variant request to approve.</param>
        /// <param name="etag">The ETag of the variant request.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The task to perform the operation.</returns>
        Task<IHttpResult> ApproveAsync(string id, string etag, RequestContext requestContext);

        /// <summary>
        /// Issues a delete call with the given variant request id.
        /// </summary>
        /// <param name="id">The id of the variant request to delete.</param>
        /// <param name="etag">The ETag of the variant request.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The task to perform the operation.</returns>
        Task<IHttpResult> DeleteAsync(string id, string etag, RequestContext requestContext);
    }
}