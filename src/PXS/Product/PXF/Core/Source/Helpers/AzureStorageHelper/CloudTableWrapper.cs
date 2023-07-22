// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    ///     Wraps CloudTable class and exposes interface from ICloudTable to allow mocking storage calls
    /// </summary>
    /// <remarks>These are intended to be very thin wrappers around CloudTable methods</remarks>
    public class CloudTableWrapper : ICloudTable
    {
        private const string AzureStorageTable = "AzureStorageTable";

        private const string WriteBatchToTable = "WriteBatchToTable";

        private const string DeleteFromTable = "DeleteFromTable";

        private const string ReadFromTable = "ReadFromTable";

        private const string WriteToTable = "WriteToTable";

        private const string UpdateTable = "UpdateTable";

        private readonly CloudTable table;

        /// <summary>
        ///     Initializes a new instance of the CloudTableWrapper class
        /// </summary>
        /// <param name="table">Cloud table to wrap</param>
        public CloudTableWrapper(CloudTable table)
        {
            this.table = table;
        }

        /// <summary>
        ///     Queries for a single row
        /// </summary>
        /// <param name="partitionKey">Partition key</param>
        /// <param name="rowKey">Row key</param>
        /// <param name="ignoreNotFound">true to ignore 404 errors; false otherwise</param>
        /// <returns>TableResult</returns>
        public Task<TableResult> QuerySingleRowAsync(
            string partitionKey, 
            string rowKey,
            bool ignoreNotFound = false)
        {
            return this.ExecuteConflictAllowedOperationAsync(
                CloudTableWrapper.ReadFromTable,
                ignoreNotFound,
                TableOperation.Retrieve(partitionKey, rowKey),
                true,
                (int)HttpStatusCode.NotFound);
        }

        /// <summary>
        ///     Queries for a collection of rows
        /// </summary>
        /// <typeparam name="T">type of entity to fetch</typeparam>
        /// <param name="query">query filter</param>
        /// <param name="maxItems">maximum items or null to get the provider defined maximum</param>
        /// <param name="columnList">column list or null/empty for all columns</param>
        /// <returns>resulting value</returns>
        public async Task<ICollection<T>> QueryAsync<T>(
            string query,
            int? maxItems,
            IEnumerable<string> columnList)
            where T : class, ITableEntity, new()
        {
            OutgoingApiEventWrapper apiEvent = this.GetApiEvent(CloudTableWrapper.ReadFromTable);
            List<T> result = new List<T>();

            try
            {
                TableQuerySegment<T> querySegment = null;
                TableQuery<T> queryObj = new TableQuery<T>().Where(query);

                if (maxItems > 0)
                {
                    queryObj.TakeCount = maxItems;
                }

                if (columnList != null)
                {
                    IList<string> list = columnList.ToList();
                    queryObj.SelectColumns = list.Count > 0 ? list : null;
                }

                apiEvent.Start();

                while (querySegment == null || querySegment.ContinuationToken != null)
                {
                    querySegment = await this.table.ExecuteQuerySegmentedAsync(queryObj, querySegment?.ContinuationToken)
                        .ConfigureAwait(false);

                    result.AddRange(querySegment);
                }

                apiEvent.Success = true;
            }
            catch (StorageException e)
            {
                apiEvent.ErrorMessage = e.ToString();
                throw;
            }
            catch (Exception e)
            {
                apiEvent.ErrorMessage = e.ToString();
                throw;
            }
            finally
            {
                apiEvent.Finish();
            }

            return result;
        }

        /// <summary>
        ///     Inserts a new row
        /// </summary>
        /// <param name="entity">Entity to insert</param>
        /// <param name="ignoreConflict">true to ignore 409 errors; false otherwise</param>
        /// <returns>TableResult</returns>
        public Task<TableResult> InsertAsync(
        ITableEntity entity, 
        bool ignoreConflict = false)
        {
            return this.ExecuteConflictAllowedOperationAsync(
                CloudTableWrapper.WriteToTable,
                ignoreConflict,
                TableOperation.Insert(entity),
                true,
                (int)HttpStatusCode.Conflict);
        }

        /// <summary>
        ///     Inserts a set of new rows atomically
        /// </summary>
        /// <param name="entities">Entities to insert</param>
        /// <param name="ignoreConflict">true to ignore 409 errors; false otherwise</param>
        /// <returns>TableResult</returns>
        public async Task<TableResult> InsertBatchAsync(
            ICollection<ITableEntity> entities,
            bool ignoreConflict = false)
        {
            TableBatchOperation tableOp = new TableBatchOperation();

            foreach (ITableEntity e in entities)
            {
                tableOp.Insert(e);
            }

            return await this.ExecuteConflictAllowedOperationAsync(
                CloudTableWrapper.WriteBatchToTable,
                ignoreConflict,
                tableOp,
                (int)HttpStatusCode.Conflict).ConfigureAwait(false);
        }

        /// <summary>
        ///     Replaces a row
        /// </summary>
        /// <param name="entity">Entity to replace</param>
        /// <param name="ignoreConflict">true to ignore 412 errors; false otherwise</param>
        /// <returns>TableResult</returns>
        public Task<TableResult> ReplaceAsync(
            ITableEntity entity, 
            bool ignoreConflict = false)
        {
            return this.ExecuteConflictAllowedOperationAsync(
                CloudTableWrapper.UpdateTable,
                ignoreConflict,
                TableOperation.Replace(entity),
                true,
                (int)HttpStatusCode.PreconditionFailed);
        }

        /// <summary>
        ///     Deletes a row
        /// </summary>
        /// <param name="entity">Entity to delete</param>
        /// <param name="ignoreConflict">true to ignore 412 errors; false otherwise</param>
        /// <returns>TableResult</returns>
        public Task<TableResult> DeleteAsync(
            ITableEntity entity, 
            bool ignoreConflict)
        {
            return this.ExecuteConflictAllowedOperationAsync(
                CloudTableWrapper.DeleteFromTable,
                ignoreConflict,
                TableOperation.Delete(entity),
                false,
                (int)HttpStatusCode.PreconditionFailed,
                (int)HttpStatusCode.NotFound);
        }

        /// <summary>
        ///     Inserts a set of new rows atomically
        /// </summary>
        /// <param name="entities">Entities to insert</param>
        /// <param name="ignoreConflict">true to ignore 412 errors; false otherwise</param>
        /// <returns>TableResult</returns>
        public async Task<TableResult> DeleteBatchAsync(
            ICollection<ITableEntity> entities,
            bool ignoreConflict = false)
        {
            TableBatchOperation tableOp = new TableBatchOperation();

            foreach (ITableEntity e in entities)
            {
                tableOp.Delete(e);
            }

            return await this.ExecuteConflictAllowedOperationAsync(
                CloudTableWrapper.WriteBatchToTable,
                ignoreConflict,
                tableOp,
                (int)HttpStatusCode.PreconditionFailed,
                (int)HttpStatusCode.NotFound).ConfigureAwait(false);
        }

        /// <summary>
        ///     Executes an operation that supports a single non-fatal conflict error response
        /// </summary>
        /// <param name="opName">operation name</param>
        /// <param name="ignoreConflict">true to ignore conflict errors; false otherwise</param>
        /// <param name="tableOp">table operation to issue</param>
        /// <param name="setEtag">true to set etag; false otherwise</param>
        /// <param name="conflictStatusCodes">status code that should be considered an 'ignorable conflict error'</param>
        /// <returns>resulting value</returns>
        private async Task<TableResult> ExecuteConflictAllowedOperationAsync(
            string opName,
            bool ignoreConflict,
            TableOperation tableOp,
            bool setEtag,
            params int[] conflictStatusCodes)
        {
            OutgoingApiEventWrapper apiEvent = this.GetApiEvent(opName);

            apiEvent.Start();

            try
            {
                TableResult tableResult;

                tableResult = await this.table.ExecuteAsync(tableOp).ConfigureAwait(false);
                apiEvent.Success = true;

                // translate all 2xx success codes to a 200 OK response to make error handling from this method simpler
                if (tableResult.HttpStatusCode >= 200 && tableResult.HttpStatusCode < 300)
                {
                    if (setEtag && tableResult.HttpStatusCode == (int)HttpStatusCode.OK && tableOp.Entity != null)
                    {
                        tableOp.Entity.ETag = tableResult.Etag;
                    }

                    tableResult.HttpStatusCode = (int)HttpStatusCode.OK;
                }

                return tableResult;
            }
            catch (StorageException e)
            {
                if (ignoreConflict && conflictStatusCodes.Contains(e.RequestInformation.HttpStatusCode))
                {
                    apiEvent.Success = true;
                    return new TableResult { HttpStatusCode = e.RequestInformation.HttpStatusCode };
                }
                else
                {
                    apiEvent.ErrorMessage = e.ToString();
                    throw;
                }
            }
            catch (Exception e)
            {
                apiEvent.ErrorMessage = e.ToString();
                throw;
            }
            finally
            {
                apiEvent.Finish();
            }
        }

        /// <summary>
        ///     Executes an operation that supports a single non-fatal conflict error response
        /// </summary>
        /// <param name="opName">operation name</param>
        /// <param name="ignoreConflict">true to ignore conflict errors; false otherwise</param>
        /// <param name="tableOp">table operation to issue</param>
        /// <param name="conflictStatusCodes">status code that should be considered an 'ignorable conflict error'</param>
        /// <returns>resulting value</returns>
        private async Task<TableResult> ExecuteConflictAllowedOperationAsync(
            string opName,
            bool ignoreConflict,
            TableBatchOperation tableOp,
            params int[] conflictStatusCodes)
        {
            OutgoingApiEventWrapper apiEvent = this.GetApiEvent(opName);

            apiEvent.Start();

            try
            {
                IList<TableResult> result = await this.table.ExecuteBatchAsync(tableOp).ConfigureAwait(false);
                apiEvent.Success = true;

                for (int i = 0; i < result.Count && i < tableOp.Count; ++i)
                {
                    tableOp[i].Entity.ETag = result[i].Etag;
                }

                return new TableResult { HttpStatusCode = (int)HttpStatusCode.OK };
            }
            catch (StorageException e)
            {
                if (ignoreConflict && conflictStatusCodes.Contains(e.RequestInformation.HttpStatusCode))
                {
                    apiEvent.Success = true;
                    return new TableResult { HttpStatusCode = e.RequestInformation.HttpStatusCode };
                }
                else
                {
                    apiEvent.ErrorMessage = e.ToString();
                    throw;
                }
            }
            catch (Exception e)
            {
                apiEvent.ErrorMessage = e.ToString();
                throw;
            }
            finally
            {
                apiEvent.Finish();
            }
        }

        /// <summary>
        ///     Gets the API event wrapper for the operation
        /// </summary>
        /// <param name="opName">operation name</param>
        /// <returns>resulting value</returns>
        private OutgoingApiEventWrapper GetApiEvent(string opName)
        {
            return new OutgoingApiEventWrapper
            {
                DependencyOperationName = opName,
                DependencyOperationVersion = string.Empty,
                DependencyName = CloudTableWrapper.AzureStorageTable,
                DependencyType = "WebService",
                PartnerId = CloudTableWrapper.AzureStorageTable,
                Success = false,
            };
        }
    }
}
