namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A data agent map factory. This is generally used to handle background refresh.
    /// </summary>
    public interface IDataAgentMapFactory
    {
        /// <summary>
        /// Gets or creates a new data agent map.
        /// </summary>
        IDataAgentMap GetDataAgentMap();

        /// <summary>
        /// Gets a data agent map corresponding to the given version.
        /// </summary>
        Task<IDataAgentMap> GetDataAgentMapAsync(long requestedVersion);

        /// <summary>
        /// Background thread that should update Data Agent Map periodically. 
        /// The thread executing indefinitely, until cancellation token has requested a cancellation.
        /// </summary>
        /// <returns>Async Task.</returns>
        Task RefreshAsync(CancellationToken token);

        /// <summary>
        /// Initializes the factory asynchronously.
        /// </summary>
        Task InitializeAsync();
    }
}
