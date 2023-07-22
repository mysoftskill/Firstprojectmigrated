namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System.Net.Http.Headers;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    /// <summary>
    /// An interface that can authenticate for different Server-To-Service auth type.
    /// </summary>
    public interface IAuthenticator
    {
        /// <summary>
        /// Validate if the request is authenticated based on the context.
        /// </summary>
        Task<PcfAuthenticationContext> AuthenticateAsync(HttpRequestHeaders requestHeaders, X509Certificate2 clientCertificate);
    }
}
