namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System.Threading.Tasks;

    /// <summary>
    /// Defines how to retrieve authentication information for a specific auth client.
    /// </summary>
    public interface IAuthClient
    {
        /// <summary>
        /// Gets the scheme used for authentication token.
        /// </summary>
        string Scheme { get; }

        /// <summary>
        /// Gets an authentication token from the provider.
        /// </summary>
        /// <returns>String that represents the token.</returns>
        Task<string> GetAccessTokenAsync();
    }
}
