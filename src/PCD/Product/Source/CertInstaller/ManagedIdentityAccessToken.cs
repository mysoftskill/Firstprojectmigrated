namespace CertInstaller
{
    using Microsoft.Azure.Services.AppAuthentication;
    using System.Threading.Tasks;

    /// <summary>
    /// Managed Identity key vault access token provider
    /// </summary>
    public class ManagedIdentityAccessToken : IAzureKeyVaultAccessToken
    {
        private readonly AzureServiceTokenProvider tokenProvider;

        /// <summary>
        /// Initializes a new instance of <see cref="ManagedIdentityAccessToken"/>
        /// </summary>
        public ManagedIdentityAccessToken()
        {
            // This reads connection string from 'AzureServicesAuthConnectionString' environment variable.
            tokenProvider = new AzureServiceTokenProvider();
        }

        /// <summary>
        /// Tenant ID where the token was acquired from
        /// </summary>
        public string TenantId => tokenProvider.PrincipalUsed?.TenantId;

        /// <summary>
        /// The App ID used to acquire the token
        /// </summary>
        /// <remarks>May not be populated if an App Id was not used to acquire the token.</remarks>
        public string AppId => tokenProvider.PrincipalUsed?.AppId;

        /// <summary>
        /// The CertificateThumbprint used to acquire the token
        /// </summary>
        /// <remarks>May not be populated if a CertificateThumbprint was not used to acquire the token.</remarks>
        public string CertificateThumbprint => tokenProvider.PrincipalUsed?.CertificateThumbprint;

        /// <inheritdoc/>
        public Task<string> GetAccessTokenAsync(string authority, string resource, string scope)
        {
            return tokenProvider.KeyVaultTokenCallback(authority, resource, scope);
        }
    }
}
