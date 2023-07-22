// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary
{
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Extensions;
    using Microsoft.Windows.Services.AuthN.Client.S2S;

    /// <summary>
    /// PrivacyExperience AuthClient
    /// </summary>
    public class PrivacyAuthClient : IPrivacyAuthClient
    {
        /// <summary>
        /// The s2s-authentication-client.
        /// </summary>
        private readonly S2SAuthClient s2SAuthClient;

        /// <summary>
        /// The target site
        /// </summary>
        private string targetSite;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrivacyAuthClient" /> class.
        /// </summary>
        /// <param name="siteId">The site identifier.</param>
        /// <param name="targetSite">The target site.</param>
        /// <param name="certificate">The certificate.</param>
        /// <param name="msaAuthenticationUrl">The msa authentication URL.</param>
        public PrivacyAuthClient(long siteId, string targetSite, X509Certificate2 certificate, Uri msaAuthenticationUrl)
        {
            targetSite.ThrowOnNull(nameof(targetSite));
            certificate.ThrowOnNull(nameof(certificate));
            msaAuthenticationUrl.ThrowOnNull(nameof(msaAuthenticationUrl));

            this.targetSite = targetSite;
            this.s2SAuthClient = S2SAuthClient.Create(siteId, certificate, msaAuthenticationUrl);
        }

        /// <summary>
        /// Gets the X509 certificate used as the client side of the mutually authenticated SSL connection which will be established
        /// by this auth client
        /// </summary>
        /// <value>
        /// The HTTP client credential.
        /// </value>
        public X509Certificate2 ClientCertificate
        {
            get { return this.s2SAuthClient.ClientCertificate;  }
        }

        /// <summary>
        /// Retrieve an access token asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to cancel the get token request.</param>
        /// <returns>
        /// A task that track the operations to fetch an auth token.
        /// If successful, the auth token will be returned as a string.
        /// </returns>
        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
        {
            return this.s2SAuthClient.GetAccessTokenAsync(this.targetSite, cancellationToken);
        }
    }
}