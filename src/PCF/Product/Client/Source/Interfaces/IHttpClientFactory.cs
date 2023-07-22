namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Provides a factory interface for creating HTTP clients.
    /// </summary>
    public interface IHttpClientFactory
    {
        /// <summary>
        /// Creates an HTTP client that uses the given client certificate.
        /// </summary>
        /// <param name="clientCertificate">The client certificate to use for STS Auth</param>
        /// <returns>The <see cref="IHttpClient"/>.</returns>
        IHttpClient CreateHttpClient(X509Certificate2 clientCertificate);
    }
}
