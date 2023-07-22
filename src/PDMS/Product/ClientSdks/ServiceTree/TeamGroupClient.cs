namespace Microsoft.PrivacyServices.DataManagement.Client.ServiceTree
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines methods that are available for interacting with Service Tree team group entities.
    /// </summary>
    public class TeamGroupClient : ITeamGroupClient
    {
        private readonly IHttpServiceProxy httpServiceProxy;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamGroupClient" /> class.
        /// </summary>
        /// <param name="httpServiceProxy">The <see cref="IHttpServiceProxy" /> to use for interacting with the service.</param>
        internal TeamGroupClient(IHttpServiceProxy httpServiceProxy)
        {
            this.httpServiceProxy = httpServiceProxy;
        }

        /// <summary>
        /// Reads a team group by its id. If the team group cannot be found, than a NotFoundError exception is thrown.
        /// </summary>
        /// <param name="id">The id of the team group.</param>
        /// <param name="requestContext">The request context.</param>
        /// <exception cref="NotFoundError">Thrown when the team group is not found.</exception>
        /// <returns>The team group with the given id.</returns>
        public async Task<IHttpResult<TeamGroup>> ReadByIdAsync(Guid id, RequestContext requestContext)
        {
            var result =
                await this.httpServiceProxy.GetAsync<TeamGroup>(
                    $"/api/TeamGroups({id})",
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get();
        }

        /// <summary>
        /// Gets the admins for the team group.
        /// </summary>
        /// <param name="id">The id of the team group.</param>
        /// <param name="requestContext">The request context.</param>
        /// <exception cref="NotFoundError">Thrown when the team group is not found.</exception>
        /// <returns>The authorizations for the team group.</returns>
        public async Task<IHttpResult<IEnumerable<Authorization>>> GetAuthorizationsAsync(Guid id, RequestContext requestContext)
        {
            var result =
                await this.httpServiceProxy.GetAsync<Collection<Authorization>>(
                    $"/api/OrganizationHierarchy({id})/ServiceTree.GetCurrentAuthorizations",
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.ConvertCollection();
        }

        /// <summary>
        /// Gets the hierarchy for the team group.
        /// </summary>
        /// <param name="id">The id of the team group.</param>
        /// <param name="requestContext">The request context.</param>
        /// <exception cref="NotFoundError">Thrown when the team group is not found.</exception>
        /// <returns>The hierarchy for the team group.</returns>
        public async Task<IHttpResult<IEnumerable<Hierarchy>>> GetHierarchyAsync(Guid id, RequestContext requestContext)
        {
            var result =
                await this.httpServiceProxy.GetAsync<Collection<Hierarchy>>(
                    $"/api/OrganizationHierarchy/ServiceTree.GetByTeamGroupId(TeamGroupId={id})",
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.ConvertCollection();
        }

        /// <summary>
        /// Finds all team groups whose names contain the given string.
        /// If no team groups are found, then an empty collection is returned.
        /// </summary>
        /// <param name="name">The name to search for.</param>
        /// <param name="requestContext">The request context.</param>
        /// <param name="filterResults">The underlying API returns both ServiceGroups and TeamGroups. Setting this value to true will only return the TeamGroups.</param>
        /// <returns>The team groups that contain this name.</returns>
        public async Task<IHttpResult<IEnumerable<Hierarchy>>> FindByNameAsync(string name, RequestContext requestContext, bool filterResults = true)
        {
            var result =
                await this.httpServiceProxy.GetAsync<Collection<Hierarchy>>(
                    $"/api/OrganizationHierarchy/ServiceTree.SearchServiceGroupOrTeamGroupByKeyword(Keyword='{name}')",
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            if (filterResults)
            {
                return result.Convert(x => x.Response?.Value?.Where(y => y.Level == ServiceTreeLevel.TeamGroup));
            }
            else
            {
                return result.ConvertCollection();
            }
        }
    }
}