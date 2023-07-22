namespace Microsoft.PrivacyServices.AnaheimId.Config
{
    using System.Collections.Generic;

    /// <summary>
    /// Anaheim Id Config interface.
    /// </summary>
    public interface IAidConfig
    {
        /// <summary>
        /// Gets or sets deployment environment.
        /// </summary>
        DeploymentEnvironment DeploymentEnvironment { get; set; }

        /// <summary>
        /// PCF base url.
        /// E.g.:
        /// onebox=https://127.0.0.1:444/pcf/
        /// ci1=https://sf-pxsmockci1.api.account.microsoft-int.com/pcf/
        /// ci2=https://sf-pxsmockci2.api.account.microsoft-int.com/pcf/
        /// </summary>
        string PcfBaseUrl { get; set; }

        /// <summary>
        /// PCF AAD token target resource.
        /// prod: https://pcf.privacy.microsoft.com
        /// ppe: https://pcf.privacy.microsoft-ppe.com
        /// ci: https://pcf.privacy.microsoft-int.com
        /// </summary>
        string AadPcfTargetResource { get; set; }

        /// <summary>
        /// Authorization token client id. In case of MSI it is managed identity id.
        /// </summary>
        string ClientAppId { get; set; }

        /// <summary>
        /// Storage accounts.
        /// </summary>
        IEnumerable<QueueAccountInfo> AidQueuesStorageAccounts { get; set; }

        /// <summary>
        /// Monitoring storage accounts.
        /// </summary>
        IEnumerable<QueueAccountInfo> AidMonitoringQueuesStorageAccounts { get; set; }

        /// <summary>
        /// ONEBOX: Cert subject name for SNI auth.
        /// </summary>
        string OneBoxCertSubjectName { get; set; }

        /// <summary>
        /// ONEBOX: Aad authority.
        /// </summary>
        string OneBoxAadAuthority { get; set; }

        /// <summary>
        /// ONEBOX: Tenant id for SNI credentials.
        /// </summary>
        string OneBoxTenantId { get; set; }

        /// <summary>
        /// Anaheim UAMI.
        /// </summary>
        string AidUamiId { get; set; }

        /// <summary>
        /// AnaheimId eventhub namespace.
        /// </summary>
        string EventHubNamespace { get; set; }

        /// <summary>
        /// AnaheimId eventhub name.
        /// </summary>
        string EventHubName { get; set; }

        /// <summary>
        /// Storage Account Name
        /// </summary>
        string StorageAccountName { get; set; }
    }
}
