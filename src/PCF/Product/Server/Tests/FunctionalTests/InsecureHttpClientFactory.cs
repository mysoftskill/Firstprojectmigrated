namespace PCF.FunctionalTests
{
    using System;
    using System.Net.Http;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using IHttpClientFactory = Microsoft.PrivacyServices.CommandFeed.Client.IHttpClientFactory;

    public class InsecureHttpClientFactory : IHttpClientFactory
    {
        public IHttpClient CreateHttpClient(X509Certificate2 clientCertificate)
        {
            return new InsecureHttpClient(clientCertificate);
        }

        private class InsecureHttpClient : IHttpClient
        {
            private readonly HttpClient httpClient;

            public InsecureHttpClient(X509Certificate2 certificate)
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
                this.httpClient = new HttpClient(handler);  // lgtm [cs/httpclient-checkcertrevlist-disabled]
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
