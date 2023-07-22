// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.PrivacyServices.KustoHelpers
{
    using System;
    using System.Data;
    using System.Net.Http;
    using System.Threading.Tasks;

    using global::Kusto.Data.Common;
    using global::Kusto.Data.Results;

    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Membership.MemberServices.Common.Utilities;

    /// <summary>
    ///     Kusto client wrapper that provides outgoing method tracing
    /// </summary>
    public sealed class KustoClientInstrumented : ICslQueryProvider
    {
        private const string DependencyType = "Kusto";
        private const string OperationVersionV1 = "1";
        private const string PartnerId = "Kusto";
        private const string Target = "ExecuteQuery";

        private readonly string tag;

        private ICslQueryProvider inner;

        /// <summary>
        ///     Initializes a new instance of the KustoClientRetryWrapper class
        /// </summary>
        /// <param name="inner">provider whose actions should be retried</param>
        /// <param name="queryTag">query tag</param>
        public KustoClientInstrumented(
            ICslQueryProvider inner,
            string queryTag)
        {
            this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
            this.tag = ArgumentCheck.ReturnIfNotNullEmptyOrWhiteSpace(queryTag, nameof(queryTag)) + KustoClientInstrumented.Target;
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
            return this.RunWithInstrumentation(
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
            return this.RunWithInstrumentation(
                "ExecuteQuery(Query,Props)",
                () => this.inner.ExecuteQuery(query, properties));
        }

        /// <summary>
        ///     Execute a CSL query and return the results. Uses the DefaultDatabaseName and a default set of default
        ///     ClientRequestProperties.
        /// </summary>
        /// <param name="query">query to execute</param>
        /// <returns>resulting value</returns>
        public IDataReader ExecuteQuery(string query)
        {
            return this.RunWithInstrumentation(
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
            return this.RunWithInstrumentationAsync(
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
            return this.RunWithInstrumentationAsync(
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
        ///     performs the method call with telemetry tracking 
        /// </summary>
        /// <typeparam name="TResult">type of the result</typeparam>
        /// <param name="operationName">operation name</param>
        /// <param name="method">method</param>
        /// <returns>resulting value</returns>
        private async Task<TResult> RunWithInstrumentationAsync<TResult>(
            string operationName,
            Func<Task<TResult>> method)
        {
            OutgoingApiEventWrapper eventWrapper;

            if (this.inner == null)
            {
                throw new ObjectDisposedException("object is disposed");
            }

            eventWrapper = OutgoingApiEventWrapper.CreateBasicOutgoingEvent(
                KustoClientInstrumented.PartnerId,
                operationName,
                KustoClientInstrumented.OperationVersionV1,
                this.tag,
                HttpMethod.Get,
                KustoClientInstrumented.DependencyType);

            eventWrapper.Start();

            try
            {
                TResult result = await method().ConfigureAwait(false);
                eventWrapper.Success = true;
                return result;
            }
            catch (Exception e)
            {
                eventWrapper.Success = false;
                eventWrapper.ErrorMessage = e.ToString();
                throw;
            }
            finally
            {
                eventWrapper.Finish();
            }
        }

        /// <summary>
        ///     performs the method call with telemetry tracking 
        /// </summary>
        /// <typeparam name="TResult">type of the result</typeparam>
        /// <param name="operationName">operation name</param>
        /// <param name="method">method</param>
        /// <returns>resulting value</returns>
        private TResult RunWithInstrumentation<TResult>(
            string operationName,
            Func<TResult> method)
        {
            OutgoingApiEventWrapper eventWrapper;

            if (this.inner == null)
            {
                throw new ObjectDisposedException("object is disposed");
            }

            eventWrapper = OutgoingApiEventWrapper.CreateBasicOutgoingEvent(
                KustoClientInstrumented.PartnerId,
                operationName,
                KustoClientInstrumented.OperationVersionV1,
                this.tag,
                HttpMethod.Get,
                KustoClientInstrumented.DependencyType);

            eventWrapper.Start();

            try
            {
                TResult result = method();
                eventWrapper.Success = true;
                return result;
            }
            catch (Exception e)
            {
                eventWrapper.Success = false;
                eventWrapper.ErrorMessage = e.ToString();
                throw;
            }
            finally
            {
                eventWrapper.Finish();
            }
        }
    }
}
