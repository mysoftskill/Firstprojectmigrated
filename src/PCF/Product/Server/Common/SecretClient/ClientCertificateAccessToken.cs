namespace Microsoft.PrivacyServices.CommandFeed.Service.Common.SecretClient
{
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Identity.Client;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    /// <summary>
    /// ClientAssertionCertificate key vault access token provider
    /// </summary>
    public class ClientCertificateAccessToken : IAzureKeyVaultAccessToken
    {
        private readonly X509Certificate2 clientCert;
        public ClientCertificateAccessToken(string azureClientId, X509Certificate2 clientCert)
        {
            this.clientCert = clientCert;
            this.CertificateThumbprint = clientCert?.Thumbprint;
            this.AppId = azureClientId;
        }

        /// <summary>
        /// Tenant ID where the token was acquired from
        /// </summary>
        public string TenantId { get; private set; }

        /// <summary>
        /// The App ID used to acquire the token
        /// </summary>
        /// <remarks>May not be populated if an App Id was not used to acquire the token.</remarks>
        public string AppId { get; }

        /// <summary>
        /// The CertificateThumbprint used to acquire the token
        /// </summary>
        /// <remarks>May not be populated if a CertificateThumbprint was not used to acquire the token.</remarks>
        public string CertificateThumbprint { get; }

        /// <inheritdoc/>
        public async Task<string> GetAccessTokenAsync(string authority, string resource, string scope)
        {
            var scopes = new[] { $"{resource}/.default" };

            var result = await ConfidentialCredential.GetTokenAsync(this.AppId, this.clientCert, new System.Uri(authority), scopes);

            // This property will be null if tenant information is not returned by the service.
            this.TenantId = result?.TenantId;

            return result?.AccessToken;
        }
    }
}
