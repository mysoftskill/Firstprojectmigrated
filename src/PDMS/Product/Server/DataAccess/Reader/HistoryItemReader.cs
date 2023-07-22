namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Reader
{
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authorization;
    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;

    /// <summary>
    /// Provides methods for reading history item information.
    /// </summary>
    public class HistoryItemReader : IHistoryItemReader
    {
        private readonly IPrivacyDataStorageReader storageReader;
        private readonly IAuthorizationProvider authorizationProvider;

        private readonly int maxPageSize;

        private readonly AuthorizationRole authorizationRoles;

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryItemReader" /> class.
        /// </summary>
        /// <param name="storageReader">The storage reader instance.</param>
        /// <param name="coreConfiguration">The core configuration.</param>
        /// <param name="authorizationProvider">The authorization provider instance.</param>
        public HistoryItemReader(IPrivacyDataStorageReader storageReader, ICoreConfiguration coreConfiguration, IAuthorizationProvider authorizationProvider)
        {
            this.storageReader = storageReader;
            this.authorizationProvider = authorizationProvider;

            this.maxPageSize = coreConfiguration.MaxPageSize;

            this.authorizationRoles = AuthorizationRole.ApplicationAccess;
        }
        
        /// <summary>
        /// Get history item based on filter criteria.
        /// </summary>
        /// <param name="filterCriteria">Filter criteria for history items.</param>
        /// <returns>History items matching filter criteria.</returns>
        public async Task<FilterResult<HistoryItem>> ReadByFiltersAsync(IFilterCriteria<HistoryItem> filterCriteria)
        {
            await this.authorizationProvider.AuthorizeAsync(this.authorizationRoles).ConfigureAwait(false);

            filterCriteria.Initialize(this.maxPageSize);

            var historyItems = await this.storageReader.GetHistoryItemsAsync(filterCriteria).ConfigureAwait(false);
            
            return historyItems;
        }
    }
}