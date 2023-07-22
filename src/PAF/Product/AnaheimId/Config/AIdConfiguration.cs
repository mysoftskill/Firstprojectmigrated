namespace Microsoft.PrivacyServices.AnaheimId.Config
{
    /// <summary>
    /// Core configuration settings for AId.
    /// </summary>
    public class AIdConfiguration : IAIdFunctionConfiguration
    {
        /// <inheritdoc/>
        public string MonitoringTenant { get; set; }

        /// <inheritdoc/>
        public string MonitoringRole { get; set; }

        /// <inheritdoc/>
        public string MetricAccount { get; set; }

        /// <inheritdoc/>
        public string MetricPrefixName { get; set; }

        /// <inheritdoc/>
        public string AppName { get; set; }

        /// <inheritdoc/>
        public string AzureAppConfigEndpoint { get; set; }

        /// <inheritdoc/>
        public string AIdUamiId { get; set; }

        /// <inheritdoc />
        public string RedisCacheEndpoint { get; set; }

        /// <inheritdoc />
        public int RedisCachePort { get; set; }

        /// <inheritdoc />
        public string RedisCachePasswordName { get; set; }

        /// <inheritdoc />
        public string RedisPasswordKeyVaultEndpoint { get; set; }
    }
}
