using Microsoft.PrivacyServices.Common.Azure;

namespace CertInstaller
{
    /// <summary>
    /// Produces managed identity key vault clients.
    /// </summary>
    class AzureManagedIdentityKeyVaultClientFactory : IAzureKeyVaultClientFactory
    {
        private readonly string defaultKeyVaultBaseUrl;

        public AzureManagedIdentityKeyVaultClientFactory(string defaultKeyVaultBaseUrl)
        {
            this.defaultKeyVaultBaseUrl = defaultKeyVaultBaseUrl;
        }

        /// <inheritdoc/>
        public IAzureKeyVaultClient CreateDefaultKeyVaultClient()
        {
            return CreateKeyVaultClient(defaultKeyVaultBaseUrl, DualLogger.Instance);
        }

        /// <inheritdoc/>
        public IAzureKeyVaultClient CreateKeyVaultClient(string baseUrl, ILogger logger)
        {
            return new AzureKeyVaultClient(baseUrl, new ManagedIdentityAccessToken(), logger);
        }
    }
}
