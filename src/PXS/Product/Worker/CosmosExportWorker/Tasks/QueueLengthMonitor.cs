// <copyright company"Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Data;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     QueueLengthMonitor class
    /// </summary>
    public class QueueLengthMonitor : MultiInstanceTask<IMonitorTaskConfig>
    {
        private readonly IPartitionedQueue<PendingDataFile, FileSizePartition> pendingQueue;
        private readonly List<IQueue> queues = new List<IQueue>();
        private readonly TimeSpan runFrequency;

        /// <summary>
        ///     Initializes a new instance of the QueueLengthMonitor class
        /// </summary>
        /// <param name="config">task config</param>
        /// <param name="doneQueue">done queue</param>
        /// <param name="pendingQueue">pending queue</param>
        /// <param name="fileSetQueue">file set queue</param>
        /// <param name="counterFactory">counter factory</param>
        /// <param name="logger">Geneva trace logger</param>
        public QueueLengthMonitor(
            IMonitorTaskConfig config,
            IQueue<CompleteDataFile> doneQueue,
            IPartitionedQueue<PendingDataFile, FileSizePartition> pendingQueue,
            IQueue<ManifestFileSet> fileSetQueue,
            ICounterFactory counterFactory,
            ILogger logger) :
            base(config, counterFactory, logger)
        {
            ArgumentCheck.ThrowIfNull(fileSetQueue, nameof(fileSetQueue));
            ArgumentCheck.ThrowIfNull(pendingQueue, nameof(pendingQueue));
            ArgumentCheck.ThrowIfNull(doneQueue, nameof(doneQueue));

            this.queues.Add(fileSetQueue);
            this.queues.Add(fileSetQueue);

            this.pendingQueue = pendingQueue;

            this.runFrequency = TimeSpan.FromSeconds(config.UpdateFrequencySeconds);
        }

        /// <summary>
        ///     Performs a single task operation
        /// </summary>
        /// <param name="ctx">operation context</param>
        /// <returns>resulting value</returns>
        protected override async Task<TimeSpan?> RunOnceAsync(OperationContext ctx)
        {
            const string CounterName = "Work Queue Lengths";

            IReadOnlyList<QueuePartitionSize<FileSizePartition>> pendingSet;
            ICounter counter = this.CounterFactory.GetCounter(Constants.CounterCategory, CounterName, CounterType.Number);

            await Task
                .WhenAll(
                    this.queues
                        .Select(
                            async q => counter.SetValue(await q.GetQueueSizeAsync(this.CancelToken).ConfigureAwait(false), q.Name)))
                .ConfigureAwait(false);

            pendingSet = await this.pendingQueue.GetPartitionSizesAsync(this.CancelToken).ConfigureAwait(false);
            foreach (QueuePartitionSize<FileSizePartition> p in pendingSet)
            {
                counter.SetValue(p.Count, this.pendingQueue.Name + "-" + p.Id.ToString());
            }

            return this.runFrequency;
        }
    }
}
