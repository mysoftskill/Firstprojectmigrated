namespace Microsoft.PrivacyServices.AzureFunctions.Common.Configuration
{
    /// <summary>
    /// Configuration settings used for local execution.
    /// </summary>
    public class PafLocalConfiguration : PafConfiguration, IFunctionLocalConfiguration
    {
        /// <inheritdoc/>
        public string AdoSecretName { get; set; }

        /// <inheritdoc/>
        public string CertificateSubjectName { get; set; }

        /// <inheritdoc/>
        public string PafClientId { get; set; }

        /// <inheritdoc/>
        public string PafKeyVaultUrl { get; set; }

        /// <inheritdoc/>
        public string PafFunctionKey { get; set; }

        /// <inheritdoc/>
        public string PafFunctionKeyName { get; set; }

        /// <inheritdoc/>
        public string PafFunctionUrl { get; set; }

        /// <inheritdoc/>
        public string PdmsClientId { get; set; }

        /// <inheritdoc/>
        public string PdmsCertName { get; set; }

        /// <inheritdoc/>
        public string PdmsKeyVaultUrl { get; set; }

        /// <inheritdoc/>
        public string NGPVariantLinkingBotCertName { get; set; }

        /// <inheritdoc/>
        public string NGPVariantLinkingBotCertSubjectName { get; set; }
    }
}
