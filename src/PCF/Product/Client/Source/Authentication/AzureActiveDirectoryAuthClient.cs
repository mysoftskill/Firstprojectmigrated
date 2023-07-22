namespace Microsoft.PrivacyServices.CommandFeed.Client.Authentication
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    using Microsoft.Identity.Client;

    /// <summary>
    /// Azure active directory client implementation.
    /// </summary>
    internal class AzureActiveDirectoryAuthClient : IAuthClient
    {
        private readonly string scheme = "Bearer";

        private readonly string authority;
        private readonly string resourceId;

        private readonly CommandFeedLogger logger;

        private readonly string clientId;
        private readonly string clientSecret;
        private readonly X509Certificate2 clientCertificate;
        private readonly IConfidentialClientApplication confidentialClientApplication;

        /// <summary>
        /// Construct AAD AuthClient with client certificate
        /// </summary>
        /// <param name="clientId">The AAD App ID.</param>
        /// <param name="clientCertificate">The certificate used to authenticate this App to AAD.</param>
        /// <param name="azureRegion">The Azure Region to be used for auth</param>
        /// <param name="logger">The command feed logger.</param>
        /// <param name="endpointConfiguration">The endpoint configuration</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "algorithm")]
        public AzureActiveDirectoryAuthClient(string clientId, X509Certificate2 clientCertificate, string azureRegion, CommandFeedLogger logger, CommandFeedEndpointConfiguration endpointConfiguration)
            : this(logger, endpointConfiguration)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                throw new ArgumentNullException(nameof(clientId));
            }

            if (clientCertificate == null)
            {
                throw new ArgumentNullException(nameof(clientCertificate));
            }

            if (!clientCertificate.HasPrivateKey)
            {
                throw new ArgumentException("Client certificate must have private key", nameof(clientCertificate));
            }

            try
            {
                // Throws if there is an error.
                var algorithm = clientCertificate.PrivateKey;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"The given client certificate does not have an accessible private key. Does the current account have access? Please see the inner exception for details.", ex);
            }

            this.clientId = clientId;
            this.clientCertificate = clientCertificate;
            
            var confAppBuidler = ConfidentialClientApplicationBuilder.Create(this.clientId)
                .WithAuthority(endpointConfiguration.AadAuthority)
                .WithCertificate(this.clientCertificate);
            if (!string.IsNullOrEmpty(azureRegion))
            {
                confAppBuidler.WithAzureRegion(azureRegion);
            }

            this.confidentialClientApplication = confAppBuidler.Build();
        }

        /// <summary>
        /// Enables the "sendx5c" flag when doing certificate based auth to AAD. Enables SNI auth.
        /// </summary>
        public bool UseX5cAuthentication { get; set; }

        /// <summary>
        /// Construct AAD AuthClient with client secret
        /// </summary>
        /// <param name="clientId">The AAD App ID.</param>
        /// <param name="clientSecret">The AAD App Key used to authenticate</param>
        /// <param name="azureRegion">The Azure Region to be used for auth</param>
        /// <param name="logger">The command feed logger.</param>
        /// <param name="endpointConfiguration">The endpoint configuration, optionally.</param>
        public AzureActiveDirectoryAuthClient(string clientId, string clientSecret, string azureRegion, CommandFeedLogger logger, CommandFeedEndpointConfiguration endpointConfiguration)
            : this(logger, endpointConfiguration)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                throw new ArgumentNullException(nameof(clientId));
            }

            this.clientId = clientId;
            this.clientSecret = clientSecret ?? throw new ArgumentNullException(nameof(clientSecret));

            var confAppBuidler = ConfidentialClientApplicationBuilder.Create(this.clientId)
                .WithAuthority(endpointConfiguration.AadAuthority)
                .WithClientSecret(this.clientSecret);

            if (!string.IsNullOrEmpty(azureRegion))
            {
                confAppBuidler.WithAzureRegion(azureRegion);
            }

            this.confidentialClientApplication = confAppBuidler.Build();
        }

        private AzureActiveDirectoryAuthClient(CommandFeedLogger logger, CommandFeedEndpointConfiguration endpointConfiguration)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (endpointConfiguration == null)
            {
                throw new ArgumentNullException(nameof(endpointConfiguration));
            }

            this.authority = endpointConfiguration.AadAuthority;
            this.resourceId = endpointConfiguration.CommandFeedAadResourceId;
        }

        /// <inheritdoc />
        public string Scheme => this.scheme;

        /// <summary>
        /// Get an AAD Token
        /// </summary>
        /// <returns>The AAD Token</returns>
        public async Task<string> GetAccessTokenAsync()
        {
            try
            {
                AuthenticationResult result;
                var scopes = new[] { $"{this.resourceId}/.default" };
                if (this.clientSecret != null)
                {
                    result = await this.confidentialClientApplication.AcquireTokenForClient(scopes).ExecuteAsync();
                }
                else if (this.clientCertificate != null)
                {
                    result = await this.confidentialClientApplication.AcquireTokenForClient(scopes).WithSendX5C(this.UseX5cAuthentication).ExecuteAsync();
                }
                else
                {
                    throw new ArgumentException("Must set either client credential or client certificate");
                }

                return result.AccessToken;
            }
            catch (Exception e)
            {
                this.logger.UnhandledException(e);
                throw;
            }
        }
    }
}
