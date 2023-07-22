namespace Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common.DistributedLocking;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    /// <summary>
    /// A periodic task that cleans up export storage.
    /// </summary>
    public class ExportStorageCleanupTask : WorkerTaskBase<object, ExportStorageCleanupTask.LockState>
    {
        public ExportStorageCleanupTask() : this(null)
        {
        }

        public ExportStorageCleanupTask(IDistributedLockPrimitives<LockState> lockPrimitives)
            : base(Config.Instance.Worker.Tasks.ExportStorageCleanupTask.CommonConfig, nameof(ExportStorageCleanupTask), lockPrimitives)
        {
        }

        protected override LockState GetFinalLockState(out TimeSpan leaseTime)
        {
            DateTimeOffset nextStartTime = DateTimeOffset.UtcNow.Date.AddDays(1).AddHours(Config.Instance.Worker.Tasks.ExportStorageCleanupTask.TaskStartHour);
            leaseTime = nextStartTime - DateTimeOffset.UtcNow + TimeSpan.FromMinutes(1);
            return new LockState { NextStartTime = nextStartTime };
        }

        protected override IEnumerable<Func<Task>> GetTasksAsync(LockState state, object parameters)
        {
            return ExportStorageManager.Instance.CleanupOldContainersAsync();
        }

        protected override bool ShouldRun(LockState lockState)
        {
            DateTimeOffset nextTimeToRun = lockState?.NextStartTime ?? DateTimeOffset.MinValue;
            return nextTimeToRun <= DateTime.UtcNow;
        }

        // State stored along with the lock.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public class LockState
        {
            public DateTimeOffset NextStartTime { get; set; }
        }
    }
}
