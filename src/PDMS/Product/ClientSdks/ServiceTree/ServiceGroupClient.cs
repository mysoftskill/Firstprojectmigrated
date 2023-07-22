namespace Microsoft.PrivacyServices.DataManagement.Client.ServiceTree
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines methods that are available for interacting with Service Tree service group entities.
    /// </summary>
    public class ServiceGroupClient : IServiceGroupClient
    {
        private readonly IHttpServiceProxy httpServiceProxy;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceGroupClient" /> class.
        /// </summary>
        /// <param name="httpServiceProxy">The <see cref="IHttpServiceProxy" /> to use for interacting with the service.</param>
        internal ServiceGroupClient(IHttpServiceProxy httpServiceProxy)
        {
            this.httpServiceProxy = httpServiceProxy;
        }

        /// <summary>
        /// Reads a service group by its id. If the service group cannot be found, then a NotFoundError exception is thrown.
        /// </summary>
        /// <param name="id">The id of the service group.</param>
        /// <param name="requestContext">The request context.</param>
        /// <exception cref="NotFoundError">Thrown when the service group is not found.</exception>
        /// <returns>The service group with the given id.</returns>
        public async Task<IHttpResult<ServiceGroup>> ReadByIdAsync(Guid id, RequestContext requestContext)
        {
            var result =
                await this.httpServiceProxy.GetAsync<ServiceGroup>(
                    $"/api/ServiceGroups({id.ToString()})",
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get();
        }

        /// <summary>
        /// Gets the admins for the service group.
        /// </summary>
        /// <param name="id">The id of the service group.</param>
        /// <param name="requestContext">The request context.</param>
        /// <exception cref="NotFoundError">Thrown when the service group is not found.</exception>
        /// <returns>The authorizations for the service group.</returns>
        public async Task<IHttpResult<IEnumerable<Authorization>>> GetAuthorizationsAsync(Guid id, RequestContext requestContext)
        {
            var result =
                await this.httpServiceProxy.GetAsync<Collection<Authorization>>(
                    $"/api/OrganizationHierarchy({id.ToString()})/ServiceTree.GetCurrentAuthorizations",
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.ConvertCollection();
        }

        /// <summary>
        /// Gets the hierarchy for the service group.
        /// </summary>
        /// <param name="id">The id of the service group.</param>
        /// <param name="requestContext">The request context.</param>
        /// <exception cref="NotFoundError">Thrown when the service group is not found.</exception>
        /// <returns>The hierarchy for the service group.</returns>
        public async Task<IHttpResult<IEnumerable<Hierarchy>>> GetHierarchyAsync(Guid id, RequestContext requestContext)
        {
            var result =
                await this.httpServiceProxy.GetAsync<Collection<Hierarchy>>(
                    $"/api/OrganizationHierarchy/ServiceTree.GetByServiceGroupId(ServiceGroupId={id.ToString()})",
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.ConvertCollection();
        }

        /// <summary>
        /// Finds all service groups whose names contain the given string.
        /// If no service groups are found, then an empty collection is returned.
        /// </summary>
        /// <param name="name">The name to search for.</param>
        /// <param name="requestContext">The request context.</param>
        /// <param name="filterResults">The underlying API returns both ServiceGroups and TeamGroups. Setting this value to true will only return the TeamGroups.</param>
        /// <returns>The service groups that contain this name.</returns>
        public async Task<IHttpResult<IEnumerable<Hierarchy>>> FindByNameAsync(string name, RequestContext requestContext, bool filterResults)
        {
            var result =
                await this.httpServiceProxy.GetAsync<Collection<Hierarchy>>(
                    $"/api/OrganizationHierarchy/ServiceTree.SearchServiceGroupOrTeamGroupByKeyword(Keyword='{name}')",
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);
            
            if (filterResults)
            {
                return result.Convert(x => x.Response?.Value?.Where(y => y.Level == ServiceTreeLevel.ServiceGroup));
            }
            else
            {
                return result.ConvertCollection();
            }
        }
    }
}