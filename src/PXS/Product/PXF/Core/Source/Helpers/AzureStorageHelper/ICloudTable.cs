// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    ///     Interface of cloud table primitives. This is used so that we can mock the storage operations for unit tests.
    /// </summary>
    public interface ICloudTable
    {
        /// <summary>
        ///     Inserts a new row
        /// </summary>
        /// <param name="entity">Entity to insert</param>
        /// <param name="ignoreConflict">true to ignore 409 errors; false otherwise</param>
        /// <returns>TableResult</returns>
        Task<TableResult> InsertAsync(
            ITableEntity entity, 
            bool ignoreConflict = false);

        /// <summary>
        ///     Inserts a set of new rows atomically
        /// </summary>
        /// <param name="entities">Entities to insert</param>
        /// <param name="ignoreConflict">true to ignore 409 errors; false otherwise</param>
        /// <returns>TableResult</returns>
        Task<TableResult> InsertBatchAsync(
            ICollection<ITableEntity> entities,
            bool ignoreConflict = false);

        /// <summary>
        ///     Queries for a single row
        /// </summary>
        /// <param name="partitionKey">Partition key</param>
        /// <param name="rowKey">Row key</param>
        /// <param name="ignoreNotFound">true to ignore 404 errors; false otherwise</param>
        /// <returns>TableResult</returns>
        Task<TableResult> QuerySingleRowAsync(
            string partitionKey, 
            string rowKey,
            bool ignoreNotFound = false);

        /// <summary>
        ///     Queries for multiple rows
        /// </summary>
        /// <typeparam name="T">type of entity to fetch</typeparam>
        /// <param name="query">query filter</param>
        /// <param name="maxItems">maximum items or null to get the provider defined maximum</param>
        /// <param name="columnList">column list or null/empty for all columns</param>
        /// <returns>result set</returns>
        Task<ICollection<T>> QueryAsync<T>(
            string query,
            int? maxItems,
            IEnumerable<string> columnList)
            where T : class, ITableEntity, new();

        /// <summary>
        ///     Replaces a row
        /// </summary>
        /// <param name="entity">Entity to replace</param>
        /// <param name="ignoreConflict">true to ignore 412 errors; false otherwise</param>
        /// <returns>TableResult</returns>
        Task<TableResult> ReplaceAsync(
            ITableEntity entity, 
            bool ignoreConflict = false);

        /// <summary>
        ///     Deletes a row
        /// </summary>
        /// <param name="entity">Entity to delete</param>
        /// <param name="ignoreConflict">true to ignore 412 errors; false otherwise</param>
        /// <returns>TableResult</returns>
        Task<TableResult> DeleteAsync(
            ITableEntity entity, 
            bool ignoreConflict);

        /// <summary>
        ///     Inserts a set of new rows atomically
        /// </summary>
        /// <param name="entities">Entities to insert</param>
        /// <param name="ignoreConflict">true to ignore 412 errors; false otherwise</param>
        /// <returns>TableResult</returns>
        Task<TableResult> DeleteBatchAsync(
            ICollection<ITableEntity> entities,
            bool ignoreConflict = false);
    }
}
