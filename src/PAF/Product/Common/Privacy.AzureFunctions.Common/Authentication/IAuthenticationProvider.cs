namespace Microsoft.PrivacyServices.AzureFunctions.Common
{
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Defines contracts to get authentication tokens
    /// </summary>
    public interface IAuthenticationProvider
    {
        /// <summary>
        /// Gets the authentication token for a resource.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <returns>the accesstoken</returns>
        Task<string> GetAccessTokenAsync(ILogger logger);
    }
}
