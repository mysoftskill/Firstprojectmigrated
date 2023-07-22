namespace Microsoft.PrivacyServices.AzureFunctions.Common
{
    using System.Threading.Tasks;
    using global::Azure.Core;
    using global::Azure.Identity;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Provides methods for acquiring tokens using a Managed Identity.
    /// </summary>
    public class ManagedIdentityProvider : IAuthenticationProvider
    {
        private const string ComponentName = nameof(ManagedIdentityProvider);

        private readonly string clientId;
        private readonly string resource;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedIdentityProvider" /> class.
        /// </summary>
        /// <param name="clientId">Id managed identity.</param>
        /// <param name="resource">Resource being accessed.</param>
        public ManagedIdentityProvider(string clientId, string resource)
        {
            this.clientId = clientId;
            this.resource = resource;
        }

        /// <summary>
        /// Gets the authentication token for a resource.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <returns>Access Token</returns>
        public Task<string> GetAccessTokenAsync(ILogger logger)
        {
            logger.Information(ComponentName, $"Attempting to get access token for identity: {this.clientId} and resource {this.resource}");

            var tokenCredential = new ManagedIdentityCredential(this.clientId);
            var accessToken = tokenCredential.GetTokenAsync(
                new TokenRequestContext(scopes: new string[] { this.resource + "/.default" }) { });

            return Task.FromResult(accessToken.Result.Token);
        }
    }
}
