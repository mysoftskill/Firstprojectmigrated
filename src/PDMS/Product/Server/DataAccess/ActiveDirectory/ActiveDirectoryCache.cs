namespace Microsoft.PrivacyServices.DataManagement.DataAccess.ActiveDirectory
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Azure.Documents;
    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;

    using static Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb.DocumentModule;

    /// <summary>
    /// Stores cache data in DocumentDB.
    /// </summary>
    public class ActiveDirectoryCache : IActiveDirectoryCache
    {
        private readonly DocumentContext documentContext;
        private readonly string idPrefix;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActiveDirectoryCache"/> class.
        /// </summary>
        /// <param name="documentClient">The document client.</param>
        /// <param name="databaseConfig">Configuration for DocumentDB.</param>
        /// <param name="cacheConfig">Cache specific information.</param>
        public ActiveDirectoryCache(IDocumentClient documentClient, IDocumentDatabaseConfig databaseConfig, IDataAccessConfiguration cacheConfig)
        {
            this.documentContext = new DocumentContext
            {
                CollectionName = databaseConfig.EntityCollectionName,
                DatabaseName = databaseConfig.DatabaseName,
                DocumentClient = documentClient
            };
            
            this.idPrefix = cacheConfig.ActiveDirectoryCacheIdPrefix;
        }

        /// <summary>
        /// Reads the cache data.
        /// </summary>
        /// <param name="principal">The user to whom the data is associated.</param>
        /// <returns>The cache data or null if not cached.</returns>
        public Task<CacheData> ReadDataAsync(AuthenticatedPrincipal principal)
        {
            return Read<CacheData>(this.GetId(principal), this.documentContext);
        }

        /// <summary>
        /// Creates the data in the cache.
        /// </summary>
        /// <param name="principal">The user to whom the data is associated.</param>
        /// <param name="cacheData">The data to store.</param>
        /// <returns>A task to run the operation.</returns>
        public Task CreateDataAsync(AuthenticatedPrincipal principal, CacheData cacheData)
        {
            return Create(this.UpdateId(principal, cacheData), this.documentContext);
        }

        /// <summary>
        /// Updates the data in the cache.
        /// </summary>
        /// <param name="principal">The user to whom the data is associated.</param>
        /// <param name="cacheData">The data to store.</param>
        /// <returns>A task to run the operation.</returns>
        public Task UpdateDataAsync(AuthenticatedPrincipal principal, CacheData cacheData)
        {
            return Update(this.UpdateId(principal, cacheData), this.documentContext);
        }
        
        private string GetId(AuthenticatedPrincipal principal)
        {
            if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal));
            }
            else if (string.IsNullOrWhiteSpace(principal.UserId))
            {
                throw new ArgumentNullException(nameof(principal.UserId), $"Value is missing: {principal.UserId}");
            }

            // This ensures we don't collide with any other ids in our system.
            return $"{idPrefix}_{principal.UserId.ToLowerInvariant()}";
        }

        private CacheData UpdateId(AuthenticatedPrincipal principal, CacheData cacheData)
        {
            cacheData.Id = this.GetId(principal);
            return cacheData;
        }
    }
}