namespace Microsoft.PrivacyServices.CommandFeed.Service.Common.SecretClient
{
    using global::Azure.Core;
    using global::Azure.Identity;
    using System.Threading.Tasks;

    /// <summary>
    /// Managed Identity key vault access token provider
    /// </summary>
    public class ManagedIdentityAccessToken : IAzureKeyVaultAccessToken
    {
        /// <summary>
        /// Tenant ID where the token was acquired from
        /// </summary>
        public string TenantId { get; private set; }

        /// <summary>
        /// The App ID used to acquire the token
        /// </summary>
        /// <remarks>May not be populated if an App Id was not used to acquire the token.</remarks>
        public string AppId { get; private set; }

        /// <summary>
        /// The CertificateThumbprint used to acquire the token
        /// </summary>
        /// <remarks>May not be populated if a CertificateThumbprint was not used to acquire the token.</remarks>
        public string CertificateThumbprint { get; private set; }

        /// <inheritdoc/>
        public async Task<string> GetAccessTokenAsync(string authority, string resource, string scope)
        {
            var credential = new ManagedIdentityCredential();
            AccessToken accessToken = await credential.GetTokenAsync(new TokenRequestContext(new[] { resource + "/.default" })).ConfigureAwait(false);
            return accessToken.Token.ToString();
        }
    }
}
