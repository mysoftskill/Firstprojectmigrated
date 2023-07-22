namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System.Threading.Tasks;

    /// <summary>
    /// Defines methods for creating incidents.
    /// </summary>
    public interface IIncidentClient
    {
        /// <summary>
        /// Create an incident and send it to ICM.
        /// </summary>
        /// <param name="incident">The incident to create.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The incident with service properties filled in.</returns>
        Task<IHttpResult<Incident>> CreateAsync(Incident incident, RequestContext requestContext);
    }
}