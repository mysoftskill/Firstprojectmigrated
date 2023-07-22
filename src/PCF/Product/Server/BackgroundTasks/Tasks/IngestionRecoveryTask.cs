namespace Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common.DistributedLocking;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// A periodic task that reinserts commands stuck in SendingToAgent state.
    /// </summary>
    public class IngestionRecoveryTask : WorkerTaskBase<object, IngestionRecoveryTask.LockState>
    {
        private readonly IAzureWorkItemQueuePublisher<IngestionRecoveryWorkItem> ingestionRecoveryPublisher;
        private DateTimeOffset currentRunNewestRecordQueryTime = DateTimeOffset.MinValue;

        public IngestionRecoveryTask(IAzureWorkItemQueuePublisher<IngestionRecoveryWorkItem> ingestionRecoveryPublisher)
            : this(ingestionRecoveryPublisher, null)
        {
        }

        public IngestionRecoveryTask(
            IAzureWorkItemQueuePublisher<IngestionRecoveryWorkItem> ingestionRecoveryPublisher,
            IDistributedLockPrimitives<LockState> lockPrimitives)
            : base(Config.Instance.Worker.Tasks.IngestionRecoveryTask.CommonConfig, nameof(IngestionRecoveryTask), lockPrimitives)
        {
            this.ingestionRecoveryPublisher = ingestionRecoveryPublisher;
        }

        protected override LockState GetFinalLockState(out TimeSpan leaseTime)
        {
            DateTimeOffset nextStartTime = DateTimeOffset.UtcNow.Date.AddDays(1).AddHours(Config.Instance.Worker.Tasks.IngestionRecoveryTask.TaskStartHour);
            IncomingEvent.Current?.SetProperty("NextStartTime", nextStartTime.ToString());

            leaseTime = nextStartTime - DateTimeOffset.UtcNow + TimeSpan.FromMinutes(1);

            return new LockState { NextStartTime = nextStartTime, NewestRecordQueryTimeFromLastRun = this.currentRunNewestRecordQueryTime };
        }

        protected override IEnumerable<Func<Task>> GetTasksAsync(LockState state, object parameters)
        {
            yield return async () => { await this.IngestionRecoveryTaskAsync(state); };
        }

        private async Task IngestionRecoveryTaskAsync(LockState state)
        {
            if (FlightingUtilities.IsEnabled(FlightingNames.IngestionRecoveryTaskDisabled))
            {
                return; // Feature disabled
            }

            // Pick up from last run with offset
            DateTimeOffset oldestRecord;
            if (state == null || state.NewestRecordQueryTimeFromLastRun == DateTimeOffset.MinValue)
            {
                // Use config values if not run previously
                oldestRecord = DateTimeOffset.UtcNow.AddDays(-Config.Instance.Worker.Tasks.IngestionRecoveryTask.MaxCreatedAgeDays);
            }
            else
            {
                // Use the last run time with an hour of overlap.
                oldestRecord = state.NewestRecordQueryTimeFromLastRun.Subtract(TimeSpan.FromHours(1));
            }

            // Task is run every day, so just run it for 1 day from last run, in case the last run takes longer than 1 day, use on demand repair to catch up.
            DateTimeOffset newestRecord = oldestRecord.AddDays(1);
            this.currentRunNewestRecordQueryTime = newestRecord;

            var splitWindowInHours = 1;

            DualLogger.Instance.Information(nameof(IngestionRecoveryTask), $"Initiating ingestion recovery for oldestRecord={oldestRecord} and newestRecord={newestRecord}");

            // Break into multiple work items for every hour.
            while (oldestRecord < newestRecord)
            {
                await this.ingestionRecoveryPublisher.PublishAsync(new IngestionRecoveryWorkItem()
                {
                    ContinuationToken = null,
                    NewestRecordCreationTime = newestRecord.Subtract(oldestRecord) > TimeSpan.FromHours(splitWindowInHours) ? oldestRecord.AddHours(splitWindowInHours) : newestRecord,
                    OldestRecordCreationTime = oldestRecord,
                    exportOnly = false,
                    nonExportOnly = false,
                    isOnDemandRepairItem = false
                });
                oldestRecord = oldestRecord.AddHours(splitWindowInHours);
            }
        }

        protected override bool ShouldRun(LockState lockState)
        {
            DateTimeOffset nextTimeToRun = lockState?.NextStartTime ?? DateTimeOffset.MinValue;
            return nextTimeToRun <= DateTime.UtcNow;
        }

        // State stored along with the lock.
        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public class LockState
        {
            /// <summary>
            /// The time this task should wake up to process more items.
            /// </summary>
            public DateTimeOffset NextStartTime { get; set; }

            /// <summary>
            /// The "NewestRecordCreationTime" value of the last run.
            /// </summary>
            public DateTimeOffset NewestRecordQueryTimeFromLastRun { get; set; }
        }
    }
}
