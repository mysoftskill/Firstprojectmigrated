namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System;
    using System.Net.Http;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides a standard set of call patterns for interacting with an http service.
    /// </summary>
    public interface IHttpClient : IDisposable
    {
        /// <summary>
        /// Sends the given request.
        /// </summary>
        /// <param name="request">The request message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task representing the <see cref="HttpResponseMessage"/>.</returns>
        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
        
        /// <summary>
        /// The certificate for requests from this client.
        /// </summary>
        X509Certificate2 Certificate { get; }
    }
}
