
namespace Microsoft.Azure.ComplianceServices.Common
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Threading;
    using global::Azure.Core;
    using global::Azure.Identity;
    using Microsoft.Azure.ComplianceServices.Common.AppConfig.Cache;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Configuration.AzureAppConfiguration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.FeatureManagement;
    using Microsoft.FeatureManagement.FeatureFilters;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Implements IAppConfiguration. Gets configurations and feature flag settings from Azure App Configuration store.
    /// NOTE: Since IFeatureManager is tightly coupled with IServiceProvider, have to create a standalone ServiceProvider instance
    ///       to get it work. It hasn't been tested that what happens if there is already a ServiceProvider instance in the process
    ///       (luckily most of our services do not use ServiceProvider for DI)
    /// </summary>
    public class AppConfiguration : IAppConfiguration
    {
        // Indicates that all settings should be refreshed when the value of this config key has changed
        // Everytime a config value (not including FeatureFlag) changed, the value of this key also needs to be updated
        private const string SentinelConfigVersion = "ConfigVersion";

        private readonly IConfiguration configuration;
        private readonly IFeatureManager featureManager;

        // Have to hold on a ServiceProvider reference to make FeatureManager refresh work
        private readonly ServiceProvider serviceProvider;

        // Azure App configuration maintains an internal cache for configs to reduce the number of HTTP calls to the cloud, which has
        // a 20K calls per hour limit.
        // Setting a long cache time to avoid being throttled
        private const int CONFIG_CACHE_EXPIRATION = 30;
        private const int MAX_CACHE_CAPACITY = 100000;

        // FeatureManager sdk has perf issues, adding mitigation to cache the feature status.
        // Cache is invalidated when we refresh
        private readonly ICache<bool> featureStatusCache;

        private IConfigurationRefresher refresher;
        private Timer refreshTimer;


        /// <summary>
        /// Creates an AppConfiguration with given local config file
        /// </summary>
        /// <param name="localConfigFile">The file name of the local config file, e.g. appsettings.json</param>
        /// <param name="logger">Optional ILogger to print logs to</param>
        /// <param name="cache">Optional cache store to be used in feature Manager caching</param>
        public AppConfiguration(string localConfigFile, ILogger logger = null, ICache<bool> cache = null)
        {
            configuration = new ConfigurationBuilder().AddJsonFile(localConfigFile).Build();
            var services = new ServiceCollection();
            services.AddSingleton(configuration)
                .AddFeatureManagement()
                .AddFeatureFilter<ContextualTargetingFilter>()
                .AddFeatureFilter<PercentageFilter>()
                .AddFeatureFilter<CustomOperatorFilter>();

            services.Configure<FeatureManagementOptions>(options => 
            {
                options.IgnoreMissingFeatureFilters = true;
            });

            serviceProvider = services.BuildServiceProvider();
            featureManager = serviceProvider.GetRequiredService<IFeatureManager>();
            featureStatusCache = cache ?? new LruCache<bool>(MAX_CACHE_CAPACITY, CONFIG_CACHE_EXPIRATION);
            CustomOperatorFilter.Logger = logger;
        }

        /// <summary>
        /// Creates an AppConfiguration with given endpoint
        /// NOTE: Most settings are hard-coded for now. Consider making them configurable in the future if we found 
        ///       different services want different settings.
        /// </summary>
        /// <param name="azureAppConfigEndpoint">The endpoint of Azure App Configuration instance.</param>
        /// <param name="labelFilter">The label filter to apply when querying Azure App Configuration for key-values.</param>
        /// <param name="logger">Optional ILogger to print logs to</param>
        /// <param name="cache">Optional cache store to be used in feature Manager caching</param>
        public AppConfiguration(Uri azureAppConfigEndpoint, string labelFilter, ILogger logger = null, TokenCredential credential = null, ICache<bool> cache = null)
        {
            var builder = new ConfigurationBuilder();

            if (credential == null)
            {
                credential = new DefaultAzureCredential();
            }

            builder.AddAzureAppConfiguration(options =>
            {
                options.Connect(azureAppConfigEndpoint, credential)
                    // Load configuration values with no label
                    .Select(KeyFilter.Any, LabelFilter.Null)
                    // Override with any configuration values specific to the specified label (defined in LabelNames)
                    .Select(KeyFilter.Any, labelFilter)
                    .ConfigureRefresh(refreshOptions =>
                    {
                        refreshOptions.Register(key: SentinelConfigVersion, label: LabelFilter.Null, refreshAll: true)
                                      .SetCacheExpiration(TimeSpan.FromMinutes(CONFIG_CACHE_EXPIRATION));
                    })
                    .UseFeatureFlags(featureFlagOptions =>
                    {
                        featureFlagOptions.CacheExpirationInterval = TimeSpan.FromMinutes(CONFIG_CACHE_EXPIRATION);
                    });

                refresher = options.GetRefresher();
            });

            configuration = builder.Build();

            var services = new ServiceCollection();
            services.AddSingleton(configuration)
                .AddFeatureManagement()
                .AddFeatureFilter<ContextualTargetingFilter>()
                .AddFeatureFilter<PercentageFilter>()
                .AddFeatureFilter<CustomOperatorFilter>();

            services.Configure<FeatureManagementOptions>(options => 
            {
                options.IgnoreMissingFeatureFilters = true;
            });
            serviceProvider = services.BuildServiceProvider();
            featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            featureStatusCache = cache ?? new LruCache<bool>(MAX_CACHE_CAPACITY, CONFIG_CACHE_EXPIRATION);
            CustomOperatorFilter.Logger = logger;

            // Set up a timer on the same cache expiration interval to refresh the config
            refreshTimer = new Timer(new TimerCallback(
                (o) =>
                {
                    refresher.TryRefreshAsync();                    
                }),
                null, TimeSpan.Zero, TimeSpan.FromMinutes(CONFIG_CACHE_EXPIRATION) );
        }

        /// <inheritdoc />
        public async ValueTask<bool> IsFeatureFlagEnabledAsync(string feature, bool useCached = true)
        {
            if (useCached)
            {
                if (featureStatusCache.GetItem(feature, out bool cachedStatus))
                {
                    return cachedStatus;
                }
                else
                {
                    bool featureStatus = await featureManager.IsEnabledAsync(feature).ConfigureAwait(false);
                    featureStatusCache.AddItem(feature, featureStatus);
                    return featureStatus;
                }
            }

            return await featureManager.IsEnabledAsync(feature).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<string>> GetEnabledFeaturesAsync<TContext>(TContext context)
        {
            IAsyncEnumerator<string> featureNames = featureManager.GetFeatureNamesAsync().GetAsyncEnumerator();
            List<string> enabledFeatures = new List<string>();

            while(await featureNames.MoveNextAsync())
            {
                if(featureManager.IsEnabledAsync(featureNames.Current,context).GetAwaiter().GetResult())
                {
                    enabledFeatures.Add(featureNames.Current);
                }
            }
            
            await featureNames.DisposeAsync();

            return enabledFeatures;
        }

        /// <inheritdoc />
        public async ValueTask<bool> IsFeatureFlagEnabledAsync<TContext>(string feature, TContext context, bool useCached = true)
        {
            if (useCached)
            {
                string cachedKey = feature + context.ToString();
                if (featureStatusCache.GetItem(cachedKey, out bool cachedStatus))
                {
                    return cachedStatus;
                }
                else
                {
                    bool featureStatus = await featureManager.IsEnabledAsync(feature, context).ConfigureAwait(false);
                    featureStatusCache.AddItem(cachedKey, featureStatus);
                    return featureStatus;
                }
            }

            return await featureManager.IsEnabledAsync(feature, context).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async ValueTask<bool> IsFeatureFlagEnabledAnyAsync<TContext>(string feature, IEnumerable<TContext> context, bool useCached = true)
        {
            foreach (TContext con in context)
            {
                if (await IsFeatureFlagEnabledAsync(feature, con, useCached).ConfigureAwait(false))
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public async ValueTask<bool> IsFeatureFlagEnabledAllAsync<TContext>(string feature, IEnumerable<TContext> context, bool useCached = true)
        {
            foreach (TContext con in context)
            {
                if (await IsFeatureFlagEnabledAsync(feature, con, useCached).ConfigureAwait(false) == false)
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc />
        public T GetConfigValue<T>(string configurationName)
        {
            return configuration.GetValue<T>(configurationName);
        }

        /// <inheritdoc />
        public T GetConfigValue<T>(string configurationName, T defaultValue)
        {
            return configuration.GetValue(configurationName, defaultValue);
        }

        /// <inheritdoc />
        public T[] GetConfigValues<T>(string configurationName)
        {
            return configuration.GetSection(configurationName)?.Get<T[]>();
        }
    }
}
