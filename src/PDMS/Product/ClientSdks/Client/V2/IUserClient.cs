namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System.Threading.Tasks;

    /// <summary>
    /// Exposes the available APIs for the service that target the User controller.
    /// </summary>
    public interface IUserClient
    {
        /// <summary>
        /// Issues a read call for getting the user information.
        /// </summary>
        /// <exception cref="ServiceFault">
        /// Thrown for unknown service responses.
        /// </exception>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The corresponding user.</returns>
        Task<IHttpResult<User>> ReadAsync(RequestContext requestContext);
    }
}