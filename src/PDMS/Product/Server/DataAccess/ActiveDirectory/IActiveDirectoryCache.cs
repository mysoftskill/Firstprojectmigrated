namespace Microsoft.PrivacyServices.DataManagement.DataAccess.ActiveDirectory
{
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;

    /// <summary>
    /// An interface for interacting with the cache.
    /// </summary>
    public interface IActiveDirectoryCache
    {
        /// <summary>
        /// Reads the cache data.
        /// </summary>
        /// <param name="principal">The user to whom the data is associated.</param>
        /// <returns>The cache data or null if not cached.</returns>
        Task<CacheData> ReadDataAsync(AuthenticatedPrincipal principal);

        /// <summary>
        /// Creates the data in the cache.
        /// </summary>
        /// <param name="principal">The user to whom the data is associated.</param>
        /// <param name="cacheData">The data to store.</param>
        /// <returns>A task to run the operation.</returns>
        Task CreateDataAsync(AuthenticatedPrincipal principal, CacheData cacheData);

        /// <summary>
        /// Updates the data in the cache.
        /// </summary>
        /// <param name="principal">The user to whom the data is associated.</param>
        /// <param name="cacheData">The data to store.</param>
        /// <returns>A task to run the operation.</returns>
        Task UpdateDataAsync(AuthenticatedPrincipal principal, CacheData cacheData);
    }
}