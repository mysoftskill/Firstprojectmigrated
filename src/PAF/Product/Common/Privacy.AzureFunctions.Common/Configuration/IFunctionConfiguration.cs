namespace Microsoft.PrivacyServices.AzureFunctions.Common.Configuration
{
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Defines all the configuration parameters used in the Azure Function
    /// </summary>
    public interface IFunctionConfiguration : IBaseConfiguration
    {
        /// <summary>
        /// Gets the AME tenant id.
        /// </summary>
        string AMETenantId { get; }

        /// <summary>
        /// Gets AzureDevOps PAT Token for accessing ADO
        /// </summary>
        string AzureDevOpsAccessToken { get; set; }

        /// <summary>
        /// Gets AzureDevOps Project Url
        /// </summary>
        string AzureDevOpsProjectUrl { get; }

        /// <summary>
        /// Gets AzureDevOps Project Name
        /// </summary>
        string AzureDevOpsProjectName { get; }

        /// <summary>
        /// Gets a value indicating whether or not to enable special non-production features such as deleting a work item.
        /// </summary>
        bool EnableNonProdFunctionality { get; }

        /// <summary>
        /// Gets the MS tenant id.
        /// </summary>
        string MSTenantId { get; }

        /// <summary>
        /// Gets Managed Identity Id for the Azure Function
        /// </summary>
        string PafUamiId { get; }

        /// <summary>
        /// Gets PdmsResourceId
        /// </summary>
        string PdmsResourceId { get; }

        /// <summary>
        /// Gets PdmsBaseUrl
        /// </summary>
        string PdmsBaseUrl { get; }

        /// <summary>
        /// Gets AadClientId
        /// </summary>
        string AadClientId { get; }

        /// <summary>
        /// Gets or sets the Azure Active Directory client certificate.
        /// </summary>
        X509Certificate2 AadClientCert { get; set; }

        /// <summary>
        /// Boolean flag to check if AAD token should be used for PAF
        /// </summary>
        bool ShouldUseAADToken { get; }

        /// <summary>
        /// Gets or sets the Azure Active Directory client secret.
        /// </summary>
        string AadClientSecret { get; set; }

    }
}
