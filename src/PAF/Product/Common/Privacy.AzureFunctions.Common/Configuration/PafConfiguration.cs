namespace Microsoft.PrivacyServices.AzureFunctions.Common.Configuration
{
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Core configuration settings for PAF.
    /// </summary>
    public class PafConfiguration : IFunctionConfiguration
    {
        /// <inheritdoc/>
        public string AMETenantId { get; set; }

        /// <inheritdoc/>
        public string AzureDevOpsAccessToken { get; set; }

        /// <inheritdoc/>
        public string AzureDevOpsProjectUrl { get; set; }

        /// <inheritdoc/>
        public string AzureDevOpsProjectName { get; set; }

        /// <inheritdoc/>
        public bool EnableNonProdFunctionality { get; set; }

        /// <inheritdoc/>
        public string MSTenantId { get; set; }

        /// <inheritdoc/>
        public string PafUamiId { get; set; }

        /// <inheritdoc/>
        public string PdmsResourceId { get; set; }

        /// <inheritdoc/>
        public string PdmsBaseUrl { get; set; }

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
        public string AadClientId { get; set; }

        /// <inheritdoc/>
        public bool ShouldUseAADToken { get; set; }

        /// <inheritdoc/>
        public X509Certificate2 AadClientCert { get; set; }

        /// <inheritdoc/>
        public string AadClientSecret { get; set; }
    }
}
