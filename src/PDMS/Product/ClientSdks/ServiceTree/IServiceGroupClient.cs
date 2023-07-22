namespace Microsoft.PrivacyServices.DataManagement.Client.ServiceTree
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines methods that are available for interacting with Service Tree service group entities.
    /// </summary>
    public interface IServiceGroupClient
    {
        /// <summary>
        /// Reads a service group by its id. If the service group cannot be found, than a NotFoundError exception is thrown.
        /// </summary>
        /// <param name="id">The id of the service group.</param>
        /// <param name="requestContext">The request context.</param>
        /// <exception cref="NotFoundError">Thrown when the service group is not found.</exception>
        /// <returns>The service group with the given id.</returns>
        Task<IHttpResult<ServiceGroup>> ReadByIdAsync(Guid id, RequestContext requestContext);

        /// <summary>
        /// Gets the admins for the service group.
        /// </summary>
        /// <param name="id">The id of the service group.</param>
        /// <param name="requestContext">The request context.</param>
        /// <exception cref="NotFoundError">Thrown when the service group is not found.</exception>
        /// <returns>The authorizations for the service group.</returns>
        Task<IHttpResult<IEnumerable<Authorization>>> GetAuthorizationsAsync(Guid id, RequestContext requestContext);
        
        /// <summary>
        /// Gets the hierarchy for the service group.
        /// </summary>
        /// <param name="id">The id of the service group.</param>
        /// <param name="requestContext">The request context.</param>
        /// <exception cref="NotFoundError">Thrown when the service group is not found.</exception>
        /// <returns>The hierarchy for the service group.</returns>
        Task<IHttpResult<IEnumerable<Hierarchy>>> GetHierarchyAsync(Guid id, RequestContext requestContext);

        /// <summary>
        /// Finds all service groups whose names contain the given string.
        /// If no service groups are found, then an empty collection is returned.
        /// </summary>
        /// <param name="name">The name to search for.</param>
        /// <param name="requestContext">The request context.</param>
        /// /// <param name="filterResults">The underlying API returns both ServiceGroups and TeamGroups. Setting this value to true will only return the TeamGroups.</param>
        /// <returns>The service groups that contain this name.</returns>
        Task<IHttpResult<IEnumerable<Hierarchy>>> FindByNameAsync(string name, RequestContext requestContext, bool filterResults = true);
    }
}