// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    ///     contact for classes implementing a tabular data store
    /// </summary>
    /// <typeparam name="T">type of entity contained by the table</typeparam>
    public interface ITable<T>
        where T : class, ITableEntity, new()
    {
        /// <summary>
        ///     Gets the max count of items that a batch operation supports
        /// </summary>
        int BatchOperationMaxItemCount { get; }

        /// <summary>
        ///     Gets an item from the table
        /// </summary>
        /// <param name="partitionId">partition id</param>
        /// <param name="rowId">row id</param>
        /// <returns>resulting value</returns>
        Task<T> GetItemAsync(
            string partitionId,
            string rowId);

        /// <summary>
        ///     Executes a particular query against the store
        /// </summary>
        /// <param name="query">query filter</param>
        /// <param name="maxItems">maximum items or null to get the provider defined maximum</param>
        /// <param name="columnList">column list or null/empty for all columns</param>
        /// <returns>result set</returns>
        Task<ICollection<T>> QueryAsync(
            string query,
            int? maxItems,
            IEnumerable<string> columnList);

        /// <summary>
        ///     Executes a particular query against the store
        /// </summary>
        /// <param name="query">query</param>
        /// <returns>resulting value</returns>
        Task<ICollection<T>> QueryAsync(string query);

        /// <summary>
        ///     Inserts a new item into the table
        /// </summary>
        /// <param name="item">item to insert</param>
        /// <returns>resulting value</returns>
        Task<bool> InsertAsync(T item);

        /// <summary>
        ///     Inserts a batch of items into the table as an atomic unit
        /// </summary>
        /// <param name="items">items to insert</param>
        /// <returns>resulting value</returns>
        Task<bool> InsertBatchAsync(ICollection<T> items);

        /// <summary>
        ///     Inserts a new item into the table
        /// </summary>
        /// <param name="item">item to update</param>
        /// <returns>resulting value</returns>
        Task<bool> ReplaceAsync(T item);

        /// <summary>
        ///     deletes an item from the table
        /// </summary>
        /// <param name="item">item to delete</param>
        /// <returns>resulting value</returns>
        Task<bool> DeleteItemAsync(T item);

        /// <summary>
        ///     Deletes a batch of items from the table as an atomic unit
        /// </summary>
        /// <param name="items">items to delete</param>
        /// <returns>resulting value</returns>
        Task<bool> DeleteBatchAsync(ICollection<T> items);
    }
}
