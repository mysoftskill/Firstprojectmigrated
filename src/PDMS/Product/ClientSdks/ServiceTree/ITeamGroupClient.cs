namespace Microsoft.PrivacyServices.DataManagement.Client.ServiceTree
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines methods that are available for interacting with Service Tree team group entities.
    /// </summary>
    public interface ITeamGroupClient
    {
        /// <summary>
        /// Reads a team group by its id. If the team group cannot be found, than a NotFoundError exception is thrown.
        /// </summary>
        /// <param name="id">The id of the team group.</param>
        /// <param name="requestContext">The request context.</param>
        /// <exception cref="NotFoundError">Thrown when the team group is not found.</exception>
        /// <returns>The team group with the given id.</returns>
        Task<IHttpResult<TeamGroup>> ReadByIdAsync(Guid id, RequestContext requestContext);

        /// <summary>
        /// Gets the admins for the team group.
        /// </summary>
        /// <param name="id">The id of the team group.</param>
        /// <param name="requestContext">The request context.</param>
        /// <exception cref="NotFoundError">Thrown when the team group is not found.</exception>
        /// <returns>The authorizations for the team group.</returns>
        Task<IHttpResult<IEnumerable<Authorization>>> GetAuthorizationsAsync(Guid id, RequestContext requestContext);

        /// <summary>
        /// Gets the hierarchy for the team group.
        /// </summary>
        /// <param name="id">The id of the team group.</param>
        /// <param name="requestContext">The request context.</param>
        /// <exception cref="NotFoundError">Thrown when the team group is not found.</exception>
        /// <returns>The hierarchy for the team group.</returns>
        Task<IHttpResult<IEnumerable<Hierarchy>>> GetHierarchyAsync(Guid id, RequestContext requestContext);

        /// <summary>
        /// Finds all team groups whose names contain the given string.
        /// If no team groups are found, then an empty collection is returned.
        /// </summary>
        /// <param name="name">The name to search for.</param>
        /// <param name="requestContext">The request context.</param>
        /// <param name="filterResults">The underlying API returns both ServiceGroups and TeamGroups. Setting this value to true will only return the TeamGroups.</param>
        /// <returns>The team groups that contain this name.</returns>
        Task<IHttpResult<IEnumerable<Hierarchy>>> FindByNameAsync(string name, RequestContext requestContext, bool filterResults = true);
    }
}