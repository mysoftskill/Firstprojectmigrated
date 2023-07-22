// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Policy;

    using Newtonsoft.Json.Linq;

    using PXSV1 = PXS.Command.Contracts.V1;

    /// <summary>
    /// Common logic for ingesting new commands, independent of the ingestion path.
    /// </summary>
    public static class CommandIngester
    {
        public static async Task AddCommandAsync(
            AgentId agentId,
            AssetGroupId assetGroupId,
            IList<DataTypeId> assetGroupDataTypes,
            string assetGroupQualifier,
            ICommandQueueFactory queueFactory,
            JObject rawPxsCommand,
            ICommandLifecycleEventPublisher eventPublisher,
            string assetGroupStreamName,
            string variantStreamName,
            string storageMoniker,
            QueueStorageType queueStorageType)
        {
            var parser = new PxsCommandParser(agentId, assetGroupId, assetGroupQualifier, queueStorageType);
            var (pcfCommand, pxsCommand) = parser.Process(rawPxsCommand);

            IncomingEvent.Current?.SetProperty("PxsRequestId", pxsCommand?.RequestId.ToString());
            IncomingEvent.Current?.SetProperty("CommandId", pcfCommand.CommandId.Value);
            IncomingEvent.Current?.SetProperty("CommandSubject", pxsCommand.Subject.ToString());
            IncomingEvent.Current?.SetProperty("CommandType", pxsCommand.RequestType.ToString());
            IncomingEvent.Current?.SetProperty("Moniker", storageMoniker);
            IncomingEvent.Current?.SetProperty("QueueStorageType", queueStorageType.ToString());

            if (ShouldFlightsDropCommand(agentId, assetGroupId))
            {
                return;
            }

            if (pxsCommand.RequestType == PXSV1.RequestType.AgeOut && (pcfCommand is AgeOutCommand ageOutCommand))
            {
                IncomingEvent.Current?.SetProperty("MsaSuspendedAccount", ageOutCommand.IsSuspended == true ? "true" : "false");
            }
           
            var exportParameters = await PreProcessExportCommandAsync(pcfCommand, pxsCommand, assetGroupDataTypes);
            ICommandQueue queue = queueFactory.CreateQueue(agentId, assetGroupId, pcfCommand.Subject.GetSubjectType(), queueStorageType);

            try
            {
                await queue.EnqueueAsync(storageMoniker, pcfCommand);
                Logger.Instance?.CommandIngested(pcfCommand);
                DualLogger.Instance.LogInformationForCommandLifeCycle(nameof(CommandIngester), $"Command={pcfCommand.CommandId} Ingested for agentId={agentId} and assetGroupId={assetGroupId}");
            }
            catch (CommandFeedException ex)
            {
                // Command has already been inserted. Maybe we got a retry? Just ignore it.
                if (ex.ErrorCode == CommandFeedInternalErrorCode.Conflict)
                {
                    DualLogger.Instance.LogWarningForCommandLifeCycle(nameof(CommandIngester), $"Conflict with EnqueueAsync failed for Command={pcfCommand.CommandId} Ingested for agentId={agentId} and assetGroupId={assetGroupId}");
                    IncomingEvent.Current?.SetProperty("InsertConflict", "true");
                }
                else
                {
                    DualLogger.Instance.LogErrorForCommandLifeCycle(nameof(CommandIngester), ex, $"Unhandled ex while EnqueueAsync failed for Command={pcfCommand.CommandId} Ingested for agentId={agentId} and assetGroupId={assetGroupId}");
                    throw;
                }
            }

            await eventPublisher.PublishCommandStartedAsync(
                pcfCommand.AgentId,
                pcfCommand.AssetGroupId,
                pcfCommand.AssetGroupQualifier,
                pcfCommand.CommandId,
                pcfCommand.CommandType,
                pcfCommand.Timestamp,
                exportParameters?.finalContainerUri,
                exportParameters?.stagingContainerUri,
                exportParameters?.stagingContainerPath,
                assetGroupStreamName,
                variantStreamName);
        }

        /// <summary>
        /// The flights below (IsIngestionBlockedForAgentId and IsIngestionBlockedForAssetGroupId) 
        /// were introduced to mitigate IcM Incident #154102965, where CosmosDb queue raises exception:
        /// "Partition key reached maximum size of 10 GB".
        /// Ingestion must be disabled due to high backlog of commands in queues. 
        /// GetCommands and other post-ingestion pathways must remain available for Agent.
        /// </summary>
        internal static bool ShouldFlightsDropCommand(AgentId agentId, AssetGroupId assetGroupId)
        {
            if (FlightingUtilities.IsIngestionBlockedForAgentId(agentId))
            {
                IncomingEvent.Current?.SetProperty("IsIngestionBlockedForAgentId", agentId.ToString());
                return true;
            }

            if (FlightingUtilities.IsIngestionBlockedForAssetGroupId(assetGroupId))
            {
                IncomingEvent.Current?.SetProperty("IsIngestionBlockedForAssetGroupId", assetGroupId.ToString());
                return true;
            }

            return false;
        }

        public static async Task<(Uri finalContainerUri, Uri stagingContainerUri, string stagingContainerPath)?> PreProcessExportCommandAsync(
            PrivacyCommand privacyCommand,
            PXSV1.PrivacyRequest pxsRequest,
            IList<DataTypeId> assetGroupDataTypes)
        {
            ExportCommand exportCommand = privacyCommand as ExportCommand;
            if (exportCommand == null)
            {
                return null;
            }

            PXSV1.ExportRequest pxsExportRequest = (PXSV1.ExportRequest)pxsRequest;

            // Scope down data types to things that this agent actually supports.
            assetGroupDataTypes = assetGroupDataTypes ?? new DataTypeId[0];
            if (!assetGroupDataTypes.Contains(Policies.Current.DataTypes.Ids.Any))
            {
                // If the asset group doesn't support "any", then simply intersect the supported data types with the big list of data
                // types in the export command.
                exportCommand.DataTypeIds = assetGroupDataTypes.Intersect(exportCommand.DataTypeIds);
            }

            var exportParameters = await GenerateExportParametersAsync(exportCommand.AgentId, exportCommand.AssetGroupId, pxsExportRequest);

            exportCommand.AzureBlobContainerPath = exportParameters.stagingContainerPath;
            exportCommand.AzureBlobContainerTargetUri = exportParameters.stagingContainerUri;

            return exportParameters;
        }

        private static async Task<(Uri finalContainerUri, Uri stagingContainerUri, string stagingContainerPath)> GenerateExportParametersAsync(
            AgentId agentId,
            AssetGroupId assetGroupId,
            PXSV1.ExportRequest pxsCommand)
        {
            CommandId commandId = new CommandId(pxsCommand.RequestId);

            if (pxsCommand.StorageUri == null)
            {
                DualLogger.Instance.Error(nameof(CommandIngester), $"Must provide a storage destination for {commandId}");
                throw new ArgumentException(nameof(pxsCommand.StorageUri), "Must provide a storage destination");
            }

            Uri finalContainerUri;
            Uri stagingContainerUri;
            string stagingContainerPath;

            if (ExportStorageManager.Instance.IsManaged(pxsCommand.StorageUri))
            {
                // Somehow PXS chose one of our managed storage accounts.
                DualLogger.Instance.Information(nameof(CommandIngester), $"New Export Request {commandId}: destination is managed, provisioning containers");

                // Create a final container. This has no shared access signature, we will generate the read signature later, and just
                // use our credentials when aggregating the staging locations.
                finalContainerUri = await ExportStorageManager.Instance.GetOrCreateFinalContainerAsync(pxsCommand.StorageUri, commandId);

                // Create a new container for the agent's command - or get the existing one. Given at this stage the command isn't yet
                // committed, we don't want to create random storage containers, since we don't have a garbage collector. Instead
                // we tie containers to commands because commands are already reliable. Maybe do something different here later.
                // This url is a write-only destination for agents
                stagingContainerUri = await ExportStorageManager.Instance.GetOrCreateStagingContainerAsync(
                    pxsCommand.StorageUri,
                    commandId,
                    agentId,
                    assetGroupId);

                stagingContainerPath = null;
            }
            else
            {
                DualLogger.Instance.Information(nameof(CommandIngester), $"New Export Request {commandId}: unmanaged destination provided");

                // Or, use the storage that was provided by PXS untouched. All agents must obey the path parameter.
                finalContainerUri = pxsCommand.StorageUri;
                stagingContainerUri = pxsCommand.StorageUri;

                // In this case, each command needs a unique path to write to in the storage.
                stagingContainerPath = $"{assetGroupId}/{agentId}";
            }

            return (finalContainerUri, stagingContainerUri, stagingContainerPath);
        }
    }
}
