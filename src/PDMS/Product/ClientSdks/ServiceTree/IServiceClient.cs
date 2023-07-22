namespace Microsoft.PrivacyServices.DataManagement.Client.ServiceTree
{
    using Microsoft.PrivacyServices.DataManagement.Client.ServiceTree.Models;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines methods that are available for interacting with Service Tree service entities.
    /// </summary>
    public interface IServiceClient
    {
        /// <summary>
        /// Reads a service by its id. If the service cannot be found, than a NotFoundError exception is thrown.
        /// </summary>
        /// <param name="id">The id of the service.</param>
        /// <param name="requestContext">The request context.</param>
        /// <exception cref="NotFoundError">Thrown when the service is not found.</exception>
        /// <returns>The service with the given id.</returns>
        Task<IHttpResult<Service>> ReadByIdAsync(Guid id, RequestContext requestContext);

        /// <summary>
        /// Retrieves all services for the authenticated user. The user must be an admin for the service.
        /// If no services are found, then an empty collection is returned.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The services for the user.</returns>
        Task<IHttpResult<IEnumerable<Service>>> FindByAuthenticatedUserAsync(RequestContext requestContext);

        /// <summary>
        /// Finds all services whose names contain the given string.
        /// If no services are found, then an empty collection is returned.
        /// </summary>
        /// <param name="name">The name to search for.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The services that contain this name.</returns>
        Task<IHttpResult<IEnumerable<ServiceSearchResult>>> FindByNameAsync(string name, RequestContext requestContext);

        /// <summary>
        /// Creates a metadata
        /// </summary>
        /// <param name="id">The id of service.</param>
        /// <param name="serviceTreeMetadata">ServiceTreeMetadata model.</param>
        /// <param name="requestContext">The request context.</param>
        Task<IHttpResult> CreateMetadata(Guid id, ServiceTreeMetadata serviceTreeMetadata, RequestContext requestContext);

        /// <summary>
        /// Deletes a metadata
        /// </summary>
        /// <param name="id">The id of service.</param>
        /// <param name="requestContext">The request context.</param>
        Task<IHttpResult> DeleteMetadata(ServiceMetadata serviceMetadata, RequestContext requestContext);

        /// <summary>
        /// Updates a metadata
        /// </summary>
        /// <param name="id">The id of service.</param>
        /// <param name="serviceTreeMetadata">ServiceTreeMetadata model.</param>
        /// <param name="requestContext">The request context.</param>
        Task<IHttpResult> UpdateMetadata(Guid id, ServiceMetadata serviceTreeMetadata, RequestContext requestContext);
        
        Task<IHttpResult<ServiceTreeMetadataGetResults>> GetMetadataAsync(Guid id, RequestContext requestContext);
    }
}