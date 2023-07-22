// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.PrivacyServices.KustoHelpers
{
    using System;
    using System.Data;
    using System.Threading.Tasks;

    using global::Kusto.Data.Common;
    using global::Kusto.Data.Results;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Practices.TransientFaultHandling;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     Kusto client wrapper that provides retry on failures
    /// </summary>
    public class KustoClientRetry : ICslQueryProvider
    {
        private readonly RetryManager retryMgr;

        private ICslQueryProvider inner;

        /// <summary>
        ///     Initializes a new instance of the KustoClientRetryWrapper class
        /// </summary>
        /// <param name="retryConfig">retry configuration</param>
        /// <param name="logger">Geneva trace logger</param>
        /// <param name="inner">provider whose actions should be retried</param>
        public KustoClientRetry(
            IRetryStrategyConfiguration retryConfig,
            ICslQueryProvider inner,
            ILogger logger)
        {
            ArgumentCheck.ThrowIfNull(logger, nameof(logger));

            this.inner = inner ?? throw new ArgumentNullException(nameof(inner));

            this.retryMgr = new RetryManager(retryConfig, logger, KustoClientRetry.KustoTransientErrorDetector.Instance);
        }

        /// <summary>
        ///     Gets or sets the name of the default database
        /// </summary>
        /// <remarks>the database operations will act upon if there's no database specified</remarks>
        public string DefaultDatabaseName
        {
            get => this.inner?.DefaultDatabaseName ?? throw new ObjectDisposedException("object is disposed");

            set
            {
                if (this.inner == null)
                {
                    throw new ObjectDisposedException("object is disposed");
                }

                this.inner.DefaultDatabaseName = value;
            }
        }

        /// <summary>
        ///     Executes a CSL query and returns the results
        /// </summary>
        /// <param name="databaseName">database name</param>
        /// <param name="query">query to execute</param>
        /// <param name="properties">query properties</param>
        /// <returns>resulting value</returns>
        public IDataReader ExecuteQuery(
            string databaseName,
            string query,
            ClientRequestProperties properties)
        {
            return this.RunWithRetry(
                "ExecuteQuery(DB,Query,Props)", 
                () => this.inner.ExecuteQuery(databaseName, query, properties));
        }

        /// <summary>
        ///     Execute a CSL query and return the results. Uses the DefaultDatabaseName
        /// </summary>
        /// <param name="query">query to execute</param>
        /// <param name="properties">query properties</param>
        /// <returns>resulting value</returns>
        public IDataReader ExecuteQuery(
            string query,
            ClientRequestProperties properties)
        {
            return this.RunWithRetry(
                "ExecuteQuery(Query,Props)",
                () => this.inner.ExecuteQuery(query, properties)); 
        }

        /// <summary>
        ///     Execute a CSL query and return the results. Uses the DefaultDatabaseName and a default set of default
        ///      ClientRequestProperties.
        /// </summary>
        /// <param name="query">query to execute</param>
        /// <returns>resulting value</returns>
        public IDataReader ExecuteQuery(string query)
        {
            return this.RunWithRetry(
                "ExecuteQuery(Query)",
                () => this.inner.ExecuteQuery(query));
        }

        /// <summary>
        ///     Asynchronously executes a CSL query and returns the results
        /// </summary>
        /// <param name="databaseName">database name</param>
        /// <param name="query">query to execute</param>
        /// <param name="properties">query properties</param>
        /// <returns>resulting value</returns>
        public Task<IDataReader> ExecuteQueryAsync(
            string databaseName,
            string query,
            ClientRequestProperties properties)
        {
            return this.RunWithRetryAsync(
                "ExecuteQueryAsync",
                () => this.inner.ExecuteQueryAsync(databaseName, query, properties));
        }

        /// <summary>
        ///     Execute a CSL query and return an enumerator over ProgressiveDataSetFrame
        /// </summary>
        /// <param name="databaseName">database name</param>
        /// <param name="query">query to execute</param>
        /// <param name="properties">query properties</param>
        /// <returns>resulting value</returns>
        /// <remarks>These frames, when combined, form the resulting DataSet</remarks>
        public Task<ProgressiveDataSet> ExecuteQueryV2Async(
            string databaseName,
            string query,
            ClientRequestProperties properties)
        {
            return this.RunWithRetryAsync(
                "ExecuteQueryV2Async",
                () => this.inner.ExecuteQueryV2Async(databaseName, query, properties));
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources
        /// </summary>
        public void Dispose()
        {
            this.inner?.Dispose();
            this.inner = null;
        }

        /// <summary>
        ///      Runs the function with retry
        /// </summary>
        /// <param name="tag">method tag</param>
        /// <param name="method">method to run</param>
        /// <returns>resulting value</returns>
        private T RunWithRetry<T>(
            string tag,
            Func<T> method)
        {
            if (this.inner == null)
            {
                throw new ObjectDisposedException("object is disposed");
            }

            return this.retryMgr.Execute("KustoClient", tag, method);
        }

        /// <summary>
        ///      Runs the function with retry
        /// </summary>
        /// <typeparam name="T">type of return value</typeparam>
        /// <param name="tag">method tag</param>
        /// <param name="method">method to run</param>
        /// <returns>resulting value</returns>
        private async Task<T> RunWithRetryAsync<T>(
            string tag,
            Func<Task<T>> method)
        {
            if (this.inner == null)
            {
                throw new ObjectDisposedException("object is disposed");
            }

            return await this.retryMgr.ExecuteAsync("KustoClient", tag, method).ConfigureAwait(false);
        }

        /// <summary>
        ///     detects if exceptions thrown by Cosmos methods are transient or not
        /// </summary>
        private class KustoTransientErrorDetector : ITransientErrorDetectionStrategy
        {
            /// <summary>
            ///      Gets a singleton instance of this class
            /// </summary>
            public static KustoTransientErrorDetector Instance { get; } = new KustoTransientErrorDetector();

            /// <summary>
            ///      determines whether the specified exception is transient
            /// </summary>
            /// <param name="e">exception to test</param>
            /// <returns>true if the exception is transient, false otherwise</returns>
            public bool IsTransient(Exception e)
            {
                // it makes me EXTREMELY unhappy to assume all exceptions are transient, but playing whack-a-mole about which 
                //  exceptions can be transient as more pop up makes me even more unhappy, especially with a turnaround time of
                //  at least several hours to fix.
                return true;
            }
        }
    }
}
