namespace Microsoft.PrivacyServices.AzureFunctions.Common.Configuration
{
    /// <summary>
    /// Interface for configuration settings used for local execution.
    /// </summary>
    public interface IFunctionLocalConfiguration : IFunctionConfiguration
    {
        /// <summary>
        /// Gets the secret name for accessing ADO.
        /// </summary>
        string AdoSecretName { get; }

        /// <summary>
        /// Gets the certificate subject name used to authenticate clients.
        /// </summary>
        string CertificateSubjectName { get; }

        /// <summary>
        /// Gets the application id of the PAF client.
        /// This application is used to access Key Vaults in the INT environment.
        /// </summary>
        string PafClientId { get; }

        /// <summary>
        /// Gets the key vault URL used by PAF.
        /// </summary>
        string PafKeyVaultUrl { get; }

        /// <summary>
        /// Gets the function key value.
        /// </summary>
        string PafFunctionKey { get; }

        /// <summary>
        /// Gets the function key name.
        /// </summary>
        string PafFunctionKeyName { get; }

        /// <summary>
        /// Gets the function url.
        /// </summary>
        string PafFunctionUrl { get; }

        /// <summary>
        /// Gets the ID of the PDMS client.
        /// </summary>
        string PdmsClientId { get; }

        /// <summary>
        /// Gets the client certificate name for accessing PDMS.
        /// </summary>
        string PdmsCertName { get; }

        /// <summary>
        /// Gets the key vault URL used by PDMS.
        /// </summary>
        string PdmsKeyVaultUrl { get; }

        /// <summary>
        /// Gets the client certificate name for accessing "NGPVariantLinkingBot".
        /// </summary>
        string NGPVariantLinkingBotCertName { get; }

        /// <summary>
        /// Gets the client certificate subject name for accessing "NGPVariantLinkingBot".
        /// </summary>
        string NGPVariantLinkingBotCertSubjectName { get; }
    }
}
