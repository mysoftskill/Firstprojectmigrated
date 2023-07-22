namespace Microsoft.PrivacyServices.DataManagement.DataGridService
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.DataPlatform.DataDiscovery;
    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;

    /// <summary>
    /// Provides methods for creating data discovery clients.
    /// </summary>
    public class DataDiscoveryClientFactory : IDataDiscoveryClientFactory
    {
        private readonly IDataGridConfiguration configuration;
        private readonly ITokenProvider tokenProvider;
        private readonly ISessionFactory sessionFactory;
        private readonly AuthenticatedPrincipal authenticatedPrincipal;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataDiscoveryClientFactory" /> class.
        /// </summary>
        /// <param name="configuration">The configuration for this component.</param>
        /// <param name="tokenProvider">The token provider.</param>
        /// <param name="sessionFactory">The session factory.</param>
        /// <param name="authenticatedPrincipal">The current authenticated principal.</param>
        public DataDiscoveryClientFactory(
            IDataGridConfiguration configuration,
            ITokenProvider tokenProvider,
            ISessionFactory sessionFactory,
            AuthenticatedPrincipal authenticatedPrincipal)
        {
            this.configuration = configuration;
            this.tokenProvider = tokenProvider;
            this.sessionFactory = sessionFactory;
            this.authenticatedPrincipal = authenticatedPrincipal;
        }

        /// <summary>
        /// Creates a data discovery client for the current authenticated user or app id.
        /// </summary>
        /// <returns>The client.</returns>
        public async Task<IDataDiscoveryClient> CreateClientAsync()
        {
            string token;
            // Task 1537476: [PDMS] Remove code that handles old app id
            // NOTE: We authenticate as App with new 1st Party App Ids
            if (this.configuration.AuthenticateWithFirstPartyAppId == true)
            {
                token = await this.tokenProvider.AcquireTokenAsync(this.configuration.ResourceId, this.sessionFactory).ConfigureAwait(false);
            }
            else
            {
                token = await this.tokenProvider.AcquireTokenAsync(this.authenticatedPrincipal, this.configuration.ResourceId, this.sessionFactory).ConfigureAwait(false);
            }

            var endpoint = new Uri(this.configuration.EndpointUrl);
            var dataDiscoveryClient = new DataDiscoveryClient(endpoint, this.configuration.ClientKey);
            await dataDiscoveryClient.SetCredentialsAsync(token).ConfigureAwait(false);

            return dataDiscoveryClient;
        }
    }
}