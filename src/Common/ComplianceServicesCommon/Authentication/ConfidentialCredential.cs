namespace Microsoft.Azure.ComplianceServices.Common
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using global::Azure.Core;
    using Microsoft.Identity.Client;
    using Microsoft.PrivacyServices.Common.Azure;

    public class ConfidentialCredential : TokenCredential
    {
        private readonly IConfidentialClientApplication confClient;

        // Empty constructor for use in unit tests
        public ConfidentialCredential() { }

        public ConfidentialCredential(string tenantId, string clientId, X509Certificate2 certificate, ILogger logger = null)
        {
            var builder = ConfidentialClientApplicationBuilder.Create(clientId)
                            .WithTenantId(tenantId)
                            .WithCertificate(certificate);

            confClient = builder.BuildCustomizedConfClient(clientId, logger);
        }

        public ConfidentialCredential(string clientId, X509Certificate2 certificate, Uri authority, ILogger logger = null)
        {
            var builder = ConfidentialClientApplicationBuilder.Create(clientId)
                            .WithAuthority(authority)
                            .WithCertificate(certificate);

            confClient = builder.BuildCustomizedConfClient(clientId, logger);
        }

        public ConfidentialCredential(string clientId, string clientSecret, Uri authority, ILogger logger = null)
        {
            var builder = ConfidentialClientApplicationBuilder.Create(clientId)
                            .WithAuthority(authority)
                            .WithClientSecret(clientSecret);

            confClient = builder.BuildCustomizedConfClient(clientId, logger);
        }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return GetTokenAsync(requestContext, cancellationToken).GetAwaiter().GetResult();
        }

        public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            var authResponse = await confClient.AcquireTokenForClient(requestContext.Scopes).WithSendX5C(true).ExecuteAsync().ConfigureAwait(false);

            return new AccessToken(authResponse.AccessToken, authResponse.ExpiresOn);
        }

        /// <summary>
        /// Gets an AAD access token using MSAL
        /// </summary>
        /// <param name="scopes">scopes of the MSAL request Example: {resource}/.default</param>
        /// <returns>AuthenticationResult</returns>
        public Task<AuthenticationResult> GetTokenAsync(string[] scopes)
        {
            return confClient.AcquireTokenForClient(scopes).WithSendX5C(true).ExecuteAsync();
        }

        /// <summary>
        /// Gets an AAD access token using MSAL
        /// This method does not utilze token caching, for that create an instance of this class
        /// and call GetTokenAsync
        /// </summary>
        /// <param name="scopes">scopes of the MSAL request Example: {resource}/.default</param>
        /// <param name="assertion">user assertion used for getting a token on behalf of</param>
        /// <returns>AuthenticationResult</returns>
        public Task<AuthenticationResult> GetTokenOnBehalfOfAsync(string[] scopes, UserAssertion assertion)
        {
            return confClient.AcquireTokenOnBehalfOf(scopes, assertion).WithSendX5C(true).ExecuteAsync();
        }

        /// <summary>
        /// Gets an AAD access token using MSAL
        /// This method does not utilze token caching, for that create an instance of this class
        /// and call GetTokenAsync
        /// </summary>
        /// <param name="clientId">client application id</param>
        /// <param name="cert">client cert</param>
        /// <param name="authority">Authority request</param>
        /// <param name="scopes">scopes of the MSAL request Example: {resource}/.default</param>
        /// <param name="logger">Optional logger</param>
        /// <returns>AuthenticationResult</returns>
        public static Task<AuthenticationResult> GetTokenAsync(string clientId, X509Certificate2 cert, Uri authority, string[] scopes, ILogger logger = null)
        {
            var builder = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithCertificate(cert)
                .WithAuthority(authority);

            var app = builder.BuildCustomizedConfClient(clientId, logger);
            return app.AcquireTokenForClient(scopes).WithSendX5C(true).ExecuteAsync();
        }

        /// <summary>
        /// Gets an AAD access token using MSAL
        /// This method does not utilze token caching, for that create an instance of this class
        /// and call GetTokenAsync
        /// </summary>
        /// <param name="clientId">client application id</param>
        /// <param name="clientSecret">client secret</param>
        /// <param name="authority">Authority request</param>
        /// <param name="scopes">scopes of the MSAL request Example: {resource}/.default</param>
        /// <param name="logger">Optional logger</param>
        /// <returns>AuthenticationResult</returns>
        public static Task<AuthenticationResult> GetTokenAsync(string clientId, string clientSecret, Uri authority, string[] scopes, ILogger logger = null)
        {
            var builder = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithClientSecret(clientSecret)
                .WithAuthority(authority);

            var app = builder.BuildCustomizedConfClient(clientId, logger);
            return app.AcquireTokenForClient(scopes).WithSendX5C(true).ExecuteAsync();
        }
    }

    public static class ConfidentialClientApplicationBuilderExtensions
    {
        private static ILogger Logger;

        // A list of AppIds that should use regional ESTS endpoint by specifiying a short azure region name (e.g. eastus2).
        // See below link for details:
        // https://identitydocs.azurewebsites.net/static/v2/msal-net-regional-adoption.html
        private static readonly List<string> RegionalEstsAppIds = new List<string> {
            "705363A0-5817-47FB-BA32-59F47CE80BB7",  // PXS-INT
            "877310D5-C81C-45D8-BA2D-BF935665A43A", // PXS
            "A9FC952E-88AE-450C-BF4F-D66026A44D99" // PXS-EUDB
        };

        public static IConfidentialClientApplication BuildCustomizedConfClient(this ConfidentialClientApplicationBuilder builder, string clientId, ILogger logger)
        {
            if (RegionalEstsAppIds.Any(appId => appId == clientId.ToUpperInvariant()))
            {
                builder.WithAzureRegion(Environment.GetEnvironmentVariable("MONITORING_DATACENTER") ?? ConfidentialClientApplication.AttemptRegionDiscovery);
                if (logger != null)
                {
                    // Temporary logging to help trouble shooting ESTSR issue. Will be removed.
                    Logger = logger;
                    // builder.WithLogging(LogMsalTraces, LogLevel.Verbose, enablePiiLogging: true, enableDefaultPlatformLogging: true);
                }
            }

            return builder.Build();
        }

        private static void LogMsalTraces(LogLevel level, string message, bool _)
        {
            Logger?.Information("MSAL", $"Process: {Process.GetCurrentProcess().ProcessName}, Level: {level}, Message {message}");
        }
    }
}
