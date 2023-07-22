namespace Microsoft.PrivacyServices.CommandFeed.Validator.KeyDiscovery
{
    using System.Diagnostics.CodeAnalysis;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <inheritdoc />
    /// <summary>
    /// Provides a default implementation of the IHttpClient interface. This implementation is a simple wrapper around <see cref="T:System.Net.Http.HttpClient" />.
    /// </summary>
    public sealed class DefaultHttpClient : IHttpClient
    {
        private readonly HttpClient httpClient;

        /// <summary>
        /// Initializes a new instance of the DefaultHttpClient class.
        /// </summary>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public DefaultHttpClient()
        {
            this.httpClient = new HttpClient();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.httpClient.Dispose();
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return this.httpClient.SendAsync(request, cancellationToken);
        }
    }
}
