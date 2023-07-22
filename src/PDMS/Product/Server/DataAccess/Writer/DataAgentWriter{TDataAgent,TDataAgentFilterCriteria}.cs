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
    /// The base class for all concrete data agent writers. Contains common behavior across data agents.
    /// </summary>
    /// <typeparam name="TDataAgent">The data agent type.</typeparam>
    /// <typeparam name="TDataAgentFilterCriteria">The data agent filter criteria type.</typeparam>
    public abstract class DataAgentWriter<TDataAgent, TDataAgentFilterCriteria>
        : NamedEntityWriter<TDataAgent, TDataAgentFilterCriteria>, IDataAgentWriter<TDataAgent>
        where TDataAgent : DataAgent
        where TDataAgentFilterCriteria : DataAgentFilterCriteria<TDataAgent>, new()
    {
        private readonly IDataOwnerReader dataOwnerReader;
        private readonly IValidator validator;
        private readonly IIcmConnector icmConnector;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataAgentWriter{TDataAgent,TDataAgentFilterCriteria}"/> class.
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
        protected DataAgentWriter(
            IPrivacyDataStorageWriter storageWriter,
            IDataAgentReader<TDataAgent> entityReader,
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
                  eventWriterFactory)
        {
            this.dataOwnerReader = dataOwnerReader;
            this.validator = validator;
            this.icmConnector = icmConnector;
        }

        /// <summary>
        /// Gets the set of valid protocols for this data agent.
        /// </summary>
        protected abstract ProtocolId[] ValidProtocols { get; }

        /// <summary>
        /// Gets the set of valid release states for this data agent.
        /// </summary>
        protected abstract ReleaseState[] ValidReleaseStates { get; }

        /// <summary>
        /// Ensure consistency between the incoming entity and any existing entities.
        /// </summary>
        /// <param name="action">The write action for the request.</param>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>A task that performs the checks.</returns>
        public override async Task ValidateConsistencyAsync(WriteAction action, TDataAgent incomingEntity)
        {
            await base.ValidateConsistencyAsync(action, incomingEntity).ConfigureAwait(false);

            if (action == WriteAction.Create)
            {
                this.SetPreProdAgentReadiness(incomingEntity);

                await this.OwnerShouldExist(incomingEntity).ConfigureAwait(false);
            }
            else if (action == WriteAction.Update)
            {
                var existingEntity = await this.GetExistingEntityAsync(incomingEntity).ConfigureAwait(false);

                if (incomingEntity.OwnerId != existingEntity.OwnerId && incomingEntity.OwnerId != Guid.Empty)
                {
                    await this.OwnerShouldExist(incomingEntity).ConfigureAwait(false);
                }

                // Must be a ServiceAdmin to alter connection details once PROD is set or
                // Production agent should be migrating in this update request
                if (existingEntity.ConnectionDetails.ContainsKey(ReleaseState.Prod))
                {
                    var isAuthorized = await this.AuthorizationProvider.TryAuthorizeAsync(AuthorizationRole.ServiceAdmin, null).ConfigureAwait(false);

                    Action<TDataAgent, TDataAgent> check = (a, b) =>
                    {
                        foreach (var connectionDetail in a.ConnectionDetails)
                        {
                            // All ConnectionDetail fields except AgentReadiness for release states except PreProd are immutable once PROD is set.
                            if (!isAuthorized && !IsProductionAgentMigrating(existingEntity, incomingEntity) && connectionDetail.Key != ReleaseState.PreProd)
                            {
                                if (!b.ConnectionDetails.ContainsKey(connectionDetail.Key))
                                {
                                    throw new ConflictException(ConflictType.InvalidValue_Immutable, "Connection details are immutable after PROD has been set.", $"connectionDetails[{connectionDetail.Key}]");
                                }

                                this.validator.Immutable(
                                    b.ConnectionDetails[connectionDetail.Key],
                                    connectionDetail.Value,
                                    (target, value, message) => Validator.ConflictInvalidValueImmutable($"connectionDetails[{connectionDetail.Key}].{target}", value, message),
                                    new string[] { "AgentReadiness", nameof(ConnectionDetail.AadAppIds) });


                                if (CompareListsofGuids(b.ConnectionDetails[connectionDetail.Key].AadAppIds, connectionDetail.Value.AadAppIds) == false)
                                {
                                    throw new ConflictException(ConflictType.InvalidValue_Immutable, "Connection details are immutable after PROD has been set.", $"connectionDetails[{connectionDetail.Key}]");
                                }
                            }
                        }
                    };

                    // Check both directions to catch any adds/removes.
                    check(incomingEntity, existingEntity);
                    check(existingEntity, incomingEntity);

                    if (!isAuthorized
                        && !IsProductionAgentMigrating(existingEntity, incomingEntity)
                        && existingEntity.ConnectionDetails[ReleaseState.Prod].AgentReadiness == AgentReadiness.ProdReady
                        && incomingEntity.ConnectionDetails[ReleaseState.Prod].AgentReadiness != AgentReadiness.ProdReady)
                    {
                        throw new ConflictException(ConflictType.InvalidValue_Immutable, "AgentReadiness cannot be changed once marked ProdReady.", "connectionDetails[Prod].agentReadiness", incomingEntity.ConnectionDetails[ReleaseState.Prod].AgentReadiness.ToString());
                    }

                    if (existingEntity.InProdDate != incomingEntity.InProdDate)
                    {
                        throw new ConflictException(ConflictType.InvalidValue_Immutable, "InProdDate value cannot be changed.", "deleteAgent.InProdDate", incomingEntity.InProdDate.ToString());
                    }
                }

                // Ensure the default value is correct for new pre-prod connection details.
                if (!existingEntity.ConnectionDetails.ContainsKey(ReleaseState.PreProd))
                {
                    this.SetPreProdAgentReadiness(incomingEntity);
                }

                // On update, require authentication values to be different only if they are changing
                // and the protocol is not for the Cosmos Delete feed.
                foreach (var key in existingEntity.ConnectionDetails.Keys)
                {
                    if (incomingEntity.ConnectionDetails.ContainsKey(key) &&
                        incomingEntity.ConnectionDetails[key].Protocol != Policies.Current.Protocols.Ids.CosmosDeleteSignalV2 &&
                       (existingEntity.ConnectionDetails[key].AadAppId != incomingEntity.ConnectionDetails[key].AadAppId ||
                        existingEntity.ConnectionDetails[key].MsaSiteId != incomingEntity.ConnectionDetails[key].MsaSiteId))
                    {
                        this.ValidateAuthenticationPropertiesUnique(incomingEntity.ConnectionDetails);
                    }
                }
            }

            // Passed all validation checks of incomingEntity against existingEntity connectionDetails.
            // Use existing AadAppIds lists to update AadAppId single fields for V1 agents
            var incomingConnectionDetail = incomingEntity.ConnectionDetails.FirstOrDefault().Value;
            if (IsV1Agent(incomingConnectionDetail))
            {
                foreach (var releaseState in incomingEntity.ConnectionDetails.Keys)
                {
                    if (!(incomingEntity.ConnectionDetails[releaseState].AadAppIds == null || !incomingEntity.ConnectionDetails[releaseState].AadAppIds.Any()))
                    {
                        incomingEntity.ConnectionDetails[releaseState].AadAppId = (Guid?)incomingEntity.ConnectionDetails[releaseState].AadAppIds.Last();
                    }
                }
            }

            // ProdReady Agent must have an ICM Connector
            var existingOwner = await this.GetExistingOwnerAsync(incomingEntity).ConfigureAwait(false);
            if (incomingEntity.ConnectionDetails.ContainsKey(ReleaseState.Prod)
                        && incomingEntity.ConnectionDetails[ReleaseState.Prod].AgentReadiness == AgentReadiness.ProdReady)
            {
                if (existingOwner.Icm == null && incomingEntity.Icm == null)
                {
                    throw new ConflictException(ConflictType.NullValue, "Agent or Owner must have a connector Id", "IcmConnectorId");
                }
            }

            // This validation must be the final validation that we perform.
            // It sends an incident to the team, and we do not want to perform that
            // if there is a chance that some other check may fail.
            // There is a risk that the call to DocDB may fail, 
            // but that should be the only failure at this point.
            if (action == WriteAction.Create && incomingEntity.Icm != null)
            {
                this.icmConnector.SendAgentRegistrationConfirmationAsync(existingOwner, incomingEntity);
            }
            else if (action == WriteAction.Update && incomingEntity.Icm != null)
            {
                var existingEntity = await this.GetExistingEntityAsync(incomingEntity).ConfigureAwait(false);

                // Only send the confirmation if the value has changed to a new connector id.
                if (existingEntity.Icm == null || existingEntity.Icm.ConnectorId != incomingEntity.Icm.ConnectorId)
                {
                    this.icmConnector.SendAgentRegistrationConfirmationAsync(existingOwner, incomingEntity);
                }
            }

            this.SetAgentInProdDate(incomingEntity);
        }

        // The production connection details are immutable except by System Admin or when the agent is in production migration
        // this method verifies if the agent is actually migrating their production protocol with this update request.
        // if yes (returns: true), then the changes are permitted without System Admin permissions. If not, return false.
        private bool IsProductionAgentMigrating(TDataAgent existingEntity, TDataAgent incomingEntity)
        {
            // If the agent not in production migration or production rollback, return false
            if (incomingEntity.MigratingConnectionDetails == null
                || !incomingEntity.MigratingConnectionDetails.Any()
                || !incomingEntity.MigratingConnectionDetails.ContainsKey(ReleaseState.Prod))
            {
                return false;
            }

            // if the MigratingConnectionDetails hasn't changed, then this change didn't involve a migration change. Return false
            if (existingEntity.MigratingConnectionDetails == null
                || !existingEntity.MigratingConnectionDetails.Any()
                || !existingEntity.MigratingConnectionDetails.ContainsKey(ReleaseState.Prod))
            {
                return true;
            }

            var incomingConnectionDetails = incomingEntity.MigratingConnectionDetails[ReleaseState.Prod];
            var existingConnectionDetails = existingEntity.MigratingConnectionDetails[ReleaseState.Prod];

            if ((IsV1Agent(incomingConnectionDetails) && !IsV1Agent(existingConnectionDetails))  // production migration
               || (!IsV1Agent(incomingConnectionDetails) && IsV1Agent(existingConnectionDetails))) // production rollback
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Validate the properties of the incoming entity.
        /// </summary>
        /// <param name="action">The write action for the request.</param>
        /// <param name="incomingEntity">The incoming entity.</param>
        public override void ValidateProperties(WriteAction action, TDataAgent incomingEntity)
        {
            base.ValidateProperties(action, incomingEntity);

            ValidationModule.PropertyRequired(incomingEntity.OwnerId, "ownerId");
            ValidationModule.PropertyRequiredAndNotEmpty(incomingEntity.ConnectionDetails, "connectionDetails");
            ValidateConnectionDetails(incomingEntity.ConnectionDetails);
            ValidateMigratingConnectionDetails(action, incomingEntity);

            // For update, we only make this check if the values have changed.
            // This is so that we grandfather in existing users data
            // and continue to allow those users to make changes to Name/Description.
            if (action == WriteAction.Create)
            {
                ValidationModule.PropertyShouldNotBeSet(incomingEntity.InProdDate, "inProdDate");
                this.ValidateAuthenticationPropertiesUnique(incomingEntity.ConnectionDetails);
            }

            this.ValidateIcm(incomingEntity.Icm);
        }

        /// <summary>
        /// Create the entity in storage.
        /// </summary>
        /// <param name="action">The write action for the request.</param>
        /// <param name="entity">The entity to be written.</param>
        /// <returns>A task that performs the checks.</returns>
        public override Task<TDataAgent> WriteAsync(WriteAction action, TDataAgent entity)
        {
            if (action == WriteAction.Create)
            {
                return this.StorageWriter.CreateDataAgentAsync(entity);
            }
            else if (action == WriteAction.Update || action == WriteAction.SoftDelete)
            {
                return this.StorageWriter.UpdateDataAgentAsync(entity);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        internal static bool CompareListsofGuids(IEnumerable<Guid> list1, IEnumerable<Guid> list2)
        {
            if (list1 == null || list2 == null)
            {
                var count1 = list1 == null ? 0 : list1.Count();
                var count2 = list2 == null ? 0 : list2.Count();

                if (count1 != count2)
                {
                    return false;
                }
            }
            else
            {
                if (!list1.Select(x => x.ToString()).OrderBy(e => e).SequenceEqual(list2.Select(x => x.ToString()).OrderBy(i => i)))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Verify the connection detail are valid for the given protocol.
        /// </summary>
        /// <param name="connectionDetail">The connection detail to validate.</param>
        internal static void ValidateConnectionDetail(ConnectionDetail connectionDetail)
        {
            if (connectionDetail.Protocol == Policies.Current.Protocols.Ids.CommandFeedV1)
            {
                if (connectionDetail.AuthenticationType == AuthenticationType.AadAppBasedAuth)
                {
                    bool aadAppIdFieldInvalid, aadAppIdsFieldInvalid;

                    // Temporary individual calls to PropertyRequired() until optional properties are implemented.
                    // Enums casted to objects to prevent incorrectly throwing MissingPropertyException.
                    ValidationModule.PropertyRequired(connectionDetail.Protocol, "protocol");
                    ValidationModule.PropertyRequired(Enum.ToObject(typeof(AuthenticationType), connectionDetail.AuthenticationType), "authenticationType");
                    ValidationModule.PropertyRequired(Enum.ToObject(typeof(ReleaseState), connectionDetail.ReleaseState), "releaseState");
                    ValidationModule.PropertyRequired(Enum.ToObject(typeof(AgentReadiness), connectionDetail.AgentReadiness), "agentReadiness");

                    // Treat an empty AadAppIds list as null to prevent misinterpretation by Validation Module.
                    if (connectionDetail.AadAppIds != null && connectionDetail.AadAppIds.Count() == 0)
                    {
                        connectionDetail.AadAppIds = null;
                    }

                    aadAppIdFieldInvalid = connectionDetail.AadAppId == null || ((Guid)connectionDetail.AadAppId).Equals(default(Guid));
                    aadAppIdsFieldInvalid = connectionDetail.AadAppIds == null || !connectionDetail.AadAppIds.Any();

                    // AadAppId and AadAppIds are mutually exclusive fields. Exactly one is required.
                    if (aadAppIdFieldInvalid && aadAppIdsFieldInvalid)
                    {
                        throw new MissingPropertyException("aadAppId(s)", "AAD Authentication requires at least one entry in either aadAppId or aadAppIds.");
                    }
                    else if (!aadAppIdFieldInvalid)
                    {
                        ValidationModule.MutuallyExclusivePropertyShouldNotBeSet("aadAppId", connectionDetail.AadAppIds, "aadAppIds");
                    }
                    else if (!aadAppIdsFieldInvalid)
                    {
                        ValidationModule.MutuallyExclusivePropertyShouldNotBeSet("aadAppIds", connectionDetail.AadAppId, "aadAppId");

                        foreach (Guid appId in connectionDetail.AadAppIds)
                        {
                            if (Guid.Empty == appId)
                                throw new MissingPropertyException("AadAppIds",
                                    "The list must not contain empty Guids. IE: 00000000-0000-0000-0000-000000000000.");
                        }
                    }
                }
                else
                {
                    ValidationModule.RequireProperties(
                            connectionDetail,
                            nameof(connectionDetail.Protocol),
                            nameof(connectionDetail.AuthenticationType),
                            nameof(connectionDetail.MsaSiteId),
                            nameof(connectionDetail.ReleaseState),
                            nameof(connectionDetail.AgentReadiness));

                    RequiredAuthenticationType(connectionDetail, AuthenticationType.MsaSiteBasedAuth);
                }
            }
            else if (connectionDetail.Protocol == Policies.Current.Protocols.Ids.CosmosDeleteSignalV2)
            {
                ValidationModule.RequireProperties(connectionDetail, nameof(connectionDetail.Protocol), nameof(connectionDetail.ReleaseState), nameof(connectionDetail.AgentReadiness));
            }
            else if (!IsV1Agent(connectionDetail))
            {
                connectionDetail.AadAppId = null; // This field is not used for V2 agent protocols.
                                                   // With this we're making sure that it's not set by mistake
                                                   // and for those existing V2 agents with non-null AadAppId,
                                                   // this should update the agents to have null AadAppId.
                ValidationModule.RequireProperties(connectionDetail, nameof(connectionDetail.Protocol), nameof(connectionDetail.ReleaseState), nameof(connectionDetail.AgentReadiness), nameof(connectionDetail.AadAppIds), nameof(connectionDetail.AuthenticationType));
            }
            else
            {
                throw new InvalidPropertyException($"connectionDetails[{connectionDetail.ReleaseState}].protocol", connectionDetail.Protocol?.Value, "Unkown protocol value.");
            }
        }

        /// <summary>
        /// Validate MigratingConnectionDetails
        /// 1. Should pass general ConnectionDetails validation rules
        /// 2. Stage 1/1R protocol should only be V2 or Batch
        /// 3. AppIds should be between PPE and Prod across both MigratingConnectionDetails and ConnectionDetails
        /// </summary>
        /// <param name="action">The action being performed on the Agent</param>
        /// <param name="incomingEntity">DataAgent</param>
        internal void ValidateMigratingConnectionDetails(WriteAction action, TDataAgent incomingEntity)
        {
            var migratingConnectionDetails = incomingEntity.MigratingConnectionDetails;
            if (action == WriteAction.Create && migratingConnectionDetails != null)
            {
                throw new InvalidPropertyException("migratingConnectionDetails", $"{migratingConnectionDetails}", "migratingConnectionDetails should be null for a new data agent.");
            }

            if (migratingConnectionDetails != null && migratingConnectionDetails.Any())
            {
                ValidateConnectionDetails(migratingConnectionDetails);

                // since the protocol is the same for all elements within the ConnectionDetails, lets just check with First element.
                var connectionDetail = incomingEntity.ConnectionDetails.Values.First();
                var migratingConnectionDetail = migratingConnectionDetails.Values.First();

                if (IsV1Agent(connectionDetail))
                {
                    // PPE Migration or rollback
                    if (IsV1Agent(migratingConnectionDetail))
                    {
                        throw new InvalidPropertyException("migratingConnectionDetail.protocol", $"{migratingConnectionDetail.Protocol}", $"Migrating Agent protocol should be PCFV2Batch or CommandFeedV2.");
                    }

                    if (incomingEntity.ConnectionDetails.ContainsKey(ReleaseState.PreProd))
                    {
                        throw new InvalidPropertyException("incomingEntity.ConnectionDetails", $"{incomingEntity.ConnectionDetails.ToArray()}", $"Migrating Agent ConnectionDetails should not have PreProd ReleaseState.");
                    }
                }

                if (!IsV1Agent(connectionDetail) && !IsV1Agent(migratingConnectionDetail))
                {
                    throw new InvalidPropertyException("MigratingConnectionDetail.protocol", $"{migratingConnectionDetail.Protocol}", $"Migrating Agent protocol should be CosmosDeleteSignalV2 or CommandFeedV1.");
                }

                // ConnectionDetails should always have Prod ReleaseState during migration
                if (!incomingEntity.ConnectionDetails.ContainsKey(ReleaseState.Prod))
                {
                    throw new InvalidPropertyException("incomingEntity.ConnectionDetails", $"{incomingEntity.ConnectionDetails}", $"Migrating Agent should always have Prod ReleaseState in ConnectionDetails.");
                }

                if (migratingConnectionDetails.ContainsKey(ReleaseState.Prod)
                    && incomingEntity.ConnectionDetails.ContainsKey(ReleaseState.Prod)
                    && (migratingConnectionDetails[ReleaseState.Prod].AgentReadiness != incomingEntity.ConnectionDetails[ReleaseState.Prod].AgentReadiness))
                {
                    throw new InvalidPropertyException("MigratingConnectionDetail.AgentReadiness", $"{migratingConnectionDetail.AgentReadiness}", $"Migrating Agent AgentReadiness should be same as in V1 ConnectionDetails.");
                }

                if (migratingConnectionDetails.ContainsKey(ReleaseState.PreProd) && incomingEntity.ConnectionDetails.ContainsKey(ReleaseState.Prod))
                {
                    var crossConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>
                    {
                        { ReleaseState.Prod, incomingEntity.ConnectionDetails[ReleaseState.Prod] },
                        { ReleaseState.PreProd, migratingConnectionDetails[ReleaseState.PreProd] }
                    };
                    this.ValidateAuthenticationPropertiesUnique(crossConnectionDetails);
                }

                if (migratingConnectionDetails.ContainsKey(ReleaseState.Prod) && incomingEntity.ConnectionDetails.ContainsKey(ReleaseState.PreProd))
                {
                    var crossConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>
                    {
                        { ReleaseState.PreProd, incomingEntity.ConnectionDetails[ReleaseState.PreProd] },
                        { ReleaseState.Prod, migratingConnectionDetails[ReleaseState.Prod] }
                    };
                    this.ValidateAuthenticationPropertiesUnique(crossConnectionDetails);
                }
            }
        }

        /// <summary>
        /// Authorize the given write action.
        /// </summary>
        /// <param name="action">The write action.</param>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>A task for authorization.</returns>
        protected override async Task AuthorizeAsync(WriteAction action, TDataAgent incomingEntity)
        {
            if (action == WriteAction.SoftDelete)
            {
                // Must be a ServiceEditor to delete a data agent.
                var requiredRoles = AuthorizationRole.ServiceEditor;
                await this.AuthorizationProvider.AuthorizeAsync(requiredRoles, () => this.GetDataOwnersAsync(action, incomingEntity)).ConfigureAwait(false);
                return;
            }

            await base.AuthorizeAsync(action, incomingEntity).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieve the owner of the given agent.
        /// </summary>
        /// <param name="incomingEntity">The agent to query.</param>
        /// <returns>The corresponding owner.</returns>
        protected Task<DataOwner> GetExistingOwnerAsync(TDataAgent incomingEntity)
        {
            return this.MemoizeAsync(incomingEntity.OwnerId, () => this.dataOwnerReader.ReadByIdAsync(incomingEntity.OwnerId, ExpandOptions.ServiceTree)); // Need to expand service tree for use in ICM incident validation.
        }

        /// <summary>
        /// Helper function to check if the linked data owner exists.
        /// </summary>
        /// <param name="incomingEntity">The incoming entity.</param>
        /// <returns>Void task.</returns>
        private async Task OwnerShouldExist(TDataAgent incomingEntity)
        {
            var existingOwner = await this.GetExistingOwnerAsync(incomingEntity).ConfigureAwait(false);

            if (existingOwner == null)
            {
                throw new ConflictException(ConflictType.DoesNotExist, "The referenced entity does not exist", "ownerId", incomingEntity.OwnerId.ToString());
            }
        }

        private static void RequiredAuthenticationType(ConnectionDetail connectionDetails, AuthenticationType expectedValue)
        {
            ValidationModule.ValidateProperty(
                    () => { return connectionDetails.AuthenticationType.Value != expectedValue; },
                    $"connectionDetails[{connectionDetails.ReleaseState}].authenticationType",
                    $"Authentication type value is not correct. Expected: {expectedValue.ToString()}.");
        }

        private static bool IsV1Agent(ConnectionDetail connectionDetail)
        {
            if (connectionDetail.Protocol == Policies.Current.Protocols.Ids.CommandFeedV1
                        || connectionDetail.Protocol == Policies.Current.Protocols.Ids.CosmosDeleteSignalV2)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sets the 'In Prod' date for the agent.
        /// </summary>
        /// <param name="entity">The agent object.</param>
        private void SetAgentInProdDate(TDataAgent entity)
        {
            if (entity.ConnectionDetails.ContainsKey(ReleaseState.Prod))
            {
                if (entity.InProdDate == null &&
                    entity.ConnectionDetails[ReleaseState.Prod].AgentReadiness == AgentReadiness.ProdReady)
                {
                    entity.InProdDate = this.DateFactory.GetCurrentTime();
                }
            }
        }

        private void SetPreProdAgentReadiness(TDataAgent entity)
        {
            foreach (var connectionDetail in entity.ConnectionDetails.Values)
            {
                if (connectionDetail.ReleaseState == ReleaseState.PreProd)
                {
                    connectionDetail.AgentReadiness = AgentReadiness.ProdReady;
                }
            }
        }

        private void ValidateAuthenticationPropertiesUnique(IDictionary<ReleaseState, ConnectionDetail> connectionDetails)
        {
            long? preprodSiteId = null;
            Guid? preprodAppId;
            IEnumerable<Guid> preprodAppIds = null;
            IEnumerable<Guid> prodOrRingAppIds;

            foreach (var connectionDetail in connectionDetails.Values)
            {
                if (connectionDetails.ContainsKey(ReleaseState.PreProd))
                {
                    preprodSiteId = connectionDetails[ReleaseState.PreProd].MsaSiteId;

                    // Mutually Exclusive fields, at most one can exist.
                    preprodAppId = connectionDetails[ReleaseState.PreProd].AadAppId;
                    preprodAppIds = connectionDetails[ReleaseState.PreProd].AadAppIds;

                    // An Enumerable is required to use intersection later on to check for duplicates.
                    if (preprodAppId.HasValue)
                    {
                        preprodAppIds = Enumerable.Empty<Guid>().Append((Guid)preprodAppId);
                    }
                    else if (!(preprodAppIds == null || !preprodAppIds.Any()))
                    {
                        // Check for duplicates in the existing list.
                        if (preprodAppIds.Count() != preprodAppIds.Distinct().Count())
                        {
                            throw new InvalidPropertyException(
                                $"connectionDetails[{ReleaseState.PreProd}].aadAppIds",
                                preprodAppIds.Distinct().First().ToString(),
                                "Duplicate AAD App IDs cannot exist in the same list.");
                        }
                    }
                }

                // PreProd connection detail authentication properties cannot be shared with Prod or Ring states.
                if (connectionDetail.ReleaseState != ReleaseState.PreProd)
                {
                    if (preprodSiteId.HasValue &&
                        connectionDetail.MsaSiteId.HasValue &&
                        preprodSiteId.Value == connectionDetail.MsaSiteId.Value)
                    {
                        ValidationModule.MutuallyExclusivePropertyShouldNotBeSet(
                            $"connectionDetails[{ReleaseState.PreProd}].msaSiteId",
                            connectionDetail.MsaSiteId,
                            $"connectionDetails[{connectionDetail.ReleaseState}].msaSiteId");
                    }

                    prodOrRingAppIds = connectionDetail.AadAppIds;

                    // An Enumerable is required to use intersection later on to check for duplicates.
                    if (connectionDetail.AadAppId.HasValue)
                    {
                        prodOrRingAppIds = Enumerable.Empty<Guid>().Append((Guid)connectionDetail.AadAppId);
                    }
                    else if (!(prodOrRingAppIds == null || !prodOrRingAppIds.Any()))
                    {
                        // Check for duplicates in the existing list.
                        if (connectionDetail.AadAppIds.Count() != connectionDetail.AadAppIds.Distinct().Count())
                        {
                            throw new InvalidPropertyException(
                                $"connectionDetails[{connectionDetail.ReleaseState}].aadAppIds",
                                connectionDetail.AadAppIds.Distinct().First().ToString(),
                                "Duplicate AAD App IDs cannot exist in the same list.");
                        }
                    }

                    // Intersect App Id lists to verify duplicate App IDs are not shared between PreProd and Prod or Ring states.
                    // Specify the state and value of the duplicate App Id and that it exists in aadAppId(s) fields.
                    if (!(preprodAppIds == null || !preprodAppIds.Any()) &&
                        !(prodOrRingAppIds == null || !prodOrRingAppIds.Any()) &&
                        preprodAppIds.Intersect(prodOrRingAppIds).Count() != 0)
                    {
                        ValidationModule.MutuallyExclusivePropertyShouldNotBeSet(
                            $"connectionDetails[{ReleaseState.PreProd}].aadAppId(s)",
                            preprodAppIds.Intersect(prodOrRingAppIds).First(),
                            $"connectionDetails[{connectionDetail.ReleaseState}].aadAppId(s)");
                    }
                }
            }
        }

        /// <summary>
        /// Ensure that the ConnectionDetails have supported release state, supported protocol, valid connectiondetails and all have the same protocol
        /// </summary>
        /// <param name="connectionDetails">Dictionary of ReleaseStates and ConnectionDetails</param>
        /// <exception cref="InvalidPropertyException"></exception>
        private void ValidateConnectionDetails(IDictionary<ReleaseState, ConnectionDetail> connectionDetails)
        {
            var connectionDetailToCheckAgainst = connectionDetails.Values.First();

            foreach (var connectionDetail in connectionDetails.Values)
            {
                // Ensure that the release state is valid for the data agent.
                if (!this.ValidReleaseStates.Any(p => p == connectionDetail.ReleaseState))
                {
                    throw new InvalidPropertyException($"connectionDetails[{connectionDetail.ReleaseState}]", connectionDetail.ReleaseState.ToString(), "Unsupported release state for this data agent.");
                }

                // First ensure the protocol is valid for the data agent, then check for expected properties.
                if (!this.ValidProtocols.Any(p => p == connectionDetail.Protocol))
                {
                    throw new InvalidPropertyException($"connectionDetails[{connectionDetail.ReleaseState}].protocol", connectionDetail.Protocol?.Value, "Unsupported protocol type for this data agent.");
                }

                if (connectionDetail.Protocol != connectionDetailToCheckAgainst.Protocol)
                {
                    throw new InvalidPropertyException("connectionDetails.protocol", $"{connectionDetail.Protocol.ToString()}", $"Agent protocol should be the same across all release states. Protocol for release state {connectionDetail.ReleaseState} is different from protocol for release state {connectionDetailToCheckAgainst.ReleaseState}");
                }

                ValidateConnectionDetail(connectionDetail);
            }
        }
    }
}