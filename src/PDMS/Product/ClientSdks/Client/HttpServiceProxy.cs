namespace Microsoft.PrivacyServices.DataManagement.Client
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Net.Http;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// The official implementation of the http proxy service interface.
    /// </summary>
    /// <remarks>
    /// This is excluded from code coverage because it can only be used when 
    /// talking to a real service, which we do not do from the unit tests.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    public sealed class HttpServiceProxy : BaseHttpServiceProxy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpServiceProxy" /> class.
        /// This version creates an HttpClient internally using the provided data.
        /// Use this if you do not want to manage the HttpClient object manually.
        /// </summary>
        /// <param name="baseAddress">The base address for the service. Example: https://management.privacy.microsoft.com/. </param>
        /// <param name="clientCert">The client certificate for authentication (if needed).</param>
        /// <param name="defaultTimeout">The timeout value for requests (defaults to 5 seconds).</param>
        public HttpServiceProxy(Uri baseAddress, X509Certificate2 clientCert = null, TimeSpan? defaultTimeout = null)
            : base(CreateClient(baseAddress, clientCert, defaultTimeout))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpServiceProxy" /> class. 
        /// The provided HttpClient must already be configured with a BaseAddress and ClientCertificates (if needed).
        /// </summary>
        /// <param name="httpClient">An HttpClient object that is already set up to contact the service.</param>
        public HttpServiceProxy(HttpClient httpClient)
            : base(httpClient)
        {
        }

        /// <summary>
        /// Creates the http client. Must use a static function in order to call this before invoking the base constructor.
        /// </summary>
        /// <param name="baseAddress">The base address for the service. Example: https://management.privacy.microsoft.com/. </param>
        /// <param name="clientCert">The client certificate for authentication.</param>
        /// <param name="defaultTimeout">The timeout value for requests (defaults to 5 seconds).</param>
        /// <returns>An http client.</returns>
        private static HttpClient CreateClient(Uri baseAddress, X509Certificate2 clientCert = null, TimeSpan? defaultTimeout = null)
        {
            var handler = new HttpClientHandler();

            if (clientCert != null)
            {
                handler.ClientCertificates.Add(clientCert);
            }

            var httpClient = new HttpClient(handler, true)
            {
                BaseAddress = baseAddress
            };

            if (defaultTimeout.HasValue)
            {
                httpClient.Timeout = defaultTimeout.Value;
            }
            else
            {
                httpClient.Timeout = TimeSpan.FromSeconds(5);
            }

            return httpClient;
        }
    }
}