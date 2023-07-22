namespace Microsoft.PrivacyServices.CommandFeed.Service.WhatIfFilterAndRouteWorkItemHost
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue.QueueStorageCommandQueue;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.Applicability;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Identity;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.PrivacyServices.SignalApplicability;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Processes FilterAndRouteCommandWorkItems out of an Azure Queue. This work item runs in "what if" mode, and doesn't commit any changes to hard
    /// state. It simply allows us to observe the effects of any changes in isolation.
    /// </summary>
    public class WhatIfFilterAndRouteWorkItemHandler : BaseFilterAndRouteCommandWorkItemHandler
    {
        private readonly AzureQueueStorageContext azureQueueStorageCommandContext;

        public WhatIfFilterAndRouteWorkItemHandler(IDataAgentMapFactory dataAgentMapFactory, AzureQueueStorageContext azureQueueStorageCommandContext) : base(dataAgentMapFactory)
        {
            this.azureQueueStorageCommandContext = azureQueueStorageCommandContext;
        }

        protected override bool IsWhatIfMode => true;

        /// <summary>
        /// If this agent/asset group combination is relevant for this command, a destination object is returned. Otherwise, null is returned.
        /// </summary>
        protected override PxsFilteredCommandDestination FilterCommandForDestination(
            AgentId agentId,
            IDataAgentMap dataAgentMap,
            IAssetGroupInfo assetGroupInfo,
            JObject rawPxsCommand,
            CommandLifecycleEventBatch eventBatch,
            CommandHistoryRecord commandHistoryRecord)
        {
            // Only process batch agent
            IDataAgentInfo dataAgentInfo = dataAgentMap[agentId];
            if (!dataAgentInfo.IsV2Agent())
            {
                return null;
            }

            // Parse to PXS and PCF command formats.
            var parser = new PxsCommandParser(agentId, assetGroupInfo.AssetGroupId, assetGroupInfo.AssetGroupQualifier, commandHistoryRecord.Core.QueueStorageType);
            var (command, _) = parser.Process(rawPxsCommand);

            // Wrap with beta filtering logic.
            assetGroupInfo = new WhatIfAssetGroupInfo(assetGroupInfo);

            PxsFilteredCommandDestination result = null;
            CommandIngestionStatus ingestionStatus;
            ApplicabilityResult applicabilityResult;

            if (assetGroupInfo.IsCommandActionable(command, out applicabilityResult))
            {
                string preferredMoniker = CommandMonikerHash.GetPreferredMoniker(command.CommandId, command.AssetGroupId, commandHistoryRecord.Core.WeightedMonikerList);

                commandHistoryRecord.StatusMap[(agentId, assetGroupInfo.AssetGroupId)] = new CommandHistoryAssetGroupStatusRecord(agentId, assetGroupInfo.AssetGroupId)
                {
                    StorageAccountMoniker = preferredMoniker,
                };

                ingestionStatus = CommandIngestionStatus.SendingToAgent;

                result = new PxsFilteredCommandDestination
                {
                    AgentId = agentId,
                    AssetGroupId = assetGroupInfo.AssetGroupId,
                    AssetGroupQualifier = assetGroupInfo.AssetGroupQualifier,
                    ApplicableVariantIds = command.GetCommandApplicableVariants(assetGroupInfo.VariantInfosAppliedByAgents).Select(v => v.VariantId).ToList(),
                    DataTypes = assetGroupInfo.SupportedDataTypes.ToList(),
                    TargetMoniker = preferredMoniker,
                    QueueStorageType = commandHistoryRecord.Core?.QueueStorageType ?? default(QueueStorageType)
                };
            }
            else
            {
                if (applicabilityResult.ReasonCode == ApplicabilityReasonCode.FilteredByVariant)
                {
                    ingestionStatus = CommandIngestionStatus.DroppedByApplyingVariant;

                    // Publish started + completed.
                    eventBatch.AddCommandStartedEvent(
                        command.AgentId,
                        command.AssetGroupId,
                        command.AssetGroupQualifier,
                        command.CommandId,
                        command.CommandType,
                        command.Timestamp,
                        null,
                        null,
                        null,
                        dataAgentMap.AssetGroupInfoStreamName,
                        dataAgentMap.VariantInfoStreamName);

                    eventBatch.AddCommandCompletedEvent(
                        command.AgentId,
                        command.AssetGroupId,
                        command.AssetGroupQualifier,
                        command.CommandId,
                        command.CommandType,
                        command.Timestamp,
                        applicabilityResult.ApplicableVariantIds.Select(id => id.ToString()).ToArray(),
                        ignoredByVariant: true,
                        rowCount: 0,
                        delinked: false,
                        completedByPcf: true,
                        nonTransientExceptions: null);
                }
                else
                {
                    ingestionStatus = CommandIngestionStatus.DroppedDueToFiltering;

                    if (applicabilityResult.ReasonCode == ApplicabilityReasonCode.TipAgentIsNotOnline)
                    {
                        ingestionStatus = CommandIngestionStatus.DroppedDueToOfflineAgent;
                    }

                    if (command.CommandType == Client.PrivacyCommandType.Export
                        && !FlightingUtilities.IsEnabled(FlightingNames.CommandLifecycleEventPublishDroppedEventDisabled))
                    {
                        eventBatch.AddCommandDroppedEvent(
                        command.AgentId,
                        command.AssetGroupId,
                        command.AssetGroupQualifier,
                        command.CommandId,
                        command.CommandType,
                        applicabilityResult.ReasonCode.ToString(),
                        dataAgentMap.AssetGroupInfoStreamName,
                        dataAgentMap.VariantInfoStreamName);
                    }
                }
            }

            commandHistoryRecord.AuditMap[(agentId, assetGroupInfo.AssetGroupId)] = new CommandIngestionAuditRecord
            {
                ApplicabilityReasonCode = applicabilityResult.ReasonCode,
                DebugText = applicabilityResult.ReasonDescription,
                IngestionStatus = ingestionStatus,
            };

            return result;
        }

        protected override Task<bool> InsertCommandHistoryAsync(CommandHistoryRecord record)
        {
            return Task.FromResult(true);
        }

        protected override Task PublishEventBatchAsync(CommandLifecycleEventBatch batch)
        {
            return Task.CompletedTask;
        }

        protected override Task<CommandHistoryRecord> QueryCommandHistoryAsync(CommandId commandId, CommandHistoryFragmentTypes fragments)
        {
            return Task.FromResult<CommandHistoryRecord>(null);
        }

        protected override Task PublishToQueueAsync(FilterAndRouteCommandWorkItem workItem, PxsFilteredCommandRequest request)
        {
            return Task.CompletedTask;
        }

        private class WhatIfAssetGroupInfo : IAssetGroupInfo
        {
            private readonly IAssetGroupInfo innerAssetGroup;

            public WhatIfAssetGroupInfo(IAssetGroupInfo innerAssetGroup)
            {
                this.innerAssetGroup = innerAssetGroup;
                this.SupportsLowPriorityQueue = innerAssetGroup?.SupportsLowPriorityQueue ?? false;
            }

            /// <inheritdoc />
            public bool SupportsLowPriorityQueue { get; }

            public IDataAgentInfo AgentInfo => this.innerAssetGroup.AgentInfo;

            public AgentId AgentId => this.innerAssetGroup.AgentId;

            public AssetGroupId AssetGroupId => this.innerAssetGroup.AssetGroupId;

            public string AssetGroupQualifier => this.innerAssetGroup.AssetGroupQualifier;

            public AssetQualifier AssetQualifier => this.innerAssetGroup.AssetQualifier;

            public bool IsFakePreProdAssetGroup => this.innerAssetGroup.IsFakePreProdAssetGroup;

            public IEnumerable<DataTypeId> SupportedDataTypes => this.innerAssetGroup.SupportedDataTypes;

            public IEnumerable<Common.SubjectType> SupportedSubjectTypes => this.innerAssetGroup.SupportedSubjectTypes;

            public IEnumerable<PdmsSubjectType> PdmsSubjectTypes => this.innerAssetGroup.PdmsSubjectTypes;

            public bool IsDeprecated => this.innerAssetGroup.IsDeprecated;

            public IEnumerable<PrivacyCommandType> SupportedCommandTypes => this.innerAssetGroup.SupportedCommandTypes;

            public IList<IAssetGroupVariantInfo> VariantInfosAppliedByPcf => this.innerAssetGroup.VariantInfosAppliedByPcf;

            public IList<IAssetGroupVariantInfo> VariantInfosAppliedByAgents => this.innerAssetGroup.VariantInfosAppliedByAgents;

            public IEnumerable<CloudInstanceId> SupportedCloudInstances => this.innerAssetGroup.SupportedCloudInstances;

            public CloudInstanceId DeploymentLocation => this.innerAssetGroup.DeploymentLocation;

            public IEnumerable<TenantId> TenantIds => this.innerAssetGroup.TenantIds;

            public bool DelinkApproved => this.innerAssetGroup.DelinkApproved;

            public AgentReadinessState AgentReadinessState => this.innerAssetGroup.AgentReadinessState;

            public IDictionary<string, string> ExtendedProps => this.innerAssetGroup.ExtendedProps;

            /// <summary>
            /// PCF V-Next Applicability check.
            /// Make sure AssetGroupInfo IsCommandActionable is updated when v-next pass verification.
            /// </summary>
            /// <param name="command">Privacy command.</param>
            /// <param name="applicabilityResult">ApplicabilityResult applicability check results.</param>
            /// <returns>True if command is applicable.</returns>
            public bool IsCommandActionable(PrivacyCommand command, out ApplicabilityResult applicabilityResult)
            {
                DataAsset dataAsset = this.ToDataAsset();
                SignalInfo signal = command.ToSignalInfo();

                applicabilityResult = dataAsset.CheckSignalApplicability(signal);

                if (!applicabilityResult.IsApplicable())
                {
                    return false;
                }

                var pcfApplicabilityResult = ApplicabilityHelper.CheckIfBlockedInPcf(command);
                if (!pcfApplicabilityResult.IsApplicable())
                {
                    applicabilityResult = pcfApplicabilityResult;
                    return false;
                }

                pcfApplicabilityResult = ApplicabilityHelper.CheckAgentReadiness(this, command);
                if (!pcfApplicabilityResult.IsApplicable())
                {
                    applicabilityResult = pcfApplicabilityResult;
                    return false;
                }

                return applicabilityResult.IsApplicable();
            }

            public bool IsValid(out string justification)
            {
                return this.innerAssetGroup.IsValid(out justification);
            }
        }
    }
}
