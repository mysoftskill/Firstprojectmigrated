namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System.Threading.Tasks;

    /// <summary>
    /// Exposes the available APIs for the service that target the User controller.
    /// </summary>
    public class UserClient : IUserClient
    {
        private readonly IHttpServiceProxy httpServiceProxy;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserClient" /> class.
        /// </summary>
        /// <param name="httpServiceProxy">The <see cref="IHttpServiceProxy" /> to use for interacting with the service.</param>
        public UserClient(IHttpServiceProxy httpServiceProxy)
        {
            this.httpServiceProxy = httpServiceProxy;
        }

        /// <summary>
        /// Issues a read call to get user information.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The corresponding user.</returns>
        public async Task<IHttpResult<User>> ReadAsync(RequestContext requestContext)
        {
            string url = $"/api/v2/users('me')?$select=id,securityGroups";

            var result =
                await this.httpServiceProxy.GetAsync<User>(
                    url,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }
    }
}