namespace Microsoft.PrivacyServices.CommandFeed.Service.PcfWorker
{
    using Microsoft.PrivacyServices.Common.Cosmos;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue.QueueStorageCommandQueue;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandReplay;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Validator;
    using Microsoft.PrivacyServices.CommandFeedv2.Common.Storage;
    using Microsoft.Azure.ComplianceServices.Common;

    /// <summary>
    /// Global variables for the static portions of the PCF Worker.
    /// </summary>
    public static class PcfWorkerGlobals
    {
        /// <summary>
        /// The data agent map factory.
        /// </summary>
        public static IDataAgentMapFactory DataAgentMapFactory { get; set; }

        /// <summary>
        /// Command queue factory.
        /// </summary>
        public static ICommandQueueFactory CommandQueueFactory { get; set; }

        /// <summary>
        /// The current cosmosdb context
        /// </summary>
        public static CosmosDbContext CosmosDbContext { get; set; }

        /// <summary>
        /// The current lifecycle event publisher.
        /// </summary>
        public static ICommandLifecycleEventPublisher EventPublisher { get; set; }

        /// <summary>
        /// The cold storage repository.
        /// </summary>
        public static ICommandHistoryRepository CommandHistory { get; set; }

        /// <summary>
        /// Factory method for creating cosmos db clients.
        /// </summary>
        public static ICosmosDbClientFactory CosmosDbClientFactory { get; set; }
        
        /// <summary>
        /// The app configuration.
        /// </summary>
        public static IAppConfiguration AppConfiguration { get; set; }

        /// <summary>
        /// The command replay job repository.
        /// </summary>
        public static ICommandReplayJobRepository ReplayJobRepo { get; set; }

        /// <summary>
        /// The validation service that checks command verifiers.
        /// </summary>
        public static IValidationService CommandValidationService { get; set; }

        /// <summary>
        /// Kusto client that ingest data to kusto
        /// </summary>
        public static IKustoClient KustoClient { get; set; }

        /// <summary>
        /// Azure queue storage command context
        /// </summary>
        public static AzureQueueStorageContext AzureQueueStorageCommandContext { get; set; }

        /// <summary>
        /// The clock
        /// </summary>
        public static IClock Clock { get; } = new Clock();

        /// <summary>
        /// The asset group azure queue tracker cache
        /// </summary>
        public static IAssetGroupAzureQueueTrackerCache QueueTrackerCache { get; } = new AssetGroupAzureQueueTrackerCache();

        /// <summary>
        /// Commands batch publisher.
        /// </summary>
        public static AzureWorkItemQueue<PublishCommandBatchWorkItem> CommandBatchWorkItemPublisher = new AzureWorkItemQueue<PublishCommandBatchWorkItem>();

        public static ICosmosResourceFactory CosmosResourceFactory { get; }  = new CosmosResourceFactory();

        public static IRedisClient RedisClient { get; set; }
    }
}
