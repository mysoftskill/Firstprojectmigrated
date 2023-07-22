namespace Microsoft.PrivacyServices.DataManagement.Client.AAD
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Net.Http.Headers;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Identity.Client;

    /// <summary>
    /// Implements the authentication provider interface using AzureActiveDirectory (AAD) for authentication.
    /// </summary>
    /// <remarks>
    /// This is excluded from code coverage because it contacts external dependencies.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    public class AzureActiveDirectoryProvider : IAuthenticationProvider
    {
        private const string Scheme = "Bearer";
        private const string Authority = "https://login.microsoftonline.com/microsoft.onmicrosoft.com";

        private readonly string clientId;
        private readonly string clientSecret;
        private readonly X509Certificate2 clientCertificate;
        private readonly UserAssertion userAssertion;
        private readonly Uri redirectUri;
        private readonly string resourceId;
        private readonly string userAccessTokenForResource;
        private readonly bool sendX5c;

        private readonly IPublicClientApplication pubClient;
        private readonly IConfidentialClientApplication confClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureActiveDirectoryProvider" /> class.
        /// </summary>
        /// <param name="clientSecret">The client secret.</param>
        /// <param name="clientId">The client app id</param>
        /// <param name="resourceId">The resource id for the request.</param>
        internal AzureActiveDirectoryProvider(string clientSecret, string clientId, string resourceId)
        {
            this.clientSecret = clientSecret;
            this.clientId = clientId;
            this.resourceId = resourceId;

            this.confClient = ConfidentialClientApplicationBuilder.Create(this.clientId)
                    .WithClientSecret(this.clientSecret)
                    .WithAuthority(Authority)
                    .Build();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureActiveDirectoryProvider" /> class.
        /// </summary>
        /// <param name="clientCertificate">The client certificate.</param>
        /// <param name="clientId">The client app id</param>
        /// <param name="resourceId">The resource id for the request.</param>
        /// <param name="sendX5c">Send the x509 public certificate to the AAD service. Enables automatic certificate rotation.</param>
        internal AzureActiveDirectoryProvider(X509Certificate2 clientCertificate, string clientId, string resourceId, bool sendX5c)
        {
            this.clientCertificate = clientCertificate;
            this.clientId = clientId;
            this.resourceId = resourceId;
            this.sendX5c = sendX5c;

            this.confClient = ConfidentialClientApplicationBuilder.Create(this.clientId)
                    .WithCertificate(clientCertificate)
                    .WithAuthority(Authority)
                    .Build();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureActiveDirectoryProvider" /> class.
        /// </summary>
        /// <param name="clientSecret">The client credentials.</param>
        /// <param name="clientId">The client app id</param>
        /// <param name="userAssertion">The user assertion.</param>
        /// <param name="resourceId">The resource id for the request.</param>
        internal AzureActiveDirectoryProvider(string clientSecret, string clientId, UserAssertion userAssertion, string resourceId)
            : this(clientSecret, clientId, resourceId)
        {
            this.userAssertion = userAssertion;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureActiveDirectoryProvider" /> class.
        /// </summary>
        /// <param name="clientCertificate">The client certificate. </param>
        /// <param name="clientId">The client app id</param>
        /// <param name="userAssertion">The user assertion.</param>
        /// <param name="resourceId">The resource id for the request.</param>
        /// <param name="sendX5c">Send the X509 public certificate to the AAD authentication service. Enables certificate auto-rotation.</param>
        internal AzureActiveDirectoryProvider(X509Certificate2 clientCertificate, string clientId, UserAssertion userAssertion, string resourceId, bool sendX5c)
            : this(clientCertificate, clientId, resourceId, sendX5c)
        {
            this.userAssertion = userAssertion;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureActiveDirectoryProvider" /> class.
        /// </summary>
        /// <param name="redirectUri">The redirect uri for authentication.</param>
        /// <param name="clientId">The client id.</param>
        /// <param name="resourceId">The resource id for the request.</param>
        internal AzureActiveDirectoryProvider(Uri redirectUri, string clientId, string resourceId)
        {
            this.redirectUri = redirectUri;
            this.clientId = clientId;
            this.resourceId = resourceId;

            this.pubClient = PublicClientApplicationBuilder.Create(clientId)
                    .WithRedirectUri(this.redirectUri.ToString())
                    .WithAuthority(Authority)
                    .Build();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureActiveDirectoryProvider" /> class.
        /// </summary>
        /// <param name="userAccessTokenForResource">User access token for resource.</param>
        internal AzureActiveDirectoryProvider(string userAccessTokenForResource)
        {
            this.userAccessTokenForResource = userAccessTokenForResource;
        }

        /// <summary>
        /// Acquires an AAD token.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A header with the appropriate scheme and value set.</returns>
        public async Task<AuthenticationHeaderValue> AcquireTokenAsync(CancellationToken cancellationToken)
        {
            try
            {
                // If the caller has already obtained a user access token for the resource, directly return it
                if (!string.IsNullOrEmpty(this.userAccessTokenForResource))
                {
                    return new AuthenticationHeaderValue(Scheme, this.userAccessTokenForResource);
                }

                // Else, obtain the access token from AAD
                var scopes = new[] { $"{this.resourceId}/.default" };

                AuthenticationResult result;

                if (this.clientSecret != null || this.clientCertificate != null)
                {
                    if (this.userAssertion == null)
                    {
                        result = await this.confClient.AcquireTokenForClient(scopes).WithSendX5C(this.sendX5c).ExecuteAsync();
                    }
                    else
                    {
                        result = await this.confClient.AcquireTokenOnBehalfOf(scopes, this.userAssertion).WithSendX5C(this.sendX5c).ExecuteAsync();
                    }
                }
                else if (this.redirectUri != null)
                {
                    var accounts = await this.pubClient.GetAccountsAsync();
                    try
                    {
                        result = await this.pubClient.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                               .ExecuteAsync();
                    }
                    catch (MsalUiRequiredException)
                    {
                        // Try to get the token silently. If that fails, prompt for it.
                        result = await this.pubClient.AcquireTokenInteractive(scopes).ExecuteAsync();
                    }
                }
                else
                {
                    throw new ArgumentNullException("Must set either client credential or user credential.");
                }

                return new AuthenticationHeaderValue(Scheme, result.AccessToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }
    }
}
