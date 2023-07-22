namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Reader
{
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;

    /// <summary>
    /// Provides methods for reading history items.
    /// </summary>
    public interface IHistoryItemReader
    {
        /// <summary>
        /// Find history items based on the filter criteria.
        /// </summary>
        /// <param name="filterCriteria">Filter criteria for the history item.</param>
        /// <returns>History items matching the filter criteria.</returns>
        Task<FilterResult<HistoryItem>> ReadByFiltersAsync(IFilterCriteria<HistoryItem> filterCriteria);
    }
}