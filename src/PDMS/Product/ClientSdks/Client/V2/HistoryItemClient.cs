namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Exposes the available APIs for the service that target the HistoryItem controller.
    /// </summary>
    internal class HistoryItemClient : IHistoryItemClient
    {
        private readonly IHttpServiceProxy httpServiceProxy;

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryItemClient" /> class.
        /// </summary>
        /// <param name="httpServiceProxy">The <see cref="IHttpServiceProxy" /> to use for interacting with the service.</param>
        public HistoryItemClient(IHttpServiceProxy httpServiceProxy)
        {
            this.httpServiceProxy = httpServiceProxy;
        }
        
        /// <summary>
        /// Issues a read call that retrieves all known history items.
        /// If the number of existing history items is larger than the configured server-side max page size,
        /// then only the first page history items are returned.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="filterCriteria">The history item filter criteria.</param>
        /// <returns>A collection result with all the returned history items, total number of existing history items and possible next page link.</returns>
        public async Task<IHttpResult<Collection<HistoryItem>>> ReadByFiltersAsync(RequestContext requestContext, HistoryItemFilterCriteria filterCriteria = null)
        {
            string url = $"/api/v2/historyItems{GetHistoryItemSelects()}{GetHistoryItemFilterCriteria(filterCriteria)}";

            var result =
                await this.httpServiceProxy.GetAsync<Collection<HistoryItem>>(
                    url,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }

        /// <summary>
        /// Issues a read call that retrieves all known history items. Automatically handles any service side paging.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="filterCriteria">The history item filter criteria.</param>
        /// <returns>All available history items.</returns>
        public Task<IHttpResult<IEnumerable<HistoryItem>>> ReadAllByFiltersAsync(RequestContext requestContext, HistoryItemFilterCriteria filterCriteria = null)
        {
            return DataManagementClient.ReadMany<HistoryItem>(
                $"/api/v2/historyItems{GetHistoryItemSelects()}{GetHistoryItemFilterCriteria(filterCriteria)}",
                requestContext,
                this.httpServiceProxy);
        }

        /// <summary>
        /// Get history item select http request string.
        /// </summary>
        /// <returns>The history item selects.</returns>
        private static string GetHistoryItemSelects()
        {
            var queryString = "?$select=id,eTag,entity,writeAction,transactionId&$expand=entity";

            return queryString;
        }

        /// <summary>
        /// Get filter criteria to be used in url from HistoryItemFilterCriteria.
        /// </summary>
        /// <param name="filterCriteria">History item filter criteria.</param>
        /// <returns>History item filter criteria in string format.</returns>
        private static string GetHistoryItemFilterCriteria(HistoryItemFilterCriteria filterCriteria)
        {
            return filterCriteria == null ? string.Empty : "&" + filterCriteria.BuildRequestString();
        }
    }
}