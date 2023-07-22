namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Net.Http;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Client.Helpers;

    /// <summary>
    /// Provides a default implementation of the IHttpClient interface. This implementation is a simple wrapper around <see cref="HttpClient"/>.
    /// </summary>
    public sealed class DefaultHttpClient : IHttpClient
    {
        private readonly HttpClient httpClient;

        /// <summary>
        /// Initializes a new instance of the DefaultHttpClient class.
        /// </summary>
        /// <param name="clientCertificate">The client certificate for STS Auth</param>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public DefaultHttpClient(X509Certificate2 clientCertificate)
        {
            var requestHandler =
#if NET452
                new WebRequestHandler();
#else
                new HttpClientHandler();
#endif
            IBackOff backoff = new ExponentialBackoff(delay: TimeSpan.FromSeconds(1), maxDelay: TimeSpan.FromSeconds(30));

            // RetryHandler will retry with a timeout of 30 seconds.
            // Max retries value is based on the exponential backoff if not given retry time in reponse header.
            var retryHandler = new RetryHandler(backoff: backoff, maxRetries: 5, retryTimeout: TimeSpan.FromSeconds(30));

            if (clientCertificate != null)
            {
                requestHandler.ClientCertificates.Add(clientCertificate);
            }

            this.Certificate = clientCertificate;

            // Will trigger requestHandler first and then retryHandler
            this.httpClient = HttpClientFactory.Create(requestHandler, retryHandler);

            // increasing timeout to 200 to handle high latency for calls to storage in pcfv2
            this.httpClient.Timeout = TimeSpan.FromSeconds(200);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.httpClient.Dispose();
        }

        /// <inheritdoc/>
        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return this.httpClient.SendAsync(request, cancellationToken);
        }
        
        /// <inheritdoc/>
        public X509Certificate2 Certificate { get; }
    }
}
