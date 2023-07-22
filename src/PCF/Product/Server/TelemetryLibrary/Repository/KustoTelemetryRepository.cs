namespace Microsoft.PrivacyServices.CommandFeed.Service.Telemetry.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using Kusto.Data.Common;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.Telemetry;
    using Microsoft.PrivacyServices.CommandFeed.Service.Telemetry.BackgroundTasks;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Kusto telemetry repository.
    /// Keeps the information about Kusto tables, databases.
    /// Provides API for communication with Kusto storage.
    /// </summary>
    public class KustoTelemetryRepository : ITelemetryRepository
    {
        private readonly IKustoClient kustoClient;
        
        /// <summary>
        /// Create KustoTelemetryRepository.
        /// </summary>
        public KustoTelemetryRepository()
        {
            this.kustoClient = new KustoClient(Config.Instance.Kusto.ClusterName, Config.Instance.Kusto.DatabaseName);
        }

        /// <summary>
        /// Gets or sets KustoTasksTableName.
        /// </summary>
        public string KustoTasksTableName { get; set; } = "PCFBaselineTaskV2";

        /// <summary>
        /// Gets or sets KustoBaselineTableName.
        /// </summary>
        public string KustoBaselineTableName { get; set; } = "PCFCollectionBaselineV2";

        /// <summary>
        /// Gets or sets KustoLifecycleEventsTableName.
        /// </summary>
        public string KustoLifecycleEventsTableName { get; set; } = "PCFLifecycleEventsV2";

        /// <summary>
        /// Gets or sets KustoAgentStatTableName.
        /// </summary>
        public string KustoAgentStatTableName { get; set; } = "PCFAgentStatisticV2";

        /// <summary>
        /// Gets or sets KustoAgentStorageQueueDepthTableName
        /// </summary>
        public string KustoAgentStorageQueueDepthTableName { get; set; } = "PCFAgentAzureStorageQueueDepthV2";

        public string KustoCosmosDbPartitionSizeTableName { get; set; } = "PCFCosmosDbPartitionSize";

        /// <summary>
        /// Check if Kusto is enabled.
        /// </summary>
        public static bool IsKustoEnabled => !FlightingUtilities.IsEnabled(FlightingNames.QueueDepthDisableKusto);

        /// <summary>
        /// Check if Kusto QueueDepthKustoFlushImmediately enabled. Disabled by default.
        /// </summary>
        public static bool FlushImmediately => FlightingUtilities.IsEnabled(FlightingNames.QueueDepthKustoFlushImmediately);

        /// <inheritdoc/>
        public async Task AddAzureStorageQueueDepthAsync(List<AgentQueueStatistics> agentQueueStatistics)
        {
            DualLogger.Instance.Verbose(nameof(KustoTelemetryRepository), $"Add agent Azure storage queue depth events, records={agentQueueStatistics?.Count ?? 0}.");

            if (agentQueueStatistics == null)
            {
                return;
            }

            if (!IsKustoEnabled)
            {
                DualLogger.Instance.Verbose(nameof(KustoTelemetryRepository), $"Kusto is disabled.");
                return;
            }

            var kustoData = agentQueueStatistics.Select(x => new
            {
                TimeStamp = x.QueryDate,
                AgentId = x.AgentId.GuidValue.ToString(),
                AssetGroupId = x.AssetGroupId.GuidValue.ToString(),
                SubjectType = x.SubjectType.ToString(),
                CommandType = x.CommandType?.ToString() ?? "Unknown",
                QueueMoniker = x.DbMoniker,
                Count = x.PendingCommandCount ?? (long)0,
            });

            if (kustoData.Any())
            {
                var first = kustoData.First();
                string tableName = this.KustoAgentStorageQueueDepthTableName;
                var dataReader = this.kustoClient.CreateDataReader(
                    kustoData,
                    nameof(first.TimeStamp),
                    nameof(first.AgentId),
                    nameof(first.AssetGroupId),
                    nameof(first.SubjectType),
                    nameof(first.CommandType),
                    nameof(first.QueueMoniker),
                    nameof(first.Count));

                await this.kustoClient.IngestAsync(tableName, dataReader, flushImmediately: FlushImmediately);
            }
        }

        /// <inheritdoc/>
        public async Task AddAsync(List<LifecycleEventTelemetry> lifecycleEvents)
        {
            DualLogger.Instance.Verbose(nameof(KustoTelemetryRepository), $"Add telemetry events, records={lifecycleEvents.Count}.");

            if (!IsKustoEnabled)
            {
                DualLogger.Instance.Verbose(nameof(KustoTelemetryRepository), $"Kusto is disabled.");
                return;
            }

            var kustoData = lifecycleEvents.Select(x => new
            {
                TimeStamp = x.Timestamp.UtcDateTime,
                AgentId = x.AgentId.GuidValue.ToString(),
                AssetGroupId = x.AssetGroupId.GuidValue.ToString(),
                CommandId = x.CommandId.GuidValue.ToString(),
                CommandType = x.CommandType.ToString(),
                EventType = x.EventType.ToString(),
                Count = (long)x.Count,
            });

            if (kustoData.Any())
            {
                var first = kustoData.First();

                string tableName = this.KustoLifecycleEventsTableName;
                var dataReader = this.kustoClient.CreateDataReader(
                    kustoData,
                    nameof(first.TimeStamp),
                    nameof(first.AgentId),
                    nameof(first.AssetGroupId),
                    nameof(first.CommandId),
                    nameof(first.CommandType),
                    nameof(first.EventType),
                    nameof(first.Count));

                await this.kustoClient.IngestAsync(tableName, dataReader, flushImmediately: FlushImmediately);
            }
        }

        /// <inheritdoc/>
        public async Task AddBaselineAsync(QueueDepthWorkItem workItem)
        {
            DualLogger.Instance.Verbose(nameof(KustoTelemetryRepository), $"Add command queue baseline, WorkItem={workItem}.");

            if (!IsKustoEnabled)
            {
                DualLogger.Instance.Verbose(nameof(KustoTelemetryRepository), $"Kusto is disabled.");
                return;
            }

            var kustoData = workItem.CommandTypeCountDictionary.Keys.Select(x => new
            {
                TaskId = workItem.TaskId.ToString(),
                TaskCreateTime = workItem.CreateTime.UtcDateTime,
                StartTime = workItem.StartTime.UtcDateTime,
                EndTime = workItem.EndTime.UtcDateTime,
                DbMoniker = workItem.DbMoniker,
                CollectionId = workItem.CollectionId,
                AgentId = workItem.AgentId.GuidValue.ToString(),
                AssetGroupId = workItem.AssetGroupId.GuidValue.ToString(),
                CommandType = x.ToString(),
                CommandCount = (long)workItem.CommandTypeCountDictionary[x]
            });

            if (kustoData.Any())
            {
                var first = kustoData.First();

                string tableName = this.KustoBaselineTableName;
                var dataReader = this.kustoClient.CreateDataReader(
                    kustoData,
                    nameof(first.TaskId),
                    nameof(first.TaskCreateTime),
                    nameof(first.StartTime),
                    nameof(first.EndTime),
                    nameof(first.DbMoniker),
                    nameof(first.CollectionId),
                    nameof(first.AgentId),
                    nameof(first.AssetGroupId),
                    nameof(first.CommandType),
                    nameof(first.CommandCount));

                await this.kustoClient.IngestAsync(tableName, dataReader, flushImmediately: FlushImmediately);
            }
        }

        /// <inheritdoc/>
        public async Task AddCosmosDbPartitionSizeAsync(List<CosmosDbPartitionSize> partitionSizes)
        {
            DualLogger.Instance.Information(nameof(KustoTelemetryRepository), $"Add Cosmos DB partition size, {partitionSizes.Count} entries.");

            if (!IsKustoEnabled)
            {
                DualLogger.Instance.Verbose(nameof(KustoTelemetryRepository), $"Kusto is disabled.");
                return;
            }

            if (partitionSizes.Any())
            {
                var first = partitionSizes.First();

                string tableName = this.KustoCosmosDbPartitionSizeTableName;
                var dataReader = this.kustoClient.CreateDataReader(
                    partitionSizes,
                    nameof(first.Timestamp),
                    nameof(first.AgentId),
                    nameof(first.AssetGroupId),
                    nameof(first.DbMoniker),
                    nameof(first.CollectionId),
                    nameof(first.PartitionSizeKb));

                await this.kustoClient.IngestAsync(tableName, dataReader, flushImmediately: FlushImmediately);
            }
        }

        /// <inheritdoc/>
        public async Task AddTasksAsync(List<QueueDepthWorkItem> workItems, TaskActionName action)
        {
            DualLogger.Instance.Verbose(nameof(KustoTelemetryRepository), $"WorkItemsCount={workItems.Count}.");

            if (!IsKustoEnabled)
            {
                DualLogger.Instance.Verbose(nameof(KustoTelemetryRepository), $"Kusto is disabled.");
                return;
            }

            var kustoData = workItems.Select(x => new
            {
                TimeStamp = DateTimeOffset.UtcNow,
                TaskId = x.TaskId.ToString(),
                Action = action,
                CreateTime = x.CreateTime,
                DbMoniker = x.DbMoniker,
                CollectionId = x.CollectionId,
                AgentId = x.AgentId.GuidValue.ToString(),
                AssetGroupId = x.AssetGroupId.GuidValue.ToString(),
                Iterations = (long)x.Iteration,
                Retries = (long)x.Retries
            });

            if (kustoData.Any())
            {
                var first = kustoData.First();

                string tableName = this.KustoTasksTableName;
                var dataReader = this.kustoClient.CreateDataReader(
                    kustoData,
                    nameof(first.TimeStamp),
                    nameof(first.TaskId),
                    nameof(first.Action),
                    nameof(first.CreateTime),
                    nameof(first.DbMoniker),
                    nameof(first.CollectionId),
                    nameof(first.AgentId),
                    nameof(first.AssetGroupId),
                    nameof(first.Iterations),
                    nameof(first.Retries));

                await this.kustoClient.IngestAsync(tableName, dataReader, flushImmediately: FlushImmediately);
            }
        }

        /// <inheritdoc/>
        public async Task AddTaskAsync(QueueDepthWorkItem workItem, TaskActionName action)
        {
            await this.AddTasksAsync(new List<QueueDepthWorkItem>() { workItem }, action);
        }

        /// <inheritdoc/>
        public async Task AppendAgentStatAsync(AgentId agentId)
        {
            await this.RunAgentSetOrAppendAsync(agentId, KustoQuery.KustoAggregationQuery);
        }

        /// <inheritdoc/>
        public async Task InterpolateAgentStatAsync(AgentId agentId)
        {
            await this.RunAgentSetOrAppendAsync(agentId, KustoQuery.KustoInterpolationQuery);
        }

        /// <inheritdoc />
        public async Task<List<QueueStats>> GetAgentStats(
            IDataAgentMap dataAgentMap,
            AgentId agentId,
            AssetGroupId assetGroupId = null,
            PrivacyCommandType privacyCommandType = PrivacyCommandType.None)
        {
            DualLogger.Instance.Information(nameof(KustoTelemetryRepository), $"AgentId={agentId}, AssetGroupId={assetGroupId}, CommandType={privacyCommandType}");
            try
            {
                var clientRequestProperties = new ClientRequestProperties();
                var reader = await this.kustoClient.QueryAsync(
                    BuildQueryForQueueStats(agentId, assetGroupId, privacyCommandType, clientRequestProperties),
                    clientRequestProperties);

                List<QueueStats> queueStats = new List<QueueStats>();
                while (reader.Read())
                {
                    var assetgroupIdFromReader = (string)reader["AssetGroupId"];
                    var commandTypeFromReader = (string)reader["CommandType"];

                    var parsedAssetGroupId = new AssetGroupId(assetgroupIdFromReader);
                    Enum.TryParse(commandTypeFromReader, out PrivacyCommandType parsedCommandType);

                    IAssetGroupInfo assetGroup = dataAgentMap.AssetGroupInfos.SingleOrDefault(x => x.AgentId == agentId && x.AssetGroupId == parsedAssetGroupId);
                    queueStats.Add(new QueueStats
                    {
                        AssetGroupQualifier = assetGroup?.AssetQualifier.Value ?? "",
                        CommandType = parsedCommandType.ToString(),
                        Timestamp = (DateTime)reader["TimeStamp"],
                        PendingCommandCount = (long)reader["CommandCount"]
                    });
                }

                return queueStats;
            }
            catch (Exception ex)
            {
                DualLogger.Instance.Error(nameof(KustoTelemetryRepository), ex, "Exception while getting queue stats from Kusto");
                throw;
            }
        }

        /// <summary>
        /// Builds query based on the optional parameters
        /// </summary>
        private static string BuildQueryForQueueStats(
            AgentId agentId,
            AssetGroupId assetGroupId,
            PrivacyCommandType privacyCommandType,
            ClientRequestProperties clientRequestProperties)
        {
            const string queryFormat = @"
                declare query_parameters (agentId:string{0});
                let MaximumTimeSpan = toscalar( PCFAgentStatisticV2 | where AgentId == agentId | summarize max(TimeStamp));
                PCFAgentStatisticV2 
                | where AgentId == agentId
                | where TimeStamp == MaximumTimeSpan
                {1}
                | order by AssetGroupId, CommandType
                | project AssetGroupId, CommandType, CommandCount, TimeStamp;";

            clientRequestProperties.SetParameter("agentId", agentId.GuidValue.ToString());

            string queryParams = string.Empty;
            string filter = string.Empty;
            if (assetGroupId != null)
            {
                queryParams = ", assetGroupId:string";
                filter = $" | where AssetGroupId == assetGroupId";
                clientRequestProperties.SetParameter("assetGroupId", assetGroupId.GuidValue.ToString());
            }

            if (privacyCommandType != PrivacyCommandType.None)
            {
                queryParams += ", privacyCommandType:string";
                filter += $" | where CommandType == privacyCommandType";
                clientRequestProperties.SetParameter("privacyCommandType", privacyCommandType.ToString());
            }

            return string.Format(CultureInfo.InvariantCulture, queryFormat, queryParams, filter);
        }

        private async Task RunAgentSetOrAppendAsync(AgentId agentId, string query)
        {
            var clientRequestProperties = new ClientRequestProperties();
            clientRequestProperties.SetParameter("AgentId", agentId.GuidValue.ToString());

            await this.kustoClient.SetOrAppendTableFromQueryAsync(
                this.KustoAgentStatTableName,
                query,
                clientRequestProperties: clientRequestProperties);
        }
    }
}
