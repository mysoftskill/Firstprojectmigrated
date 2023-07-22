// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.PrivacyServices.KustoHelpers
{
    using System;
    using System.Data;
    using System.Threading.Tasks;

    using Kusto.Cloud.Platform.Data;
    using Kusto.Data.Common;

    using Microsoft.Membership.MemberServices.Common.Utilities;

    /// <summary>
    ///     implements a Kusto client
    /// </summary>
    public sealed class KustoClient : IKustoClient
    {
        private ICslQueryProvider client;

        /// <summary>
        ///     Initializes a new instance of the KustoClient class
        /// </summary>
        /// <param name="client">client</param>
        public KustoClient(ICslQueryProvider client)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <summary>
        ///     Executes a Kusto query and returns a result
        /// </summary>
        /// <param name="query">query text</param>
        /// <param name="queryOptions">query options</param>
        /// <returns>data set resulting from executing the query</returns>
        public async Task<IDataReader> ExecuteQueryAsync(
            string query,
            KustoQueryOptions queryOptions)
        {
            ClientRequestProperties props = null;
            string defaultDb = null;

            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(query, nameof(query));

            if (this.client == null)
            {
                throw new ObjectDisposedException("Object has been disposed");
            }

            if (queryOptions != null)
            {
                props = new ClientRequestProperties(queryOptions.Options, queryOptions.Parameters)
                { 
                    Application = queryOptions.ApplicationId,
                    ClientRequestId = queryOptions.ClientRequestId,
                };

                defaultDb = queryOptions.DefaultDatabase;
            }

            return await this.client.ExecuteQueryAsync(defaultDb, query, props).ConfigureAwait(false);
        }

        /// <summary>
        ///     Executes a Kusto query and returns a result
        /// </summary>
        /// <param name="query">query text</param>
        /// <returns>data set resulting from executing the query</returns>
        public async Task<IDataReader> ExecuteQueryAsync(string query)
        {
            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(query, nameof(query));

            if (this.client == null)
            {
                throw new ObjectDisposedException("Object has been disposed");
            }

            return await this.client.ExecuteQueryAsync(null, query, null).ConfigureAwait(false);
        }
        
        /// <summary>
        ///     Converts a data reader to a data set
        /// </summary>
        /// <param name="reader">reader</param>
        /// <returns>resulting value</returns>
        public DataSet ConvertToDataSet(IDataReader reader)
        {
            ArgumentCheck.ThrowIfNull(reader, nameof(reader));
            return reader.ToDataSet();
        }

        /// <summary>
        ///     frees, releases, or resets unmanaged resources
        /// </summary>
        public void Dispose()
        {
            this.client?.Dispose();
            this.client = null;
        }
    }
}
