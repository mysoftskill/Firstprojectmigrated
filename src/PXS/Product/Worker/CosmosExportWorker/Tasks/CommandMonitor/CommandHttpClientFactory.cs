// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Tasks
{
    using System;
    using System.Net.Http;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;

    using IPxsHttpClientFactory = Microsoft.Membership.MemberServices.PrivacyAdapters.Factory.IHttpClientFactory;
    using IPcfHttpClientFactory = Microsoft.PrivacyServices.CommandFeed.Client.IHttpClientFactory;
    using IPcfHttpClient = Microsoft.PrivacyServices.CommandFeed.Client.IHttpClient;
    using IPxsHttpClient = Microsoft.OSGS.HttpClientCommon.IHttpClient;
  
    /// <summary>
    ///     Http client factory to be used for PCF
    /// </summary>
    public class CommandHttpClientFactory : IPcfHttpClientFactory
    {
        private readonly IPrivacyPartnerAdapterConfiguration httpConfig;
        private readonly IPxsHttpClientFactory pxsFactory;
        private readonly ICounterFactory counterFactory;

        /// <summary>
        ///     Initializes a new instance of the CommandHttpClientFactory class
        /// </summary>
        /// <param name="httpConfig">HTTP configuration</param>
        /// <param name="pxsClientFactory">PXS client factory</param>
        /// <param name="counterFactory">counter factory</param>
        public CommandHttpClientFactory(
            IPrivacyPartnerAdapterConfiguration httpConfig,
            IPxsHttpClientFactory pxsClientFactory,
            ICounterFactory counterFactory)
        {
            this.counterFactory = counterFactory ?? throw new ArgumentNullException(nameof(counterFactory));
            this.pxsFactory = pxsClientFactory ?? throw new ArgumentNullException(nameof(pxsClientFactory));
            this.httpConfig = httpConfig ?? throw new ArgumentNullException(nameof(httpConfig));
        }

        /// <summary>
        ///     Creates an HTTP client that uses the given client certificate
        /// </summary>
        /// <param name="clientCertificate">client certificate to use for STS Auth</param>
        /// <returns>resulting value</returns>
        public IPcfHttpClient CreateHttpClient(X509Certificate2 clientCertificate)
        {
            ArgumentCheck.ThrowIfNull(clientCertificate, nameof(clientCertificate));

            return new HttpClientWrapper(
                clientCertificate,
                clientCertificate != null ?
                    this.pxsFactory.CreateHttpClient(this.httpConfig, clientCertificate, this.counterFactory) :
                    this.pxsFactory.CreateHttpClient(this.httpConfig, this.counterFactory));
        }

        /// <summary>
        ///     translates between the PCF HttpClient interface and the one used by PXS
        /// </summary>
        /// <remarks>the PXS factory  gives us all the trace logging that PXS normally uses</remarks>
        private class HttpClientWrapper : IPcfHttpClient
        {
            private readonly IPxsHttpClient client;

            /// <summary>
            ///     Initializes a new instance of the HttpClientWrapper class
            /// </summary>
            /// <param name="clientCert">client certificate</param>
            /// <param name="client">PXS http client</param>
            public HttpClientWrapper(
                X509Certificate2 clientCert,
                IPxsHttpClient client)
            {
                this.Certificate = clientCert;
                this.client = client;
            }

            /// <summary>
            ///     The certificate for requests from this client
            /// </summary>
            public X509Certificate2 Certificate { get; }

            /// <summary>
            ///     frees, releases, or resets unmanaged resources
            /// </summary>
            public void Dispose()
            {
                this.client?.Dispose();
            }

            /// <summary>
            ///     Sends the given request
            /// </summary>
            /// <param name="request">request message</param>
            /// <param name="cancellationToken">cancellation token</param>
            /// <returns>resulting value</returns>
            public Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                ArgumentCheck.ThrowIfNull(request, nameof(request));
                return this.client.SendAsync(request, cancellationToken);
            }
        }
    }
}
