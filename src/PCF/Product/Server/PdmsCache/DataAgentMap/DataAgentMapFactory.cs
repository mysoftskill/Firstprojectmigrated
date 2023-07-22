namespace Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Contains an in-process and parsed version of a remote data source.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "Lifetime object")]
    public class DataAgentMapFactory : IDataAgentMapFactory
    {
        private const string DatabaseName = "OnlineAgentDb";
        private const string CollectionName = "OnlineAgentCollection";

        private readonly DocumentClient onlineAgentMapClient;

        private readonly IAssetGroupInfoReader reader;
        private IDataAgentMap currentMap;
        private ConcurrentDictionary<long, IDataAgentMap> versionedDataAgentMaps;
        private HashSet<AgentId> currentOnlineAgents;

        public DataAgentMapFactory(IAssetGroupInfoReader reader)
        {
            this.reader = reader;
            this.versionedDataAgentMaps = new ConcurrentDictionary<long, IDataAgentMap>();

            this.onlineAgentMapClient = new DocumentClient(
                Config.Instance.PdmsCache.DocumentDb.DatabaseUri,
                Config.Instance.PdmsCache.DocumentDb.DatabaseKey);
        }

        public async Task InitializeAsync()
        {
            await this.onlineAgentMapClient.CreateDatabaseIfNotExistsAsync(new Azure.Documents.Database
            {
                Id = DatabaseName
            });

            await this.onlineAgentMapClient.CreateDocumentCollectionIfNotExistsAsync(
                UriFactory.CreateDatabaseUri(DatabaseName),
                new Azure.Documents.DocumentCollection { Id = CollectionName },
                new RequestOptions { OfferThroughput = 1000 });
            
            if (this.currentMap == null)
            {
                await this.GetOnlineAgentsAsync();
                await this.LoadDataAsync();
            }
        }

        public IDataAgentMap GetDataAgentMap()
        {
            if (Config.Instance.Common.ApplyTestDataAgentMapDecorator)
            {
                return new TestDataAgentMap();
            }

            return this.currentMap;
        }

        public async Task<IDataAgentMap> GetDataAgentMapAsync(long requestedVersion)
        {
            if (Config.Instance.Common.ApplyTestDataAgentMapDecorator)
            {
                return new TestDataAgentMap();
            }

            if (this.versionedDataAgentMaps.TryGetValue(requestedVersion, out var map))
            {
                return map;
            }

            var result = await this.reader.ReadVersionAsync(requestedVersion);

            // inserts into the dictionary.
            return this.ParseReadResult(result);
        }

        public async Task RefreshAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(RandomHelper.Next(3, 8)));

                try
                {
                    await Logger.InstrumentAsync(
                        new IncomingEvent(SourceLocation.Here()),
                        async ev =>
                        {
                            await this.GetOnlineAgentsAsync();
                            await this.LoadDataAsync();
                            ev.StatusCode = HttpStatusCode.OK;
                        });
                }
                catch
                {
                    // Swallow to prevent data refresh from stopping. Exception has been logged in the inner instrumentation block.
                }
            }
        }

        private async Task LoadDataAsync()
        {
            var latestVersion = await this.reader.GetLatestVersionAsync();
            if (this.versionedDataAgentMaps.TryGetValue(latestVersion, out var map))
            {
                // The latest version has already been loaded, skip reading and set currentMap to the latest one
                this.currentMap = map;
                return;
            }

            AssetGroupInfoCollectionReadResult result = await this.reader.ReadAsync();

            Logger.Instance?.LogPdmsDataSetAgeEvent(result.AssetGroupInfoStream, result.VariantInfoStream, result.CreatedTime, result.DataVersion);
            PerfCounterUtility.GetOrCreate(PerformanceCounterType.Number, "PdmsDataSetAgeSeconds").Set((int)(DateTimeOffset.UtcNow - result.CreatedTime).TotalSeconds);

            IDataAgentMap newMap = this.ParseReadResult(result);

            // Atomic switch is thread-safe.
            this.currentMap = newMap;
        }

        private IDataAgentMap ParseReadResult(AssetGroupInfoCollectionReadResult result)
        {
            // Do a flexible parse of the results.
            List<AssetGroupInfo> parsedData = new List<AssetGroupInfo>();
            foreach (var item in result.AssetGroupInfos)
            {
                parsedData.Add(new AssetGroupInfo(item, enableTolerantParsing: true));
            }

            Dictionary<AgentId, List<AssetGroupInfo>> grouping = parsedData
                .Where(x => x != null)
                .GroupBy(x => x.AgentId)
                .ToDictionary(x => x.Key, x => x.ToList());

            DualLogger.Instance.Information(nameof(DataAgentMapFactory), $"Load PDMS data. stream = ({result.AssetGroupInfoStream}), created=({result.CreatedTime.ToString("yyyy-MM-ddTHH:mm:ss.fff")}), version={result.DataVersion}");
            
            IDataAgentMap newMap = new DataAgentMap(grouping, result.DataVersion, result.AssetGroupInfoStream, result.VariantInfoStream, this.currentOnlineAgents, this.SetAgentOnlineAsync);
            
#if INCLUDE_TEST_HOOKS

            if (Config.Instance.PPEHack.Enabled)
            {
                newMap = new HackedDataAgentMap(newMap);
            }
#endif

            this.versionedDataAgentMaps[newMap.Version] = newMap;
            return newMap;
        }

        private async Task SetAgentOnlineAsync(AgentId agentId)
        {
            try
            {
                await this.onlineAgentMapClient.InstrumentedCreateDocumentAsync(
                    UriFactory.CreateDocumentCollectionUri(DatabaseName, CollectionName),
                    DatabaseName,
                    CollectionName,
                    new Document { Id = agentId.Value },
                    expectConflicts: true);
            }
            catch (CommandFeedException ex)
            {
                if (ex.ErrorCode != CommandFeedInternalErrorCode.Conflict)
                {
                    throw;
                }
            }
        }

        private async Task GetOnlineAgentsAsync()
        {
            HashSet<AgentId> onlineAgents = new HashSet<AgentId>();

            var query = this.onlineAgentMapClient.CreateDocumentQuery(UriFactory.CreateDocumentCollectionUri(DatabaseName, CollectionName)).AsDocumentQuery();
            while (query.HasMoreResults)
            {
                var result = await query.InstrumentedExecuteNextAsync(DatabaseName, CollectionName);
                IEnumerable<Document> documents = result.items;
                foreach (var item in documents)
                {
                    onlineAgents.Add(new AgentId(item.Id));
                }
            }

            IncomingEvent.Current?.SetProperty("OnlineAgentCount", onlineAgents.Count.ToString());
            this.currentOnlineAgents = onlineAgents;
        }
    }
}
