// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue
{
    using System;
    using System.Collections.Generic;
    using CacheManager.Core;
    using Microsoft.Azure.Storage.Queue;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue.Cache;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue.QueueStorageCommandQueue;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;


    /// <inheritdoc />
    public class CommandQueueFactory : ICommandQueueFactory
    {
        private readonly AzureQueueStorageContext azureQueueStorageContext;

        private readonly CosmosDbContext cosmosDbContext;

        private readonly IClock clock;

        private readonly IAssetGroupAzureQueueTrackerCache queueTrackerCache;

        private MemoryCacheEntryOptions cacheEntryOptions;
        private CustomInMemoryCache<LogicalCommandQueue> cache;

        //create a cache manager to cache the logical command queues with 15 minutes ttl
        private static CustomInMemoryCache<LogicalCommandQueue> customInMemoryCache = new CustomInMemoryCache<LogicalCommandQueue>(new MemoryCacheOptions{ SizeLimit = 1000 }, 
            new MemoryCacheEntryOptions().SetSize(1).SetSlidingExpiration(TimeSpan.FromMinutes(15)).SetAbsoluteExpiration(TimeSpan.FromMinutes(30)));
        /// <summary>
        /// Creates a new instance based on the given cosmosDbContext.
        /// </summary>
        public CommandQueueFactory(CosmosDbContext cosmosDbContext, AzureQueueStorageContext azureQueueStorageContext, IClock clock, IAssetGroupAzureQueueTrackerCache queueTrackerCache)
        {
            this.cosmosDbContext = cosmosDbContext ?? throw new ArgumentNullException(nameof(cosmosDbContext));
            this.azureQueueStorageContext = azureQueueStorageContext ?? throw new ArgumentNullException(nameof(azureQueueStorageContext));
            this.clock = clock;
            this.queueTrackerCache = queueTrackerCache;
        }

        /// <inheritdoc />
        public ICommandQueue CreateQueue(
            AgentId agentId,
            AssetGroupId assetGroupId,
            SubjectType subjectType,
            QueueStorageType queueStorageType)
        {
            // Create a cache key using agentId, assetGroupId, subjectType, and queueStorageType
            string cacheKey = $"{agentId}-{assetGroupId}-{subjectType}-{queueStorageType}";
            LogicalCommandQueue commandQueue = customInMemoryCache.Get(cacheKey);
            if(commandQueue != null)
            {
                return commandQueue;
            }
            switch (queueStorageType)
            {
                case QueueStorageType.AzureQueueStorage:
                    List<(ICommandQueue commandQueue, int weight, string dbMoniker)> queueStorageQueues = new List<(ICommandQueue commandQueue, int weight, string dbMoniker)>();

                    foreach (CloudQueueClient queueClient in this.azureQueueStorageContext.GetQueueClients())
                    {
                        foreach (PrivacyCommandType privacyCommandType in AzureQueueStorageCommandQueue.SupportedCommandTypes)
                        {
                            // Every CommandType needs its own queue because more than one AgentId may be linked to an AssetGroupId, but only one AgentId may be linked to an AssetGroupId per CommandType
                            CloudQueue cloudQueue = queueClient.GetQueueReference(AzureQueueStorageCommandQueue.CreateAzureQueueName(assetGroupId, subjectType, privacyCommandType));
                            var commandAzureCloudQueue = new CommandAzureCloudQueue(cloudQueue, TimeSpan.FromSeconds(900));
                            queueStorageQueues.Add(
                                (new AzureQueueStorageCommandQueue(
                                        commandAzureCloudQueue,
                                        agentId,
                                        assetGroupId,
                                        privacyCommandType,
                                        subjectType,
                                        this.clock,
                                        this.queueTrackerCache),
                                    weight: 1,
                                    dbMoniker: queueClient.Credentials.AccountName));
                        }
                    }

                    commandQueue = new LogicalCommandQueue(queueStorageQueues, CommandQueuePriority.Low);
                    customInMemoryCache.Add(cacheKey, commandQueue);
                    return commandQueue;

                case QueueStorageType.AzureCosmosDb:
                default:
                    List<(ICommandQueue commandQueue, int weight, string dbMoniker)> cosmosDbQueues = new List<(ICommandQueue commandQueue, int weight, string dbMoniker)>();
                    foreach (var collection in this.cosmosDbContext.GetCollections(subjectType))
                    {
                        cosmosDbQueues.Add(
                            (new CosmosDbCommandQueue(collection, agentId, assetGroupId),
                                collection.Weight,
                                collection.DatabaseMoniker));
                    }

                    commandQueue =  new LogicalCommandQueue(cosmosDbQueues, CommandQueuePriority.High);
                    customInMemoryCache.Add(cacheKey, commandQueue);
                    return commandQueue;
            }
        }
    }
}
