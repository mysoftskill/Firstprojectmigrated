namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Exposes the available APIs for the service that target the history item controller.
    /// </summary>
    public interface IHistoryItemClient
    {
        /// <summary>
        /// Issues a read call that retrieves all known history items.
        /// If the number of existing history items is larger than the configured server-side max page size,
        /// then only the first page history items are returned.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="filterCriteria">The history item filter criteria.</param>
        /// <returns>A collection result with all the returned history items, total number of existing history items and possible next page link.</returns>
        Task<IHttpResult<Collection<HistoryItem>>> ReadByFiltersAsync(RequestContext requestContext, HistoryItemFilterCriteria filterCriteria = null);

        /// <summary>
        /// Issues a read call that retrieves all known history items. Automatically handles any service side paging.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="filterCriteria">The history item filter criteria.</param>
        /// <returns>All available history items.</returns>
        Task<IHttpResult<IEnumerable<HistoryItem>>> ReadAllByFiltersAsync(RequestContext requestContext, HistoryItemFilterCriteria filterCriteria = null);
    }
}