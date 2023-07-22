namespace Microsoft.PrivacyServices.AnaheimId.Config
{
    using System;

    /// <summary>
    /// Builder for Common configuration.
    /// </summary>
    public class AIdConfigurationBuilder : IAIdFunctionConfigurationBuilder
    {
        /// <inheritdoc/>
        public IAIdFunctionConfiguration Build()
        {
            var config = new AIdConfiguration
            {
                AIdUamiId = GetStringValue("AIdUamiId"),
                MonitoringTenant = GetStringValue("MONITORING_TENANT"),
                MonitoringRole = GetStringValue("MONITORING_ROLE"),
                MetricAccount = GetStringValue("METRIC_ACCOUNT"),
                MetricPrefixName = GetStringValue("METRIC_PREFIX_NAME"),
                AppName = GetStringValue("App_Name"),
                AzureAppConfigEndpoint = GetStringValue("PAF_APP_CONFIG_ENDPOINT"),
                RedisCacheEndpoint = GetStringValue("AID_REDIS_CACHE_ENDPOINT"),
                RedisCachePort = int.Parse(GetStringValue("AID_REDIS_CACHE_PORT")),
                RedisCachePasswordName = GetStringValue("AID_REDIS_CACHE_PASSWORD_NAME"),
                RedisPasswordKeyVaultEndpoint = GetStringValue("AID_REDIS_PASSWORD_KEY_VAULT_ENDPOINT")
            };

            return config;
        }

        /// <summary>
        /// Get the value associated with key
        /// </summary>
        /// <param name="key">Value to retrieve.</param>
        /// <returns>Value associated with key.</returns>
        protected static string GetStringValue(string key)
        {
            return Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Process);
        }
    }
}
