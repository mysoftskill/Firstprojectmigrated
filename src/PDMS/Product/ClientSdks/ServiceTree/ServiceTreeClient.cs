namespace Microsoft.PrivacyServices.DataManagement.Client.ServiceTree
{
    using Microsoft.PrivacyServices.DataManagement.Client.ServiceTree.Models;
    using Microsoft.PrivacyServices.Policy;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines operations for interacting with service tree.
    /// </summary>
    public class ServiceTreeClient : IServiceTreeClient
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceTreeClient" /> class.
        /// </summary>
        /// <param name="httpServiceProxy">The <see cref="IHttpServiceProxy" /> to use for interacting with the service.</param>
        public ServiceTreeClient(IHttpServiceProxy httpServiceProxy)
        {
            this.ServiceGroups = new ServiceGroupClient(httpServiceProxy);
            this.TeamGroups = new TeamGroupClient(httpServiceProxy);
            this.Services = new ServiceClient(httpServiceProxy);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceTreeClient" /> class.
        /// </summary>
        /// <param name="serviceGroupClient">The service group client instance.</param>
        /// <param name="teamGroupClient">The team group client instance.</param>
        /// <param name="serviceClient">The group client instance.</param>
        internal ServiceTreeClient(
            IServiceGroupClient serviceGroupClient,
            ITeamGroupClient teamGroupClient,
            IServiceClient serviceClient)
        {
            this.ServiceGroups = serviceGroupClient;
            this.TeamGroups = teamGroupClient;
            this.Services = serviceClient;
        }

        /// <summary>
        /// Gets the client that can act on service group entities.
        /// </summary>
        public IServiceGroupClient ServiceGroups { get; private set; }

        /// <summary>
        /// Gets the client that can act on team group entities.
        /// </summary>
        public ITeamGroupClient TeamGroups { get; private set; }

        /// <summary>
        /// Gets the client that can act on service entities.
        /// </summary>
        public IServiceClient Services { get; private set; }
        
        /// <summary>
        /// Finds all services whose names contain the given string.
        /// If no services are found, then an empty collection is returned.
        /// </summary>
        /// <param name="name">The name to search for.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The services that contain this name.</returns>
        public async Task<IHttpResult<IEnumerable<Hierarchy>>> FindServicesByName(string name, RequestContext requestContext)
        {
            var services = await this.Services.FindByNameAsync(name, requestContext).ConfigureAwait(false);

            return services.Convert(r => 
                r.Response
                .Select(x => new Hierarchy
                {
                    Id = x.Id,
                    Name = x.Name,
                    Level = ServiceTreeLevel.Service
                }));
        }


        /// <summary>
        /// Finds all nodes whose names contain the given string.
        /// If no nodes are found, then an empty collection is returned.
        /// Only ServiceGroups, TeamGroups and Services are searched.
        /// </summary>
        /// <param name="name">The name to search for.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The services that contain this name.</returns>
        public async Task<IHttpResult<IEnumerable<Hierarchy>>> FindNodesByName(string name, RequestContext requestContext)
        {
            var servicesTask = this.Services.FindByNameAsync(name, requestContext);

            var othersTask = this.ServiceGroups.FindByNameAsync(name, requestContext, filterResults: false);

            await Task.WhenAll(servicesTask, othersTask).ConfigureAwait(false);

            return servicesTask.Result.Convert(r =>
                r.Response
                .Select(x => new Hierarchy
                {
                    Id = x.Id,
                    Name = x.Name,
                    Level = ServiceTreeLevel.Service
                }).Concat(othersTask.Result.Response));
        }

        /// <summary>
        /// Loads the service group along with it's full hierarchy and administrator users.
        /// </summary>
        /// <param name="id">The id to search for.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The service group will all data populated.</returns>
        public async Task<IHttpResult<ServiceGroup>> ReadServiceGroupWithExtendedProperties(Guid id, RequestContext requestContext)
        {
            var serviceGroup = await this.ServiceGroups.ReadByIdAsync(id, requestContext).ConfigureAwait(false);
            serviceGroup = serviceGroup.Get(); // Bail out early for NotFound errors.

            var hierarchyTask = this.ServiceGroups.GetHierarchyAsync(id, requestContext);

            var authorizationTask = this.ServiceGroups.GetAuthorizationsAsync(id, requestContext); // This call fails with unhandled exception for NotFound.

            await Task.WhenAll(hierarchyTask, authorizationTask).ConfigureAwait(false);

            // Set authorization values.
            serviceGroup.Response.AdminUserNames = authorizationTask.Result.Get().Response.Select(x => x.Id);

            // Set hierarchy values.
            var hierarchy = hierarchyTask.Result.Get().Response;
            var division = hierarchy.Single(x => x.Level == ServiceTreeLevel.Division);
            var organization = hierarchy.Single(x => x.Level == ServiceTreeLevel.Organization);

            serviceGroup.Response.DivisionId = division.Id;
            serviceGroup.Response.DivisionName = division.Name;
            serviceGroup.Response.OrganizationId = organization.Id;
            serviceGroup.Response.OrganizationName = organization.Name;

            return serviceGroup;
        }

        /// <summary>
        /// Loads the team group along with it's full hierarchy and administrator users.
        /// </summary>
        /// <param name="id">The id to search for.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The team group will all data populated.</returns>
        public async Task<IHttpResult<TeamGroup>> ReadTeamGroupWithExtendedProperties(Guid id, RequestContext requestContext)
        {
            var teamGroup = await this.TeamGroups.ReadByIdAsync(id, requestContext).ConfigureAwait(false);
            teamGroup = teamGroup.Get(); // Bail out early for NotFound errors.

            var hierarchyTask = this.TeamGroups.GetHierarchyAsync(id, requestContext);

            var authorizationTask = this.TeamGroups.GetAuthorizationsAsync(id, requestContext); // This call fails with unhandled exception for NotFound.

            await Task.WhenAll(hierarchyTask, authorizationTask).ConfigureAwait(false);

            // Set authorization values.
            teamGroup.Response.AdminUserNames = authorizationTask.Result.Get().Response.Select(x => x.Id);
            
            // Set hierarchy values.
            var hierarchy = hierarchyTask.Result.Get().Response;
            var division = hierarchy.Single(x => x.Level == ServiceTreeLevel.Division);
            var organization = hierarchy.Single(x => x.Level == ServiceTreeLevel.Organization);
            var serviceGroup = hierarchy.Single(x => x.Level == ServiceTreeLevel.ServiceGroup);

            teamGroup.Response.DivisionId = division.Id;
            teamGroup.Response.DivisionName = division.Name;
            teamGroup.Response.OrganizationId = organization.Id;
            teamGroup.Response.OrganizationName = organization.Name;
            teamGroup.Response.ServiceGroupId = serviceGroup.Id;
            teamGroup.Response.ServiceGroupName = serviceGroup.Name;

            return teamGroup;
        }

        /// <summary>
        /// Loads the service along with it's full hierarchy and administrator users.
        /// </summary>
        /// <param name="id">The id to search for.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The service will all data populated.</returns>
        public async Task<IHttpResult<Service>> ReadServiceWithExtendedProperties(Guid id, RequestContext requestContext)
        {
            var serviceResult = await this.Services.ReadByIdAsync(id, requestContext).ConfigureAwait(false);
                        
            var service = serviceResult.Get();
            service.Response.AdminUserNames = service.Response.AdminSecurityGroups.Split(';');

            IHttpResult<IEnumerable<Hierarchy>> hierarchyResult;

            if (service.Response.TeamGroupId.HasValue)
            {
                hierarchyResult = await this.TeamGroups.GetHierarchyAsync(service.Response.TeamGroupId.Value, requestContext).ConfigureAwait(false);
            }
            else
            {
                hierarchyResult = await this.ServiceGroups.GetHierarchyAsync(service.Response.ServiceGroupId.Value, requestContext).ConfigureAwait(false);
            }

            var hierarchy = hierarchyResult.Get().Response;
            var division = hierarchy.Single(x => x.Level == ServiceTreeLevel.Division);
            var organization = hierarchy.Single(x => x.Level == ServiceTreeLevel.Organization);
            var serviceGroup = hierarchy.Single(x => x.Level == ServiceTreeLevel.ServiceGroup);

            service.Response.DivisionId = division.Id;
            service.Response.DivisionName = division.Name;
            service.Response.OrganizationId = organization.Id;
            service.Response.OrganizationName = organization.Name;
            service.Response.ServiceGroupId = serviceGroup.Id;
            service.Response.ServiceGroupName = serviceGroup.Name;

            if (service.Response.TeamGroupId.HasValue)
            {
                var teamGroup = hierarchy.Single(x => x.Level == ServiceTreeLevel.TeamGroup);
                service.Response.TeamGroupId = teamGroup.Id;
                service.Response.TeamGroupName = teamGroup.Name;
            }

            return service;
        }

        /// <summary>
        /// Creates a metadata
        /// </summary>
        /// <param name="id">The id of service.</param>
        /// <param name="serviceTreeMetadata">ServiceTreeMetadata model.</param>
        /// <param name="requestContext">The request context.</param>

        public async Task<IHttpResult> CreateMetadata(Guid id, ServiceTreeMetadata serviceTreeMetadata, RequestContext requestContext)
        {
            var response = await this.Services.CreateMetadata(id, serviceTreeMetadata, requestContext).ConfigureAwait(false);
            return response;
        }

        /// <summary>
        /// Deletes a metadata
        /// </summary>
        /// <param name="id">The id of service.</param>
        /// <param name="requestContext">The request context.</param>

        public async Task<IHttpResult> DeleteMetadata(Guid id, RequestContext requestContext)
        {
            var getResult = await this.GetMetadata(id, requestContext).ConfigureAwait(false);
            var serviceMetadata = getResult.Values.FirstOrDefault();
            var response = await this.Services.DeleteMetadata(serviceMetadata, requestContext).ConfigureAwait(false);
            return response;
        }

        /// <summary>
        /// Updates a metadata
        /// </summary>
        /// <param name="id">The id of service.</param>
        /// <param name="serviceTreeMetadata">ServiceTreeMetadata model.</param>
        /// <param name="requestContext">The request context.</param>

        public async Task<IHttpResult> UpdateMetadata(Guid id, ServiceTreeMetadata serviceTreeMetadata, RequestContext requestContext)
        {
            var getResult = await this.GetMetadata(id, requestContext).ConfigureAwait(false);
            var serviceMetadata = getResult.Values.FirstOrDefault();
            serviceMetadata.NGP_PowerBI_URL = serviceTreeMetadata.Value.NGPPowerBIUrl;
            serviceMetadata.Privacy_Compliance_Dashboard = serviceTreeMetadata.Value.PrivacyComplianceDashboard;
            var response = await this.Services.UpdateMetadata(id, serviceMetadata, requestContext).ConfigureAwait(false);
            return response;
        }

        public async Task<ServiceTreeMetadataGetResults> GetMetadata(Guid guid, RequestContext requestContext)
        {
            var response = await this.Services.GetMetadataAsync(guid, requestContext).ConfigureAwait(false);
            return response.Response;
        }
    }
}