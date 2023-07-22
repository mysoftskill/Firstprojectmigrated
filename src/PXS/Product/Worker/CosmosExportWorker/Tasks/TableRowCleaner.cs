// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Locks;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Data;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility.Storage;
    using Microsoft.Azure.Cosmos.Table;

    using Microsoft.PrivacyServices.Common.Azure;
    using OperationContext = Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.OperationContext;

    /// <summary>
    ///     task to clean old items from the table store
    /// </summary>
    public class TableRowCleaner : TrackCountersBaseTask<ICleanerConfig>
    {
        internal const string CleanerLockGroup = "CosmosExportWorkerInfra";

        internal static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(30);

        internal static readonly IList<string> ColumnList = 
            new ReadOnlyCollection<string>(new List<string> { "RowKey", "PartitionKey" });

        private readonly ITableManager tableMgr;
        private readonly ILockManager lockMgr;
        private readonly IRandom rng;
        private readonly IClock clock;

        /// <summary>
        ///     Initializes a new instance of the TrackCountersBaseTask class
        /// </summary>
        /// <param name="config">task config</param>
        /// <param name="tableManager">command set state</param>
        /// <param name="lockManager">lock manager</param>
        /// <param name="counterFactory">performance counter factory</param>
        /// <param name="clock">time of day clock</param>
        /// <param name="random">random number generator</param>
        /// <param name="logger">Geneva trace logger</param>
        public TableRowCleaner(
            ICleanerConfig config,
            ITableManager tableManager,
            ILockManager lockManager,
            ICounterFactory counterFactory,
            IClock clock,
            IRandom random,
            ILogger logger) : 
            base(config, counterFactory, logger)
        {
            this.tableMgr = tableManager ?? throw new ArgumentNullException(nameof(tableManager));
            this.lockMgr = lockManager ?? throw new ArgumentNullException(nameof(lockManager));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.rng = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>
        ///     Performs a single task operation
        /// </summary>
        /// <param name="ctx">operation context</param>
        /// <returns>resulting value</returns>
        /// <remarks>
        ///     'a single operation' is highly task dependent. It could mean processing a single queue item or enumerating and 
        ///      processing a directory of files
        /// </remarks>
        protected override async Task<TimeSpan?> RunOnceAsync(OperationContext ctx)
        {
            ILockLease lease = null;
            int delaySeconds;
            int offsetRange;
            int rowCount = 0;

            try
            {
                ctx.Op = "acquire data manifest lease";

                lease = await this.lockMgr
                    .AttemptAcquireAsync(
                        TableRowCleaner.CleanerLockGroup,
                        this.Config.Table.ToString(),
                        ctx.TaskId,
                        TableRowCleaner.LockDuration,
                        true)
                    .ConfigureAwait(false);

                if (lease != null)
                {
                    ctx.Op = "cleaning table";

                    rowCount = await this
                        .CleanTableAsync(ctx, this.tableMgr.GetTable<BasicTableState>(this.Config.Table.ToStringInvariant()))
                        .ConfigureAwait(false);
                }
            }
            finally
            {
                lease?.ReleaseAsync(false).ConfigureAwait(false);
            }

            delaySeconds = rowCount > 0 || lease == null ? 
                this.Config.NonEmptyBatchDelaySeconds : 
                this.Config.EmptyBatchDelaySeconds;

            offsetRange = this.Config.DelayRandomLimit;

            return TimeSpan.FromSeconds(
                Math.Max(0, delaySeconds + this.rng.Next(-1 * offsetRange, offsetRange)));
        }

        /// <summary>
        ///     Deletes the rows for partition
        /// </summary>
        /// <typeparam name="T">type of t</typeparam>
        /// <param name="table">table to delete from</param>
        /// <param name="itemSet">item set to delete</param>
        /// <returns>resulting value</returns>
        public async Task DeleteRowsForPartition<T>(
            ITable<T> table,
            List<T> itemSet)
            where T : class, ITableEntity, new()
        {
            const int LogBatchSize = 30;

            int batchSize = table.BatchOperationMaxItemCount;

            for (int offset = 0; offset < itemSet.Count; offset += batchSize)
            {
                List<T> batch = itemSet.GetRange(offset, Math.Min(batchSize, itemSet.Count - offset));
                if (batch.Count > 0)
                {
                    string agent = batch[0].PartitionKey;

                    // functionality-wise, we don't care if this succeeds or not as we'll always retry later, but only emit
                    //  the log entry if we did succeed
                    if (await table.DeleteBatchAsync(batch).ConfigureAwait(false))
                    {
                        for (int i = 0; i < batch.Count; i += LogBatchSize)
                        {
                            this.TraceInfo(
                                "Removed commands for agent {0}: [{1}]",
                                agent,
                                string.Join(",", batch.Skip(offset).Take(LogBatchSize).Select(o => o.RowKey)));
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Cleans a table
        /// </summary>
        /// <typeparam name="T">type of t</typeparam>
        /// <param name="ctx">Operation context</param>
        /// <param name="table">table to delete from</param>
        /// <returns>number of items found to delete</returns>
        private async Task<int> CleanTableAsync<T>(
            OperationContext ctx,
            ITable<T> table)
            where T : class, ITableEntity, new()
        {
            DateTimeOffset threshold = this.clock.UtcNow.AddDays(-1 * this.Config.LastModifiedThresholdDays);
            IDictionary<string, List<T>> itemSetMap = new Dictionary<string, List<T>>();
            ICollection<T> result;
            string query;

            // using an explicit timestamp format because the "O" format wants to put an explicit "+00:00" at the end instead of 
            //  the "Z" shorthand for this and Azure (at least the storage emulator) doesn't like the full timezone indicator.
            //  Yay.
            query = "Timestamp lt datetime'" + threshold.ToString("yyyy-MM-ddTHH:mm:ssZ") + "'";

            ctx.Op = "Querying for rows to delete";

            result = await table.QueryAsync(query, this.Config.MaxBatchSize, TableRowCleaner.ColumnList).ConfigureAwait(false);

            ctx.Op = "Building partition delete batch";

            // we can only issue batch requests to table store for items in the same partition, so sort items into
            //  groups by partition and then delete them in batches by partition
            foreach (T item in result)
            {
                List<T> itemSet;
                if (itemSetMap.TryGetValue(item.PartitionKey, out itemSet) == false)
                {
                    itemSet = new List<T>();
                    itemSetMap.Add(item.PartitionKey, itemSet);
                }

                itemSet.Add(item);
            }

            ctx.Op = "Deleting batches";

            await Task.WhenAll(itemSetMap.Values.Select(o => this.DeleteRowsForPartition(table, o))).ConfigureAwait(false);

            return result.Count;
        }
    }
}
