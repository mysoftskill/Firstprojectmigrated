namespace Microsoft.PrivacyServices.DataManagement.Client
{
    /// <summary>
    /// Defines methods to create authentication providers based on different inputs.
    /// Factory implementations are not required to support all interface methods.
    /// Some methods may not be supported by every authentication provider.
    /// </summary>
    public interface IAuthenticationProviderFactory
    {
        /// <summary>
        /// Creates a provider that can generate an access token for based on the user delegate token.
        /// </summary>
        /// <param name="userDelegateToken">The user delegate token to use for authentication.</param>
        /// <returns>An authentication provider for the request.</returns>
        IAuthenticationProvider CreateForUserDelegate(string userDelegateToken);

        /// <summary>
        /// Creates a provider that can generate an access token for a client call (i.e. no user involved).
        /// </summary>
        /// <returns>An authentication provider for the request.</returns>
        IAuthenticationProvider CreateForClient();

        /// <summary>
        /// Creates a provider that can generate an access token for the current logged on user.
        /// </summary>
        /// <returns>An authentication provider for the request.</returns>
        IAuthenticationProvider CreateForCurrentUser();

        /// <summary>
        /// Creates a provider that can generate an user access token for the resource. 
        /// Should be used in scenarios - where the client app generates the user token for the resource, and passes it for authentication.
        /// </summary>
        /// <returns>An authentication provider for the request.</returns>
        IAuthenticationProvider CreateForUserAccessTokenOnResource();        
    }
}