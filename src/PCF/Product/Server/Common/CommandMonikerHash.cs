namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Management.Automation.Runspaces;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.Storage;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.Helpers;
    using Microsoft.PrivacyServices.Common.Azure;
    using Newtonsoft.Json;

    /// <summary>
    /// A shared hash function that returns a "preferred" storage account moniker for the given
    /// command ID + asset group ID.
    /// </summary>
    public static class CommandMonikerHash
    {
        private static readonly TimeSpan RefreshFrequency = TimeSpan.FromSeconds(30);
        private static readonly object SyncRoot = new object();

        private static IDictionary<QueueStorageType, List<string>> weightedMonikers = new Dictionary<QueueStorageType, List<string>>();
        private static IDictionary<QueueStorageType, List<string>> allMonikers = new Dictionary<QueueStorageType, List<string>>();
        private static DateTimeOffset lastRefreshTime = DateTimeOffset.MinValue;

        // In-memory cache for redis data, key is the same key in redis, value is a tuple for last cache refresh time and redis value
        private static ConcurrentDictionary<string, (DateTime, string)> partitionSizeCache = new ConcurrentDictionary<string, (DateTime, string)>();

        /// <summary>
        /// Gets the current set of weighted monikers. Repeated calls to this method may return different results.
        /// </summary>
        public static IReadOnlyList<string> GetCurrentWeightedMonikers(QueueStorageType storageType)
        {
            RefreshIfNecessary();

            // TODO: use visitor pattern here
            switch (storageType)
            {
                case QueueStorageType.AzureQueueStorage:
                    return weightedMonikers[QueueStorageType.AzureQueueStorage];

                case QueueStorageType.AzureCosmosDb:
                default:
                    return weightedMonikers[QueueStorageType.AzureCosmosDb];
            }
        }

        public static IReadOnlyList<string> GetAllMonikers(QueueStorageType storageType)
        {
            if (allMonikers.TryGetValue(storageType, out List<string> monikers))
            {
                return monikers;
            }

            lock (SyncRoot)
            {
                allMonikers[QueueStorageType.AzureCosmosDb] = Config.Instance.CosmosDBQueues.Instances.Select(x=>x.Moniker).ToList();
                allMonikers[QueueStorageType.AzureQueueStorage] = Config.Instance.AgentAzureQueues.StorageAccounts
                    .Where(c => !string.IsNullOrWhiteSpace(c.ConnectionString))
                    .Select(c => CloudStorageAccount.Parse(c.ConnectionString))
                    .Select(c => c.Credentials.AccountName).ToList();
            }

            return allMonikers[storageType];
        }

        /// <summary>
        /// Hashes the command ID and asset group ID to get a preferred moniker for enqueuing the command.
        /// </summary>
        public static string GetPreferredMoniker(CommandId commandId, AssetGroupId assetGroupId, IReadOnlyList<string> weightedMonikerList)
        {
            // Don't send all traffic for the same command ID to the same database.
            ulong hash = NonCryptoMurmur3Hash.GetHash64(commandId.GuidValue.ToByteArray()) ^
                         NonCryptoMurmur3Hash.GetHash64(assetGroupId.GuidValue.ToByteArray());

            int index = (int)(hash % (ulong)weightedMonikerList.Count);
            return weightedMonikerList[index];
        }

        /// <summary>
        /// Generate a weighted monikers list based on cached partition size information for (agentId-assetGroupId-dbCollection) combo
        /// Only re-balance partitions when there is any partition size greater than 10G
        /// Weight distribution:
        /// more than 19GB: 0
        ///         15-19G: 1
        ///         10-15G: 2
        ///          5-10G: 4
        ///   less than 5G: 8
        /// </summary>
        public static IReadOnlyList<string> GetWeightedMonikersByPartitionSize(IRedisClient redisClient, AgentId agentId, AssetGroupId assetGroupId, string collectionId, IReadOnlyList<string> monikerList)
        {
            redisClient.SetDatabaseNumber(RedisDatabaseId.CosmosDbPartitionSizeWorkerData);

            var lastRunTime = redisClient.GetDataTime(PartitionSizeRedisHelper.LastRunRedisKey);
            if ((lastRunTime == default) || (DateTime.UtcNow - lastRunTime.ToUniversalTime() > TimeSpan.FromHours(6)))
            {
                // No record of previous run or the previous run was too long ago
                return monikerList;
            }

            var key = new PartitionSizeRedisHelper.PartitionSizeEntryKey()
            {
                AgentId = agentId.GuidValue,
                AssetGroupId = assetGroupId.GuidValue,
                CollectionId = collectionId
            };

            string cachedPartitionSize = string.Empty;
            if (partitionSizeCache.TryGetValue(key.RedisCacheKey, out var cacheValue))
            {
                if (DateTime.UtcNow - cacheValue.Item1 < TimeSpan.FromHours(2))
                {
                    cachedPartitionSize = cacheValue.Item2;
                }
            }

            if (string.IsNullOrEmpty(cachedPartitionSize))
            {
                cachedPartitionSize = redisClient.GetString(key.RedisCacheKey);
                if (string.IsNullOrEmpty(cachedPartitionSize))
                {
                    return monikerList;
                }

                partitionSizeCache[key.RedisCacheKey] = (DateTime.UtcNow, cachedPartitionSize);
            }

            var cachedSizeInfo = JsonConvert.DeserializeObject<List<PartitionSizeRedisHelper.PartitionSizeEntryValue>>(cachedPartitionSize);

            // Remove all cached monikers that are disabled
            cachedSizeInfo.RemoveAll(x => !monikerList.Contains(x.DbMoniker));

            // If we don't have enough cached partition size data, or the largest partition is not greater 10GB, skip
            const int KbGbFactor = 1000 * 1000; // Not sure what CosmosDb uses to convert Kb to Gb. Use 1000 instead of 1024 to be safe
            const int MinPartitionSizeGb = 10 * KbGbFactor;
            if (cachedSizeInfo.Count < monikerList.Count * 0.8 || cachedSizeInfo.Last().PartitionSizeKb < MinPartitionSizeGb)
            {
                return monikerList;
            }

            var weightedMonikerList = new List<string>();
            foreach (var monikor in monikerList)
            {
                int weight = 2;
                foreach (var partitionSizeInfo in cachedSizeInfo)
                {
                    if (string.Equals(monikor, partitionSizeInfo.DbMoniker, StringComparison.OrdinalIgnoreCase))
                    {
                        long gbSize = partitionSizeInfo.PartitionSizeKb / KbGbFactor;
                        if (gbSize >= 19)
                        {
                            weight = 0;
                        }
                        else if (gbSize >= 15)
                        {
                            weight = 1;
                        }
                        else if (gbSize >= 10)
                        {
                            weight = 2;
                        }
                        else if (gbSize >= 5)
                        {
                            weight = 4;
                        }
                        else
                        {
                            weight = 8;
                        }

                        break;
                    }
                }

                for (int i = 0; i < weight; i++)
                {
                    weightedMonikerList.Add(monikor);
                }
            }

            return weightedMonikerList.Count > monikerList.Count * 0.5 ? weightedMonikerList : monikerList;
        }

        private static void RefreshIfNecessary()
        {
            if (DateTimeOffset.UtcNow - lastRefreshTime < RefreshFrequency)
            {
                return;
            }

            lock (SyncRoot)
            {
                if (DateTimeOffset.UtcNow - lastRefreshTime < RefreshFrequency)
                {
                    return;
                }

                List<string> newWeightedCosmosDbQueueMonikers = new List<string>();
                foreach (var queue in Config.Instance.CosmosDBQueues.Instances)
                {
                    if (FlightingUtilities.IsStringValueEnabled(FlightingNames.CommandQueueEnqueueDisabled, queue.Moniker))
                    {
                        continue;
                    }

                    for (int i = 0; i < queue.Weight; ++i)
                    {
                        newWeightedCosmosDbQueueMonikers.Add(queue.Moniker);
                    }
                }

                List<string> newWeightedAzureQueueMonikers = new List<string>();
                foreach (var queue in Config.Instance.AgentAzureQueues.StorageAccounts)
                {
                    string moniker = CloudStorageAccount.Parse(queue.ConnectionString).Credentials.AccountName;

                    for (int i = 0; i < queue.Weight; ++i)
                    {
                        newWeightedAzureQueueMonikers.Add(moniker);
                    }
                }

                weightedMonikers[QueueStorageType.AzureCosmosDb] = newWeightedCosmosDbQueueMonikers;
                weightedMonikers[QueueStorageType.AzureQueueStorage] = newWeightedAzureQueueMonikers;

                lastRefreshTime = DateTimeOffset.UtcNow;
            }
        }
    }
}
