namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Authentication
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;

    using Microsoft.Graph;
    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;

    /// <summary>
    /// A factory for creating graph clients. This is expected to be a singleton.
    /// </summary>
    /// <remarks>This is excluded from code coverage because it is not possible to mock the authentication classes.</remarks>
    [ExcludeFromCodeCoverage]
    public sealed class GraphServiceClientFactory : IGraphServiceClientFactory, IDisposable
    {
        private readonly ITokenProviderConfig tokenProviderConfig;
        private readonly IHttpProvider httpProvider;
        private readonly ITokenProvider tokenProvider;
        private const string GraphApiResourceId = "https://graph.microsoft.com";

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphServiceClientFactory" /> class.
        /// </summary>
        /// <param name="tokenProviderConfig">The token provider config.</param>
        /// <param name="tokenProvider">The token provider.</param>
        public GraphServiceClientFactory(
            ITokenProviderConfig tokenProviderConfig,
            ITokenProvider tokenProvider)
        {
            this.tokenProviderConfig = tokenProviderConfig;      
            this.tokenProvider = tokenProvider;
            this.httpProvider = new HttpProvider(); // Keep a single http client for the life of the service.      
        }
        
        /// <summary>
        /// Creates a graph client from the authenticated principal.
        /// </summary>
        /// <param name="principal">The principal.</param>
        /// <param name="sessionFactory">The sessionFactory.</param>
        /// <returns>The graph client.</returns>
        public IGraphServiceClient Create(AuthenticatedPrincipal principal, ISessionFactory sessionFactory)
        {
            Task<string> token = this.tokenProvider.AcquireTokenAsync(principal, GraphApiResourceId, sessionFactory); // Ensure we get the token only once.

            return new GraphServiceClient(
                new DelegateAuthenticationProvider(async (requestMessage) =>
                {
                    var tokenResult = await token.ConfigureAwait(false);
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue(this.tokenProviderConfig.Scheme, tokenResult);
                }),
                this.httpProvider);
        }

        /// <summary>
        /// Dispose of internal resources.
        /// </summary>
        public void Dispose()
        {
            this.httpProvider.Dispose();
        }
    }
}