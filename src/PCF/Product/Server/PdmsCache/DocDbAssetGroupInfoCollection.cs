namespace Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Newtonsoft.Json;

    /// <summary>
    /// Loads asset group information from cosmos streams.
    /// $Metadata$ document has the CurrentVersion and StreamPath and is a simgle unique document per collection
    /// $Metadata$.Version contains the metadata for each version, Verion, CreatedDate, StreamPath TTL: 60 days
    /// Documents have AgentId.AssetGroupId as Id, contains Version and the info for the AssetGroup. TTL: 60 days
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    public class DocDbAssetGroupInfoCollection : IAssetGroupInfoReader
    {
        private static readonly TimeSpan NewDocumentTtl = TimeSpan.FromDays(120);

        private const string MetadataDocumentId = "$Metadata$";
        private const string DatabaseName = "Pdms";
        private const string CollectionName = "VersionedAssetGroupData2";

        private static readonly Uri DatabaseUri = UriFactory.CreateDatabaseUri(DatabaseName);
        private static readonly Uri CollectionUri = UriFactory.CreateDocumentCollectionUri(DatabaseName, CollectionName);
        private static readonly Uri MetadataDocumentUri = UriFactory.CreateDocumentUri(DatabaseName, CollectionName, MetadataDocumentId);

        private readonly DocumentClient consistentPrefixClient;
        private readonly DocumentClient boundedStalenessClient;

        private DocDbAssetGroupInfoCollection()
        {
            var config = Config.Instance.PdmsCache.DocumentDb;

            this.consistentPrefixClient = new DocumentClient(
                config.DatabaseUri,
                config.DatabaseKey,
                DocumentClientHelpers.CreateConnectionPolicy(),
                ConsistencyLevel.ConsistentPrefix);

            this.boundedStalenessClient = new DocumentClient(
                config.DatabaseUri,
                config.DatabaseKey,
                DocumentClientHelpers.CreateConnectionPolicy(enableEndpointDiscovery: false),
                ConsistencyLevel.BoundedStaleness);
        }

        /// <summary>
        /// Creates an initializes a DocDbAssetGroupInfoCollection.
        /// </summary>
        public static async Task<DocDbAssetGroupInfoCollection> CreateAsync()
        {
            var collection = new DocDbAssetGroupInfoCollection();
            await collection.InitializeAsync();
            return collection;
        }

        /// <summary>
        /// Reads the current version of the data set.
        /// </summary>
        public async Task<AssetGroupInfoCollectionReadResult> ReadAsync()
        {
            var metadata = await this.GetCurrentMetadataAsync(requireConsistent: false);
            var result = await this.ReadAsync(metadata);

            return result;
        }

        /// <summary>
        /// Reads the latest version of the data set.
        /// </summary>
        public async Task<long> GetLatestVersionAsync()
        {
            var metadata = await this.GetCurrentMetadataAsync(requireConsistent: false);
            return metadata.CurrentVersion;
        }

        /// <summary>
        /// Reads the given version of the data set.
        /// </summary>
        public async Task<AssetGroupInfoCollectionReadResult> ReadVersionAsync(long version)
        {
            var metadata = await this.GetMetadataForVersionAsync(version);
            var result = await this.ReadAsync(metadata);

            return result;
        }

        public async Task LoadFromAsync(IAssetGroupInfoReader source, bool forceRefresh)
        {
            AssetGroupInfoCollectionReadResult sourceData = await source.ReadAsync();

            //HashSet<(AgentId, AssetGroupId)> knownTuples = new HashSet<(AgentId, AssetGroupId)>();
            //Key for dictionary is agentId.assetgroupId, value is the source document.
            var sourceEntries = new Dictionary<string, AssetGroupInfoDocument>();
            var duplicateEntryCount = 0;
            var duplicateEntryForLogging = string.Empty;

            // 0) Make sure that we can read and authoritatively parse the source data before putting it in DocDB
            foreach (var item in sourceData.AssetGroupInfos)
            {
                // Call AssetGroupInfo to validate asset group data to prevent any bogus entry in the documentDb.
                _ = new AssetGroupInfo(item, enableTolerantParsing: false);

                // check the entry in dictionary, if already exists, keep the one which is not deprecated.
                var agentIdAndAssetGroupIdkey = $"{item.AgentId}.{item.AssetGroupId}";

                if (sourceEntries.ContainsKey(agentIdAndAssetGroupIdkey))
                {
                    // replace existing entry unless this item is deprecated.
                    if (!item.IsDeprecated)
                    {
                        sourceEntries.Remove(agentIdAndAssetGroupIdkey);
                        sourceEntries.Add(agentIdAndAssetGroupIdkey, item);
                    }

                    // increment the duplicate count and value.
                    duplicateEntryCount++;
                    duplicateEntryForLogging += $"{agentIdAndAssetGroupIdkey},";
                    //throw new InvalidOperationException($"Stream '{sourceData.AssetGroupInfoStream}' contained a duplicate tuple: {item.AgentId},{item.AssetGroupId}");
                }
                else
                {
                    sourceEntries.Add(agentIdAndAssetGroupIdkey, item);
                }
            }

            // Add logging for duplicate entries.
            IncomingEvent.Current?.SetProperty("DuplicateEntries", duplicateEntryCount.ToString());
            IncomingEvent.Current?.SetProperty("DuplicateEntryValues", duplicateEntryForLogging.ToString());

            // 1) Discover current version of data set in use. This allows us to quickly see if we need to do anything.
            MetadataDocument metadata = await this.GetCurrentMetadataAsync(requireConsistent: true);

            long currentVersion = metadata?.CurrentVersion ?? -1;
            string currentStream = metadata?.Stream ?? "(none)";
            string currentVariantStream = metadata?.VariantInfoStream ?? "(none)";
            string currentAssetGroupStream = metadata?.AssetGroupInfoStream ?? "(none)";

            IncomingEvent.Current?.SetProperty("CurrentVersion", currentVersion.ToString());
            IncomingEvent.Current?.SetProperty("CurrentStream", currentStream);
            IncomingEvent.Current?.SetProperty("CurrentVariantStream", currentVariantStream);
            IncomingEvent.Current?.SetProperty("CurrentAssetGroupStream", currentAssetGroupStream);
            IncomingEvent.Current?.SetProperty("SourceVariantStream", sourceData.VariantInfoStream);
            IncomingEvent.Current?.SetProperty("SourceAssetGroupStream", sourceData.AssetGroupInfoStream);

            // Build a composite key that combines the stream source with the last time it was regenerated.
            // This allows us to pick up inter-day PDMS config refreshes.
            string sourceAssetGroupStream = $"{sourceData.AssetGroupInfoStream}?lmt={sourceData.CreatedTime.ToNearestMsUtc():O}";

            // 2) Compare with stream from cosmos to see if anything has changed.
            if (!forceRefresh && StringComparer.OrdinalIgnoreCase.Equals(currentStream, sourceAssetGroupStream))
            {
                // This version is already loaded into DocDb. Nothing to do!
                IncomingEvent.Current?.SetProperty("Unchanged", "true");
                return;
            }

            // 2) Discover max version of the data set so far. This is not necessarily the same as the current version,
            //    since a previous update could have failed.
            long maxVersion = 0;

            var query = this.boundedStalenessClient.CreateDocumentQuery<AssetGroupInfoWrapper>(CollectionUri)
                .OrderByDescending(x => x.Version)
                .Take(1)
                .AsDocumentQuery();

            while (query.HasMoreResults)
            {
                var queryResult = await query.InstrumentedExecuteNextAsync("PDMS", "PDMS");
                if (queryResult.items.Any())
                {
                    maxVersion = queryResult.items.First().Version;
                    break;
                }
            }

            // 3 Pick a new version higher than max
            maxVersion++;
            IncomingEvent.Current?.SetProperty("NextVersion", maxVersion.ToString());

            List<Task> runningTasks = new List<Task>();

            // 4 Insert the existing dataset using the new max version
            int i = 0;
            foreach (var item in sourceEntries.Values)
            {
                i++;

                // limit number of active threads to avoid unhandled 429 throttling errors 
                const int maxInsertDocumentThreads = 2;
                while (runningTasks.Count > maxInsertDocumentThreads)

                {
                    Task completed = await Task.WhenAny(runningTasks);
                    runningTasks.Remove(completed);
                    await completed;
                }

                var document = new AssetGroupInfoWrapper
                {
                    Version = maxVersion,
                    AssetGroupInfo = item,
                    Id = $"{item.AgentId}.{item.AssetGroupId}.{maxVersion}",
                };

                Task newTask = this.boundedStalenessClient.InstrumentedCreateDocumentAsync(CollectionUri, "PDMS", "PDMS", document);
                runningTasks.Add(newTask);
            }

            await Task.WhenAll(runningTasks);
            IncomingEvent.Current?.SetProperty("ItemCount", i.ToString());

            // 5 Insert VersionMetadata document
            var versionedMetadata = new MetadataDocument(GetVersionedMetadataId(maxVersion), ttl: (int)NewDocumentTtl.TotalSeconds)
            {
                Stream = sourceAssetGroupStream,
                AssetGroupInfoStream = sourceAssetGroupStream,
                VariantInfoStream = sourceData.VariantInfoStream,
                CurrentVersion = maxVersion,
                CreateDateTime = sourceData.CreatedTime,
            };

            await this.boundedStalenessClient.InstrumentedUpsertDocumentAsync(CollectionUri, "PDMS", "PDMS", versionedMetadata);

            // 6 Update current version.
            var updatedMetadata = new MetadataDocument(MetadataDocumentId, ttl: -1)
            {
                Stream = sourceAssetGroupStream,
                AssetGroupInfoStream = sourceAssetGroupStream,
                VariantInfoStream = sourceData.VariantInfoStream,
                CurrentVersion = maxVersion,
                CreateDateTime = sourceData.CreatedTime,
            };

            await this.boundedStalenessClient.InstrumentedUpsertDocumentAsync(CollectionUri, "PDMS", "PDMS", updatedMetadata);
        }

        /// <summary>
        /// Gets the source stream for the given version of the data.
        /// </summary>
        private async Task<MetadataDocument> GetMetadataForVersionAsync(long version)
        {
            Uri versionMetadataDocumentUri = UriFactory.CreateDocumentUri(DatabaseName, CollectionName, GetVersionedMetadataId(version));

            MetadataDocument document = await this.consistentPrefixClient.InstrumentedReadDocumentAsync<MetadataDocument>(
                versionMetadataDocumentUri,
                "PDMS",
                "PDMS",
                requestOptions: new RequestOptions { PartitionKey = PartitionKey.None },
                expectThrottles: true);

            return document;
        }

        /// <summary>
        /// Fetches the current metadata. The "requireConsistent" flag indicates whether the read should care about consistency.
        /// </summary>
        private async Task<MetadataDocument> GetCurrentMetadataAsync(bool requireConsistent)
        {
            var client = requireConsistent ? this.boundedStalenessClient : this.consistentPrefixClient;

            MetadataDocument document = await this.consistentPrefixClient.InstrumentedReadDocumentAsync<MetadataDocument>(
                MetadataDocumentUri,
                "PDMS",
                "PDMS",
                requestOptions: new RequestOptions { PartitionKey = PartitionKey.None },
                expectThrottles: true);

            return document;
        }

        /// <summary>
        /// Reads the data set referenced by this version of the metadata.
        /// </summary>
        private async Task<AssetGroupInfoCollectionReadResult> ReadAsync(MetadataDocument metadata)
        {
            if (metadata == null)
            {
                return new AssetGroupInfoCollectionReadResult
                {
                    AssetGroupInfoStream = string.Empty,
                    VariantInfoStream = string.Empty,
                    AssetGroupInfos = new List<AssetGroupInfoDocument>(),
                    DataVersion = -1,
                };
            }

            IncomingEvent.Current?.SetProperty("CurrentVersion", metadata.CurrentVersion.ToString());
            IncomingEvent.Current?.SetProperty("CurrentStream", metadata.Stream);
            IncomingEvent.Current?.SetProperty("AssetGroupInfoStream", metadata.AssetGroupInfoStream ?? "(none)");
            IncomingEvent.Current?.SetProperty("VariantInfoStream", metadata.VariantInfoStream ?? "(none)");

            // 2) Query data matching current version
            var query = this.consistentPrefixClient.CreateDocumentQuery<AssetGroupInfoWrapper>(CollectionUri)
                .Where(x => x.Version == metadata.CurrentVersion)
                .AsDocumentQuery();

            var items = new List<AssetGroupInfoWrapper>();
            while (query.HasMoreResults)
            {
                var page = await query.InstrumentedExecuteNextAsync<AssetGroupInfoWrapper>("PDMS", "PDMS");
                items.AddRange(page.items);
            }

            IncomingEvent.Current?.SetProperty("ItemCount", items.Count.ToString());

            var assetGroupInfos = new List<AssetGroupInfoDocument>();
            foreach (var item in items)
            {
                assetGroupInfos.Add(item.AssetGroupInfo);
            }

            // 3) Profit
            return new AssetGroupInfoCollectionReadResult
            {
                AssetGroupInfoStream = metadata.Stream,
                VariantInfoStream = metadata.VariantInfoStream,
                DataVersion = metadata.CurrentVersion,
                CreatedTime = metadata.Timestamp,
                AssetGroupInfos = assetGroupInfos,
            };
        }
        
        private async Task InitializeAsync()
        {
            await this.boundedStalenessClient.OpenAsync();
            await this.consistentPrefixClient.OpenAsync();
            
            var result = await this.boundedStalenessClient.CreateDocumentCollectionIfNotExistsAsync(
                DatabaseUri,
                new DocumentCollection
                {
                    Id = CollectionName,
                },
                new RequestOptions { DisableRUPerMinuteUsage = false, OfferEnableRUPerMinuteThroughput = true, OfferThroughput = Config.Instance.CosmosDBQueues.DefaultRUProvisioning });
        }

        private static string GetVersionedMetadataId(long version)
        {
            return $"{MetadataDocumentId}.{version}";
        }
        
        private sealed class MetadataDocument : Document
        {
            public MetadataDocument(string id, int ttl)
            {
                this.Id = id;
                this.TimeToLive = ttl;
            }

            [Obsolete("JSON.NET use only.")]
            public MetadataDocument()
            {
            }

            [JsonProperty]
            public long CurrentVersion { get; set; }

            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            [JsonProperty]
            public string Stream { get; set; }

            [JsonProperty]
            public string AssetGroupInfoStream { get; set; }

            [JsonProperty]
            public string VariantInfoStream { get; set; }

            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            [JsonProperty]
            public DateTimeOffset CreateDateTime { get; set; }
        }

        private class AssetGroupInfoWrapper : Document
        {
            public AssetGroupInfoWrapper()
            {
                this.TimeToLive = (int)NewDocumentTtl.TotalSeconds;
            }

            [JsonProperty]
            public long Version { get; set; }

            [JsonProperty]
            public AssetGroupInfoDocument AssetGroupInfo { get; set; }
        }
    }
}
