namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue
{
    using System;
    using System.Linq;

    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.Azure.Documents;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Policy;

    /// <summary>
    /// Parses commands from storage using the visitor pattern.
    /// </summary>
    internal class StorageCommandParser : ICommandVisitor<Document, PrivacyCommand>
    {
        private readonly AgentId agentId;
        private readonly AssetGroupId assetGroupId;
        private readonly QueueStorageType queueStorageType;

        public StorageCommandParser(AgentId agentId, AssetGroupId assetGroupId, QueueStorageType queueStorageType)
        {
            this.agentId = agentId;
            this.assetGroupId = assetGroupId;
            this.queueStorageType = queueStorageType;
        }

        public PrivacyCommandType Classify(Document document) => DynamicCast(document).CommandType;

        public PrivacyCommand VisitDelete(Document document)
        {
            var command = DynamicCast(document);
            ExtractBatchAndExpiration(command, out var batchId, out var absoluteExpirationTime);

            StorageDeleteCommandInfo deleteInfo = command.CommandInfo.ToObject<StorageDeleteCommandInfo>();
            return new DeleteCommand(
                agentId: this.agentId,
                assetGroupId: this.assetGroupId,
                assetGroupQualifier: command.AssetGroupQualifier,
                verifier: command.Verifier,
                verifierV3: command.VerifierV3,
                batchId: batchId,
                clientCommandState: command.AgentState,
                commandId: new CommandId(command.Id),
                dataType: Policies.Current.DataTypes.CreateId(deleteInfo.PrivacyDataType),
                dataTypePredicate: deleteInfo.Predicate,
                timePredicate: deleteInfo.TimeRangePredicate,
                nextVisibleTime: DateTimeOffset.FromUnixTimeSeconds(command.UnixNextVisibleTimeSeconds).ToNearestMsUtc(),
                subject: command.Subject,
                correlationVector: command.CorrelationVector,
                processorApplicable: command.ProcessorApplicable,
                controllerApplicable: command.ControllerApplicable,
                cloudInstance: command.CloudInstance,
                commandSource: command.CommandSource,
                timestamp: command.CreatedTime.ToNearestMsUtc(),
                absoluteExpirationTime: absoluteExpirationTime,
                queueStorageType: this.queueStorageType);
        }

        public PrivacyCommand VisitExport(Document document)
        {
            var command = DynamicCast(document);
            ExtractBatchAndExpiration(command, out var batchId, out var absoluteExpirationTime);

            StorageExportCommandInfo exportInfo = command.CommandInfo.ToObject<StorageExportCommandInfo>();
            return new ExportCommand(
                agentId: this.agentId,
                assetGroupId: this.assetGroupId,
                assetGroupQualifier: command.AssetGroupQualifier,
                verifier: command.Verifier,
                verifierV3: command.VerifierV3,
                batchId: batchId,
                clientCommandState: command.AgentState,
                commandId: new CommandId(command.Id),
                nextVisibleTime: DateTimeOffset.FromUnixTimeSeconds(command.UnixNextVisibleTimeSeconds).ToNearestMsUtc(),
                subject: command.Subject,
                dataTypes: exportInfo.PrivacyDataTypes.Select(Policies.Current.DataTypes.CreateId),
                correlationVector: command.CorrelationVector,
                timestamp: command.CreatedTime.ToNearestMsUtc(),
                processorApplicable: command.ProcessorApplicable,
                controllerApplicable: command.ControllerApplicable,
                cloudInstance: command.CloudInstance,
                commandSource: command.CommandSource,
                absoluteExpirationTime: absoluteExpirationTime,
                queueStorageType: this.queueStorageType)
            {
                AzureBlobContainerTargetUri = exportInfo.AzureBlobStorageUri,
                AzureBlobContainerPath = exportInfo.AzureBlobStoragePath
            };
        }

        public PrivacyCommand VisitAccountClose(Document document)
        {
            var command = DynamicCast(document);
            ExtractBatchAndExpiration(command, out var batchId, out var absoluteExpirationTime);

            return new AccountCloseCommand(
                agentId: this.agentId,
                assetGroupId: this.assetGroupId,
                assetGroupQualifier: command.AssetGroupQualifier,
                verifier: command.Verifier,
                verifierV3: command.VerifierV3,
                batchId: batchId,
                clientCommandState: command.AgentState,
                commandId: new CommandId(command.Id),
                nextVisibleTime: DateTimeOffset.FromUnixTimeSeconds(command.UnixNextVisibleTimeSeconds).ToNearestMsUtc(),
                subject: command.Subject,
                correlationVector: command.CorrelationVector,
                cloudInstance: command.CloudInstance,
                commandSource: command.CommandSource,
                timestamp: command.CreatedTime.ToNearestMsUtc(),
                processorApplicable: command.ProcessorApplicable,
                controllerApplicable: command.ControllerApplicable,
                absoluteExpirationTime: absoluteExpirationTime,
                queueStorageType: this.queueStorageType);
        }

        public PrivacyCommand VisitAgeOut(Document document)
        {
            var command = DynamicCast(document);
            ExtractBatchAndExpiration(command, out var batchId, out var absoluteExpirationTime);

            StorageAgeOutCommandInfo ageOutInfo = command.CommandInfo.ToObject<StorageAgeOutCommandInfo>();
            return new AgeOutCommand(
                agentId: this.agentId,
                assetGroupId: this.assetGroupId,
                assetGroupQualifier: command.AssetGroupQualifier,
                verifier: command.Verifier,
                verifierV3: command.VerifierV3,
                batchId: batchId,
                clientCommandState: command.AgentState,
                commandId: new CommandId(command.Id),
                nextVisibleTime: DateTimeOffset.FromUnixTimeSeconds(command.UnixNextVisibleTimeSeconds).ToNearestMsUtc(),
                subject: command.Subject,
                correlationVector: command.CorrelationVector,
                cloudInstance: command.CloudInstance,
                commandSource: command.CommandSource,
                timestamp: command.CreatedTime.ToNearestMsUtc(),
                processorApplicable: command.ProcessorApplicable,
                controllerApplicable: command.ControllerApplicable,
                absoluteExpirationTime: absoluteExpirationTime,
                lastActive: ageOutInfo.UnixLastActiveTimeSeconds.HasValue ? DateTimeOffset.FromUnixTimeSeconds(ageOutInfo.UnixLastActiveTimeSeconds.Value).ToNearestMsUtc() : (DateTimeOffset?)null,
                isSuspended: ageOutInfo.IsSuspended,
                queueStorageType: this.queueStorageType);
        }

        private static StoragePrivacyCommand DynamicCast(Document document)
        {
            if (document is StoragePrivacyCommand storageCommand)
            {
                return storageCommand;
            }

            // If not a storage privacy command, then we need to cast it to one using
            // the magic of "dynamic". This is a much cheaper process than doing a 
            // json serialize / parse.
            dynamic dynamicCommand = (dynamic)document;
            return (StoragePrivacyCommand)dynamicCommand;
        }

        private static void ExtractBatchAndExpiration(StoragePrivacyCommand command, out RequestBatchId requestBatchId, out DateTimeOffset absoluteExpirationTime)
        {
            if (!Identifier.TryParse(command.RequestBatchId, out requestBatchId))
            {
                requestBatchId = null;
                IncomingEvent.Current?.SetProperty("InvalidRequestBatchId", command.RequestBatchId);
                PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "InvalidRequestBatchId").Increment();
            }

            if (command.TimeToLive == null)
            {
                absoluteExpirationTime = DateTimeOffset.UtcNow.AddDays(Config.Instance.CosmosDBQueues.TimeToLiveDays);
                PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "DocumentTtlNull").Increment();
            }
            else
            {
                DateTimeOffset lastModifiedTime = command.Timestamp.ToUniversalTime();
                absoluteExpirationTime = lastModifiedTime.AddSeconds(command.TimeToLive.Value);
            }
        }
    }
}
