namespace Microsoft.PrivacyServices.DataManagement.Client.ServiceTree
{
    using Microsoft.PrivacyServices.DataManagement.Client.ServiceTree.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines methods that are available for interacting with Service Tree service entities.
    /// </summary>
    public class ServiceClient : IServiceClient
    {
        private readonly IHttpServiceProxy httpServiceProxy;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceClient" /> class.
        /// </summary>
        /// <param name="httpServiceProxy">The <see cref="IHttpServiceProxy" /> to use for interacting with the service.</param>
        internal ServiceClient(IHttpServiceProxy httpServiceProxy)
        {
            this.httpServiceProxy = httpServiceProxy;
        }

        /// <summary>
        /// Reads a service by its id. If the service cannot be found, than a NotFoundError exception is thrown.
        /// </summary>
        /// <param name="id">The id of the service.</param>
        /// <param name="requestContext">The request context.</param>
        /// <exception cref="NotFoundError">Thrown when the service is not found.</exception>
        /// <returns>The service with the given id.</returns>
        public async Task<IHttpResult<Service>> ReadByIdAsync(Guid id, RequestContext requestContext)
        {
            var result =
                await this.httpServiceProxy.GetAsync<Service>(
                    $"/api/Services({id})",
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get();
        }

        /// <summary>
        /// Retrieves all services for the authenticated user. The user must be an admin for the service.
        /// If no services are found, then an empty collection is returned.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The services for the user.</returns>
        public async Task<IHttpResult<IEnumerable<Service>>> FindByAuthenticatedUserAsync(RequestContext requestContext)
        {
            var result =
                await this.httpServiceProxy.GetAsync<Collection<Service>>(
                    "/api/PeopleHierarchy/ServiceTree.GetServicesForCurrentUser",
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.ConvertCollection();
        }

        /// <summary>
        /// Finds all services whose names contain the given string.
        /// If no services are found, then an empty collection is returned.
        /// </summary>
        /// <param name="name">The name to search for.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The services that contain this name.</returns>
        public async Task<IHttpResult<IEnumerable<ServiceSearchResult>>> FindByNameAsync(string name, RequestContext requestContext)
        {
            var result =
                await this.httpServiceProxy.GetAsync<IEnumerable<IEnumerable<ServiceSearchResult>>>(
                    "/api/ServiceHierarchy/ServiceTree.SearchServiceHierarchyByKeyword(Keyword='" + WebUtility.UrlEncode(name) + "')",
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);
             
            return result.Convert(x => x.Response?.SelectMany(y => y));
        }

        /// <summary>
        /// Creates a metadata
        /// </summary>
        /// <param name="id">The id of service.</param>
        /// <param name="serviceTreeMetadata">ServiceTreeMetadata model.</param>
        /// <param name="requestContext">The request context.</param>

        public async Task<IHttpResult> CreateMetadata(Guid id, ServiceTreeMetadata serviceTreeMetadata, RequestContext requestContext)
        {
            ServiceMetadata serviceMetadata = new ServiceMetadata
            {
                ServiceHierarchyId = id.ToString(),
                Privacy_Compliance_Dashboard = serviceTreeMetadata.Value.PrivacyComplianceDashboard,
                NGP_PowerBI_URL = serviceTreeMetadata.Value.NGPPowerBIUrl,
                AzureCloud = "Public",
                EntityState = "Active",
                MetadataDefinitionId = "NGP_Entities",
            };
            ServiceMetadataPostBody payload = new ServiceMetadataPostBody
            {
                ServiceMetadata = serviceMetadata,
            };
            var result =
                await this.httpServiceProxy.PostAsync<ServiceMetadataPostBody, IHttpResult>(
                    $"/api/ServiceHierarchy({id})/ServiceTree.AddMetadata",
                    payload,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get();
        }

        /// <summary>
        /// Updates a metadata
        /// </summary>
        /// <param name="id">The id of service.</param>
        /// <param name="serviceMetadata">ServiceTreeMetadata model.</param>
        /// <param name="requestContext">The request context.</param>

        public async Task<IHttpResult> UpdateMetadata(Guid id, ServiceMetadata serviceMetadata, RequestContext requestContext)
        {
            var result =
                await this.httpServiceProxy.PutAsync<ServiceMetadata, IHttpResult>(
                    $"api/ServiceMetadata({serviceMetadata.MetadataId})",
                    serviceMetadata,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get();
        }

        /// <summary>
        /// Deletes a metadata
        /// </summary>
        /// <param name="id">The id of service.</param>
        /// <param name="requestContext">The request context.</param>

        public async Task<IHttpResult> DeleteMetadata(ServiceMetadata serviceMetadata, RequestContext requestContext)
        {
            var result =
                await this.httpServiceProxy.DeleteAsync<ServiceMetadata>(
                    $"/api/ServiceMetadata({serviceMetadata.MetadataId})",
                    serviceMetadata,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get();
        }

        public async Task<IHttpResult<ServiceTreeMetadataGetResults>> GetMetadataAsync(Guid id, RequestContext requestContext)
        {
            var result =
                await this.httpServiceProxy.GetAsync<ServiceTreeMetadataGetResults>(
                    $"/api/ServiceHierarchy({id})/ServiceTree.GetMetadata(MetadataDefinitionId=%27NGP_Entities%27)",
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result;
        }
    }
}