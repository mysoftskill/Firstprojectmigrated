namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Linq;

    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Newtonsoft.Json.Linq;

    using AccountCloseCommand = Microsoft.PrivacyServices.CommandFeed.Service.Common.AccountCloseCommand;
    using AgeOutCommand = Microsoft.PrivacyServices.CommandFeed.Service.Common.AgeOutCommand;
    using DeleteCommand = Microsoft.PrivacyServices.CommandFeed.Service.Common.DeleteCommand;
    using ExportCommand = Microsoft.PrivacyServices.CommandFeed.Service.Common.ExportCommand;
    using PrivacyCommand = Microsoft.PrivacyServices.CommandFeed.Service.Common.PrivacyCommand;

#if INCLUDE_TEST_HOOKS

    /// <summary>
    /// Parses JSON into internal models.
    /// </summary>
    internal class CommandFeedParser
    {
        private readonly AgentId agentId;
        private readonly RequestBatchId requestBatchId;

        /// <summary>
        /// Initializes a new CommandfeedParser with the given agent ID.
        /// </summary>
        /// <param name="agentId">The agent ID to be applied to all commands.</param>
        /// <param name="batchId">The batch ID to be applied to all commands.</param>
        public CommandFeedParser(AgentId agentId, RequestBatchId batchId)
        {
            this.agentId = agentId;
            this.requestBatchId = batchId;
        }

        /// <summary>
        /// Parses a JSON blob into a command. Throws a format exception when encountering a parsing error.
        /// </summary>
        public PrivacyCommand Parse(JObject item)
        {
            try
            {
                PrivacyCommand command = this.ParseImpl(item);
                return command;
            }
            catch (ArgumentException ex)
            {
                // Our data models throw argument exceptions when getting invalid data, so wrap this up into a format exception.
                throw new FormatException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Parses a JSON blob into a command. 
        /// </summary>
        public PrivacyCommand ParseImpl(JObject item)
        {
            // Look at the "type" property to figure out how to interpret the rest of it.
            string type = item.Property("type").Value.Value<string>();

            if (StringComparer.OrdinalIgnoreCase.Equals(type, Client.AgeOutCommand.CommandTypeName))
            {
                return this.Parse(item.ToObject<Client.AgeOutCommand>());
            }

            if (StringComparer.OrdinalIgnoreCase.Equals(type, Client.AccountCloseCommand.CommandTypeName))
            {
                return this.Parse(item.ToObject<Client.AccountCloseCommand>());
            }
            
            if (StringComparer.OrdinalIgnoreCase.Equals(type, Client.DeleteCommand.CommandTypeName))
            {
                return this.Parse(item.ToObject<Client.DeleteCommand>());
            }

            if (StringComparer.OrdinalIgnoreCase.Equals(type, Client.ExportCommand.CommandTypeName))
            {
                return this.Parse(item.ToObject<Client.ExportCommand>());
            }

            throw new ArgumentOutOfRangeException($"The type '{type}' was out of range.");
        }

        private PrivacyCommand Parse(Client.ExportCommand item)
        {
            return new ExportCommand(
                agentId: this.agentId,
                assetGroupQualifier: item.AssetGroupQualifier,
                verifier: item.Verifier,
                verifierV3: item.Verifier,
                commandId: new CommandId(item.CommandId),
                batchId: this.requestBatchId,
                nextVisibleTime: item.ApproximateLeaseExpiration,
                subject: item.Subject,
                clientCommandState: item.AgentState,
                assetGroupId: new AssetGroupId(item.AssetGroupId),
                correlationVector: item.CorrelationVector,
                timestamp: item.Timestamp,
                processorApplicable: item.ProcessorApplicable,
                controllerApplicable: item.ControllerApplicable,
                cloudInstance: item.CloudInstance,
                commandSource: string.Empty,
                dataTypes: item.PrivacyDataTypes.ToArray(),
                absoluteExpirationTime: DateTimeOffset.UtcNow.AddDays(Config.Instance.CosmosDBQueues.TimeToLiveDays),
                queueStorageType: new FrontDoorQueueStorageSelector(PrivacyCommandType.Export).Process(item))
            {
                AzureBlobContainerTargetUri = item.AzureBlobContainerTargetUri,
                AzureBlobContainerPath = item.AzureBlobContainerPath
            };
        }

        private PrivacyCommand Parse(Client.DeleteCommand item)
        {
            return new DeleteCommand(
                agentId: this.agentId,
                assetGroupQualifier: item.AssetGroupQualifier,
                verifier: item.Verifier,
                verifierV3: item.Verifier,
                commandId: new CommandId(item.CommandId),
                batchId: this.requestBatchId,
                nextVisibleTime: item.ApproximateLeaseExpiration,
                subject: item.Subject,
                clientCommandState: item.AgentState,
                assetGroupId: new AssetGroupId(item.AssetGroupId),
                correlationVector: item.CorrelationVector,
                timestamp: item.Timestamp,
                cloudInstance: item.CloudInstance,
                commandSource: string.Empty,
                processorApplicable: item.ProcessorApplicable,
                controllerApplicable: item.ControllerApplicable,
                dataTypePredicate: item.DataTypePredicate,
                timePredicate: item.TimeRangePredicate,
                dataType: item.PrivacyDataType,
                absoluteExpirationTime: DateTimeOffset.UtcNow.AddDays(Config.Instance.CosmosDBQueues.TimeToLiveDays),
                queueStorageType: new FrontDoorQueueStorageSelector(PrivacyCommandType.Delete).Process(item));
        }

        private PrivacyCommand Parse(Client.AccountCloseCommand item)
        {
            return new AccountCloseCommand(
                agentId: this.agentId,
                assetGroupQualifier: item.AssetGroupQualifier,
                verifier: item.Verifier,
                verifierV3: item.Verifier,
                commandId: new CommandId(item.CommandId),
                batchId: this.requestBatchId,
                nextVisibleTime: item.ApproximateLeaseExpiration,
                subject: item.Subject,
                clientCommandState: item.AgentState,
                assetGroupId: new AssetGroupId(item.AssetGroupId),
                correlationVector: item.CorrelationVector,
                timestamp: item.Timestamp,
                cloudInstance: item.CloudInstance,
                commandSource: string.Empty,
                processorApplicable: item.ProcessorApplicable,
                controllerApplicable: item.ControllerApplicable,
                absoluteExpirationTime: DateTimeOffset.UtcNow.AddDays(Config.Instance.CosmosDBQueues.TimeToLiveDays),
                queueStorageType: new FrontDoorQueueStorageSelector(PrivacyCommandType.AccountClose).Process(item));
        }

        private PrivacyCommand Parse(Client.AgeOutCommand item)
        {
            return new AgeOutCommand(
                agentId: this.agentId,
                assetGroupQualifier: item.AssetGroupQualifier,
                verifier: item.Verifier,
                verifierV3: item.Verifier,
                commandId: new CommandId(item.CommandId),
                batchId: this.requestBatchId,
                nextVisibleTime: item.ApproximateLeaseExpiration,
                subject: item.Subject,
                clientCommandState: item.AgentState,
                assetGroupId: new AssetGroupId(item.AssetGroupId),
                correlationVector: item.CorrelationVector,
                timestamp: item.Timestamp,
                cloudInstance: item.CloudInstance,
                commandSource: string.Empty,
                processorApplicable: item.ProcessorApplicable,
                controllerApplicable: item.ControllerApplicable,
                absoluteExpirationTime: DateTimeOffset.UtcNow.AddDays(Config.Instance.CosmosDBQueues.TimeToLiveDays),
                lastActive: item.LastActive,
                isSuspended: item.IsSuspended,
                queueStorageType: new FrontDoorQueueStorageSelector(PrivacyCommandType.AgeOut).Process(item));
        }
    }

    public class FrontDoorQueueStorageSelector : ICommandVisitor<Client.PrivacyCommand, QueueStorageType>
    {
        private readonly PrivacyCommandType privacyCommandType;

        public FrontDoorQueueStorageSelector(PrivacyCommandType privacyCommandType)
        {
            this.privacyCommandType = privacyCommandType;
        }

        public PrivacyCommandType Classify(Client.PrivacyCommand command)
        {
            return this.privacyCommandType;
        }

        public QueueStorageType VisitDelete(Client.PrivacyCommand deleteCommand)
        {
            return QueueStorageType.AzureCosmosDb;
        }

        public QueueStorageType VisitExport(Client.PrivacyCommand exportCommand)
        {
            return QueueStorageType.AzureCosmosDb;
        }

        public QueueStorageType VisitAccountClose(Client.PrivacyCommand accountCloseCommand)
        {
            return QueueStorageType.AzureCosmosDb;
        }

        public QueueStorageType VisitAgeOut(Client.PrivacyCommand ageOutCommand)
        {
            if (ageOutCommand.Subject is MsaSubject)
            {
                return QueueStorageType.AzureQueueStorage;
            }

            return QueueStorageType.AzureCosmosDb;
        }
    }

#endif

}
