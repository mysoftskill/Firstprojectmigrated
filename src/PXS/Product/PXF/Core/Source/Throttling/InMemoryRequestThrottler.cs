// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Throttling
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;

    /// <inheritdoc />
    public class InMemoryRequestThrottler : IRequestThrottler
    {
        /// <summary>
        ///     Arbitrary upper limit on memory cache.
        /// </summary>
        public const int MaxRecordCount = 100000;

        private readonly ICounterFactory counterFactory;

        private readonly int maxRequests;

        private readonly string name;

        private readonly TimeSpan period;

        private readonly LinkedList<ThrottleData> throttlesByAge = new LinkedList<ThrottleData>();

        private readonly Dictionary<string, LinkedListNode<ThrottleData>> throttlesByKey = new Dictionary<string, LinkedListNode<ThrottleData>>();

        /// <summary>
        ///     The current count of records being tracked.
        /// </summary>
        public int CurrentRecordCount => this.throttlesByAge.Count;

        /// <summary>
        ///     Constructs an in-memory version of a <see cref="IRequestThrottler" />
        /// </summary>
        /// <param name="name">The name of the throttler, used in performance counters.</param>
        /// <param name="maxRequests">The maximum number of requests to be made in a period.</param>
        /// <param name="period">The period of the throttler.</param>
        /// <param name="counterFactory">A counter factory for fetching and incrementing performance counters.</param>
        public InMemoryRequestThrottler(string name, int maxRequests, TimeSpan period, ICounterFactory counterFactory)
        {
            if (period <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(period));
            this.name = name ?? throw new ArgumentNullException(nameof(name));
            this.counterFactory = counterFactory ?? throw new ArgumentNullException(nameof(counterFactory));
            this.maxRequests = maxRequests;
            this.period = period;
        }

        /// <inheritdoc />
        public bool ShouldThrottle(string key)
        {
            ThrottleData throttleData;
            lock (this.throttlesByKey)
            {
                // Try to find the existing entry again
                if (!this.throttlesByKey.TryGetValue(key, out LinkedListNode<ThrottleData> node))
                {
                    // If we don't find it, create it
                    node = new LinkedListNode<ThrottleData>(new ThrottleData(key, DateTimeOffset.UtcNow + this.period));
                    this.throttlesByAge.AddLast(node);
                    this.throttlesByKey.Add(key, node);

                    this.Trim();
                }

                // Extract the throttle data
                throttleData = node.Value;

                // If it's expired, reset it as if we just added it new
                if (throttleData.IsExpired)
                {
                    throttleData.Reset(DateTimeOffset.UtcNow + this.period);
                    this.throttlesByAge.Remove(node);
                    this.throttlesByAge.AddLast(node);
                }
            }

            // Increment the hit count
            int newCount = throttleData.IncrementHits();
            if (newCount > this.maxRequests)
            {
                // Too many hits, time to throttle.
                this.counterFactory.GetCounter(nameof(InMemoryRequestThrottler), this.name, CounterType.Rate).Increment("Throttled");
                return true;
            }

            // Not throttled
            this.counterFactory.GetCounter(nameof(InMemoryRequestThrottler), this.name, CounterType.Rate).Increment("NotThrottled");
            return false;
        }

        private void Trim()
        {
            // First, remove expired entries
            while (this.throttlesByAge.Count > 0 && this.throttlesByAge.First.Value.IsExpired)
            {
                this.throttlesByKey.Remove(this.throttlesByAge.First.Value.Key);
                this.throttlesByAge.RemoveFirst();
            }

            // Second remove any over the max count
            while (this.throttlesByAge.Count > MaxRecordCount)
            {
                this.throttlesByKey.Remove(this.throttlesByAge.First.Value.Key);
                this.throttlesByAge.RemoveFirst();
            }
        }

        public Task<bool> ShouldThrottleAsync(string key, CancellationToken cToken = default(CancellationToken))
        {
            return Task.FromResult(this.ShouldThrottle(key));
        }

        private class ThrottleData
        {
            private readonly string key;

            private DateTimeOffset expiresAt;

            private int hitCount;

            /// <summary>
            ///     Has this data expired?
            /// </summary>
            public bool IsExpired => this.expiresAt <= DateTimeOffset.UtcNow;

            /// <summary>
            ///     The key for this throttle.
            /// </summary>
            public string Key => this.key;

            /// <summary>
            ///     Construct a new <see cref="ThrottleData" />
            /// </summary>
            public ThrottleData(string key, DateTimeOffset expiresAt)
            {
                this.key = key;
                this.expiresAt = expiresAt;
            }

            /// <summary>
            ///     Increment the number of hits on the data.
            /// </summary>
            /// <returns></returns>
            public int IncrementHits()
            {
                return Interlocked.Increment(ref this.hitCount);
            }

            /// <summary>
            ///     Resets the throttle data.
            /// </summary>
            public void Reset(DateTimeOffset expiresAt)
            {
                this.expiresAt = expiresAt;
                this.hitCount = 0;
            }
        }
    }
}
