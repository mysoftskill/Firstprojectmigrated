namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Remoting.Messaging;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.FeatureManagement.FeatureFilters;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;

    /// <summary>
    /// Static flighting utilities.
    /// </summary>
    public static class FlightingUtilities
    {

        // Test flight context created with AppConfig initialized with local 
        // settings file.
        private static IFlightContext defaultContext = null;

        private static IAppConfiguration AppConfiguration { get; set; }
        public static IFlightContext Instance
        {
            get
            {
                object context = CallContext.LogicalGetData("PCF.FlightingUtilitiesContext");
                context = context ?? GetDefaultContext();
                return (IFlightContext)context;
            }

            set
            {
                CallContext.LogicalSetData("PCF.FlightingUtilitiesContext", value);
            }
        }

        private static IFlightContext GetDefaultContext()
        {
            try
            {
                defaultContext = defaultContext ?? new AppConfigFlightContext(new AppConfiguration("local.settings.test.json"));
            }
            catch(Exception)
            {
                // local.settings.test.json is available only in test environment
            }

            return defaultContext;
        }

        /// <summary>
        /// Sets the default flight context. Call this at app startup.
        /// </summary>
        public static void Initialize(IAppConfiguration appConfiguration)
        {
            AppConfiguration = appConfiguration ?? throw new ArgumentException(nameof(appConfiguration));
            Instance = new AppConfigFlightContext(AppConfiguration);
        }

        /// <summary>
        /// Tests a key/value pair to see if a feature is enabled.
        /// </summary>
        public static bool IsFeatureEnabled(string flightName, string key, string value)
        {
            return IsEnabled(flightName, CustomOperatorContextFactory.CreateDefaultStringComparisonContextWithKeyValue(key, value));
        }

        /// <summary>
        /// Tests the given integer value to see if the flight is enabled.
        /// </summary>
        public static bool IsIntegerValueEnabled(string flightName, int value)
        {
            return IsEnabled(flightName, FlightingContext.FromIntegerValue(value));
        }

        /// <summary>
        /// Tests the given key and determines if its enabled for a feature. Works only with % operator.
        /// </summary>
        public static bool IsKeyEnabledForFlight(string flightName, string key)
        {
            return IsFeatureEnabled(flightName, key, null);
        }

        /// <summary>
        /// Tests the given string value to see if the flight is enabled.
        /// </summary>
        public static bool IsStringValueEnabled(string flightName, string value)
        {
            return IsEnabled(flightName, FlightingContext.FromStringValue(value));
        }

        /// <summary>
        /// Tests to see if the given agent ID is enabled.
        /// </summary>
        public static bool IsAgentIdEnabled(string flightName, AgentId agentId)
        {
            return IsEnabled(flightName, FlightingContext.FromAgentId(agentId));
        }

        /// <summary>
        /// Tests to see if the given asset group ID is enabled.
        /// </summary>
        public static bool IsAssetGroupIdEnabled(string flightName, AssetGroupId assetGroupId)
        {
            return IsEnabled(flightName, FlightingContext.FromAssetGroupId(assetGroupId));
        }

        /// <summary>
        /// Tests to see if the given agent ID is blocked. Blocks ingestion AND getCommands!
        /// </summary>
        public static bool IsAgentBlocked(AgentId agentId)
        {
            return IsAgentIdEnabled(FlightingNames.BlockedAgents, agentId);
        }

        /// <summary>
        /// Tests to see if command ingestion for the given agent is blocked.
        /// </summary>
        public static bool IsIngestionBlockedForAgentId(AgentId agentId)
        {
            return IsAgentIdEnabled(FlightingNames.IngestionBlockedForAgentId, agentId);
        }

        /// <summary>
        /// Tests to see if the given asset group ID is blocked. Blocks ingestion AND getCommands!
        /// </summary>
        public static bool IsAssetGroupIdBlocked(AssetGroupId assetGroupId)
        {
            return IsAssetGroupIdEnabled(FlightingNames.BlockedAssetGroups, assetGroupId);
        }

        /// <summary>
        /// Tests to see if command ingestion for the given asset group is blocked.
        /// </summary>
        public static bool IsIngestionBlockedForAssetGroupId(AssetGroupId assetGroupId)
        {
            return IsAssetGroupIdEnabled(FlightingNames.IngestionBlockedForAssetGroupId, assetGroupId);
        }

        /// <summary>
        /// Tests to see if the given tenant id is enabled.
        /// </summary>
        public static bool IsTenantIdEnabled(string flightName, TenantId tenantId)
        {
            return IsEnabled(flightName, FlightingContext.FromTenantId(tenantId));
        }

        /// <summary>
        /// Tests to see if the subject is enabled.
        /// </summary>
        public static bool IsSubjectEnabled(string flightName, IPrivacySubject subject)
        {
            if (subject is MsaSubject)
            {
                return IsEnabled(flightName, FlightingContext.FromMsaSubject(subject as MsaSubject));
            }

            if (subject is AadSubject)
            {
                return IsEnabled(flightName, FlightingContext.FromAadSubject(subject as AadSubject));
            }

            return false;
        }

        /// <summary>
        /// Checks if a feature is enabled against a set of parameters. 
        /// </summary>
        /// <param name="feature"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static bool IsEnabledAll(string feature, IEnumerable<ICustomOperatorContext> context)
        {
            return Instance.IsEnabledAll(feature, context);
        }

        /// <summary>
        /// Checks if a feature is enabled against a set of parameters. 
        /// </summary>
        /// <param name="feature"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static bool IsEnabledAny(string feature, IEnumerable<ICustomOperatorContext> context)
        {
            return Instance.IsEnabledAny(feature, context);
        }

        /// <summary>
        /// Checks whether a given feature is enabled.
        /// </summary>
        /// <param name="feature">The name of the feature to check.</param>
        /// <param name="context">Additional feature evaluation context.</param>
        /// <param name="useCached">boolean indicating whether cache should be checked first for the feature.</param>
        /// <returns>True if the feature is enabled, otherwise false.</returns>
        public static bool IsEnabled<TContext>(string feature, TContext context, bool useCached = true)
        {
            return Instance.IsEnabled(feature, context, useCached);
        }


        /// <summary>
        /// Checks whether a given feature is enabled.
        /// </summary>
        /// <param name="feature">The name of the feature to check.</param>
        /// <param name="useCached">boolean indicating whether cache should be checked first for the feature.</param>
        /// <returns>True if the feature is enabled, otherwise false.</returns>
        public static bool IsEnabled(string feature, bool useCached = true)
        {
            return Instance.IsEnabled(feature, useCached);
        }

        /// <summary>
        /// Checks whether a given feature is enabled for the given groups
        /// </summary>
        /// <param name="feature">The name of the feature to check.</param>
        /// <param name="groups">A set of feature audience groups to test the feature flag on. A feature can have separate percentage settings for each group.</param>
        /// <returns>True if the feature is enabled, otherwise false.</returns>
        public static bool IsFeatureEnabled(string feature, string[] groups)
        {
            TargetingContext targetingContext = new TargetingContext
            {
                Groups = groups
            };

            return Instance.IsEnabled(feature, new List<TargetingContext> { targetingContext });
        }

        /// <summary>
        /// Gets value of the specified configuration key.
        /// </summary>
        /// <param name="config">The name of the configuration key.</param>
        /// <returns>The value of the give configuration key.</returns>
        public static T GetConfigValue<T>(string config)
        {
            return AppConfiguration.GetConfigValue<T>(config);
        }

        /// <summary>
        /// Gets value of the specified configuration key.
        /// </summary>
        /// <param name="config">The name of the configuration key.</param>
        /// <param name="defaultValue">Default value to be returned if config is missing.</param>
        /// <returns>The value of the give configuration key.</returns>
        public static T GetConfigValue<T>(string config, T defaultValue)
        {
            return AppConfiguration.GetConfigValue<T>(config, defaultValue);
        }

        /// <summary>
        /// Gets array of values for the specified configuration key in Azure app config.
        /// </summary>
        /// <param name="config">The name of the configuration key.</param>
        /// <returns>The array of values of the give configuration key.</returns>
        public static T[] GetConfigValues<T>(string config)
        {
            return AppConfiguration.GetConfigValues<T>(config);
        }

        private class AppConfigFlightContext : IFlightContext
        {
            private IAppConfiguration AppConfiguration;
            public AppConfigFlightContext(IAppConfiguration appConfig)
            {
                AppConfiguration = appConfig;
            }
            public bool IsEnabledAny<TContext>(string flightName, IEnumerable<TContext> parameters)
            {
                return AppConfiguration.IsFeatureFlagEnabledAnyAsync(flightName, parameters).GetAwaiter().GetResult();
            }

            public bool IsEnabledAll<TContext>(string flightName, IEnumerable<TContext> parameters)
            {
                return AppConfiguration.IsFeatureFlagEnabledAllAsync(flightName, parameters).GetAwaiter().GetResult();
            }

            public bool IsEnabled(string flightName, bool enableCache = true)
            {
                return AppConfiguration.IsFeatureFlagEnabledAsync(flightName, enableCache).GetAwaiter().GetResult();
            }

            public bool IsEnabled<TContext>(string flightName, TContext context, bool enableCache = true)
            {
                return AppConfiguration.IsFeatureFlagEnabledAsync(flightName, context, enableCache).GetAwaiter().GetResult();
            }
        }
    }
}
