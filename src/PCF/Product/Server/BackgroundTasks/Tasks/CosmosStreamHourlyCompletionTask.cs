namespace Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common.DistributedLocking;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.Cosmos;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// A periodic task that create a completion signal cosmos stream for each hourly stream.
    /// This "capstone" completion stream is needed for consumer to decide when it is done with writing/appending to the stream.
    /// </summary>
    public class CosmosStreamHourlyCompletionTask : WorkerTaskBase<object, CosmosStreamHourlyCompletionTask.LockState>
    {
        private DateTimeOffset lastCompletedHourWindow;

        public CosmosStreamHourlyCompletionTask() : this(null)
        {
        }

        public CosmosStreamHourlyCompletionTask(IDistributedLockPrimitives<LockState> queueLockPrimitives)
            : base(Config.Instance.Worker.Tasks.CosmosStreamHourlyCompletion.CommonConfig, nameof(CosmosStreamHourlyCompletionTask), queueLockPrimitives)
        {
            // Set default value for cosmos stream last completed hour
            this.lastCompletedHourWindow = GetDefaultCompletedHour();
        }

        protected override LockState GetFinalLockState(out TimeSpan leaseTime)
        {
            var timeToCloseNextHourWindow = this.lastCompletedHourWindow.AddHours(1).AddMinutes(Config.Instance.Worker.Tasks.CosmosStreamHourlyCompletion.MinutesToWaitToCloseCurrentHour);
            leaseTime = (timeToCloseNextHourWindow <= DateTimeOffset.UtcNow) ? TimeSpan.FromMinutes(1) : (timeToCloseNextHourWindow - DateTimeOffset.UtcNow);

            IncomingEvent.Current?.SetProperty("LastCompleteHourWindow", this.lastCompletedHourWindow.ToString());
            IncomingEvent.Current?.SetProperty("TimeToCloseNextHourWindow", timeToCloseNextHourWindow.ToString());
            IncomingEvent.Current?.SetProperty("ExtendLeaseMin", leaseTime.TotalMinutes.ToString());
            DualLogger.Instance.Information(nameof(CosmosStreamHourlyCompletionTask), $"[{nameof(CosmosStreamHourlyCompletionTask)}] Updating LastCompleteHourWindow to: {this.lastCompletedHourWindow}");

            return new LockState { LastCompletedHourWindow = this.lastCompletedHourWindow };
        }

        protected override IEnumerable<Func<Task>> GetTasksAsync(LockState state, object parameters)
        {
            var tasks = new List<Func<Task>>();

            while (CanCloseHourWindow(this.lastCompletedHourWindow.AddHours(1)))
            {
                DateTimeOffset nextHourWindow = this.lastCompletedHourWindow.AddHours(1);
                tasks.Add(() => CosmosStreamWriter.AuditLogCosmosWriter().CloseHourWindowAsync(nextHourWindow));
                tasks.Add(() => CosmosStreamWriter.PrivacyCommandCosmosWriter().CloseHourWindowAsync(nextHourWindow));
                this.lastCompletedHourWindow = nextHourWindow;
            }

            return tasks;
        }

        protected override bool ShouldRun(LockState lockState)
        {
            if (lockState?.LastCompletedHourWindow != null)
            {
                this.lastCompletedHourWindow = lockState.LastCompletedHourWindow;
            }

            DateTimeOffset hourToClose = this.lastCompletedHourWindow.AddHours(1);

            return CanCloseHourWindow(hourToClose);
        }

        private static bool CanCloseHourWindow(DateTimeOffset hourWindow)
        {
            TimeSpan timeToWaitToCloseCurrentHour = TimeSpan.FromMinutes(Config.Instance.Worker.Tasks.CosmosStreamHourlyCompletion.MinutesToWaitToCloseCurrentHour);
            DateTimeOffset timeToClose = hourWindow + timeToWaitToCloseCurrentHour;

            return timeToClose <= DateTimeOffset.UtcNow;
        }

        private static DateTimeOffset GetDefaultCompletedHour()
        {
            DateTimeOffset defaultCompletedHourWindow =
                DateTimeOffset.UtcNow.AddDays(Config.Instance.Worker.Tasks.CosmosStreamHourlyCompletion.StartDateOffsetFromCurrentTime);

            return new DateTimeOffset(
                defaultCompletedHourWindow.Year,
                defaultCompletedHourWindow.Month,
                defaultCompletedHourWindow.Day,
                defaultCompletedHourWindow.Hour,
                0,
                0,
                defaultCompletedHourWindow.Offset);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public class LockState
        {
            public DateTimeOffset LastCompletedHourWindow { get; set; }
        }
    }
}