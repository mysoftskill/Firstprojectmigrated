namespace Microsoft.PrivacyServices.CommandFeed.Service.Common.SecretClient
{
    using Microsoft.Azure.ComplianceServices.Common.SecretClient;

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
            return this.CreateKeyVaultClient(this.defaultKeyVaultBaseUrl);
        }

        /// <inheritdoc/>
        public IAzureKeyVaultClient CreateKeyVaultClient(string baseUrl)
        {
            return new AzureKeyVaultClient(baseUrl, new ManagedIdentityAccessToken());
        }
    }
}
