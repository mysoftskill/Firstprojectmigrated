namespace Microsoft.PrivacyServices.AnaheimId.Config
{
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Anaheim Id Config.
    /// </summary>
    public class AidConfig : IAidConfig
    {
        /// <inheritdoc />
        public DeploymentEnvironment DeploymentEnvironment { get; set; }

        /// <inheritdoc />
        public string PcfBaseUrl { get; set; }

        /// <inheritdoc />
        public string AadPcfTargetResource { get; set; }

        /// <inheritdoc />
        public string ClientAppId { get; set; }

        /// <inheritdoc />
        public IEnumerable<QueueAccountInfo> AidQueuesStorageAccounts { get; set; }

        /// <inheritdoc />
        public IEnumerable<QueueAccountInfo> AidMonitoringQueuesStorageAccounts { get; set; }

        /// <inheritdoc />
        public string OneBoxCertSubjectName { get; set; }

        /// <inheritdoc />
        public string OneBoxAadAuthority { get; set; }

        /// <inheritdoc />
        public string OneBoxTenantId { get; set; }

        /// <inheritdoc />
        public string AidUamiId { get; set; }

        /// <inheritdoc />
        public string EventHubNamespace { get; set; }

        /// <inheritdoc />
        public string EventHubName { get; set; }

        /// <inheritdoc />
        public string StorageAccountName { get; set; }

        /// <summary>
        /// Build AidConfig.
        /// </summary>
        /// <returns>AidConfig.</returns>
        /// <param name="deploymentEnvironment">Deployment environment.</param>
        public static IAidConfig Build(DeploymentEnvironment deploymentEnvironment)
        {
            var builder = new ConfigurationBuilder();

            string configFileName = $"aid.{deploymentEnvironment}.settings.json".ToLowerInvariant();
            configFileName = Path.Combine("Settings", configFileName);

            var rootDirectory = AidHelpers.GetAzureFunctionsRootDirectory();
            builder.AddJsonFile(Path.Combine(rootDirectory, configFileName), optional: false, reloadOnChange: true);
            var config = builder.Build().GetSection("Values").Get<AidConfig>();

            return config;
        }
    }
}
