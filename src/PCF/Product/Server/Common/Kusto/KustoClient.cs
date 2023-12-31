namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading.Tasks;
    using Kusto.Cloud.Platform.Data;
    using Kusto.Data;
    using Kusto.Data.Common;
    using Kusto.Data.Exceptions;
    using Kusto.Data.Net.Client;
    using Kusto.Ingest;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// An interface that communicates with Kusto.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    public class KustoClient : IKustoClient
    {
        private readonly IKustoIngestClient kustoIngestClient;
        private readonly ICslQueryProvider queryClient;
        private readonly string hostAddress;

        /// <summary>
        /// Uses the specified Kusto Cluster and Database
        /// </summary>
        /// <param name="cluster">Kusto Cluster Name</param>
        /// <param name="database">Kusto Database Name</param>
        public KustoClient(string cluster, string database)
        {
            DualLogger.Instance.Information(nameof(KustoClient), $"Cluster={cluster}, Database={database}");

            this.DatabaseName = database;
            this.hostAddress = $@"https://{cluster}.kusto.windows.net";
            var ingestHostAddress = $@"https://ingest-{cluster}.kusto.windows.net";

            var ingestKustoConnectionStringBuilder = this.CreateKustoConnectionString(ingestHostAddress);
            var queryKustoConnectionStringBuilder = this.CreateKustoConnectionString(this.hostAddress);

            ingestKustoConnectionStringBuilder.InitialCatalog = this.DatabaseName;
            queryKustoConnectionStringBuilder.InitialCatalog = this.DatabaseName;

            this.kustoIngestClient = KustoIngestFactory.CreateQueuedIngestClient(ingestKustoConnectionStringBuilder);
            this.queryClient = KustoClientFactory.CreateCslQueryProvider(queryKustoConnectionStringBuilder);
        }

        /// <summary>
        /// Kusto database name.
        /// </summary>
        public string DatabaseName { get; }

        /// <inheritdoc />
        public async Task<IDataReader> QueryAsync(string query, ClientRequestProperties clientRequestProperties)
        {
            return await Logger.InstrumentAsync(
                new OutgoingEvent(SourceLocation.Here()),
                async ev =>
                {
                    ev["DatabaseName"] = this.DatabaseName;
                    ev["Query"] = query;
                    clientRequestProperties.ClientRequestId = Logger.Instance?.CorrelationVector;
                    return await this.queryClient.ExecuteQueryAsync(this.DatabaseName, query, clientRequestProperties);
                });
        }

        /// <inheritdoc />
        public Task IngestAsync(string tableName, IDataReader reader, bool flushImmediately)
        {
            return Logger.InstrumentAsync(
                new OutgoingEvent(SourceLocation.Here()),
                async ev =>
                {
                    ev["DatabaseName"] = this.DatabaseName;
                    ev["TableName"] = tableName;
                    ev["DataReaderType"] = reader.GetType().Name;
                    ev["FlushImmediately"] = flushImmediately.ToString();

                    var ingestProps = new KustoQueuedIngestionProperties(this.DatabaseName, tableName)
                    {
                        FlushImmediately = flushImmediately,
                    };

                    await this.kustoIngestClient.IngestFromDataReaderAsync(reader, ingestProps);
                });
        }

        /// <inheritdoc />
        public IDataReader CreateDataReader<T>(IEnumerable<T> items, params string[] propertyNames)
        {
            return new EnumerableDataReader<T>(items, propertyNames);
        }

        /// <inheritdoc />
        public async Task SetOrAppendTableFromQueryAsync(
            string tableName,
            string innerQuery,
            bool isAsync = false,
            IEnumerable<string> tags = null,
            IEnumerable<string> ingestIfNotExistsValues = null,
            DateTime? creationTime = null,
            ClientRequestProperties clientRequestProperties = null)
        {
            await Logger.InstrumentAsync(
                new OutgoingEvent(SourceLocation.Here()),
                async ev =>
                {
                    var maxRetries = Config.Instance.Telemetry.MaxRetries;

                    ev["DatabaseName"] = this.DatabaseName;
                    ev["TableName"] = tableName;
                    ev["Query"] = innerQuery;
                    ev["MaxRetries"] = maxRetries.ToString();

                    var kcsb = this.CreateKustoConnectionString(this.hostAddress);

                    using (var client = KustoClientFactory.CreateCslAdminProvider(kcsb))
                    {
                        var diagnosticsCommand = CslCommandGenerator.GenerateTableSetOrAppendCommand(tableName, innerQuery, isAsync, tags, ingestIfNotExistsValues, creationTime);

                        int retries = 0;

                        while (true)
                        {
                            try
                            {
                                await client.ExecuteControlCommandAsync(this.DatabaseName, diagnosticsCommand, clientRequestProperties);
                                break;
                            }
                            catch (KustoRequestThrottledException ex)
                            {
                                var message = $"DatabaseName={this.DatabaseName}, TableName={tableName}, Retry={retries}, MaxRetries={maxRetries}, Query={innerQuery}";

                                if (retries >= maxRetries)
                                {
                                    DualLogger.Instance.Error(nameof(KustoClient), ex, $"{message}");
                                    throw;
                                }

                                // warning and retry
                                retries++;
                                DualLogger.Instance.Warning(nameof(KustoClient), $"{message}");
                            }

                            await Task.Delay(TimeSpan.FromSeconds(RandomHelper.Next(1, 10)));
                        }
                    }
                });
        }

        /// <inheritdoc />
        public async Task CreateTableAsync(
            string tableName,
            IEnumerable<Tuple<string, string>> rowFields,
            bool forceNormalizeColumnName = false)
        {
            await Logger.InstrumentAsync(
                new OutgoingEvent(SourceLocation.Here()),
                async ev =>
                {
                    ev["DatabaseName"] = this.DatabaseName;
                    ev["TableName"] = tableName;

                    var kcsb = this.CreateKustoConnectionString(this.hostAddress);

                    using (var client = KustoClientFactory.CreateCslAdminProvider(kcsb))
                    {
                        var diagnosticsCommand = CslCommandGenerator.GenerateTableCreateCommand(tableName, rowFields, forceNormalizeColumnName);
                        await client.ExecuteControlCommandAsync(this.DatabaseName, diagnosticsCommand);
                    }
                });
        }

        private KustoConnectionStringBuilder CreateKustoConnectionString(string hostAddress)
        {
            return new KustoConnectionStringBuilder(hostAddress)
            {
                FederatedSecurity = true,
                ApplicationClientId = Config.Instance.AzureManagement.ApplicationId,
                ApplicationCertificateBlob = Config.Instance.Common.ServiceToServiceCertificate,
                Authority = Config.Instance.Kusto.Authority,
                ApplicationCertificateSendX5c = true,
                AzureRegion = Environment.GetEnvironmentVariable("MONITORING_DATACENTER") ?? "TryAutoDetect",
            };
        }
    }
}