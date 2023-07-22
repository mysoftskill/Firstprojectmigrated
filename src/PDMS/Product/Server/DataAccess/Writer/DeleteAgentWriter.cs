namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Writer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using AutoMapper;

    using Microsoft.PrivacyServices.DataManagement.Common;
    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authorization;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Icm;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Reader;
    using Microsoft.PrivacyServices.DataManagement.Models;
    using Microsoft.PrivacyServices.DataManagement.Models.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;
    using Microsoft.PrivacyServices.Policy;

    /// <summary>
    /// Provides methods for writing data agents.
    /// </summary>
    public class DeleteAgentWriter : DataAgentWriter<DeleteAgent, DeleteAgentFilterCriteria>, IDeleteAgentWriter
    {
        private static readonly ProtocolId[] Protocols = Policies.Current.Protocols.Set.Select(v => v.Id).ToArray();

        private static readonly ReleaseState[] ReleaseStates = new[]
        {
            ReleaseState.PreProd,
            ReleaseState.Prod
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteAgentWriter"/> class.
        /// </summary>
        /// <param name="storageWriter">The storage writer instance.</param>
        /// <param name="entityReader">The entity reader instance.</param>
        /// <param name="authenticatedPrincipal">The principal object created for this specific request.</param>
        /// <param name="authorizationProvider">The authorization provider.</param>
        /// <param name="dateFactory">The date factory instance.</param>
        /// <param name="mapper">A utility for mapping data types by convention.</param>
        /// <param name="dataOwnerReader">The reader for data owners.</param>
        /// <param name="validator">The validator instance.</param>
        /// <param name="icmConnector">The ICM connector.</param>
        /// <param name="eventWriterFactory">The event writer factory instance.</param>
        public DeleteAgentWriter(
            IPrivacyDataStorageWriter storageWriter,
            IDeleteAgentReader entityReader,
            AuthenticatedPrincipal authenticatedPrincipal,
            IAuthorizationProvider authorizationProvider,
            IDateFactory dateFactory,
            IMapper mapper,
            IDataOwnerReader dataOwnerReader,
            IValidator validator,
            IIcmConnector icmConnector,
            IEventWriterFactory eventWriterFactory)
            : base(
                  storageWriter,
                  entityReader,
                  authenticatedPrincipal,
                  authorizationProvider,
                  dateFactory,
                  mapper,
                  dataOwnerReader,
                  validator,
                  icmConnector,
                  eventWriterFactory)
        {
            this.AuthorizationRoles = AuthorizationRole.ServiceEditor;
        }

        /// <summary>
        /// Gets the set of valid protocols for this data agent.
        /// </summary>
        protected override ProtocolId[] ValidProtocols
        {
            get
            {
                return Protocols;
            }
        }

        /// <summary>
        /// Gets the set of valid release states for this data agent.
        /// </summary>
        protected override ReleaseState[] ValidReleaseStates
        {
            get
            {
                return ReleaseStates;
            }
        }

        /// <summary>
        /// Validate the properties of the incoming entity.
        /// </summary>
        /// <param name="action">The write action for the request.</param>
        /// <param name="incomingEntity">The incoming entity.</param>
        public override void ValidateProperties(WriteAction action, DeleteAgent incomingEntity)
        {
            base.ValidateProperties(action, incomingEntity);

            if (action == WriteAction.Create)
            {
                ValidationModule.PropertyShouldNotBeSet(incomingEntity.Capabilities, "capabilities");
            }

            // Temporary if statement to set these properties until PCD has implemented the new properties.
            if (incomingEntity.DeploymentLocation == null && incomingEntity.SupportedClouds == null)
            {
                incomingEntity.DeploymentLocation = Policies.Current.CloudInstances.Ids.Public;
                incomingEntity.SupportedClouds = new[] { Policies.Current.CloudInstances.Ids.Public };
            }

            ValidationModule.PropertyRequired(incomingEntity.DeploymentLocation, "deploymentLocation");
            ValidationModule.PropertyRequiredAndNotEmpty(incomingEntity.SupportedClouds, "supportedClouds");
            
            Func<string> getStringOfSupportedClouds = 
                () => string.Join(",", incomingEntity.SupportedClouds.Select(cloud => cloud.Value));

            if (incomingEntity.DeploymentLocation == Policies.Current.CloudInstances.Ids.Public)
            {
                if (incomingEntity.SupportedClouds.Distinct().Count() != incomingEntity.SupportedClouds.Count())
                {
                    throw new InvalidPropertyException(
                        "supportedClouds",
                        getStringOfSupportedClouds(),
                        "Agents cannot have duplicate SupportedClouds.");
                }
                else if (incomingEntity.SupportedClouds.Any(sc => sc == Policies.Current.CloudInstances.Ids.All) &&
                    incomingEntity.SupportedClouds.Count() > 1)
                {
                    throw new InvalidPropertyException(
                        "supportedClouds",
                        getStringOfSupportedClouds(),
                        "Agents with All CloudInstances in SupportedClouds should not have additional CloudInstances.");
                }
            }
            else if (!incomingEntity.SupportedClouds.Any(sc => sc == incomingEntity.DeploymentLocation) ||
                incomingEntity.SupportedClouds.Count() > 1)
            {
                throw new InvalidPropertyException(
                    "supportedClouds",
                    getStringOfSupportedClouds(),
                    "Agents with a Sovereign Cloud DeploymentLocation can only have the Sovereign Cloud in the SupportedClouds list");
            }
            // In case of sovereign cloud, data residency should not be set
            if(incomingEntity.DeploymentLocation != Policies.Current.CloudInstances.Ids.CN_Azure_Mooncake && incomingEntity.DeploymentLocation != Policies.Current.CloudInstances.Ids.US_Azure_Fairfax)
            {
                ValidationModule.PropertyRequired(incomingEntity.DataResidencyBoundary, "dataResidencyBoundary");
            }
            else if(incomingEntity.DataResidencyBoundary != null)
            {
                throw new InvalidPropertyException(
                    "dataResidencyBoundary",
                    incomingEntity.DataResidencyBoundary.Value,
                    "In case of sovereign cloud agent, data residency should not be set");
            }
        }

        /// <summary>
        /// Ensure consistency between the incoming entity and any existing entities.
        /// </summary>
        /// <param name="action">The write action for the request.</param>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>A task that performs the checks.</returns>
        public override async Task ValidateConsistencyAsync(WriteAction action, DeleteAgent incomingEntity)
        {
            await base.ValidateConsistencyAsync(action, incomingEntity).ConfigureAwait(false);

            if (incomingEntity.SharingEnabled)
            {
                var existingOwner = await this.GetExistingOwnerAsync(incomingEntity).ConfigureAwait(false);

                if (existingOwner.SharingRequestContacts?.Any() != true)
                {
                    throw new ConflictException(ConflictType.NullValue, "Owner must have sharing request contacts set to enable sharing.", "owner.sharingRequestContacts");
                }
            }
        }

        /// <summary>
        /// Get the data owners linked to the incoming entity.
        /// </summary>
        /// <param name="action">The write action for the request.</param>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>The data owner list.</returns>
        public override async Task<IEnumerable<DataOwner>> GetDataOwnersAsync(WriteAction action, DeleteAgent incomingEntity)
        {
            var incomingDataOwner = await this.GetDataOwnerForEntityAsync(incomingEntity).ConfigureAwait(false);

            if (incomingDataOwner == null)
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
                    var existingDataOwner = await this.GetDataOwnerForEntityAsync(existingEntity).ConfigureAwait(false);

                    return new[] { existingDataOwner, incomingDataOwner };
                }
            }
            else
            {
                return new[] { incomingDataOwner };
            }
        }

        private async Task<DataOwner> GetDataOwnerForEntityAsync(DeleteAgent entity)
        {
            if (entity != null)
            {
                var existingOwner = await this.GetExistingOwnerAsync(entity).ConfigureAwait(false);

                return existingOwner;
            }
            else
            {
                return null;
            }
        }
    }
}