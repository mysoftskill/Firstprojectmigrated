namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Net.Http;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.Azure.ComplianceServices.Common.Interfaces;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    using PerformanceCounterType = Microsoft.Azure.ComplianceServices.Common.Instrumentation.PerformanceCounterType;

    /// <summary>
    /// A disjoint set of threads intended to send and receive responses from requests sent to the stress environment. This is kept separate from
    /// the main set of threads so as to not interfere with production traffic and not overwhelm the machine.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    [ExcludeFromCodeCoverage]
    internal class StressThreadpool
    {
        private static Lazy<StressThreadpool> instance = new Lazy<StressThreadpool>(() => new StressThreadpool());
        private const int MaxPendingRequests = 1000;

        private readonly IPerformanceCounter requestsForwardedPerSecond;
        private readonly IPerformanceCounter requestsErroredPerSecond;
        private readonly IPerformanceCounter forwardsDroppedPerSecond;
        private readonly IPerformanceCounter latencyCounter;

        private readonly BlockingCollection<Tuple<SendOrPostCallback, object>> sendOrPostCallbacks;
        private readonly BlockingCollection<Func<Task<HttpResponseMessage>>> requests;
        private readonly StressThreadPoolSyncContext synchronizationContext;
        private readonly List<Thread> threads;

        private StressThreadpool()
        {
            this.requestsForwardedPerSecond = PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "StressSource:RequestsForwarded");
            this.requestsErroredPerSecond = PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "StressSource:RequestsErrored");
            this.forwardsDroppedPerSecond = PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "StressSource:RequestsDropped");
            this.latencyCounter = PerfCounterUtility.GetOrCreate(PerformanceCounterType.Percentile, "StressSource:Latency");

            this.sendOrPostCallbacks = new BlockingCollection<Tuple<SendOrPostCallback, object>>();
            this.synchronizationContext = new StressThreadPoolSyncContext(this.sendOrPostCallbacks);
            this.requests = new BlockingCollection<Func<Task<HttpResponseMessage>>>(StressThreadpool.MaxPendingRequests);

            this.threads = new List<Thread>();
            int threadCount = (int)Config.Instance.StressForwarding.ForwardingThreadCount;

            for (int i = 0; i < threadCount; ++i)
            {
                Thread t = new Thread(this.HttpThreadStart)
                {
                    Priority = ThreadPriority.Lowest,
                    IsBackground = true,
                };

                Thread t2 = new Thread(this.SyncContextThreadStart)
                {
                    Priority = ThreadPriority.Lowest,
                    IsBackground = true
                };

                t.Start();
                t2.Start();

                this.threads.Add(t);
                this.threads.Add(t2);
            }
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        public static StressThreadpool Instance => StressThreadpool.instance.Value;

        /// <summary>
        /// Enqueues the request.
        /// </summary>
        public void EnqueueRequest(Func<Task<HttpResponseMessage>> request)
        {
            if (this.requests.Count > StressThreadpool.MaxPendingRequests)
            {
                Func<Task<HttpResponseMessage>> tempRequest;
                if (this.requests.TryTake(out tempRequest))
                {
                    // If we've overrun our buffer, then drop the oldest request. Chances are that it's old and stress won't like it anyway.
                    this.forwardsDroppedPerSecond.Increment();
                }
            }

            // Use try add since it does not block if the collection is full.
            if (!this.requests.TryAdd(request))
            {
                this.forwardsDroppedPerSecond.Increment();
            }
        }

        private async void HttpThreadStart()
        {
            // Set sync context of this thread so async/await traffic will always stay inside the stress threadpool.
            // https://msdn.microsoft.com/en-us/magazine/jj991977.aspx
            SynchronizationContext.SetSynchronizationContext(this.synchronizationContext);

            while (true)
            {
                Stopwatch sw = Stopwatch.StartNew();

                try
                {
                    this.requestsForwardedPerSecond.Increment();

                    Func<Task<HttpResponseMessage>> request = this.requests.Take();
                    await request().TimeoutAfter(TimeSpan.FromMilliseconds(500));
                }
                catch
                {
                    // Let's not crash, mmmkay?
                    this.requestsErroredPerSecond.Increment();
                }
                finally
                {
                    this.latencyCounter.Set((int)sw.ElapsedMilliseconds);
                }
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void SyncContextThreadStart()
        {
            // Set sync context of this thread so async/await traffic will always stay inside the stress threadpool.
            // https://msdn.microsoft.com/en-us/magazine/jj991977.aspx
            SynchronizationContext.SetSynchronizationContext(this.synchronizationContext);

            while (true)
            {
                try
                {
                    Tuple<SendOrPostCallback, object> item = this.sendOrPostCallbacks.Take();
                    item?.Item1(item.Item2);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        /// <summary>
        /// Synchronization context implementation that forces all async/await activity to live only on the stress threadpool.
        /// </summary>
        private class StressThreadPoolSyncContext : SynchronizationContext
        {
            private readonly BlockingCollection<Tuple<SendOrPostCallback, object>> callbackQueue;

            public StressThreadPoolSyncContext(BlockingCollection<Tuple<SendOrPostCallback, object>> callbackQueue)
            {
                this.callbackQueue = callbackQueue;
            }

            public override void Post(SendOrPostCallback d, object state)
            {
                this.callbackQueue.Add(Tuple.Create(d, state));
            }

            public override void Send(SendOrPostCallback d, object state)
            {
                this.callbackQueue.Add(Tuple.Create(d, state));
            }
        }
    }
}
