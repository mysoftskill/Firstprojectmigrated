namespace Microsoft.PrivacyServices.AzureFunctions.Common
{
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    using Microsoft.Identity.Client;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Provides methods for acquiring tokens for outbound requests to a resource using a client certificate.
    /// </summary>
    public class ClientSecretProvider : IAuthenticationProvider
    {
        private const string AuthorityStringFormat = "https://login.microsoftonline.com/{0}";
        private const string ComponentName = nameof(ClientSecretProvider);

        private readonly string clientId;
        private readonly string tenantId;
        private readonly string resource;
        private readonly X509Certificate2 cert;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientSecretProvider" /> class.
        /// </summary>
        /// <param name="clientId">Client id to authenticate.</param>
        /// <param name="tenantId">Tenant to authenticate with.</param>
        /// <param name="resource">Resource to authenticate.</param>
        /// <param name="cert">Client certificate.</param>
        public ClientSecretProvider(
            string clientId,
            string tenantId,
            string resource,
            X509Certificate2 cert)
        {
            // Token config
            this.clientId = clientId;
            this.tenantId = tenantId;
            this.resource = resource;
            this.cert = cert;
        }

        /// <summary>
        /// Gets the authentication token for a resource.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <returns>Access Token</returns>
        public async Task<string> GetAccessTokenAsync(ILogger logger)
        {
            logger.Information(ComponentName, $"Attempting to get access token for clientId: {this.clientId} and resource: {this.resource}");

            // Even if this is a console application here, a daemon application is a confidential client application
            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(this.clientId)
                    .WithCertificate(this.cert)
                    .WithAuthority(new Uri(string.Format(AuthorityStringFormat, this.tenantId)))
                    .Build();

            // With client credentials flows the scopes is ALWAYS of the shape "resource/.default", as the
            // application permissions need to be set statically (in the portal or by PowerShell), and then granted by
            // a tenant administrator.
            string[] scopes = new string[] { $"{this.resource}/.default" };

            Identity.Client.AuthenticationResult result = null;
            try
            {
                result = await app.AcquireTokenForClient(scopes).WithSendX5C(true).ExecuteAsync().ConfigureAwait(false);
                logger.Information(ComponentName, "Token acquired");
            }
            catch (MsalClientException ex) when (ex.Message.Contains("AADSTS70011"))
            {
                // Invalid scope. The scope has to be of the form "https://resourceurl/.default"
                // Mitigation: change the scope to be as expected
                logger.Error(ComponentName, "Scope provided is not supported");
            }

            return result.AccessToken;
        }
    }
}
