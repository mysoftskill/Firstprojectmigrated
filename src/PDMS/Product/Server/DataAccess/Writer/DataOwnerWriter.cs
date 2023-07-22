namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Writer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using System.Linq;
    using System.Threading.Tasks;

    using AutoMapper;

    using Microsoft.PrivacyServices.DataManagement.Client;
    using Microsoft.PrivacyServices.DataManagement.Client.ServiceTree;
    using Microsoft.PrivacyServices.DataManagement.Common;
    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authentication;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authorization;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Icm;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Reader;
    using Microsoft.PrivacyServices.DataManagement.Models;
    using Microsoft.PrivacyServices.DataManagement.Models.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;

    /// <summary>
    /// Provides methods for writing data owner information.
    /// </summary>
    public class DataOwnerWriter : NamedEntityWriter<DataOwner, DataOwnerFilterCriteria>, IDataOwnerWriter
    {
        /// <summary>
        /// Creates an invalid property exception.
        /// </summary>
        public static readonly CreateException InvalidServiceTreeProperty = (target, value, message) => new ConflictException(ConflictType.InvalidValue_Immutable, message, $"serviceTree.{target}", value);

        private readonly IEventWriterFactory eventWriterFactory;
        private readonly IAuthenticationProviderFactory authenticationProviderFactory;
        private readonly IServiceTreeClient serviceTreeClient;
        private readonly IValidator validator;
        private readonly IDataAgentReader dataAgentReader;
        private readonly IAssetGroupReader assetGroupReader;
        private readonly IActiveDirectory activeDirectory;
        private readonly IIcmConnector icmConnector;

        private readonly string componentName = nameof(DataOwnerWriter);

        /// <summary>
        /// Initializes a new instance of the <see cref="DataOwnerWriter" /> class.
        /// </summary>
        /// <param name="storageWriter">The storage writer instance.</param>
        /// <param name="entityReader">The entity reader instance.</param>
        /// <param name="authenticatedPrincipal">The principal object created for this specific request.</param>
        /// <param name="authorizationProvider">The authorization provider.</param>
        /// <param name="dateFactory">The date factory instance.</param>
        /// <param name="mapper">A utility for mapping data types by convention.</param>
        /// <param name="authenticationProviderFactory">The authentication provider factory.</param>
        /// <param name="serviceTreeClient">The service tree client to get service information.</param>
        /// <param name="validator">The validator instance.</param>
        /// <param name="dataAgentReader">The data agent reader instance.</param>
        /// <param name="assetGroupReader">The asset group reader instance.</param>
        /// <param name="activeDirectory">The active directory instance.</param>
        /// <param name="eventWriterFactory">The event writer factory instance.</param>
        /// <param name="icmConnector">The ICM connector.</param>
        public DataOwnerWriter(
            IPrivacyDataStorageWriter storageWriter,
            IDataOwnerReader entityReader,
            AuthenticatedPrincipal authenticatedPrincipal,
            IAuthorizationProvider authorizationProvider,
            IDateFactory dateFactory,
            IMapper mapper,
            IAuthenticationProviderFactory authenticationProviderFactory,
            IServiceTreeClient serviceTreeClient,
            IValidator validator,
            IDataAgentReader dataAgentReader,
            IAssetGroupReader assetGroupReader,
            IActiveDirectory activeDirectory,
            IEventWriterFactory eventWriterFactory,
            IIcmConnector icmConnector)
            : base(
                  storageWriter,
                  entityReader,
                  authenticatedPrincipal,
                  authorizationProvider,
                  dateFactory,
                  mapper,
                  eventWriterFactory)
        {
            this.authenticationProviderFactory = authenticationProviderFactory;
            this.serviceTreeClient = serviceTreeClient;
            this.validator = validator;
            this.dataAgentReader = dataAgentReader;
            this.assetGroupReader = assetGroupReader;
            this.activeDirectory = activeDirectory;
            this.eventWriterFactory = eventWriterFactory;
            this.icmConnector = icmConnector;

            this.AuthorizationRoles =
                AuthorizationRole.ServiceEditor |
                AuthorizationRole.NoCachedSecurityGroups;
        }

        /// <summary>
        /// Creates the service group name with proper formatting.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The name to use.</returns>
        public static string CreateServiceGroupName(string name)
        {
            return $"(SG) {name}";
        }

        /// <summary>
        /// Creates the team group name with proper formatting.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The name to use.</returns>
        public static string CreateTeamGroupName(string name)
        {
            return $"(TG) {name}";
        }

        /// <summary>
        /// Creates the service name with proper formatting.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The name to use.</returns>
        public static string CreateServiceName(string name)
        {
            return name;
        }

        /// <summary>
        /// Validate the properties of the incoming entity.
        /// </summary>
        /// <param name="action">The write action for the request.</param>
        /// <param name="incomingEntity">The incoming entity.</param>
        public override void ValidateProperties(WriteAction action, DataOwner incomingEntity)
        {
            ValidationModule.PropertyShouldNotBeSet(incomingEntity.DataAgents, "dataAgents", false);
            ValidationModule.PropertyShouldNotBeSet(incomingEntity.AssetGroups, "assetGroups", false);
            if (action == WriteAction.Create)
            {
                ValidationModule.PropertyShouldNotBeSet(incomingEntity.HasInitiatedTransferRequests, "hasInitiatedTransferRequests", false);
                ValidationModule.PropertyShouldNotBeSet(incomingEntity.HasPendingTransferRequests, "hasPendingTransferRequests", false);
            }

            // Write security group is always required.
            ValidationModule.PropertyRequiredAndNotEmpty(incomingEntity.WriteSecurityGroups, "writeSecurityGroups");

            if (incomingEntity.ServiceTree == null)
            {
                base.ValidateProperties(action, incomingEntity);

                ValidationModule.PropertyRequiredAndNotEmpty(incomingEntity.AlertContacts, "alertContacts");
            }
            else
            {
                this.ValidateEntityProperties(action, incomingEntity);

                if (action == WriteAction.Create)
                {
                    this.DataOwnerPropertiesCannotBeSetExceptSecurityGroups(incomingEntity);
                    this.ServiceTreePropertiesCannotBeSetExceptServiceId(incomingEntity.ServiceTree, true);                    
                }
            }

            this.ValidateIcm(incomingEntity.Icm);
        }

        /// <summary>
        /// Ensure consistency between the incoming entity and any existing entities.
        /// </summary>
        /// <param name="action">The write action for the request.</param>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>A task that performs the checks.</returns>
        public override async Task ValidateConsistencyAsync(WriteAction action, DataOwner incomingEntity)
        {
            if (incomingEntity.ServiceTree == null)
            {
                await base.ValidateConsistencyAsync(action, incomingEntity).ConfigureAwait(false);
            }
            else
            {
                await this.ValidateEntityConsistencyAsync(action, incomingEntity).ConfigureAwait(false);

                if (action == WriteAction.Create)
                {
                    await this.ServiceTreeServiceIdMustExist(incomingEntity).ConfigureAwait(false);

                    // Require the user to be in the service admins for create.
                    this.AuthorizationRoles |= AuthorizationRole.ServiceTreeAdmin;
                    await this.AuthorizeAsync(action, incomingEntity).ConfigureAwait(false);

                    await this.ServiceTreeServiceIdShouldBeUnique(incomingEntity).ConfigureAwait(false);
                }
                else if (action == WriteAction.Update)
                {
                    var existingEntity = await this.GetExistingEntityAsync(incomingEntity).ConfigureAwait(false);

                    if (existingEntity.WriteSecurityGroups?.Any() != true)
                    {
                        // Refresh the admin user list. The cosmos pipeline takes 24hrs to refresh, which is slow.
                        // Also the pipeline cannot sync admin users for service group or team group nodes.
                        // If there is a failure, we can just fallback to the values already in the system.
                        await this.eventWriterFactory.SuppressExceptionAsync(
                            componentName,
                            "ServiceTreeServiceIdMustExist", () => this.ServiceTreeServiceIdMustExist(incomingEntity)).ConfigureAwait(false);

                        // Require the user to be in the service admins if there are no write security groups.
                        this.AuthorizationRoles |= AuthorizationRole.ServiceTreeAdmin;
                        await this.AuthorizeAsync(action, incomingEntity).ConfigureAwait(false);
                    }

                    this.ServiceTreeValuesAreImmutableOnUpdate(existingEntity, incomingEntity);

                    this.validator.Immutable(
                        existingEntity,
                        incomingEntity,
                        Validator.InvalidProperty,
                        nameof(DataOwner.ServiceTree),
                        nameof(DataOwner.TrackingDetails),
                        nameof(DataOwner.WriteSecurityGroups),
                        nameof(DataOwner.TagSecurityGroups),
                        nameof(DataOwner.TagApplicationIds),
                        nameof(DataOwner.SharingRequestContacts),
                        nameof(DataOwner.Icm), // Only security groups can change.
                        nameof(DataOwner.HasInitiatedTransferRequests),
                        nameof(DataOwner.HasPendingTransferRequests)); 
                }
            }

            // Common checks regardless of service tree.
            if (action == WriteAction.Create)
            {
                await this.AssertSecurityGroupsExist(incomingEntity.TagSecurityGroups).ConfigureAwait(false);
            }
            else if (action == WriteAction.Update)
            {
                var existingEntity = await this.GetExistingEntityAsync(incomingEntity).ConfigureAwait(false);

                if (existingEntity.TagSecurityGroups?.SequenceEqual(incomingEntity.TagSecurityGroups ?? Enumerable.Empty<Guid>()) == false)
                {
                    await this.AssertSecurityGroupsExist(incomingEntity.TagSecurityGroups).ConfigureAwait(false);
                }

                if (existingEntity.SharingRequestContacts?.Any() == true &&
                    incomingEntity.SharingRequestContacts?.Any() != true)
                {
                    throw new ConflictException(ConflictType.NullValue, "Sharing request contacts cannot be removed once set.", "sharingRequestContacts");
                }
            }

            // This validation must be the final validation that we perform.
            // It sends an incident to the team, and we do not want to perform that
            // if there is a chance that some other check may fail.
            // There is a risk that the call to DocDB may fail, 
            // but that should be the only failure at this point.
            if (action == WriteAction.Create && incomingEntity.Icm != null)
            {
                this.icmConnector.SendOwnerRegistrationConfirmationAsync(incomingEntity);
            }
            else if (action == WriteAction.Update && incomingEntity.Icm != null)
            {
                var existingEntity = await this.GetExistingEntityAsync(incomingEntity).ConfigureAwait(false);

                // Only send the confirmation if the value has changed to a new connector id.
                if (existingEntity.Icm == null || existingEntity.Icm.ConnectorId != incomingEntity.Icm.ConnectorId)
                {
                    this.icmConnector.SendOwnerRegistrationConfirmationAsync(incomingEntity);
                }
            }
        }

        /// <summary>
        /// Create the entity in storage.
        /// </summary>
        /// <param name="action">The write action for the request.</param>
        /// <param name="entity">The entity to be written.</param>
        /// <returns>A task that performs the checks.</returns>
        public override Task<DataOwner> WriteAsync(WriteAction action, DataOwner entity)
        {
            if (action == WriteAction.Create)
            {
                return this.StorageWriter.CreateDataOwnerAsync(entity);
            }
            else if (action == WriteAction.Update || action == WriteAction.SoftDelete)
            {
                return this.StorageWriter.UpdateDataOwnerAsync(entity);
            }
            else
            {
                return Task.FromException<DataOwner>(new NotImplementedException());
            }
        }

        /// <summary>
        /// Get the data owners linked to the incoming entity.
        /// </summary>
        /// <param name="action">The write action for the request.</param>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>The data owner list.</returns>
        public override async Task<IEnumerable<DataOwner>> GetDataOwnersAsync(WriteAction action, DataOwner incomingEntity)
        {
            var incomingSecurityGroups = incomingEntity.WriteSecurityGroups;

            if (incomingSecurityGroups == null || !incomingSecurityGroups.Any())
            {
                return null; // Exit early. Other validations will fail if no security groups are set.
            }
            else if (action == WriteAction.Update)
            {
                var existingEntity = await this.GetExistingEntityAsync(incomingEntity).ConfigureAwait(false);

                if (existingEntity == null)
                {
                    return null;
                }
                else
                {
                    return new[] { existingEntity, incomingEntity };
                }
            }
            else
            {
                return new[] { incomingEntity };
            }
        }

        /// <summary>
        /// This method finds the existing data owner that contains the provided service tree id and delete it. 
        /// It then updates the provided data owner with the given service tree id.
        /// </summary>
        /// <param name="dataOwner">The data owner to edit.</param>
        /// <returns>The updated data owner.</returns>
        public async Task<DataOwner> ReplaceServiceIdAsync(DataOwner dataOwner)
        {
            // Authorize to ensure user is in the provided data owner's write security groups.
            await this.AuthorizeAsync(WriteAction.Update, dataOwner).ConfigureAwait(false);

            // For re-authorization checks, ensure user is in the service admin list.
            this.AuthorizationRoles |= AuthorizationRole.ServiceTreeAdmin;

            // Validate parameters.            
            this.ServiceTreePropertiesCannotBeSetExceptServiceId(dataOwner.ServiceTree, false);

            var existingOwner = await this.GetExistingEntityAsync(dataOwner).ConfigureAwait(false);

            if (existingOwner == null)
            {
                throw new EntityNotFoundException(dataOwner.Id, "DataOwner");
            }

            if (!existingOwner.ETag.Equals(dataOwner.ETag, StringComparison.OrdinalIgnoreCase))
            {
                throw new ETagMismatchException("ETag mismatch.", null, dataOwner.ETag);
            }

            // Clear non-service tree based properties.
            existingOwner.ServiceTree = dataOwner.ServiceTree;
            existingOwner.AlertContacts = null;
            existingOwner.AnnouncementContacts = null;

            // Set tracking details.
            this.PopulateProperties(WriteAction.Update, existingOwner);

            // This performs the check and populates the values.
            await this.ServiceTreeServiceIdMustExist(existingOwner).ConfigureAwait(false);

            // Re-authorize now that service admins are copied over (with updated required roles)
            await this.AuthorizeAsync(WriteAction.Update, existingOwner).ConfigureAwait(false);

            // Find the data owner to replace.
            var existingServiceIdOwner = await this.FindByServiceTreeAsync(existingOwner.ServiceTree).ConfigureAwait(false);

            if (existingServiceIdOwner == null)
            {
                return await this.StorageWriter.UpdateDataOwnerAsync(existingOwner).ConfigureAwait(false);
            }
            else
            {
                // Ensure that the service id owner is not linked to anything.
                var agentTask = this.AssertNoLinkedDataAgentsAsync(existingServiceIdOwner);
                var assetTask = this.AssertNoLinkedAssetGroupsAsync(existingServiceIdOwner);
                await Task.WhenAll(agentTask, assetTask).ConfigureAwait(false);

                // Migrate the properties.
                existingOwner.Name = existingServiceIdOwner.Name;
                existingOwner.Description = existingServiceIdOwner.Description;
                existingOwner.TrackingDetails.EgressedOn = existingServiceIdOwner.TrackingDetails.EgressedOn;
                
                // Update the existing service id owner for soft delete.
                this.PopulateProperties(WriteAction.SoftDelete, existingServiceIdOwner);
                existingServiceIdOwner.IsDeleted = true;

                // Store the changes.
                var updatedValues = await this.StorageWriter.UpdateEntitiesAsync(new[] { existingOwner, existingServiceIdOwner }).ConfigureAwait(false);

                // Return the updated entity.
                return (DataOwner)updatedValues.Single(x => x.Id == existingOwner.Id);
            }
        }

        /// <summary>
        /// Calls function to get the existing entity and cache it if called the first time.
        /// </summary>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>The existing entity.</returns>
        protected override Task<DataOwner> GetExistingEntityAsync(DataOwner incomingEntity)
        {
            return this.MemoizeAsync(incomingEntity.Id, () => this.EntityReader.ReadByIdAsync(incomingEntity.Id, ExpandOptions.WriteProperties));
        }

        /// <summary>
        /// Check if the data owner properties are not set except write security groups.
        /// </summary>
        /// <param name="incomingEntity">The incoming entity.</param>
        private void DataOwnerPropertiesCannotBeSetExceptSecurityGroups(DataOwner incomingEntity)
        {
            ValidationModule.MutuallyExclusivePropertyShouldNotBeSet("serviceTree.serviceId", incomingEntity.Name, "name");
            ValidationModule.MutuallyExclusivePropertyShouldNotBeSet("serviceTree.serviceId", incomingEntity.Description, "description");

            ValidationModule.MutuallyExclusivePropertyShouldNotBeSet("serviceTree.serviceId", incomingEntity.AlertContacts, "alertContacts", false);
            ValidationModule.MutuallyExclusivePropertyShouldNotBeSet("serviceTree.serviceId", incomingEntity.AnnouncementContacts, "announcementContacts", false);
        }

        /// <summary>
        /// Check if the service tree properties are not set except service ID.
        /// </summary>
        /// <param name="serviceTree">The incoming entities' service tree property.</param>
        /// <param name="checkMetadataProperties">Whether or not the metadata properties should be validated.</param>
        private void ServiceTreePropertiesCannotBeSetExceptServiceId(ServiceTree serviceTree, bool checkMetadataProperties)
        {
            bool valueSet = false;

            Action<string, string> propertyRequiredAndNotEmpty = (value, prop) =>
            {
                if (!string.IsNullOrEmpty(value) && !valueSet)
                {
                    Guid id;
                    bool valid = Guid.TryParse(value, out id);

                    if (!valid || id == Guid.Empty)
                    {
                        throw new InvalidPropertyException(prop, value, "The service tree id must be a valid guid and not equal to Guid.Empty.");
                    }

                    valueSet = true;
                }
                else
                {
                    ValidationModule.PropertyShouldNotBeSet(value, prop);
                }
            };

            propertyRequiredAndNotEmpty(serviceTree.ServiceGroupId, "serviceTree.serviceGroupId");
            propertyRequiredAndNotEmpty(serviceTree.TeamGroupId, "serviceTree.teamGroupId");
            propertyRequiredAndNotEmpty(serviceTree.ServiceId, "serviceTree.serviceId");

            if (!valueSet)
            {
                ValidationModule.PropertyRequiredAndNotEmpty(serviceTree.ServiceId, "serviceTree.serviceId");
            }

            if (checkMetadataProperties)
            {
                ValidationModule.PropertyShouldNotBeSet(serviceTree.ServiceAdmins, "serviceTree.serviceAdmins", false);
                ValidationModule.PropertyShouldNotBeSet(serviceTree.DivisionId, "serviceTree.divisionId");
                ValidationModule.PropertyShouldNotBeSet(serviceTree.DivisionName, "serviceTree.divisionName");
                ValidationModule.PropertyShouldNotBeSet(serviceTree.OrganizationId, "serviceTree.organizationId");
                ValidationModule.PropertyShouldNotBeSet(serviceTree.OrganizationName, "serviceTree.organizationName");
                ValidationModule.PropertyShouldNotBeSet(serviceTree.ServiceGroupName, "serviceTree.serviceGroupName");
                ValidationModule.PropertyShouldNotBeSet(serviceTree.TeamGroupName, "serviceTree.teamGroupName");
                ValidationModule.PropertyShouldNotBeSet(serviceTree.ServiceName, "serviceTree.serviceName");
            }
        }

        /// <summary>
        /// Check if the service tree service id exists.
        /// </summary>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>Void task.</returns>
        private async Task ServiceTreeServiceIdMustExist(DataOwner incomingEntity)
        {
            // The order here matters for update scenarios.
            // Service links will have all three ids, so we must check for service first.
            if (!string.IsNullOrEmpty(incomingEntity.ServiceTree.ServiceId))
            {
                var service = await this.CallServiceTree(incomingEntity.ServiceTree.ServiceId, this.serviceTreeClient.ReadServiceWithExtendedProperties).ConfigureAwait(false);

                this.Mapper.Map(service, incomingEntity.ServiceTree);
                incomingEntity.Name = DataOwnerWriter.CreateServiceName(service.Name);
                incomingEntity.Description = service.Description;
            }
            else if (!string.IsNullOrEmpty(incomingEntity.ServiceTree.TeamGroupId))
            {
                var teamGroup = await this.CallServiceTree(incomingEntity.ServiceTree.TeamGroupId, this.serviceTreeClient.ReadTeamGroupWithExtendedProperties).ConfigureAwait(false);

                this.Mapper.Map(teamGroup, incomingEntity.ServiceTree);
                incomingEntity.Name = DataOwnerWriter.CreateTeamGroupName(teamGroup.Name);
                incomingEntity.Description = teamGroup.Description;
            }
            else if (!string.IsNullOrEmpty(incomingEntity.ServiceTree.ServiceGroupId))
            {
                var serviceGroup = await this.CallServiceTree(incomingEntity.ServiceTree.ServiceGroupId, this.serviceTreeClient.ReadServiceGroupWithExtendedProperties).ConfigureAwait(false);

                this.Mapper.Map(serviceGroup, incomingEntity.ServiceTree);
                incomingEntity.Name = DataOwnerWriter.CreateServiceGroupName(serviceGroup.Name);
                incomingEntity.Description = serviceGroup.Description;
            }
        }

        /// <summary>
        /// Call the service tree client to get the service with the data owner service tree service id.
        /// </summary>
        /// <typeparam name="T">The service tree node type.</typeparam>
        /// <param name="id">The id to use for the service tree query.</param>
        /// <param name="execute">The service tree client method to call.</param>
        /// <returns>The service tree service.</returns>
        private async Task<T> CallServiceTree<T>(string id, Func<Guid, RequestContext, Task<IHttpResult<T>>> execute)
        {
            Guid serviceId = Guid.Parse(id);

            var requestContext = new RequestContext
            {
                AuthenticationProvider = this.authenticationProviderFactory.CreateForClient()
            };

            try
            {
                var result = await this.MemoizeAsync<IHttpResult<T>>(serviceId, () => execute(serviceId, requestContext)).ConfigureAwait(false);

                return result.Response;
            }
            catch (NotFoundError notFoundError)
            {
                throw new ServiceNotFoundException(notFoundError.Id);
            }
        }

        /// <summary>
        /// Helper function to check if the service tree service id of the incomingEntity is unique in PDMS collection.
        /// </summary>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>Void task.</returns>
        private async Task ServiceTreeServiceIdShouldBeUnique(DataOwner incomingEntity)
        {
            var serviceTree = incomingEntity.ServiceTree;
            string propertyName = string.Empty;
            string propertyValue = string.Empty;

            Func<string, string, StringFilter> getFilter = (s, p) =>
            {
                if (!string.IsNullOrEmpty(s))
                {
                    propertyName = p;
                    propertyValue = s;
                }

                return new StringFilter(s, StringComparisonType.EqualsCaseSensitive);
            };

            var filterCriteria = new DataOwnerFilterCriteria
            {
                ServiceTree = new ServiceTreeFilterCriteria
                {
                    ServiceGroupId = getFilter(incomingEntity.ServiceTree.ServiceGroupId, "serviceGroupId"),
                    TeamGroupId = getFilter(incomingEntity.ServiceTree.TeamGroupId, "teamGroupId"),
                    ServiceId = getFilter(incomingEntity.ServiceTree.ServiceId, "serviceId"),
                }
            };

            var existingEntities = await this.EntityReader.ReadByFiltersAsync(filterCriteria, ExpandOptions.None).ConfigureAwait(false);

            if (existingEntities.Values.Any())
            {
                throw new ConflictException(ConflictType.AlreadyExists, "The service tree entity is already in use.", $"serviceTree.{propertyName}", propertyValue);
            }
        }

        /// <summary>
        /// Check if the service tree values are immutable.
        /// </summary>
        /// <param name="existingEntity">The existing entity.</param>
        /// <param name="incomingEntity">The incoming entity.</param>
        private void ServiceTreeValuesAreImmutableOnUpdate(DataOwner existingEntity, DataOwner incomingEntity)
        {
            if (existingEntity.ServiceTree == null)
            {
                throw new ConflictException(ConflictType.InvalidValue_Immutable, "On update, ServiceTree values are immutable.", "serviceTree");
            }

            this.validator.Immutable(
                existingEntity.ServiceTree, 
                incomingEntity.ServiceTree, 
                InvalidServiceTreeProperty, 
                nameof(ServiceTree.DivisionId),
                nameof(ServiceTree.DivisionName),
                nameof(ServiceTree.Level),
                nameof(ServiceTree.OrganizationId),
                nameof(ServiceTree.OrganizationName),
                nameof(ServiceTree.ServiceAdmins),
                nameof(ServiceTree.ServiceGroupName),
                nameof(ServiceTree.ServiceName),
                nameof(ServiceTree.TeamGroupName));
        }

        private async Task<DataOwner> FindByServiceTreeAsync(ServiceTree serviceTree)
        {
            var filterCriteria = new DataOwnerFilterCriteria
            {
                ServiceTree = new ServiceTreeFilterCriteria
                {
                    ServiceId = new StringFilter(serviceTree.ServiceId, StringComparisonType.EqualsCaseSensitive),
                    ServiceGroupId = new StringFilter(serviceTree.ServiceGroupId, StringComparisonType.EqualsCaseSensitive),
                    TeamGroupId = new StringFilter(serviceTree.TeamGroupId, StringComparisonType.EqualsCaseSensitive)
                },
                Count = 1
            };

            var existingServiceIdOwnerResult = await this.EntityReader.ReadByFiltersAsync(filterCriteria, ExpandOptions.WriteProperties).ConfigureAwait(false);

            if (existingServiceIdOwnerResult.Total == 0)
            {
                return null;
            }
            else if (existingServiceIdOwnerResult.Total > 1)
            {
                throw new InvalidOperationException("This action cannot be performed. Found multiple entities with the same service tree ids.");
            }

            return existingServiceIdOwnerResult.Values.Single();
        }

        private async Task AssertNoLinkedDataAgentsAsync(DataOwner entity)
        {
            var agentFilterCriteria = new DataAgentFilterCriteria
            {
                OwnerId = entity.Id,
                Count = 0 // We don't need results, just the total.
            };

            var agentResults = await this.dataAgentReader.ReadByFiltersAsync(agentFilterCriteria, ExpandOptions.None).ConfigureAwait(false);

            if (agentResults.Total > 0)
            {
                throw new ConflictException(ConflictType.AlreadyExists, "A data agent is already linked to the data owner.", "dataAgent.ownerId", entity.Id.ToString());
            }
        }

        private async Task AssertNoLinkedAssetGroupsAsync(DataOwner entity)
        {
            var assetFilterCriteria = new AssetGroupFilterCriteria
            {
                OwnerId = entity.Id,
                Count = 0 // We don't need results, just the total.
            };

            var assetResults = await this.assetGroupReader.ReadByFiltersAsync(assetFilterCriteria, ExpandOptions.None).ConfigureAwait(false);

            if (assetResults.Total > 0)
            {
                throw new ConflictException(ConflictType.AlreadyExists, "An asset group is already linked to the data owner.", "assetGroup.ownerId", entity.Id.ToString());
            }
        }

        private async Task AssertSecurityGroupsExist(IEnumerable<Guid> securityGroups)
        {
            var sgs = securityGroups ?? Enumerable.Empty<Guid>();

            foreach (var id in sgs)
            {
                var exists = await this.activeDirectory.SecurityGroupIdExistsAsync(this.AuthenticatedPrincipal, id).ConfigureAwait(false);

                if (!exists)
                {
                    throw new SecurityGroupNotFoundException(id);
                }
            }
        }
    }
}
 
 