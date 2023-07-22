namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.Azure.ComplianceServices.Common.Interfaces;

    using PerformanceCounterType = Microsoft.Azure.ComplianceServices.Common.Instrumentation.PerformanceCounterType;

    public enum SemaphorePriority
    {
        /// <summary>
        /// Highest priority. Should be processed as soon as possible.
        /// Example: Anything affecting agent traffic, such as DeleteFromQueue work item.
        /// </summary>
        RealTime = 0,

        /// <summary>
        /// High priority. Should be procssed soon.
        /// Examples: Event Hubs, Anything involving draining existing traffic out of queues.
        /// </summary>
        High = 1,

        /// <summary>
        /// Normal priority. This is the default.
        /// </summary>
        Normal = 2,

        /// <summary>
        /// Low priority. These will be processed last.
        /// Examples: Anything that is bringing net-new commands into the system, such as FilterAndRouteWorkItem.
        /// </summary>
        Low = 3,

        /// <summary>
        /// Absolute lowest priority. Examples are replay, agent queue drain, etc.
        /// </summary>
        Background = 4,
    }

    public class PrioritySemaphore
    {
        // The index is the priority. 0 maps to realtime.
        private readonly SemaphoreSlim[] semaphores;

        private readonly IPerformanceCounter acquireTimeCounter = PerfCounterUtility.GetOrCreate(PerformanceCounterType.Number, "PrioritySemaphoreAcquisitionTime");
        private readonly IPerformanceCounter tokensInUseCounter = PerfCounterUtility.GetOrCreate(PerformanceCounterType.Number, "PrioritySemaphoreTokensInUse");
        private readonly IPerformanceCounter waitersCounter = PerfCounterUtility.GetOrCreate(PerformanceCounterType.Number, "PrioritySemaphoreWaiters");

        /// <summary>
        /// Global intance of priority semaphore. Used to distribute work.
        /// </summary>
        public static PrioritySemaphore Instance { get; } = new PrioritySemaphore();

        private PrioritySemaphore()
        {
            var values = Enum.GetValues(typeof(SemaphorePriority));
            this.semaphores = new SemaphoreSlim[values.Length];

            // 25 tokens per core, divided by the number of differnet pools.
            int tokensPool = 25 * Environment.ProcessorCount / values.Length;

            for (int i = 0; i < values.Length; ++i)
            {
                if (!Enum.IsDefined(typeof(SemaphorePriority), i))
                {
                    throw new InvalidOperationException($"Unexpected condition. Expected to find an entry in PrioritySemaphore with value = {i}");
                }

                this.semaphores[i] = new SemaphoreSlim(tokensPool, tokensPool);
                this.TotalTokenCount += tokensPool;
            }
        }

        /// <summary>
        /// The number of tokens per priority bucket.
        /// </summary>
        public int TokensPerPriority { get; }

        /// <summary>
        /// The global number of tokens.
        /// </summary>
        public int TotalTokenCount { get; }
        
        /// <summary>
        /// Gets the current number of tokens for the given priority. Note that this is not a reservation
        /// and that calls to acquire a token may still block.
        /// </summary>
        public int GetAvailableTokenCount(SemaphorePriority priority) => this.semaphores[(int)priority].CurrentCount;

        public async Task<IDisposable> WaitAsync(SemaphorePriority taskPriority)
        {
            Stopwatch sw = Stopwatch.StartNew();
            string taskPriorityString = taskPriority.ToString();

            this.waitersCounter.Increment(taskPriorityString);
            int priority = (int)taskPriority;

            try
            {
                // Attempt to enter all of the semaphores that are at or lower than our priority.
                // This allows high value tasks to preempt low value tasks.
                for (int i = this.semaphores.Length - 1; i >= priority; --i)
                {
                    // .Wait(0) returns immediately.
                    if (this.semaphores[i].Wait(0))
                    {
                        return new SemaphoreReleaser(this.semaphores[i], (SemaphorePriority)i, this.tokensInUseCounter);
                    }
                }

                // Okay, no luck. Let's just wait on the semaphore from our pool.
                await this.semaphores[priority].WaitAsync();
                return new SemaphoreReleaser(this.semaphores[priority], taskPriority, this.tokensInUseCounter);
            }
            finally
            {
                // Microseconds.
                this.acquireTimeCounter.Set(taskPriorityString, (int)(sw.Elapsed.TotalMilliseconds * 1000));
                this.waitersCounter.Decrement(taskPriorityString);
            }
        }

        private class SemaphoreReleaser : IDisposable
        {
            private readonly SemaphoreSlim semaphore;
            private readonly SemaphorePriority priority;
            private readonly IPerformanceCounter counter;

            [SuppressMessage("Microsoft.Reliability", "CA2004:RemoveCallsToGCKeepAlive")]
            public SemaphoreReleaser(SemaphoreSlim semaphore, SemaphorePriority priority, IPerformanceCounter counter)
            {
                this.semaphore = semaphore;
                this.priority = priority;
                this.counter = counter;
                this.counter.Increment(this.priority.ToString());

                // Note: in the paranoid case, GC can claim an object before the constructor has finished running, once there are no more references to "this".
                // Indeed, it's also possible for the finalizer and the .ctor to run in parallel!
                // In such a case, it's unlikely but possible for the semaphore to be unassigned, which would prevent it from being released,
                // which could lead to system-wide deadlock. We use GC.KeepAlive to store a reference to "this" at least until the instance fields are assigned.
                GC.KeepAlive(this);
            }

            ~SemaphoreReleaser()
            {
                this.Dispose(false);
            }

            public void Dispose()
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                this.counter?.Decrement(this.priority.ToString());
                this.semaphore?.Release();
            }
        }
    }
}
