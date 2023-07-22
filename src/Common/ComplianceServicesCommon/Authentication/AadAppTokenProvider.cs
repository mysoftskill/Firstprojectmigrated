// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Azure.ComplianceServices.Common
{
    using Microsoft.Identity.Client;
    using System;
    using System.Diagnostics;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    /// <inheritdoc />
    public class AadAppTokenProvider : IAppTokenProvider
    {
        private readonly string authority;
        private readonly string clientId;
        private readonly string resource;
        private readonly X509Certificate2 certificate;
        private readonly string clientSecret;
        private readonly ConfidentialCredential credsClient;

        private AuthenticationResult authResult = null;
        private DateTimeOffset tokenExpiresOn { get; set; } = DateTimeOffset.MinValue;

        /// <summary>
        /// Create AadTokenManager.
        /// </summary>
        /// <param name="authority">The token authority</param>
        /// <param name="clientId">The client id</param>
        /// <param name="resource">The resource</param>
        /// <param name="certificate">The certificate</param>
        public AadAppTokenProvider(
            string authority, 
            string clientId, 
            string resource, 
            X509Certificate2 certificate)
        {
            this.authority = authority;
            this.clientId = clientId;
            this.resource = resource;
            this.certificate = certificate;
            this.credsClient = new ConfidentialCredential(this.clientId, certificate, new Uri(authority));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AadAppTokenProvider"/> class.
        /// </summary>
        /// <param name="authority">The token authority.</param>
        /// <param name="clientId">The client ID.</param>
        /// <param name="resource">The resource.</param>
        /// <param name="clientSecret">The client secret.</param>
        public AadAppTokenProvider(
            string authority,
            string clientId,
            string resource,
            string clientSecret)
        {
            this.authority = authority;
            this.clientId = clientId;
            this.resource = resource;
            this.clientSecret = clientSecret;
            this.credsClient = new ConfidentialCredential(this.clientId, clientSecret, new Uri(authority));
        }

        /// <inheritdoc />
        public async Task<string> GetAppTokenAsync()
        {
            try
            {
                string[] scopes = new string[] { $"{this.resource}/.default" };
                var result = await this.credsClient.GetTokenAsync(scopes).ConfigureAwait(false);
                return result.AccessToken;
            }
            catch (Exception e)
            {
                Trace.TraceError($"[{nameof(AadAppTokenProvider)}]: {e}");
                throw;
            }
        }

        /// <summary>
        /// Gets the authentication result for an app token.
        /// </summary>
        /// <returns>The authentication result.</returns>
        public async Task<AuthenticationResult> GetAuthenticationResultAsync()
        {
            if (this.authResult != null && this.tokenExpiresOn > DateTimeOffset.UtcNow)
            {
                return authResult; // returning cache
            }
            string[] scopes = new string[] { $"{this.resource}/.default" };
            try
            {
                authResult = await this.credsClient.GetTokenAsync(scopes).ConfigureAwait(false);
                this.tokenExpiresOn = authResult.ExpiresOn;
                return authResult;
            }
            catch (Exception e)
            {
                Trace.TraceError($"[{nameof(AadAppTokenProvider)}]: {e}");
                throw;
            }
        }
    }
}
