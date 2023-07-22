namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Authentication
{
    using Microsoft.Graph;
    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;

    /// <summary>
    /// A factory for creating graph clients.
    /// </summary>
    public interface IGraphServiceClientFactory
    {
        /// <summary>
        /// Creates a graph client from the authenticated principal.
        /// </summary>
        /// <param name="principal">The principal.</param>
        /// <param name="sessionFactory">The sessionFactory.</param>
        /// <returns>The graph client.</returns>
        IGraphServiceClient Create(AuthenticatedPrincipal principal, ISessionFactory sessionFactory);
    }
}