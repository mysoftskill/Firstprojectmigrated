// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Tables;
    using Microsoft.Azure.Cosmos.Table;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     AzureTable class
    /// </summary>
    public class AzureTable<T> : 
        ITable<T>
        where T : class, ITableEntity, ITableEntityInitializer, new()
    {
        private const int MaxBatchSize = 90;

        private readonly IAzureStorageProvider storage;

        private readonly ILogger logger;

        private readonly string name;

        private ICloudTable table;

        /// <summary>
        ///     Initializes a new instance of the AzureTable class
        /// </summary>
        /// <param name="storage">azure storage accessor</param>
        /// <param name="logger">Geneva trace logger</param>
        /// <param name="name">table name</param>
        public AzureTable(
            IAzureStorageProvider storage,
            ILogger logger,
            string name)
        {
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this.name = (string.IsNullOrWhiteSpace(name) ? typeof(T).Name : name).ToLowerInvariant();
        }

        /// <summary>
        ///     Gets the max count of items that a batch operation supports
        /// </summary>
        public int BatchOperationMaxItemCount => AzureTable<T>.MaxBatchSize;

        /// <summary>
        ///     Gets an item from the table
        /// </summary>
        /// <param name="partitionId">partition id</param>
        /// <param name="rowId">row id</param>
        /// <returns>resulting value</returns>
        public async Task<T> GetItemAsync(
            string partitionId,
            string rowId)
        {
            TableResult rawResult;

            await this.EnsureInitializedAsync().ConfigureAwait(false);

            rawResult = await this.table.QuerySingleRowAsync(partitionId, rowId, true).ConfigureAwait(false);
            if (rawResult.HttpStatusCode == (int)HttpStatusCode.OK)
            {
                T result = rawResult.Result as T;
                if (result == null)
                {
                    result = new T();
                    result.Initialize(rawResult.Result);
                }

                return result;
            }
           
            this.logger.Error($"ComponentName: [AzureTable-{this.name}]", $"GetItemAsync operation failed with status={rawResult.HttpStatusCode}");
            
            return null;
        }

        /// <summary>
        ///     Executes a particular query against the store
        /// </summary>
        /// <param name="query">query filter</param>
        /// <param name="maxItems">maximum items or null to get the provider defined maximum</param>
        /// <param name="columnList">column list or null/empty for all columns</param>
        /// <returns>result set</returns>
        public async Task<ICollection<T>> QueryAsync(
            string query,
            int? maxItems,
            IEnumerable<string> columnList)
        {
            await this.EnsureInitializedAsync().ConfigureAwait(false);
            return await this.table.QueryAsync<T>(query, maxItems, columnList).ConfigureAwait(false);
        }

        /// <summary>
        ///     Executes a particular query against the store
        /// </summary>
        /// <param name="query">query</param>
        /// <returns>resulting value</returns>
        public async Task<ICollection<T>> QueryAsync(string query)
        {
            await this.EnsureInitializedAsync().ConfigureAwait(false);
            return await this.table.QueryAsync<T>(query, null, null).ConfigureAwait(false);
        }

        /// <summary>
        ///     Inserts a new item into the table
        /// </summary>
        /// <param name="item">item to insert</param>
        /// <returns>resulting value</returns>
        public async Task<bool> InsertAsync(T item)
        {
            // ReSharper disable once SuspiciousTypeConversion.Global : the interface is optional
            ITableEntity toStore = (item as ITableEntityStorageExtractor)?.ExtractStorageRepresentation() ?? item;

            await this.EnsureInitializedAsync().ConfigureAwait(false);

            return await this.RunTaskAndLogIfError(this.table.InsertAsync(toStore, true), (x) => x.HttpStatusCode == (int)HttpStatusCode.OK, "InsertAsync").ConfigureAwait(false);
        }

        /// <summary>
        ///     Inserts a batch of items into the table as an atomic unit
        /// </summary>
        /// <param name="items">items to insert</param>
        /// <returns>resulting value</returns>
        public async Task<bool> InsertBatchAsync(ICollection<T> items)
        {
            ICollection<ITableEntity> toStore;

            // ReSharper disable once SuspiciousTypeConversion.Global : the interface is optional
            toStore = items?.Select(o => (o as ITableEntityStorageExtractor)?.ExtractStorageRepresentation() ?? o).ToList();
                
            await this.EnsureInitializedAsync().ConfigureAwait(false);

            return await this.RunTaskAndLogIfError(this.table.InsertBatchAsync(toStore, true), (x) => x.HttpStatusCode == (int)HttpStatusCode.OK, "InsertBatchAsync").ConfigureAwait(false);
        }

        /// <summary>
        ///     Inserts a new item into the table
        /// </summary>
        /// <param name="item">item to update</param>
        public async Task<bool> ReplaceAsync(T item)
        {
            // ReSharper disable once SuspiciousTypeConversion.Global : the interface is optional
            ITableEntity toStore = (item as ITableEntityStorageExtractor)?.ExtractStorageRepresentation() ?? item;

            await this.EnsureInitializedAsync().ConfigureAwait(false);

            return await this.RunTaskAndLogIfError(this.table.ReplaceAsync(toStore, true), (x) => x.HttpStatusCode == (int)HttpStatusCode.OK, "ReplaceAsync").ConfigureAwait(false);
        }

        /// <summary>
        ///     Deletes an item from the table
        /// </summary>
        /// <param name="item">item to delete</param>
        /// <returns>resulting value</returns>
        public async Task<bool> DeleteItemAsync(T item)
        {
            await this.EnsureInitializedAsync().ConfigureAwait(false);

            return await this.RunTaskAndLogIfError(this.table.DeleteAsync(item, true), (x) => x.HttpStatusCode == (int)HttpStatusCode.OK, "DeleteItemAsync").ConfigureAwait(false);
        }
        
        /// <summary>
        ///     Deletes a batch of items from the table as an atomic unit
        /// </summary>
        /// <param name="items">items to delete</param>
        /// <returns>resulting value</returns>
        public async Task<bool> DeleteBatchAsync(ICollection<T> items)
        {
            await this.EnsureInitializedAsync().ConfigureAwait(false);

            return await this.RunTaskAndLogIfError(this.table.DeleteBatchAsync(items.Cast<ITableEntity>().ToList(), true), (x) => x.HttpStatusCode == (int)HttpStatusCode.OK || x.HttpStatusCode == (int)HttpStatusCode.NotFound, "DeleteBatchAsync").ConfigureAwait(false);
        }

        /// <summary>
        ///     Ensures that this class instance is initialized
        /// </summary>
        /// <returns>resulting value</returns>
        private async Task EnsureInitializedAsync()
        {
            if (this.table == null)
            {
                ICloudTable tableLocal;

                try
                {
                    tableLocal = await this.storage.GetCloudTableAsync(this.name).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    this.logger.Error(nameof(AzureTable<T>), $"Failed to open table {this.name}: {e}");
                    throw;
                }

                Interlocked.CompareExchange(ref this.table, tableLocal, null);
            }
        }

       /// <summary>
       /// Await on task and log error if task failed.
       /// </summary>
       /// <param name="task"> Task to run</param>
       /// <param name="evaluateResult">Function to evaluate TableResult returned from task</param>
       /// <param name="operationName">Operation Name to log in error logs.</param>
        private async Task<bool> RunTaskAndLogIfError( Task<TableResult> task, Func<TableResult, bool> evaluateResult, string operationName)
        {
            TableResult result = await task.ConfigureAwait(false);
            bool operationSucceded = evaluateResult(result);

            if (!operationSucceded)
            {
                this.logger.Error($"ComponentName: [AzureTable-{this.name}]", $"{operationName} operation failed with status={result.HttpStatusCode}");
            }
            return operationSucceded;
        }
    }
}
