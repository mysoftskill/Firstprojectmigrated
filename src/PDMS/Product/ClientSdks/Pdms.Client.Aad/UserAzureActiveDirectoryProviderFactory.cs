namespace Microsoft.PrivacyServices.DataManagement.Client.AAD
{
    using System;

    /// <summary>
    /// Azure active directory based authentication provider factory. This version should be used within a client/app.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class UserAzureActiveDirectoryProviderFactory : IAuthenticationProviderFactory
    {        
        private readonly string clientId;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserAzureActiveDirectoryProviderFactory" /> class.
        /// </summary>
        /// <param name="clientId">The client id to use for authentication.</param> 
        /// <param name="targetProductionEnvironment">Whether or not the client should target the production environment.</param>
        public UserAzureActiveDirectoryProviderFactory(string clientId, bool targetProductionEnvironment)
        {
            this.clientId = clientId;
            this.RedirectUri = new Uri(Defaults.RedirectUri);
            this.ResourceId = Defaults.GetResourceId(clientId, targetProductionEnvironment);
        }

        /// <summary>
        /// Gets or sets the redirect uri for user based authentication. Do not set if you would like to use the default value.
        /// </summary>
        public Uri RedirectUri { private get; set; }

        /// <summary>
        /// Gets or sets the resource id. Do not set if you would like to use the default value.
        /// </summary>
        public string ResourceId { private get; set; }

        /// <summary>
        /// Creates a provider that can generate an access token for a client call (i.e. no user involved).
        /// </summary>
        /// <exception cref="InvalidOperationException">This authentication type is not supported for this provider.</exception>
        /// <returns>An authentication provider for the request.</returns>
        public IAuthenticationProvider CreateForClient()
        {
            throw new InvalidOperationException("This authentication type is not supported for this provider.");
        }

        /// <summary>
        /// Creates a provider that can generate an access token for the current logged on user.
        /// </summary>
        /// <returns>An authentication provider for the request.</returns>
        public IAuthenticationProvider CreateForCurrentUser()
        {
            return new AzureActiveDirectoryProvider(this.RedirectUri, this.clientId, this.ResourceId);
        }

        /// <summary>
        /// Creates a provider that can generate an access token for based on the user delegate token.
        /// </summary>
        /// <param name="userDelegateToken">The user delegate token to use for authentication.</param>
        /// <exception cref="InvalidOperationException">This authentication type is not supported for this provider.</exception>
        /// <returns>An authentication provider for the request.</returns>
        public IAuthenticationProvider CreateForUserDelegate(string userDelegateToken)
        {
            throw new InvalidOperationException("This authentication type is not supported for this provider.");
        }

        /// <summary>
        /// Creates a provider that can generate an user access token for the resource. 
        /// Should be used in scenarios - where the client app generates the user token for the resource, and passes it for authentication.
        /// </summary>
        /// <exception cref="InvalidOperationException">This authentication type is not supported for this provider.</exception>
        /// <returns>An authentication provider for the request.</returns>
        public IAuthenticationProvider CreateForUserAccessTokenOnResource()
        {
            throw new InvalidOperationException("This authentication type is not supported for this provider.");       
        }
    }
}
