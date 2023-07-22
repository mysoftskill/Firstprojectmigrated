namespace Microsoft.PrivacyServices.AzureFunctions.AIdFunctionalTests.AnaheimId
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Extensions.Configuration;
    using Microsoft.PrivacyServices.AnaheimId;
    using Microsoft.PrivacyServices.AnaheimId.Config;

    /// <summary>
    /// Anaheim Id Config.
    /// </summary>
    public class AidFctConfig
    {
        /// <summary>
        /// Storage accounts.
        /// </summary>
        public IEnumerable<QueueAccountInfo> AidQueuesStorageAccounts { get; set; }

        /// <summary>
        /// Service principal client app id.
        /// </summary>
        public string AidClientId { get; set; }

        /// <summary>
        /// Service principal tenant id.
        /// </summary>
        public string AmeTenantId { get; set; }

        /// <summary>
        /// Deployment environment.
        /// </summary>
        public DeploymentEnvironment DeploymentEnvironment { get; set; }

        /// <summary>
        /// CloudTest service principal cert subject name.
        /// </summary>
        public string CloudTestCertSubjectName { get; set; }

        /// <summary>
        /// Do we Base64 encode messages in the Azure Queue?
        /// </summary>
        public bool MessageEncoding { get; set; }

        /// <summary>
        /// AID EventHub name.
        /// </summary>
        public string EventHubHostName { get; set; }

        /// <summary>
        /// EventHub name.
        /// </summary>
        public string EventHubName { get; set; }

        /// <summary>
        /// PXS Api Host name.
        /// </summary>
        public string PxsApiHost { get; set; }

        /// <summary>
        /// PCF Api Host name.
        /// </summary>
        public string PcfApiHost { get; set; }

        /// <summary>
        /// Blob storage account name.
        /// </summary>
        public string StorageAccountName { get; set; }

        /// <summary>
        /// Build AidConfig.
        /// </summary>
        /// <returns>AidFctConfig config.</returns>
        public static AidFctConfig Build()
        {
            var builder = new ConfigurationBuilder();
            var deploymentEnvironment = AidTestHelpers.GetDeploymentEnvironment();

            string configFileName = Path.Combine("Config", $"aid.{deploymentEnvironment}.test.settings.json");
            builder.AddJsonFile(Path.Combine(Environment.CurrentDirectory, configFileName), optional: false, reloadOnChange: true);

            var config = builder.Build().GetSection("Values").Get<AidFctConfig>();
            config.DeploymentEnvironment = deploymentEnvironment;

            return config;
        }
    }
}
