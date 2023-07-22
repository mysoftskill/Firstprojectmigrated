// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.PrivacyOperation.Client.Clients.Implementations
{
    using System;
    using System.Net.Http.Headers;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Identity.Client;
    using Microsoft.PrivacyServices.PrivacyOperation.Client.Clients.Interfaces;

    /// <summary>
    ///     PrivacyOperationAuthClient.
    /// </summary>
    public class PrivacyOperationAuthClient : IPrivacyOperationAuthClient
    {
        private const string Scheme = "Bearer";
        private const string Authority = "https://login.microsoftonline.com/microsoft.onmicrosoft.com";

        private readonly string clientAppId;
        private readonly X509Certificate2 certificate;
        private readonly string resource;
        private readonly string appTokenAuthority;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrivacyOperationAuthClient" /> class.
        /// </summary>
        /// <param name="certificate">The client certificate assertion.</param>
        /// <param name="clientAppId">The appId calling into PXS</param>
        /// <param name="resource">The resource that is trying to be connected to</param>
        public PrivacyOperationAuthClient(X509Certificate2 certificate, string clientAppId, string resource)
        {
            this.clientAppId = clientAppId ?? throw new ArgumentNullException(nameof(clientAppId));
            this.resource = resource ?? throw new ArgumentNullException(nameof(clientAppId));
            this.appTokenAuthority = Authority;
            this.certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
        }

        /// <inheritdoc />
        public async Task<AuthenticationHeaderValue> GetAadAuthToken(CancellationToken cancellationToken, UserAssertion userAssertion = null)
        {
            string[] scopes = new string[] { $"{this.resource}/.default" };
            AuthenticationResult result;

            if (userAssertion != null)
            {
                var app = ConfidentialClientApplicationBuilder.Create(this.clientAppId)
                    .WithCertificate(this.certificate)
                    .WithAuthority(new Uri(appTokenAuthority))
                    .Build();
                result = await app.AcquireTokenOnBehalfOf(scopes,userAssertion).WithSendX5C(true).ExecuteAsync();
            }
            else
            {
                string azureRegion = Environment.GetEnvironmentVariable("MONITORING_DATACENTER") ?? "TryAutoDetect";

                var app = ConfidentialClientApplicationBuilder.Create(this.clientAppId)
                    .WithAzureRegion(azureRegion)
                    .WithCertificate(this.certificate)
                    .WithAuthority(new Uri(appTokenAuthority))
                    .Build();
                result = await app.AcquireTokenForClient(scopes).WithSendX5C(true).ExecuteAsync();
            }

            return new AuthenticationHeaderValue(Scheme, result.AccessToken);
        }
    }
}
