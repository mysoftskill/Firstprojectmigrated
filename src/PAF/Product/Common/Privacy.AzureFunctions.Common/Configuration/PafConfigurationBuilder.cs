namespace Microsoft.PrivacyServices.AzureFunctions.Common.Configuration
{
    using System;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.PrivacyServices.AzureFunctions.Common.Authentication;

    /// <summary>
    /// Builder for PAF configuration.
    /// </summary>
    public class PafConfigurationBuilder : IFunctionConfigurationBuilder
    {
        /// <inheritdoc/>
        public IFunctionConfiguration Build()
        {
            var config = new PafConfiguration
            {
                AMETenantId = GetStringValue("AMETenantId"),
                AzureDevOpsAccessToken = GetStringValue("AzureDevOpsAccessToken"),
                AzureDevOpsProjectName = GetStringValue("AzureDevOpsProjectName"),
                AzureDevOpsProjectUrl = GetStringValue("AzureDevOpsProjectUrl"),
                EnableNonProdFunctionality = string.Compare(
                "true",
                Environment.GetEnvironmentVariable("EnableNonProdFunctionality", EnvironmentVariableTarget.Process),
                StringComparison.OrdinalIgnoreCase) == 0,
                MSTenantId = GetStringValue("MSTenantId"),
                PafUamiId = GetStringValue("PafUamiId"),
                PdmsBaseUrl = GetStringValue("PdmsBaseUrl"),
                PdmsResourceId = GetStringValue("PdmsResourceId"),
                MonitoringTenant = GetStringValue("MONITORING_TENANT"),
                MonitoringRole = GetStringValue("MONITORING_ROLE"),
                MetricAccount = GetStringValue("METRIC_ACCOUNT"),
                MetricPrefixName = GetStringValue("METRIC_PREFIX_NAME"),
                AppName = GetStringValue("App_Name"),
                AzureAppConfigEndpoint = GetStringValue("PAF_APP_CONFIG_ENDPOINT"),
                ShouldUseAADToken = Convert.ToBoolean(GetStringValue("ShouldUseAADToken")),
                AadClientId = GetStringValue("AadClientId"),
                AadClientSecret = GetStringValue("AadClientSecret")
            };
            return config;
        }

        /// <summary>
        /// Get the value associated with key.
        /// </summary>
        /// <param name="key">Value to retrieve.</param>
        /// <returns>Value associated with key.</returns>
        protected static string GetStringValue(string key)
        {
            return Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Process);
        }
    }
}
