namespace Microsoft.PrivacyServices.DataManagement.DataAccess.ActiveDirectory
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Common;
    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authentication;

    /// <summary>
    /// A decorator for the <see cref="IActiveDirectory"/> interface that caches the results.
    /// </summary>
    public class CachedActiveDirectory : ICachedActiveDirectory
    {
        private readonly IActiveDirectory activeDirectory;
        private readonly IAzureActiveDirectoryProviderConfig activeDirectoryProviderConfig;
        private readonly IActiveDirectoryCache activeDirectoryCache;
        private readonly TimeSpan ttl;
        private readonly IDateFactory dateFactory;
        private readonly IEventWriterFactory eventFactory;

        private readonly string componentName = nameof(CachedActiveDirectory);

        /// <summary>
        /// Initializes a new instance of the <see cref="CachedActiveDirectory"/> class.
        /// </summary>
        /// <param name="activeDirectory">The original active directory object.</param>
        /// <param name="activeDirectoryCache">The cache data access object.</param>
        /// <param name="aadProviderConfig"></param>
        /// <param name="cacheConfig">The cache configuration.</param>
        /// <param name="dateFactory">The date factory.</param>
        /// <param name="eventFactory">The event factory.</param>
        public CachedActiveDirectory(
            IActiveDirectory activeDirectory,
            IActiveDirectoryCache activeDirectoryCache,
            IAzureActiveDirectoryProviderConfig aadProviderConfig,
            IDataAccessConfiguration cacheConfig,
            IDateFactory dateFactory,
            IEventWriterFactory eventFactory)
        {
            this.activeDirectory = activeDirectory;
            this.activeDirectoryCache = activeDirectoryCache;
            this.activeDirectoryProviderConfig = aadProviderConfig;
            this.ttl = TimeSpan.FromMilliseconds(cacheConfig.ActiveDirectoryCacheExpirationInMilliseconds);
            this.dateFactory = dateFactory;
            this.eventFactory = eventFactory;

            this.ForceRefreshCache = false;
        }

        /// <summary>
        /// Gets or sets a value indicating whether refreshing the cache is always needed.
        /// </summary>
        public bool ForceRefreshCache { get; set; }

        /// <summary>
        /// Reads the data from cache if available. Otherwise, reads the original data and stores it in the cache.
        /// </summary>
        /// <param name="principal">The user whose data is requested.</param>
        /// <returns>The requested data.</returns>
        public async Task<IEnumerable<Guid>> GetSecurityGroupIdsAsync(AuthenticatedPrincipal principal)
        {
            if (this.activeDirectoryProviderConfig.EnableIntegrationTestOverrides && principal.UserId == this.activeDirectoryProviderConfig.IntegrationTestUserName)
            {
                return this.activeDirectoryProviderConfig.IntegrationTestSecurityGroups.Select(s => Guid.Parse(s));
            }

            var currentTime = this.dateFactory.GetCurrentTime();

            CacheData cacheData = null;

            await this.eventFactory.SuppressExceptionAsync(
                componentName,
                "ReadActiveDirectoryCache",
                async () =>
                {
                    cacheData = await this.activeDirectoryCache.ReadDataAsync(principal).ConfigureAwait(false);
                }).ConfigureAwait(false);

            if (cacheData != null && cacheData.Expiration > currentTime && !this.ForceRefreshCache)
            {
                return cacheData.SecurityGroupIds;
            }
            else
            {
                var securityGroupIds = await this.activeDirectory.GetSecurityGroupIdsAsync(principal).ConfigureAwait(false);

                cacheData = new CacheData
                {
                    Expiration = currentTime.Add(this.ttl),
                    SecurityGroupIds = securityGroupIds,
                    ETag = cacheData?.ETag
                };

                // TODO: Consider not waiting on this so that latencies are improved.
                await this.eventFactory.SuppressExceptionAsync(
                    componentName,
                    "WriteActiveDirectoryCache", 
                    async () =>
                    {
                        if (cacheData.ETag == null)
                        {
                            await this.activeDirectoryCache.CreateDataAsync(principal, cacheData).ConfigureAwait(false);
                        }
                        else
                        {
                            await this.activeDirectoryCache.UpdateDataAsync(principal, cacheData).ConfigureAwait(false);
                        }
                    }).ConfigureAwait(false);

                return securityGroupIds;
            }
        }

        /// <summary>
        /// Determines if a security group id exists or not.
        /// This is necessary for scenarios where the user does not need to be in the security group.
        /// </summary>
        /// <param name="principal">The current authenticated user.</param>
        /// <param name="id">The id of the security group.</param>
        /// <returns>True if it exists. Otherwise, false.</returns>
        public Task<bool> SecurityGroupIdExistsAsync(AuthenticatedPrincipal principal, Guid id)
        {
            return this.activeDirectory.SecurityGroupIdExistsAsync(principal, id);
        }
    }
}