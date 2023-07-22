namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Icm
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.ServiceModel;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.AzureAd.Icm.Types;
    using Microsoft.PrivacyServices.DataManagement.Common;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.FileSystem;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authorization;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Reader;
    using Microsoft.PrivacyServices.DataManagement.Models.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;
    using Incident = DataManagement.Models.V2.Incident;

    /// <summary>
    /// A class for creating incidents in ICM.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class IcmConnector : IIcmConnector
    {
        private const string OwnerRegistrationKey = "ownerRegistration";
        private const string AgentRegistrationKey = "agentRegistration";
        private static readonly ConcurrentDictionary<string, string> TemplateData = new ConcurrentDictionary<string, string>();

        private readonly IDateFactory dateFactory;
        private readonly IConnectorIncidentManager icmClient;
        private readonly IDataOwnerReader dataOwnerReader;
        private readonly IDeleteAgentReader agentReader;
        private readonly IAssetGroupReader assetGroupReader;
        private readonly IIcmConfiguration configuration;
        private readonly IAuthorizationProvider authorizationProvider;
        private readonly AuthorizationRole authorizationRoles;
        private readonly ISessionFactory sessionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="IcmConnector" /> class.
        /// </summary>
        /// <param name="dateFactory">The date factory instance.</param>
        /// <param name="icmClient">The ICM client instance.</param>
        /// <param name="dataOwnerReader">The data owner reader instance.</param>
        /// <param name="agentReader">The agent reader instance.</param>
        /// <param name="assetGroupReader">The asset group reader instance.</param>
        /// <param name="configuration">The configuration for this component.</param>
        /// <param name="authorizationProvider">The authorization provider instance.</param>
        /// <param name="fileSystem">The file system instance.</param>
        /// <param name="sessionFactory">The session factory instance.</param>
        public IcmConnector(
            IDateFactory dateFactory,
            IConnectorIncidentManager icmClient,
            IDataOwnerReader dataOwnerReader,
            IDeleteAgentReader agentReader,
            IAssetGroupReader assetGroupReader,
            IIcmConfiguration configuration,
            IAuthorizationProvider authorizationProvider,
            IFileSystem fileSystem,
            ISessionFactory sessionFactory)
        {
            this.dateFactory = dateFactory;
            this.icmClient = icmClient;
            this.dataOwnerReader = dataOwnerReader;
            this.agentReader = agentReader;
            this.assetGroupReader = assetGroupReader;
            this.configuration = configuration;
            this.authorizationProvider = authorizationProvider;
            this.sessionFactory = sessionFactory;

            this.authorizationRoles = AuthorizationRole.IncidentManager | AuthorizationRole.ApplicationAccess;

            if (!TemplateData.ContainsKey(OwnerRegistrationKey))
            {
                string body = this.configuration.OwnerRegistrationBody;

                if (this.configuration.OwnerRegistrationBodyFromFile)
                {
                    body = Encoding.Default.GetString(fileSystem.ReadFile(this.configuration.OwnerRegistrationBody));
                }

                TemplateData.GetOrAdd(OwnerRegistrationKey, body);
            }

            if (!TemplateData.ContainsKey(AgentRegistrationKey))
            {
                string body = this.configuration.AgentRegistrationBody;

                if (this.configuration.AgentRegistrationBodyFromFile)
                {
                    body = Encoding.Default.GetString(fileSystem.ReadFile(this.configuration.AgentRegistrationBody));
                }

                TemplateData.GetOrAdd(AgentRegistrationKey, body);
            }
        }

        /// <summary>
        /// Given some incident information, loads the necessary connector ID from storage, 
        /// populates an incident envelope and logs the incident to ICM.
        /// </summary>
        /// <param name="incident">The incident to send.</param>
        /// <returns>The incident with data updated.</returns>
        public async Task<Incident> CreateIncidentAsync(Incident incident)
        {
            await this.authorizationProvider.AuthorizeAsync(this.authorizationRoles).ConfigureAwait(false);

            var incidentMetadata = await this.LoadIncidentMetadata(incident).ConfigureAwait(false);

            var incidentEnvelope = this.PopulateIncident(incidentMetadata);

            var incidentResult = this.SendIncident(incidentMetadata.ConnectorId, incidentEnvelope);

            if (incidentResult?.IncidentId.HasValue == true)
            {
                return new Incident
                {
                    Id = incidentResult.IncidentId.Value,
                    AlertSourceId = incidentEnvelope.Source.IncidentId,
                    ResponseMetadata = new IncidentResponseMetadata
                    {
                        Status = (int) incidentResult.Status,
                        Substatus = (int) incidentResult.SubStatus
                    }
                };
            }

            throw new ConflictException(ConflictType.DoesNotExist, "Chosen connector id does not exist in ICM.",
                "icm.connectorId", incidentMetadata.ConnectorId.ToString());
        }

        /// <summary>
        /// Sends the owner registration confirmation incident to the given owner.
        /// If the registration is not valid, then it throws an exception.
        /// </summary>
        /// <param name="owner">The owner that has ICM properties set.</param>
        public void SendOwnerRegistrationConfirmationAsync(DataOwner owner)
        {
            // Only send validation incidents if enabled, and the source is manual.
            // We trust that the automated source data is correct.
            if (this.configuration.Enabled && owner.Icm.Source == IcmSource.Manual)
            {
                var incidentMetadata = new IcmIncidentMetadata();

                string body = string.Empty;
                TemplateData.TryGetValue(OwnerRegistrationKey, out body);

                incidentMetadata.OwnerName = owner.Name;
                incidentMetadata.OwnerServiceTree = owner.ServiceTree;
                incidentMetadata.ConnectorId = owner.Icm.ConnectorId;

                incidentMetadata.Incident = new Incident();
                incidentMetadata.Incident.Title = this.configuration.OwnerRegistrationTitle;
                incidentMetadata.Incident.Body = body;
                incidentMetadata.Incident.Severity = this.configuration.OwnerRegistrationSeverity;
                incidentMetadata.Incident.Routing = new RouteData();

                incidentMetadata.Incident.Routing.EventName = this.configuration.OwnerRegistrationEventName;
                incidentMetadata.Incident.Routing.OwnerId = owner.Id;

                var incidentEnvelope = this.PopulateIncident(incidentMetadata);

                var incidentId = this.SendIncident(incidentMetadata.ConnectorId, incidentEnvelope);

                if (incidentId == null)
                {
                    throw new ConflictException(ConflictType.DoesNotExist, "Owner connector id does not exist in ICM.",
                        "icm.connectorId", incidentMetadata.ConnectorId.ToString());
                }
            }
        }

        /// <summary>
        /// Sends the agent registration confirmation incident to the given agent.
        /// If the registration is not valid, then it throws an exception.
        /// </summary>
        /// <param name="owner">The agent's owner.</param>
        /// <param name="agent">The agent that has ICM properties set.</param>
        public void SendAgentRegistrationConfirmationAsync(DataOwner owner, DataAgent agent)
        {
            // Only send validation incidents if enabled, and the source is manual.
            // We trust that the automated source data is correct.
            if (this.configuration.Enabled && agent.Icm.Source == IcmSource.Manual)
            {
                if (owner.Id != agent.OwnerId)
                {
                    throw new InvalidOperationException("Provided owner must belong to the agent.");
                }

                var incidentMetadata = new IcmIncidentMetadata();

                string body = string.Empty;
                TemplateData.TryGetValue(AgentRegistrationKey, out body);

                incidentMetadata.OwnerName = owner.Name;
                incidentMetadata.OwnerServiceTree = owner.ServiceTree;
                incidentMetadata.AgentName = agent.Name;
                incidentMetadata.AgentOwnerId = agent.OwnerId;
                incidentMetadata.AgentOwnerName = owner.Name;
                incidentMetadata.ConnectorId = agent.Icm.ConnectorId;

                incidentMetadata.Incident = new Incident();
                incidentMetadata.Incident.Title = this.configuration.AgentRegistrationTitle;
                incidentMetadata.Incident.Body = body;
                incidentMetadata.Incident.Severity = this.configuration.AgentRegistrationSeverity;
                incidentMetadata.Incident.Routing = new RouteData();

                incidentMetadata.Incident.Routing.EventName = this.configuration.AgentRegistrationEventName;
                incidentMetadata.Incident.Routing.OwnerId = agent.OwnerId;
                incidentMetadata.Incident.Routing.AgentId = agent.Id;

                var incidentEnvelope = this.PopulateIncident(incidentMetadata);

                var incidentId = this.SendIncident(incidentMetadata.ConnectorId, incidentEnvelope);

                if (incidentId == null)
                {
                    throw new ConflictException(ConflictType.DoesNotExist, "Agent connector id does not exist in ICM.",
                        "icm.connectorId", incidentMetadata.ConnectorId.ToString());
                }
            }
        }

        private async Task<IcmIncidentMetadata> LoadIncidentMetadata(Incident incident)
        {
            IcmIncidentMetadata metadata = new IcmIncidentMetadata {Incident = incident};

            // Do this first to set the initial connector ID value.
            if (metadata.Incident.Routing.OwnerId.HasValue)
            {
                var owner = await this.dataOwnerReader
                    .ReadByIdAsync(metadata.Incident.Routing.OwnerId.Value, ExpandOptions.ServiceTree)
                    .ConfigureAwait(false);

                if (owner != null)
                {
                    metadata.OwnerName = owner.Name;
                    metadata.OwnerServiceTree = owner.ServiceTree;

                    if (owner.Icm != null)
                    {
                        metadata.ConnectorId = owner.Icm.ConnectorId;
                    }
                }
                else
                {
                    // If owner is provided, then we will always fail.
                    // The assumption is that the call specifically wants to send data to this owner.
                    throw new EntityNotFoundException(metadata.Incident.Routing.OwnerId.Value, "dataOwner");
                }
            }

            // Do this after loading owner ID routing data,
            // so that the connector ID is over-ridden by the agent connector ID.
            if (metadata.Incident.Routing.AgentId.HasValue)
            {
                var agent = await this.agentReader
                    .ReadByIdAsync(metadata.Incident.Routing.AgentId.Value, ExpandOptions.None).ConfigureAwait(false);

                if (agent != null)
                {
                    metadata.AgentName = agent.Name;

                    var agentOwner = await this.dataOwnerReader.ReadByIdAsync(agent.OwnerId, ExpandOptions.ServiceTree)
                        .ConfigureAwait(false);
                    metadata.AgentOwnerId = agent.OwnerId;
                    metadata.AgentOwnerName = agentOwner.Name;

                    if (metadata.ConnectorId == Guid.Empty)
                    {
                        if (agent.Icm != null)
                        {
                            metadata.ConnectorId = agent.Icm.ConnectorId;
                        }
                        else if (agentOwner.Icm != null)
                        {
                            metadata.ConnectorId = agentOwner.Icm.ConnectorId;
                        }
                    }

                    // Set the owner ID if it was not previously set.
                    if (!metadata.Incident.Routing.OwnerId.HasValue)
                    {
                        metadata.Incident.Routing.OwnerId = agent.OwnerId;
                        metadata.OwnerName = agentOwner.Name;
                        metadata.OwnerServiceTree = agentOwner.ServiceTree;
                    }
                }
                else if (!metadata.Incident.Routing.OwnerId.HasValue)
                {
                    // Only fail if we haven't already found an owner.
                    throw new EntityNotFoundException(metadata.Incident.Routing.AgentId.Value, "deleteAgent");
                }
            }

            // Do this after loading agent ID routing data,
            // so that the connector ID is over-ridden by the asset group owner's connector ID.
            if (metadata.Incident.Routing.AssetGroupId.HasValue)
            {
                var assetGroup = await this.assetGroupReader
                    .ReadByIdAsync(metadata.Incident.Routing.AssetGroupId.Value, ExpandOptions.None)
                    .ConfigureAwait(false);

                if (assetGroup != null)
                {
                    metadata.AssetGroupQualifierValue = assetGroup.Qualifier.Value;

                    var assetGroupOwner = await this.dataOwnerReader
                        .ReadByIdAsync(assetGroup.OwnerId, ExpandOptions.ServiceTree).ConfigureAwait(false);
                    metadata.AssetGroupOwnerId = assetGroup.OwnerId;
                    metadata.AssetGroupOwnerName = assetGroupOwner.Name;

                    if (metadata.ConnectorId == Guid.Empty && assetGroupOwner.Icm != null)
                    {
                        metadata.ConnectorId = assetGroupOwner.Icm.ConnectorId;
                    }

                    // Set the owner ID if it was not previously set.
                    // Also account for asset groups that do not have an owner.
                    if (!metadata.Incident.Routing.OwnerId.HasValue && assetGroup.OwnerId != Guid.Empty)
                    {
                        metadata.Incident.Routing.OwnerId = assetGroup.OwnerId;
                        metadata.OwnerName = assetGroupOwner.Name;
                        metadata.OwnerServiceTree = assetGroupOwner.ServiceTree;
                    }
                }
                else if (!metadata.Incident.Routing.OwnerId.HasValue)
                {
                    // Only fail if we haven't already found an owner.
                    throw new EntityNotFoundException(metadata.Incident.Routing.AssetGroupId.Value, "assetGroup");
                }
            }

            // Fail if no connector ID was found.
            if (metadata.ConnectorId == Guid.Empty)
            {
                throw new ConflictException(ConflictType.DoesNotExist, "Connector id not found.", "icm.connectorId",
                    metadata.Incident.Routing.OwnerId.Value.ToString());
            }

            return metadata;
        }

        private IncidentAddUpdateResult SendIncident(Guid connectorId, AlertSourceIncident incidentEnvelope)
        {
            const string notFound = "ConnectorIdNotFound";
            const string notActivated = "ConnectorIdNotActivated";

            var value =
                this.sessionFactory.Instrument(
                    "Icm.SendIncident",
                    SessionType.Outgoing,
                    () =>
                    {
                        try
                        {
                            IncidentAddUpdateResult result =
                                this.icmClient.AddOrUpdateIncident2(connectorId, incidentEnvelope, RoutingOptions.None);

                            return new Tuple<Guid, AlertSourceIncident, IncidentAddUpdateResult, string>(connectorId,
                                incidentEnvelope, result, null);
                        }
                        catch (FaultException<IcmFault> icmException)
                        {
                            if (icmException.Detail != null)
                            {
                                if (icmException.Detail.Code == "DataNotFoundException")
                                {
                                    return new Tuple<Guid, AlertSourceIncident, IncidentAddUpdateResult, string>(
                                        connectorId, incidentEnvelope, null, notFound);
                                }

                                // have to actually compare the error message because the exception code is generic
                                if (icmException.Detail.Code == "IcmException" &&
                                    icmException.Detail.Message.Equals(
                                        $"Connector with id {connectorId} is not enabled",
                                        StringComparison.InvariantCultureIgnoreCase))
                                {
                                    return new Tuple<Guid, AlertSourceIncident, IncidentAddUpdateResult, string>(
                                        connectorId, incidentEnvelope, null, notActivated);
                                }
                            }

                            throw;
                        }
                    });

            if (!string.IsNullOrWhiteSpace(value.Item4))
            {
                if (value.Item4 == notActivated)
                {
                    throw new ConflictException(ConflictType.InvalidValue,
                        "Chosen connector id is not activated in ICM.", "icm.connectorId", connectorId.ToString());
                }

                throw new ConflictException(ConflictType.DoesNotExist, "Chosen connector id does not exist in ICM.",
                    "icm.connectorId", connectorId.ToString());
            }

            return value.Item3;
        }

        private AlertSourceIncident PopulateIncident(IcmIncidentMetadata incidentMetadata)
        {
            var now = this.dateFactory.GetCurrentTime().DateTime;
            var connectorName = this.configuration.SourceName;
            var userName = "Automated";

            // Avoid null references for when the values are not provided (due to backwards compatibility).
            incidentMetadata.Incident.InputParameters =
                incidentMetadata.Incident.InputParameters ?? new IncidentInputParameters();

            incidentMetadata.Incident.Title = this.FormatData(incidentMetadata.Incident.Title,
                incidentMetadata.Incident.InputParameters.DisableTitleSubstitutions, incidentMetadata);
            incidentMetadata.Incident.Body = this.FormatData(incidentMetadata.Incident.Body,
                incidentMetadata.Incident.InputParameters.DisableBodySubstitutions, incidentMetadata);

            return new AlertSourceIncident
            {
                Source = new AlertSourceInfo
                {
                    CreatedBy = userName,
                    Origin = connectorName,
                    CreateDate = now,
                    ModifiedDate = now,
                    IncidentId = string.IsNullOrEmpty(incidentMetadata.Incident.AlertSourceId)
                        ? Guid.NewGuid().ToString("N")
                        : incidentMetadata.Incident.AlertSourceId
                },
                CorrelationId = this.GetCorrelationId(incidentMetadata.Incident),
                RoutingId =
                    $"ownerId:{incidentMetadata.Incident.Routing.OwnerId.Value}", // Intentionally cause a NullReference exception if OwnerId is missing. 
                OccurringLocation = new IncidentLocation
                {
                    Environment = "PROD",
                    DeviceName = incidentMetadata.Incident.Routing.AgentId.HasValue
                        ? $"agentId:{incidentMetadata.Incident.Routing.AgentId}"
                        : null,
                    DeviceGroup = incidentMetadata.Incident.Routing.AssetGroupId.HasValue
                        ? $"assetGroupId:{incidentMetadata.Incident.Routing.AssetGroupId}"
                        : null,
                    ServiceInstanceId =
                        "NGPDataAgentLivesite" // A fixed value that OSOC can use for correlation rules. This will appear as the Slice value in the UI.
                },
                RaisingLocation = new IncidentLocation
                {
                    Environment = "PROD",
                    DeviceName = incidentMetadata.OwnerServiceTree?.DivisionName ?? "Unknown",
                    DeviceGroup = incidentMetadata.OwnerServiceTree?.OrganizationName ?? "Unknown",
                    ServiceInstanceId = this.GetLeafNode(incidentMetadata.OwnerServiceTree).Replace("-", string.Empty)
                },
                Status = IncidentStatus.Active,
                Severity = incidentMetadata.Incident.Severity,
                Title = incidentMetadata.Incident.Title,
                Keywords = incidentMetadata.Incident.Keywords,
                DescriptionEntries = new[]
                {
                    new DescriptionEntry
                    {
                        Cause = DescriptionEntryCause.Created,
                        RenderType = DescriptionTextRenderType.Html,
                        Text = incidentMetadata.Incident.Body,
                        Date = now,
                        ChangedBy = userName,
                        SubmitDate = now,
                        SubmittedBy = connectorName
                    }
                }
            };
        }

        private string FormatData(string stringFormat, bool disableFormatting, IcmIncidentMetadata incidentMetadata)
        {
            if (disableFormatting)
            {
                return stringFormat;
            }
            else
            {
                return string.Format(
                    stringFormat,
                    incidentMetadata.OwnerName,
                    incidentMetadata.AgentName,
                    incidentMetadata.AgentOwnerName,
                    incidentMetadata.AssetGroupQualifierValue,
                    incidentMetadata.AssetGroupOwnerName,
                    incidentMetadata.Incident.Routing.OwnerId,
                    incidentMetadata.Incident.Routing.AgentId,
                    incidentMetadata.Incident.Routing.AssetGroupId);
            }
        }

        /// <summary>
        /// Adds a prefix to the event name so that incidents correlate properly.
        /// We give preference to agent id for correlation since that is typically
        /// the context under which we create an incident.
        /// Next we fall back to asset group id, since that is the more specific,
        /// and finally, we use owner id to ensure teams receive distinct incidents.
        /// Severity is then added if source incident id not set, because IcM will
        /// only bump hit counts if correlation ids match; it won't update severities.
        /// </summary>
        private string GetCorrelationId(Incident incident)
        {
            var routeData = incident.Routing;
            string prefix;

            if (routeData.AgentId.HasValue)
            {
                prefix = routeData.AgentId.Value.ToString("N");
            }
            else if (routeData.AssetGroupId.HasValue)
            {
                prefix = routeData.AssetGroupId.Value.ToString("N");
            }
            else
            {
                prefix = routeData.OwnerId.Value.ToString("N");
            }

            var parts = new List<string>() {prefix, routeData.EventName};
            if (string.IsNullOrEmpty(incident.AlertSourceId))
            {
                parts.Add($"sev{incident.Severity}");
            }

            return string.Join(":", parts);
        }

        private string GetLeafNode(ServiceTree serviceTree)
        {
            if (serviceTree == null)
            {
                return Guid.Empty.ToString();
            }
            else
            {
                switch (serviceTree.Level)
                {
                    case ServiceTreeLevel.ServiceGroup:
                        return serviceTree.ServiceGroupId;
                    case ServiceTreeLevel.TeamGroup:
                        return serviceTree.TeamGroupId;
                    case ServiceTreeLevel.Service:
                    default:
                        return serviceTree.ServiceId;
                }
            }
        }
    }
}