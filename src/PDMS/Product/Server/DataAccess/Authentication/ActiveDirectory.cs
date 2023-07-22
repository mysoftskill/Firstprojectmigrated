namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Authentication
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Graph;
    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;

    /// <summary>
    /// A concrete implementation for the active directory APIs.
    /// </summary>
    public class ActiveDirectory : IActiveDirectory
    {
        private readonly IGraphServiceClientFactory graphServiceClientFactory;
        private readonly ISessionFactory sessionFactory;
        private readonly IAzureActiveDirectoryProviderConfig configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActiveDirectory"/> class.
        /// </summary>
        /// <param name="graphServiceClientFactory">The graph client factory.</param>
        /// <param name="sessionFactory">The sessionFactory.</param>
        /// <param name="configuration">The configuration.</param>
        public ActiveDirectory(IGraphServiceClientFactory graphServiceClientFactory, ISessionFactory sessionFactory, IAzureActiveDirectoryProviderConfig configuration)
        {
            this.graphServiceClientFactory = graphServiceClientFactory;
            this.sessionFactory = sessionFactory;
            this.configuration = configuration;
        }

        /// <summary>
        /// Given an authenticated principal, retrieves the associated security group ids.
        /// </summary>
        /// <param name="principal">The authenticated principal.</param>
        /// <returns>The security group ids.</returns>
        public async Task<IEnumerable<Guid>> GetSecurityGroupIdsAsync(AuthenticatedPrincipal principal)
        {
            if (principal.UserId == null)
            {
                return Enumerable.Empty<Guid>(); // S2S based auth cannot be used for getting security groups.
            }
            else if (this.configuration.EnableIntegrationTestOverrides && principal.UserId == this.configuration.IntegrationTestUserName)
            {
                return this.configuration.IntegrationTestSecurityGroups.Select(s => Guid.Parse(s));
            }
            else
            {
                var graphClient = this.graphServiceClientFactory.Create(principal, this.sessionFactory);

                // Gets all of the groups the user is a member of, including nested groups, and filters them
                // for groups with security enabled.
                var allMySecurityGroups = await this.GetAllMySecurityGroups(graphClient).ConfigureAwait(false);

                return allMySecurityGroups;
            }
        }

        /// <summary>
        /// Determines if a security group id exists or not.
        /// This is necessary for scenarios where the user does not need to be in the security group.
        /// </summary>
        /// <param name="principal">The current authenticated user.</param>
        /// <param name="id">The id of the security group.</param>
        /// <returns>True if it exists. Otherwise, false.</returns>
        public async Task<bool> SecurityGroupIdExistsAsync(AuthenticatedPrincipal principal, Guid id)
        {
            if (this.configuration.EnableIntegrationTestOverrides)
            {
                // This is to avoid breaking the existing tests.
                // Since we do not have a user context in automation, we do not call the graph.
                return true;
            }
            else
            {
                var graphClient = this.graphServiceClientFactory.Create(principal, this.sessionFactory);
                var group = await this.GetGroupByIdAsync(graphClient.Groups[id.ToString()].Request()).ConfigureAwait(false);
                return group?.SecurityEnabled == true;
            }
        }

        private Task<Group> GetGroupByIdAsync(IGroupRequest request)
        {
            return this.sessionFactory.InstrumentAsync<Group, ServiceException>(
                "ActiveDirectory.GetGroupByIdAsync",
                SessionType.Outgoing,
                async () =>
                {
                    try
                    {
                        return await request.GetAsync().ConfigureAwait(false);
                    }
                    catch (ServiceException ex)
                    {
                        if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            return null;
                        }
                        else
                        {
                            throw;
                        }
                    }
                });
        }

        private Task<IUserTransitiveMemberOfCollectionWithReferencesPage> GetTransitiveMemberOfAsync(IUserTransitiveMemberOfCollectionWithReferencesRequest request)
        {
            return this.sessionFactory.InstrumentAsync<IUserTransitiveMemberOfCollectionWithReferencesPage, ServiceException>("ActiveDirectory.GetTransitiveMemberOfAsync", SessionType.Outgoing, request.GetAsync);
        }

        // Get the security group ids from the given page result.
        private IEnumerable<Guid> GetSecurityGroups(IUserTransitiveMemberOfCollectionWithReferencesPage result, IEnumerable<Guid> existingGroups)
        {
            return existingGroups.Concat(
                                    result.CurrentPage
                                    .Select(item => item as Group)
                                    .Where(group => group != null && group.SecurityEnabled.HasValue && group.SecurityEnabled.Value)
                                    .Select(group => Guid.Parse(group.Id)));
        }

        private async Task<IEnumerable<Guid>> GetAllMySecurityGroups(IGraphServiceClient graphClient)
        {
            var request = graphClient.Me.TransitiveMemberOf.Request();

            var pageResult = await this.GetTransitiveMemberOfAsync(request).ConfigureAwait(false);

            var securityGroups = this.GetSecurityGroups(pageResult, Enumerable.Empty<Guid>());

            while (pageResult.NextPageRequest != null)
            {
                pageResult = await this.GetTransitiveMemberOfAsync(pageResult.NextPageRequest).ConfigureAwait(false);
                securityGroups = this.GetSecurityGroups(pageResult, securityGroups);
            }

            return securityGroups;
        }
    }
}