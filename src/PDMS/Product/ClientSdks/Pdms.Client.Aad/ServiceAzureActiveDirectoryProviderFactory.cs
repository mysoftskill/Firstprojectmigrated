namespace Microsoft.PrivacyServices.DataManagement.Client.AAD
{
    using System;
    using System.Security.Cryptography.X509Certificates;

    using Microsoft.Identity.Client;


    /// <summary>
    /// Azure active directory based authentication provider factory. This version should be used within a service.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ServiceAzureActiveDirectoryProviderFactory : IAuthenticationProviderFactory
    {
        private readonly string clientSecret;
        private readonly string clientId;
        private readonly X509Certificate2 clientCertificate;
        private readonly bool sendX5c;

        /// <summary>
        /// User access token for the Resource already obtained by the client application.
        /// </summary>
        private readonly string userAccessTokenForResource;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceAzureActiveDirectoryProviderFactory" /> class.
        /// </summary>
        /// <param name="clientId">The client id to use for authentication.</param>
        /// <param name="clientSecret">The client secret to use for authentication.</param>
        /// <param name="targetProd">Whether or not the client is targeting the production environment.</param>
        public ServiceAzureActiveDirectoryProviderFactory(string clientId, string clientSecret, bool targetProd)
        {
            this.clientSecret = clientSecret;
            this.clientId = clientId;
            this.RedirectUri = new Uri(Defaults.RedirectUri);
            this.ResourceId = Defaults.GetResourceId(clientId, targetProd);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceAzureActiveDirectoryProviderFactory" /> class.
        /// </summary>
        /// <param name="clientId">The client id to use for authentication.</param>
        /// <param name="clientCertificate">The client certificate to use for authentication.</param>
        /// <param name="targetProductionEnvironment">Whether or not the client should target the production environment.</param>
        /// <param name="sendX5c">Whether or not the client should send the entire public certificate to AAD. Enables certificate auto-rotation.</param>
        public ServiceAzureActiveDirectoryProviderFactory(string clientId, X509Certificate2 clientCertificate, bool targetProductionEnvironment, bool sendX5c = false)
        {
            this.clientCertificate = clientCertificate;
            this.clientId = clientId;
            this.RedirectUri = new Uri(Defaults.RedirectUri);
            this.ResourceId = Defaults.GetResourceId(clientId, targetProductionEnvironment);
            this.sendX5c = sendX5c;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceAzureActiveDirectoryProviderFactory" /> class.
        /// Should be used in scenarios - where the client app generates the user token for the resource, and passes it for authentication.
        /// </summary>
        /// <param name="userAccessTokenForResource">User access token for the Resource already obtained by the client application.</param>
        public ServiceAzureActiveDirectoryProviderFactory(string userAccessTokenForResource)
        {
            this.userAccessTokenForResource = userAccessTokenForResource;
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
        /// <returns>An authentication provider for the request.</returns>
        public IAuthenticationProvider CreateForClient()
        {
            if (this.clientSecret != null)
            {
                return new AzureActiveDirectoryProvider(this.clientSecret, this.clientId, this.ResourceId);
            }
            else
            {
                return new AzureActiveDirectoryProvider(this.clientCertificate, this.clientId, this.ResourceId, this.sendX5c);
            }
        }

        /// <summary>
        /// Creates a provider that can generate an access token for the current logged on user.
        /// </summary>
        /// <exception cref="InvalidOperationException">This authentication type is not supported for this provider.</exception>
        /// <returns>An authentication provider for the request.</returns>
        public IAuthenticationProvider CreateForCurrentUser()
        {
            throw new InvalidOperationException("This authentication type is not supported for this provider.");
        }

        /// <summary>
        /// Creates a provider that can generate an access token based on the user delegate token.
        /// </summary>
        /// <param name="userDelegateToken">The user delegate token to use for authentication.</param>
        /// <returns>An authentication provider for the request.</returns>
        public IAuthenticationProvider CreateForUserDelegate(string userDelegateToken)
        {
            var userAssertion = new UserAssertion(userDelegateToken, "urn:ietf:params:oauth:grant-type:jwt-bearer");

            if (this.clientSecret != null)
            {
                return new AzureActiveDirectoryProvider(this.clientSecret, this.clientId, userAssertion, this.ResourceId);
            }
            else
            {
                return new AzureActiveDirectoryProvider(this.clientCertificate, this.clientId, userAssertion, this.ResourceId, this.sendX5c);
            }
        }

        /// <summary>
        /// Creates a provider that can generate an user access token for the resource. 
        /// Should be used in scenarios - where the client app generates the user token for the resource, and passes it for authentication.
        /// </summary>
        /// <returns>An authentication provider for the request.</returns>
        public IAuthenticationProvider CreateForUserAccessTokenOnResource()
        {
            if (string.IsNullOrEmpty(this.userAccessTokenForResource))
            {
                throw new InvalidOperationException("User access token for the resource is null or empty.");
            }

            return new AzureActiveDirectoryProvider(this.userAccessTokenForResource);         
        }
    }
}