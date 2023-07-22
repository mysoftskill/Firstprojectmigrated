namespace Microsoft.PrivacyServices.DataManagement.Common.Authentication
{
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;

    /// <summary>
    /// Defines methods for acquiring AAD tokens for outbound requests.
    /// </summary>
    public interface ITokenProvider
    {
        /// <summary>
        /// Takes the current token for the user and converts it into a token for an external service.
        /// </summary>
        /// <param name="principal">The principal whose token needs to be converted.</param>
        /// <param name="resourceId">The target resource id.</param>
        /// <param name="sessionFactory">The sessionFactory.</param>
        /// <returns>The token for the external service.</returns>
        Task<string> AcquireTokenAsync(AuthenticatedPrincipal principal, string resourceId, ISessionFactory sessionFactory);

        /// <summary>
        /// Gets the authentication token for a given resource.
        /// </summary>
        /// <param name="resourceId">The target resource id.</param>
        /// <param name="sessionFactory">The sessionFactory.</param>
        /// <returns>The token for the external service.</returns>
        Task<string> AcquireTokenAsync(string resourceId, ISessionFactory sessionFactory);
    }
}