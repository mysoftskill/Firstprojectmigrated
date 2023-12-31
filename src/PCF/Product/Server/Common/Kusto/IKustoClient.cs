namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading.Tasks;
    using Kusto.Data.Common;

    /// <summary>
    /// An interface that communicates with Kusto.
    /// </summary>
    public interface IKustoClient
    {
        /// <summary>
        /// Gets an IDataReader representing the query on the given Kusto database.
        /// </summary>
        Task<IDataReader> QueryAsync(string query, ClientRequestProperties clientRequestProperties);

        /// <summary>
        /// Sends the given data reader to the designated database and table.
        /// </summary>
        Task IngestAsync(string tableName, IDataReader reader, bool flushImmediately);

        /// <summary>
        /// Create enumerable data set for ingestion.
        /// </summary>
        /// <param name="items">The set of items to serialize to Kusto.</param>
        /// <param name="propertyNames">The set of properties to send to Kusto.</param>
        IDataReader CreateDataReader<T>(IEnumerable<T> items, params string[] propertyNames);

        /// <summary>
        /// Executes given innerQuery and append results into the table.
        /// Create table if it does not exist.
        /// </summary>
        Task SetOrAppendTableFromQueryAsync(
            string tableName, 
            string innerQuery, 
            bool isAsync = false, 
            IEnumerable<string> tags = null, 
            IEnumerable<string> ingestIfNotExistsValues = null, 
            DateTime? creationTime = null,
            ClientRequestProperties clientRequestProperties = null);

        /// <summary>
        /// Create table async.
        /// </summary>
        Task CreateTableAsync(
            string tableName, 
            IEnumerable<Tuple<string, string>> rowFields, 
            bool forceNormalizeColumnName = false);
   }
}