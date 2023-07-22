namespace Microsoft.PrivacyServices.DataManagement.Client.ServiceTree
{
    using Microsoft.PrivacyServices.DataManagement.Client.ServiceTree.Models;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines operations for interacting with service tree.
    /// </summary>
    public interface IServiceTreeClient
    {
        /// <summary>
        /// Gets the client that can act on service group entities.
        /// </summary>
        IServiceGroupClient ServiceGroups { get; }

        /// <summary>
        /// Gets the client that can act on team group entities.
        /// </summary>
        ITeamGroupClient TeamGroups { get; }

        /// <summary>
        /// Gets the client that can act on service entities.
        /// </summary>
        IServiceClient Services { get; }

        /// <summary>
        /// Finds all services whose names contain the given string.
        /// If no services are found, then an empty collection is returned.
        /// </summary>
        /// <param name="name">The name to search for.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The services that contain this name.</returns>
        Task<IHttpResult<IEnumerable<Hierarchy>>> FindServicesByName(string name, RequestContext requestContext);

        /// <summary>
        /// Finds all nodes whose names contain the given string.
        /// If no nodes are found, then an empty collection is returned.
        /// Only ServiceGroups, TeamGroups and Services are searched.
        /// </summary>
        /// <param name="name">The name to search for.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The services that contain this name.</returns>
        Task<IHttpResult<IEnumerable<Hierarchy>>> FindNodesByName(string name, RequestContext requestContext);

        /// <summary>
        /// Loads the service group along with it's full hierarchy and administrator users.
        /// </summary>
        /// <param name="id">The id to search for.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The service group will all data populated.</returns>
        Task<IHttpResult<ServiceGroup>> ReadServiceGroupWithExtendedProperties(Guid id, RequestContext requestContext);

        /// <summary>
        /// Loads the team group along with it's full hierarchy and administrator users.
        /// </summary>
        /// <param name="id">The id to search for.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The team group will all data populated.</returns>
        Task<IHttpResult<TeamGroup>> ReadTeamGroupWithExtendedProperties(Guid id, RequestContext requestContext);

        /// <summary>
        /// Loads the service along with it's full hierarchy and administrator users.
        /// </summary>
        /// <param name="id">The id to search for.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The service will all data populated.</returns>
        Task<IHttpResult<Service>> ReadServiceWithExtendedProperties(Guid id, RequestContext requestContext);

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

        Task<IHttpResult> DeleteMetadata(Guid id, RequestContext requestContext);

        /// <summary>
        /// Updates a metadata
        /// </summary>
        /// <param name="id">The id of service.</param>
        /// <param name="serviceTreeMetadata">ServiceTreeMetadata model.</param>
        /// <param name="requestContext">The request context.</param>

        Task<IHttpResult> UpdateMetadata(Guid id, ServiceTreeMetadata serviceTreeMetadata, RequestContext requestContext);
        
        Task<ServiceTreeMetadataGetResults> GetMetadata(Guid guid, RequestContext requestContext);
    }
}