namespace Microsoft.PrivacyServices.DataManagement.Common.Authentication
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IdentityModel.Tokens;
    using System.Linq;
    using System.Security.Claims;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Identity.Client;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.FileSystem.KeyVault;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;

    /// <summary>
    /// Provides methods for acquiring AAD tokens for outbound requests.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public sealed class TokenProvider : ITokenProvider, IDisposable
    {
        private readonly ITokenProviderConfig configuration;
        private readonly X509Certificate2 clientCertificate;
        private readonly IEventWriterFactory eventWriterFactory;
        private readonly ConfidentialCredential credsClient;
        private readonly IAppConfiguration appConfig;

        // Task 1537476: [PDMS] Remove code that handles MS Tenant app id

        // MS Tenant Resource App Ids
        private const string PdmsIntAppId = "364e9b3d-cb8c-45b7-9e78-8eeb5c9672f7";
        private const string PdmsIntResourceId = "https://management.privacy.microsoft-int.com";
        private const string PdmsProdAppId = "05bff9ab-0118-4731-8890-468948eba2e8";
        private const string PdmsProdResourceId = "https://management.privacy.microsoft.com";

        // List of MS tenant PDMS resource urls/ids
        private readonly IList<string> msAudiences = new List<string>()
        {
            PdmsProdResourceId,
            PdmsProdAppId,
            PdmsIntResourceId,
            PdmsIntAppId
        };

        // Credential providers for the old App Ids
        private readonly ConfidentialCredential oldPdmsIntCredsClient;
        private readonly ConfidentialCredential oldPdmsProdCredsClient;

        // TODO: End Cleanup

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenProvider" /> class.
        /// </summary>
        /// <param name="configuration">The configuration for this component.</param>
        /// <param name="azureKeyVaultReader">The secure file system component.</param>
        /// <param name="eventWriterFactory">The event writer factory.</param>
        /// <param name="appConfig"></param>
        public TokenProvider(ITokenProviderConfig configuration, IAzureKeyVaultReader azureKeyVaultReader, IEventWriterFactory eventWriterFactory, IAppConfiguration appConfig)
        {
            this.configuration = configuration;
            this.clientCertificate = azureKeyVaultReader.GetCertificateByNameAsync(configuration.KeyVaultCertificateName, includePrivateKey: true).GetAwaiter().GetResult();
            this.eventWriterFactory = eventWriterFactory;
            this.appConfig = appConfig;

            this.credsClient = new ConfidentialCredential(this.configuration.ClientId, clientCertificate, new Uri(this.configuration.Authority));

            // Task 1537476: [PDMS] Remove code that handles MS Tenant app id
            this.oldPdmsIntCredsClient = new ConfidentialCredential(PdmsIntAppId, clientCertificate, new Uri(this.configuration.Authority));
            this.oldPdmsProdCredsClient = new ConfidentialCredential(PdmsProdAppId, clientCertificate, new Uri(this.configuration.Authority));

            // ECR Drill
            this.eventWriterFactory.Trace(nameof(TokenProvider), this.clientCertificate.ToLogMessage("PDMS", "The PDMS TokenProvider certificate."));
        }

        /// <summary>
        /// Takes the current token for the user and converts it into a token for an external service.
        /// </summary>
        /// <param name="principal">The principal whose token needs to be converted.</param>
        /// <param name="resourceId">The target resource id.</param>
        /// <param name="sessionFactory">The sessionFactory.</param>
        /// <returns>The token for the external service.</returns>
        public async Task<string> AcquireTokenAsync(AuthenticatedPrincipal principal, string resourceId, ISessionFactory sessionFactory)
        {
            var claimsPrincipal = principal.ClaimsPrincipal;

            var identity = claimsPrincipal.Identities.First();

            var bootstrapContext = identity.BootstrapContext as BootstrapContext;

            if (bootstrapContext is null && identity.BootstrapContext is string)
            {
                bootstrapContext = new BootstrapContext(identity.BootstrapContext as string);
            }

            var scopes = new[] { $"{resourceId}/.default" };

            var userAssertion = new UserAssertion(bootstrapContext.Token, this.configuration.AssertionType);

            var credsClient = this.credsClient;

            if (await appConfig.IsFeatureFlagEnabledAsync(FeatureNames.PDMS.DualAppIdSupport).ConfigureAwait(false))
            {
                // Task 1537476: [PDMS] Remove code that handles MS Tenant app id
                // During the transition period, we need to handle calls using the old MS Tenant Resource Ids;
                // The client id use in the on-behalf-of call must be the correct one for the resource id in the user assertion claim.
                // If the audience is one of the MS resource ids, change the ClientCredentials in the OBO call to the corresponding MS Tenant app id.
                var audience = claimsPrincipal.FindFirst("aud").Value ?? string.Empty;
                if (msAudiences.Contains(audience))
                {
                    credsClient = (audience == PdmsIntResourceId || audience == PdmsIntAppId) ? oldPdmsIntCredsClient : oldPdmsProdCredsClient;

                    var issuer = claimsPrincipal.FindFirst("iss")?.Value ?? string.Empty;
                    var appId = claimsPrincipal.FindFirst("appId").Value ?? string.Empty;

                    string userName =
                        claimsPrincipal.FindFirst(ClaimTypes.Upn) != null ?
                        claimsPrincipal.FindFirst(ClaimTypes.Upn).Value :
                        claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value;

                    // Log clients using MS Tenant app ids
                    this.eventWriterFactory.Trace(nameof(TokenProvider),
                        $"AcquireTokenAsync: issuer: {issuer}, audience: {audience}, appid: {appId}, userName: {userName}");
                }
            }

            var result = await sessionFactory.InstrumentAsync<AuthenticationResult, MsalException>(
                        "TokenProvider.AcquireTokenAsync",
                        SessionType.Outgoing,
                        () => credsClient.GetTokenOnBehalfOfAsync(scopes, userAssertion));

            return result.AccessToken;
        }

        /// <summary>
        /// Gets the authentication token for a given resource using the default configuration.
        /// </summary>
        /// <param name="resourceId">The target resource id.</param>
        /// <param name="sessionFactory">The sessionFactory.</param>
        /// <returns>The token for the external service.</returns>
        public async Task<string> AcquireTokenAsync(string resourceId, ISessionFactory sessionFactory)
        {
            var scopes = new[] { $"{resourceId}/.default" };

            var result = await sessionFactory.InstrumentAsync<AuthenticationResult, MsalException>(
                "TokenProvider.AcquireTokenAsync",
                SessionType.Outgoing,
                () => this.credsClient.GetTokenAsync(scopes));

            return result.AccessToken;
        }

        /// <summary>
        /// Dispose of internal resources.
        /// </summary>
        public void Dispose()
        {
            this.clientCertificate.Dispose();
        }
    }
}
