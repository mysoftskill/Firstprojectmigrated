namespace Microsoft.PrivacyServices.DataManagement.Client
{
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines how to retrieve authentication information for a specific provider.
    /// </summary>
    public interface IAuthenticationProvider
    {
        /// <summary>
        /// Retrieves the authentication token for the provider.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The header to use for authentication.</returns>
        Task<AuthenticationHeaderValue> AcquireTokenAsync(CancellationToken cancellationToken);
    }
}