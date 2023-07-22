// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.PrivacyServices.KustoHelpers
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading.Tasks;

    public class KustoQueryOptions
    {
        /// <summary>
        ///     Gets or sets properties
        /// </summary>
        public IDictionary<string, string> Parameters { get; set; }

        /// <summary>
        ///     Gets or sets options
        /// </summary>
        public IDictionary<string, object> Options { get; set; }

        /// <summary>
        ///     Gets or sets client request id
        /// </summary>
        public string ClientRequestId { get; set; }

        /// <summary>
        ///     Gets or sets application id
        /// </summary>
        public string ApplicationId { get; set; }

        /// <summary>
        ///     Gets or sets the default database to use with the query
        /// </summary>
        public string DefaultDatabase { get; set; }
    }

    /// <summary>
    ///     contract for a client that can be used to talk to Kusto
    /// </summary>
    public interface IKustoClient : IDisposable
    {
        /// <summary>
        ///     Executes a Kusto query and returns a result
        /// </summary>
        /// <param name="query">Kusto query</param>
        /// <param name="queryOptions">Kusto query options</param>
        /// <returns>data set resulting from executing the query</returns>
        Task<IDataReader> ExecuteQueryAsync(
            string query,
            KustoQueryOptions queryOptions);

        /// <summary>
        ///     Executes a Kusto query and returns a result
        /// </summary>
        /// <param name="query">query</param>
        /// <returns>data set resulting from executing the query</returns>
        Task<IDataReader> ExecuteQueryAsync(string query);

        /// <summary>
        ///     Converts a data reader to a data set
        /// </summary>
        /// <param name="reader">reader</param>
        /// <returns>resulting value</returns>
        DataSet ConvertToDataSet(IDataReader reader);
    }
}
