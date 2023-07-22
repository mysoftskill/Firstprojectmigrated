namespace Microsoft.Membership.MemberServices.Common.Worker
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.DistributedLocking;
    using Microsoft.PrivacyServices.Common.Azure;
    using Newtonsoft.Json;

    /// <summary>
    ///     DistributedBackgroundWorker
    /// </summary>
    public abstract class DistributedBackgroundWorker : BackgroundWorker
    {
        private readonly string componentName;
        private readonly DistributedLock<LockState> distributedLock;
        private readonly LockState lockState;
        private readonly CancellationToken cancellationToken;
        private readonly IDistributedLockPrimitives<DistributedBackgroundWorker.LockState> lockPrimitives;

        /// <summary>
        ///     Creates a new instance of <see cref="DistributedBackgroundWorker" />
        /// </summary>
        /// <param name="distributedLock">distributed lock instance</param>
        /// <param name="lockPrimitives"></param>
        /// <param name="lockState">lock state instance</param>
        /// <param name="cancellationToken">cancellationTokenSource instance</param>
        public DistributedBackgroundWorker
            (DistributedLock<LockState> distributedLock,
            IDistributedLockPrimitives<DistributedBackgroundWorker.LockState> lockPrimitives,
            LockState lockState,
            CancellationToken cancellationToken)
        {
            this.distributedLock = distributedLock ?? throw new ArgumentNullException(nameof(distributedLock));
            this.lockState = lockState ?? throw new ArgumentNullException(nameof(lockState));
            this.cancellationToken = cancellationToken;
            this.lockPrimitives = lockPrimitives;
            this.componentName = this.GetType().Name;
        }

        /// <summary>
        ///     Does the work.
        /// </summary>
        /// <returns>A boolean indicates whether the task succeeds or not.</returns>
        public async override Task<bool> DoWorkAsync()
        {
            var cancellationSource = new CancellationTokenSource();

            try
            {
                // Avoid recursive locking: same worker acquiring the lock it's already holding
                await this.lockPrimitives.CreateIfNotExistsAsync().ConfigureAwait(false);
                var status = await this.lockPrimitives.GetStatusAsync().ConfigureAwait(false);
                if (status.ExpirationTime >= DateTimeOffset.UtcNow)
                {
                    return false;
                }

                var acquireLockResult = await this.distributedLock.TryAcquireAsync(this.lockState.MinLeaseTime, DualLogger.Instance);
                if (!acquireLockResult.Succeeded)
                {
                    return false;
                }

                // Record the time when the lock is acquired
                this.lockState.LockAcquiredTime = DateTimeOffset.UtcNow;

                var distributedWorkTask = Task<bool>.Run(() =>
                {
                    return DoDistributedWorkAsync(cancellationSource.Token);
                }, cancellationSource.Token);

                while (!distributedWorkTask.IsCompleted && !this.cancellationToken.IsCancellationRequested)
                {
                    // When the lock is close to expire we try to extend
                    if (this.distributedLock.RemainingTime <= this.lockState.ExtensionThreshold)
                    {
                        var ttl = DateTimeOffset.UtcNow - this.lockState.LockAcquiredTime;
                        if (ttl > this.lockState.MaxExtensionTtl)
                        {
                            var currentState = JsonConvert.SerializeObject(this.lockState);
                            DualLogger.Instance.Information(this.componentName, $"Reached max limit for extension. Abort the task. Current state: {currentState}.");
                            cancellationSource.Cancel();
                            break;
                        }

                        bool extended = await this.distributedLock.TryExtendAsync(this.lockState.MinLeaseTime, this.lockState, DualLogger.Instance);
                        if (!extended)
                        {
                            // Failed to extend lock. Abort
                            DualLogger.Instance.Error(this.componentName, $"Failed to extend the lock. Abort the task. Lock acquired at {this.lockState.LockAcquiredTime}.");
                            cancellationSource.Cancel();
                            break;
                        }
                    }
                    else
                    {
                        await Task.Yield();
                    }
                }

                if (!distributedWorkTask.IsCompleted || this.cancellationToken.IsCancellationRequested)
                {
                    var currentState = JsonConvert.SerializeObject(this.lockState);
                    DualLogger.Instance.Information(this.componentName, $"Task got canceled. Current State: {currentState}");
                    return false;
                }

                // Calculate the next time to start running task and hold the lock util that time
                this.lockState.NextStartTime = DateTimeOffset.UtcNow + this.lockState.TaskRunFrequency;
                DualLogger.Instance.Information(this.componentName, $"The next time a worker can start should be {this.lockState.NextStartTime}");
                bool extendedUtilNextRun = await this.distributedLock.TryExtendAsync(this.lockState.TaskRunFrequency, this.lockState, DualLogger.Instance);
                if (!extendedUtilNextRun)
                {
                    var currentState = JsonConvert.SerializeObject(this.lockState);
                    DualLogger.Instance.Error(this.componentName, $"Failed to extend the lock util the next time to run.\r\nCurrent State: {currentState}");
                }

                return true;
            }
            catch (Exception ex)
            {
                var currentState = JsonConvert.SerializeObject(this.lockState);
                DualLogger.Instance.Error(this.componentName, $"Exception occurred during runtime. Error Message: {ex.Message}.\r\nCurrent State: {currentState}");
                await Task.Delay(TimeSpan.FromSeconds(RandomHelper.Next(1,30))).ConfigureAwait(false);
                return false;
            }
            finally
            {
                cancellationSource.Dispose();
            }
        }

        /// <summary>
        ///     Does the work only if the lock is acquired.
        /// </summary>
        /// <returns>A task that does the work.</returns>
        public abstract Task<bool> DoDistributedWorkAsync(CancellationToken cancellationToken);

        /// <summary>
        ///     LockState
        /// </summary>
        public class LockState
        {
            /// <summary>
            /// The time when the lock is acquired 
            /// </summary>
            public DateTimeOffset LockAcquiredTime { get; set; }

            /// <summary>
            /// Regular time duration to acquire / extend a lock 
            /// </summary>
            public TimeSpan MinLeaseTime { get; set; }

            /// <summary>
            /// How often a worker runs a task
            /// </summary>
            public TimeSpan TaskRunFrequency { get; set; }

            /// <summary>
            /// The max time limit allowed for lock extension
            /// </summary>
            public TimeSpan MaxExtensionTtl { get; set; }

            /// <summary>
            /// Extend the lock when we have leass than this time remaining 
            /// </summary>
            public TimeSpan ExtensionThreshold { get; set; }

            /// <summary>
            /// The time indicates when a worker can start the next run
            /// </summary>
            public DateTimeOffset NextStartTime { get; set; }
        }
    }
}
