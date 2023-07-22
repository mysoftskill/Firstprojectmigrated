namespace Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.AuditLog;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.Cosmos;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.Helpers;

    /// <summary>
    /// Receives from event hub and write batch into cosmos
    /// </summary>
    public class AuditLogReceiver : ICommandLifecycleCheckpointProcessor
    {
        private readonly CommandStartedVisitor startedVisitor = new CommandStartedVisitor();
        private readonly CommandCompletedVisitor completedVisitor = new CommandCompletedVisitor();
        private readonly CommandSoftDeleteVisitor softDeleteVisitor = new CommandSoftDeleteVisitor();
        private readonly CommandDroppedVisitor droppedVisitor = new CommandDroppedVisitor();

        private readonly ConcurrentQueue<string> records;

        private int approximateQueueSizeBytes;
        private DateTimeOffset lastCheckpointTime;

        public AuditLogReceiver()
        {
            this.records = new ConcurrentQueue<string>();
            this.approximateQueueSizeBytes = 0;
            this.lastCheckpointTime = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Test hook to validate data.
        /// </summary>
        internal IEnumerable<string> Lines => this.records;

        public void Process(CommandCompletedEvent completedEvent)
        {
            if (completedEvent?.AuditLogCommandAction == null || AuditLogCommandAction.None.Equals(completedEvent.AuditLogCommandAction))
            {
                completedEvent.AuditLogCommandAction = this.GetAuditLogAction(completedEvent);
            }

            this.ProcessAuditLogRecord(new AuditLogRecord(
                    completedEvent.CommandId,
                    completedEvent.Timestamp,
                    completedEvent.AgentId,
                    completedEvent.AssetGroupId,
                    completedEvent.AssetGroupQualifier,
                    completedEvent.AuditLogCommandAction,
                    completedEvent.AffectedRows,
                    completedEvent.ClaimedVariantIds,
                    completedEvent.NonTransientExceptions,
                    completedEvent.CommandType,
                    string.Empty,
                    string.Empty,
                    string.Empty));
        }

        public void Process(CommandSoftDeleteEvent softDeleteEvent)
        {
            if (softDeleteEvent?.AuditLogCommandAction == null || AuditLogCommandAction.None.Equals(softDeleteEvent.AuditLogCommandAction))
            {
                softDeleteEvent.AuditLogCommandAction = this.softDeleteVisitor.Process(softDeleteEvent.CommandType);
            }

            this.ProcessAuditLogRecord(new AuditLogRecord(
                    softDeleteEvent.CommandId,
                    softDeleteEvent.Timestamp,
                    softDeleteEvent.AgentId,
                    softDeleteEvent.AssetGroupId,
                    softDeleteEvent.AssetGroupQualifier,
                    softDeleteEvent.AuditLogCommandAction,
                    0,
                    new string[0],
                    softDeleteEvent.NonTransientExceptions,
                    softDeleteEvent.CommandType,
                    string.Empty,
                    string.Empty,
                    string.Empty));
        }

        public void Process(CommandStartedEvent startedEvent)
        {
            if (startedEvent?.AuditLogCommandAction == null || AuditLogCommandAction.None.Equals(startedEvent.AuditLogCommandAction))
            {
                startedEvent.AuditLogCommandAction = this.startedVisitor.Process(startedEvent.CommandType);
            }

            this.ProcessAuditLogRecord(new AuditLogRecord(
                startedEvent.CommandId,
                startedEvent.Timestamp,
                startedEvent.AgentId,
                startedEvent.AssetGroupId,
                startedEvent.AssetGroupQualifier,
                startedEvent.AuditLogCommandAction,
                0,
                new string[0],
                string.Empty,
                startedEvent.CommandType,
                string.Empty,
                startedEvent.AssetGroupStreamName,
                startedEvent.VariantStreamName));
        }

        public void Process(CommandDroppedEvent droppedEvent)
        {
            if (droppedEvent?.AuditLogCommandAction == null || AuditLogCommandAction.None.Equals(droppedEvent.AuditLogCommandAction))
            {
                droppedEvent.AuditLogCommandAction = this.droppedVisitor.Process(droppedEvent.CommandType);
            }

            this.ProcessAuditLogRecord(new AuditLogRecord(
                droppedEvent.CommandId,
                droppedEvent.Timestamp,
                droppedEvent.AgentId,
                droppedEvent.AssetGroupId,
                droppedEvent.AssetGroupQualifier,
                droppedEvent.AuditLogCommandAction,
                0,
                new string[0],
                string.Empty,
                droppedEvent.CommandType,
                droppedEvent.NotApplicableReasonCode,
                droppedEvent.AssetGroupStreamName,
                droppedEvent.VariantStreamName));
        }

        public void Process(CommandRawDataEvent rawDataEvent)
        {
            // No-op
        }

        public void Process(CommandSentToAgentEvent sentToAgentEvent)
        {
            // No-op
        }

        public void Process(CommandPendingEvent pendingEvent)
        {
            // No-op
        }

        public void Process(CommandFailedEvent failedEvent)
        {
            // No-op
        }

        public void Process(CommandUnexpectedEvent unexpectedEvent)
        {
            // No-op
        }

        public void Process(CommandVerificationFailedEvent verificationFailedEvent)
        {
            // No-op
        }

        public void Process(CommandUnexpectedVerificationFailureEvent unexpectedVerificationFailureEvent)
        {
            // No-op
        }

        public bool ShouldCheckpoint()
        {
            bool isTimeToRun = (DateTimeOffset.UtcNow - this.lastCheckpointTime) > TimeSpan.FromSeconds(Config.Instance.Cosmos.Streams.AuditLog.MaxCheckpointIntervalSecs);
            bool isQueueRecordsEnoughToRun = this.approximateQueueSizeBytes > Config.Instance.Cosmos.Streams.AuditLog.MaxCheckpointQueueSizeBytes;
            return isTimeToRun || isQueueRecordsEnoughToRun;
        }

        public async Task CheckpointAsync()
        {
            var sb = new StringBuilder();
            int count = 0;

            while (this.records.TryDequeue(out string record))
            {
                count++;
                sb.Append(record);
                sb.Append(Environment.NewLine);
                this.approximateQueueSizeBytes -= record.Length;
            }

            if (count > 0)
            {
                int writeAttempts = 0;
                var backoff = new ExponentialBackoff
                        (delay: TimeSpan.FromSeconds(1),
                        maxDelay: TimeSpan.FromSeconds(13),
                        delayStartsWith: TimeSpan.FromSeconds(5));

                while (true)
                {
                    writeAttempts++;
                    try
                    {
                        await CosmosStreamWriter.AuditLogCosmosWriter().WriteBatchRecordsToCosmosAsync(DateTimeOffset.UtcNow, sb.ToString(), count).ConfigureAwait(false);

                        // successfully makes it here, exits the retry loop
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (writeAttempts >= 4)
                        {
                            var separateRecords = sb.ToString().Split(new String[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                            DualLogger.Instance.Error(nameof(AuditLogReceiver), ex, $"Reached write to cosmos retry limit. Failed to checkpoint audit log records={String.Join(" | ", separateRecords)}");
                            throw;
                        }

                        DualLogger.Instance.Warning(nameof(AuditLogReceiver), ex, $"Failed to write to cosmos stream. Sleeping for a couple seconds and retry. ");
                        await backoff.Delay().ConfigureAwait(false);
                    }
                }
            }

            this.lastCheckpointTime = DateTimeOffset.UtcNow;
        }

        private void ProcessAuditLogRecord(AuditLogRecord auditLogRecord)
        {
            if (auditLogRecord.Action == AuditLogCommandAction.None)
            {
                // Drop the record if action is None.
                return;
            }

            string serializedAuditLogRecord = auditLogRecord.ToCosmosRawString();
            this.records.Enqueue(serializedAuditLogRecord);

            this.approximateQueueSizeBytes += serializedAuditLogRecord.Length;
        }

        private AuditLogCommandAction GetAuditLogAction(CommandCompletedEvent completedEvent)
        {
            if (completedEvent.ForceCompleteReasonCode != null)
            {
                if (Config.Instance.Common.IsTestEnvironment)
                {
                    ProductionSafetyHelper.EnsureNotInProduction();
                }
                else if (completedEvent.CommandType != PrivacyCommandType.Export)
                {
                    // Force completions for Delete and Account should not occur and should not show up in audit.
                    throw new ArgumentException("Delete and Account close should not be force completed");
                }

                switch (completedEvent.ForceCompleteReasonCode.Value)
                {
                    case ForceCompleteReasonCode.ForceCompleteFromPartnerTestPage when completedEvent.CommandType == PrivacyCommandType.Export:
                        return AuditLogCommandAction.ExportFailedByManualComplete;

                    case ForceCompleteReasonCode.ForceCompleteFromAgeoutTimer when completedEvent.CommandType == PrivacyCommandType.Export:
                        return AuditLogCommandAction.ExportFailedByAutoComplete;

                    default:
                        throw new InvalidOperationException($"Invalid {nameof(ForceCompleteReasonCode)} '{completedEvent.ForceCompleteReasonCode}' and {nameof(PrivacyCommandType)} '{completedEvent.CommandType}' combination.");
                }
            }

            if (completedEvent.IgnoredByVariant)
            {
                return AuditLogCommandAction.IgnoredByVariant;
            }

            return this.completedVisitor.Process(completedEvent.CommandType);
        }

        /// <summary>
        /// Maps privacy command type to audit log command action for started events.
        /// </summary>
        private class CommandStartedVisitor : ICommandVisitor<PrivacyCommandType, AuditLogCommandAction>
        {
            public PrivacyCommandType Classify(PrivacyCommandType command) => command;

            public AuditLogCommandAction VisitAccountClose(PrivacyCommandType accountCloseCommand) => AuditLogCommandAction.DeleteStart;

            public AuditLogCommandAction VisitDelete(PrivacyCommandType deleteCommand) => AuditLogCommandAction.DeleteStart;

            public AuditLogCommandAction VisitExport(PrivacyCommandType exportCommand) => AuditLogCommandAction.ExportStart;

            public AuditLogCommandAction VisitAgeOut(PrivacyCommandType ageOutCommand) => AuditLogCommandAction.None;
        }

        /// <summary>
        /// Maps privacy command type to audit log command action for soft delete events.
        /// </summary>
        private class CommandSoftDeleteVisitor : ICommandVisitor<PrivacyCommandType, AuditLogCommandAction>
        {
            public PrivacyCommandType Classify(PrivacyCommandType command) => command;

            public AuditLogCommandAction VisitAccountClose(PrivacyCommandType accountCloseCommand) => AuditLogCommandAction.SoftDelete;

            public AuditLogCommandAction VisitDelete(PrivacyCommandType deleteCommand) => AuditLogCommandAction.SoftDelete;

            public AuditLogCommandAction VisitExport(PrivacyCommandType exportCommand) => AuditLogCommandAction.None;

            public AuditLogCommandAction VisitAgeOut(PrivacyCommandType ageOutCommand) => AuditLogCommandAction.None;
        }

        /// <summary>
        /// Maps privacy command type to audit log command action for completed events.
        /// </summary>
        private class CommandCompletedVisitor : ICommandVisitor<PrivacyCommandType, AuditLogCommandAction>
        {
            public PrivacyCommandType Classify(PrivacyCommandType command) => command;

            public AuditLogCommandAction VisitAccountClose(PrivacyCommandType accountCloseCommand) => AuditLogCommandAction.HardDelete;

            public AuditLogCommandAction VisitDelete(PrivacyCommandType deleteCommand) => AuditLogCommandAction.HardDelete;

            public AuditLogCommandAction VisitExport(PrivacyCommandType exportCommand) => AuditLogCommandAction.ExportComplete;

            public AuditLogCommandAction VisitAgeOut(PrivacyCommandType ageOutCommand) => AuditLogCommandAction.None;
        }

        /// <summary>
        /// Maps privacy command type to audit log command action for dropped events.
        /// </summary>
        private class CommandDroppedVisitor : ICommandVisitor<PrivacyCommandType, AuditLogCommandAction>
        {
            public PrivacyCommandType Classify(PrivacyCommandType command) => command;

            public AuditLogCommandAction VisitAccountClose(PrivacyCommandType accountCloseCommand) => AuditLogCommandAction.None;

            public AuditLogCommandAction VisitDelete(PrivacyCommandType deleteCommand) => AuditLogCommandAction.None;

            public AuditLogCommandAction VisitExport(PrivacyCommandType exportCommand) => AuditLogCommandAction.NotApplicable;

            public AuditLogCommandAction VisitAgeOut(PrivacyCommandType ageOutCommand) => AuditLogCommandAction.None;
        }
    }
}
