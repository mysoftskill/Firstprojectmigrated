namespace Microsoft.PrivacyServices.AzureFunctions.Common.Configuration
{
    /// <summary>
    /// Defines all the configuration parameters used in the Azure Function
    /// </summary>
    public interface IBaseConfiguration
    {
        /// <summary>
        /// Monitoring Tenant: ADGCS-[Environment]-[Region]
        /// </summary>
        string MonitoringTenant { get; set; }

        /// <summary>
        /// Role: PAF.Function
        /// </summary>
        string MonitoringRole { get; set; }

        /// <summary>
        /// Geneva monitoring Account: [PreProd: "ADGCS_NonProdHotPath" Prod: "adgcsprod"]
        /// </summary>
        string MetricAccount { get; set; }

        /// <summary>
        /// Geneva Metric Name Prefix
        /// </summary>
        string MetricPrefixName { get; set; }

        /// <summary>
        /// App Name.
        /// </summary>
        string AppName { get; set; }

        /// <summary>
        /// AzureAppConfigEndpoint. File path in case of onebox run.
        /// </summary>
        string AzureAppConfigEndpoint { get; set; }
    }
}
