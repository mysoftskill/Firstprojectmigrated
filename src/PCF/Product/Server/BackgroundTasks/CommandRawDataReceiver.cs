namespace Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks
{
    using System;
    using System.Collections.Concurrent;
    using System.Globalization;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.Cosmos;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class CommandRawDataReceiver : ICommandLifecycleCheckpointProcessor
    {
        private readonly ConcurrentQueue<string> records;
        private int approximateByteCount;
        private DateTimeOffset lastCheckpointTime;

        public CommandRawDataReceiver()
        {
            this.records = new ConcurrentQueue<string>();
            this.approximateByteCount = 0;
            this.lastCheckpointTime = DateTimeOffset.UtcNow;
        }

        public void Process(CommandRawDataEvent rawDataEvent)
        {
            if (rawDataEvent.PxsCommands != null)
            {
                foreach (var command in rawDataEvent.PxsCommands)
                {
                    this.Enqueue(command);
                }
            }
            
            if (rawDataEvent.PxsCommand != null)
            {
                this.Enqueue(rawDataEvent.PxsCommand);
            }
        }

        private void Enqueue(JObject pxsCommand)
        {
            if (TrySerializeToCosmosRawString(pxsCommand, out string serializedCommand))
            {
                this.records.Enqueue(serializedCommand);
                this.approximateByteCount += serializedCommand.Length;
            }
        }

        public void Process(CommandCompletedEvent completedEvent)
        {
            // No-op
        }

        public void Process(CommandSoftDeleteEvent softDeleteEvent)
        {
            // No-op
        }

        public void Process(CommandStartedEvent startedEvent)
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

        public void Process(CommandDroppedEvent droppedEvent)
        {
            // No-op
        }

        public bool ShouldCheckpoint()
        {
            // Read checkpoint interval from app config first. If it's not
            // available in app config, fall back to the original value set in template.
            var checkPointInterval = FlightingUtilities.GetConfigValue<long>(
                config: ConfigNames.PCF.CommandRawDataReceiverMaxCheckpointIntervalSecs, 
                defaultValue: Config.Instance.Cosmos.Streams.PrivacyCommand.MaxCheckpointIntervalSecs);
            
            bool isTimeToRun = (DateTimeOffset.UtcNow - this.lastCheckpointTime) > TimeSpan.FromSeconds(checkPointInterval);
            bool isQueueRecordsEnoughToRun = this.approximateByteCount > Config.Instance.Cosmos.Streams.PrivacyCommand.MaxCheckpointQueueSizeBytes;
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
                this.approximateByteCount -= record.Length;
            }

            if (count > 0)
            {
                await CosmosStreamWriter.PrivacyCommandCosmosWriter().WriteBatchRecordsToCosmosAsync(DateTimeOffset.UtcNow, sb.ToString(), count);
            }

            this.lastCheckpointTime = DateTimeOffset.UtcNow;
        }

        public static bool TrySerializeToCosmosRawString(JObject rawCommand, out string cosmosRawString)
        {
            var (pcfCommand, pxsCommand) = PxsCommandParser.DummyParser.Process(rawCommand);

            string lastActiveTime = string.Empty;
            if (pcfCommand is AgeOutCommand ageOutCommand)
            {
                if (ageOutCommand.LastActive.HasValue)
                {
                    lastActiveTime = ageOutCommand.LastActive.Value.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
                }
            }

            string startDateRange = string.Empty;
            string endDateRange = string.Empty;
            string predicate = string.Empty;

            if (pcfCommand.CommandType == PrivacyCommandType.Delete)
            {
                var deleteCommand = pcfCommand as DeleteCommand;
                startDateRange = deleteCommand.TimeRangePredicate.StartTime.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
                endDateRange = deleteCommand.TimeRangePredicate.EndTime.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);

                if (deleteCommand.Predicate != null)
                {
                    predicate = JsonConvert.SerializeObject(deleteCommand.Predicate);
                }
            }

            var sb = new StringBuilder();
            sb.Append($"{JsonConvert.SerializeObject(pcfCommand.DataTypeIds)}\t");
            sb.Append($"{pcfCommand.Timestamp.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)}\t");
            sb.Append($"{pcfCommand.CommandId.GuidValue:D}\t");
            sb.Append($"{pcfCommand.RequestBatchId.GuidValue:D}\t");
            sb.Append($"{pcfCommand.CorrelationVector ?? string.Empty}\t");
            sb.Append($"{JsonConvert.SerializeObject(pcfCommand.Subject)}\t");
            sb.Append($"{pxsCommand.AuthorizationId ?? string.Empty}\t");
            sb.Append($"{startDateRange}\t");
            sb.Append($"{endDateRange}\t");
            sb.Append($"{predicate}\t");
            sb.Append($"{pcfCommand.CommandType.ToString()}\t");
            sb.Append($"{pcfCommand.CloudInstance ?? string.Empty}\t");
            sb.Append($"{pcfCommand.CommandSource ?? string.Empty}\t");
            sb.Append($"{pcfCommand.ProcessorApplicable?.ToString() ?? string.Empty}\t");
            sb.Append($"{pcfCommand.ControllerApplicable?.ToString() ?? string.Empty}\t");
            sb.Append($"{pxsCommand.Requester ?? string.Empty}\t");
            sb.Append($"{pxsCommand.IsTestRequest}\t");
            sb.Append($"{lastActiveTime}");

            cosmosRawString = sb.ToString();
            return true;
        }
    }
}
