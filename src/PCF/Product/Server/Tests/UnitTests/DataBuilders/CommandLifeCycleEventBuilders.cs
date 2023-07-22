namespace PCF.UnitTests
{
    using System;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.SignalApplicability;
    using Newtonsoft.Json.Linq;

    public class ColdStorageCommandRecordBuilder : TestDataBuilder<CommandHistoryRecord>, INeedDataBuilders
    {
        private readonly CommandId commandId;

        public ColdStorageCommandRecordBuilder(CommandId commandId)
        {
            this.commandId = commandId;
        }

        protected override CommandHistoryRecord CreateNewObject()
        {
            var record = new CommandHistoryRecord(this.commandId);

            record.Core.CreatedTime = DateTimeOffset.UtcNow;
            record.Core.Context = "context";
            record.Core.IsSynthetic = false;
            record.Core.RawPxsCommand = "{}";
            record.Core.IsGloballyComplete = false;
            record.Core.IngestionDataSetVersion = 27;
            record.Core.IngestionAssemblyVersion = "1.2.3.4";
            record.Core.TotalCommandCount = 0;
            record.Core.CommandType = PrivacyCommandType.Delete;
            record.Core.Subject = this.AnMsaSubject().Build();

            record.ClearDirty();
            return record;
        }
    }

    public class CommandStartedEventBuilder : TestDataBuilder<CommandStartedEvent>, INeedDataBuilders
    {
        protected override CommandStartedEvent CreateNewObject()
        {
            return new CommandStartedEvent
            {
                AgentId = this.AnAgentId(),
                AssetGroupId = this.AnAssetGroupId(),
                CommandId = this.ACommandId(),
                CommandType = PrivacyCommandType.Delete,
                AssetGroupQualifier = "qualifier",
                ExportStagingDestinationUri = new Uri("https://exportstaging"),
                ExportStagingPath = "staging path",
                FinalExportDestinationUri = new Uri("https://finalexportdestination"),
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }

    public class CommandSoftDeleteEventBuilder : TestDataBuilder<CommandSoftDeleteEvent>, INeedDataBuilders
    {
        protected override CommandSoftDeleteEvent CreateNewObject()
        {
            return new CommandSoftDeleteEvent
            {
                AgentId = this.AnAgentId(),
                AssetGroupId = this.AnAssetGroupId(),
                CommandId = this.ACommandId(),
                AssetGroupQualifier = "qualifier",
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }

    public class CommandCompletedEventBuilder : TestDataBuilder<CommandCompletedEvent>, INeedDataBuilders
    {
        protected override CommandCompletedEvent CreateNewObject()
        {
            return new CommandCompletedEvent
            {
                AgentId = this.AnAgentId(),
                AssetGroupId = this.AnAssetGroupId(),
                CommandId = this.ACommandId(),
                AssetGroupQualifier = "qualifier",
                Timestamp = DateTimeOffset.UtcNow,
                AffectedRows = 3,
                ClaimedVariantIds = new string[0],
                CommandType = PrivacyCommandType.Delete,
                Delinked = false,
                IgnoredByVariant = false,
                NonTransientExceptions = string.Empty
            };
        }
    }

    public class CommandSentToAgentEventBuilder : TestDataBuilder<CommandSentToAgentEvent>, INeedDataBuilders
    {
        protected override CommandSentToAgentEvent CreateNewObject()
        {
            return new CommandSentToAgentEvent
            {
                AgentId = this.AnAgentId(),
                AssetGroupId = this.AnAssetGroupId(),
                CommandId = this.ACommandId(),
                AssetGroupQualifier = "qualifier",
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }

    public class CommandPendingEventBuilder : TestDataBuilder<CommandPendingEvent>, INeedDataBuilders
    {
        protected override CommandPendingEvent CreateNewObject()
        {
            return new CommandPendingEvent
            {
                AgentId = this.AnAgentId(),
                AssetGroupId = this.AnAssetGroupId(),
                CommandId = this.ACommandId(),
                AssetGroupQualifier = "qualifier",
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }

    public class CommandFailedEventBuilder : TestDataBuilder<CommandFailedEvent>, INeedDataBuilders
    {
        protected override CommandFailedEvent CreateNewObject()
        {
            return new CommandFailedEvent
            {
                AgentId = this.AnAgentId(),
                AssetGroupId = this.AnAssetGroupId(),
                CommandId = this.ACommandId(),
                AssetGroupQualifier = "qualifier",
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }

    public class CommandUnexpectedEventBuilder : TestDataBuilder<CommandUnexpectedEvent>, INeedDataBuilders
    {
        protected override CommandUnexpectedEvent CreateNewObject()
        {
            return new CommandUnexpectedEvent
            {
                AgentId = this.AnAgentId(),
                AssetGroupId = this.AnAssetGroupId(),
                CommandId = this.ACommandId(),
                AssetGroupQualifier = "qualifier",
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }

    public class CommandVerificationFailedEventBuilder : TestDataBuilder<CommandVerificationFailedEvent>, INeedDataBuilders
    {
        protected override CommandVerificationFailedEvent CreateNewObject()
        {
            return new CommandVerificationFailedEvent
            {
                AgentId = this.AnAgentId(),
                AssetGroupId = this.AnAssetGroupId(),
                CommandId = this.ACommandId(),
                AssetGroupQualifier = "qualifier",
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }

    public class CommandUnexpectedVerificationFailureEventBuilder : TestDataBuilder<CommandUnexpectedVerificationFailureEvent>, INeedDataBuilders
    {
        protected override CommandUnexpectedVerificationFailureEvent CreateNewObject()
        {
            return new CommandUnexpectedVerificationFailureEvent
            {
                AgentId = this.AnAgentId(),
                AssetGroupId = this.AnAssetGroupId(),
                CommandId = this.ACommandId(),
                AssetGroupQualifier = "qualifier",
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }

    public class CommandRawDataEventBuilder : TestDataBuilder<CommandRawDataEvent>, INeedDataBuilders
    {
        protected override CommandRawDataEvent CreateNewObject()
        {
            return new CommandRawDataEvent
            {
                AgentId = this.AnAgentId(),
                AssetGroupId = this.AnAssetGroupId(),
                CommandId = this.ACommandId(),
                AssetGroupQualifier = "qualifier",
                Timestamp = DateTimeOffset.UtcNow,
                PxsCommand = JObject.FromObject(this.ADeletePxsCommand())
            };
        }
    }

    public class CommandDroppedEventBuilder : TestDataBuilder<CommandDroppedEvent>, INeedDataBuilders
    {
        protected override CommandDroppedEvent CreateNewObject()
        {
            return new CommandDroppedEvent
            {
                AgentId = this.AnAgentId(),
                AssetGroupId = this.AnAssetGroupId(),
                CommandId = this.ACommandId(),
                AssetGroupQualifier = "qualifier",
                Timestamp = DateTimeOffset.UtcNow,
                NotApplicableReasonCode = ApplicabilityReasonCode.DoesNotMatchAssetGroupSubjects.ToString()
            };
        }
    }
}
