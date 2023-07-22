namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System.Threading.Tasks;

    /// <summary>
    /// Exposes the available APIs for the service that target the DataOwner controller.
    /// This class is internal to force callers to access this through IDataManagementClient.
    /// </summary>
    internal class IncidentClient : IIncidentClient
    {
        private readonly IHttpServiceProxy httpServiceProxy;

        /// <summary>
        /// Initializes a new instance of the <see cref="IncidentClient" /> class.
        /// </summary>
        /// <param name="httpServiceProxy">The <see cref="IHttpServiceProxy" /> to use for interacting with the service.</param>
        public IncidentClient(IHttpServiceProxy httpServiceProxy)
        {
            this.httpServiceProxy = httpServiceProxy;
        }

        /// <summary>
        /// Create an incident and send it to ICM.
        /// </summary>
        /// <param name="incident">The incident to create.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The incident with service properties filled in.</returns>
        public async Task<IHttpResult<Incident>> CreateAsync(Incident incident, RequestContext requestContext)
        {
            var result =
                await this.httpServiceProxy.PostAsync<Incident, Incident>(
                    "/api/v2/incidents",
                    incident,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }
    }
}