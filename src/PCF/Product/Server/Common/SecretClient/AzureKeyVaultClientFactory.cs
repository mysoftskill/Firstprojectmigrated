namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.SecretClient;
    using System;
    using System.Security.Cryptography.X509Certificates;

    using Microsoft.Azure.ComplianceServices.Common.SecretClient;

    /// <summary>
    /// Produces key vault clients.
    /// </summary>
    public class AzureKeyVaultClientFactory : IAzureKeyVaultClientFactory
    {
        private readonly string clientId;
        private readonly X509Certificate2 clientCertificate;
        private readonly string defaultKeyVaultBaseUrl;

        public AzureKeyVaultClientFactory(string defaultKeyVaultBaseUrl, string clientId, X509Certificate2 certificate)
        {
            this.clientId = clientId;
            this.clientCertificate = certificate;
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
            return new AzureKeyVaultClient(baseUrl, new ClientCertificateAccessToken(this.clientId, this.clientCertificate));
        }
    }
}
