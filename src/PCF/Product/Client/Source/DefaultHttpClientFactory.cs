namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Provides a default implementation of the IHttpClientFactory interface.
    /// </summary>
    public class DefaultHttpClientFactory : IHttpClientFactory
    {
        /// <summary>
        /// Creates a new Default HTTP Client.
        /// </summary>
        /// <param name="clientCertificate">The client certificate for STS Auth</param>
        /// <returns>The <see cref="IHttpClient"/>.</returns>
        public IHttpClient CreateHttpClient(X509Certificate2 clientCertificate)
        {
            return new DefaultHttpClient(clientCertificate);
        }
    }
}
