namespace Microsoft.PrivacyServices.AzureFunctions.AIdFunctionalTests.AnaheimId
{
    using System;
    using System.Net.Http;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using IHttpClientFactory = Microsoft.PrivacyServices.CommandFeed.Client.IHttpClientFactory;

    /// <summary>
    /// Insecure HttpClientFactory used for creating HttpClient used in PCF
    /// </summary>
    public class PcfHttpClientFactory : IHttpClientFactory
    {
        /// <summary>
        /// HttpClient used in PCF
        /// </summary>
        public IHttpClient CreateHttpClient(X509Certificate2 clientCertificate)
        {
            return new PcfHttpClient(clientCertificate);
        }

        private class PcfHttpClient : IHttpClient
        {
            private readonly HttpClient httpClient;

            public PcfHttpClient(X509Certificate2 certificate)
            {
                WebRequestHandler handler = new WebRequestHandler
                {
                    ServerCertificateValidationCallback = (a, b, c, d) => true
                };

                if (certificate != null)
                {
                    handler.ClientCertificates.Add(certificate);
                }

                this.Certificate = certificate;
                this.httpClient = new HttpClient(handler) // lgtm [cs/httpclient-checkcertrevlist-disabled]
                {
                    Timeout = TimeSpan.FromMinutes(2)
                };
            }

            public X509Certificate2 Certificate { get; }

            public void Dispose()
            {
                this.httpClient.Dispose();
            }

            public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return this.httpClient.SendAsync(request, cancellationToken);
            }
        }
    }
}
