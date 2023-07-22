namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Authentication
{
    using System.Security.Principal;

    using global::Owin;

    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;

    /// <summary>
    /// Defines a consistent function for registering authentication with OWIN.
    /// </summary>
    public interface IAuthenticationProvider
    {
        /// <summary>
        /// Gets a value indicating whether or not this provider is enabled.
        /// </summary>
        bool Enabled { get; }

        /// <summary>
        /// Register the authentication provider with OWIN.
        /// </summary>
        /// <param name="app">The app builder.</param>
        void ConfigureAuth(IAppBuilder app);

        /// <summary>
        /// Given a parsed token, copy the values to the principal object.
        /// </summary>
        /// <param name="source">The parse token.</param>
        /// <param name="destination">The object whose values should be set.</param>
        void SetPrincipal(IPrincipal source, AuthenticatedPrincipal destination);

        /// <summary>
        /// Retrieve the application id to identify the calling source.
        /// </summary>
        /// <param name="source">The principal information.</param>
        /// <returns>The application id.</returns>
        string GetApplicationId(IPrincipal source);
    }
}
