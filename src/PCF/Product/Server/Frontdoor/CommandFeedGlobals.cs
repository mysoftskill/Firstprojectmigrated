namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks.DeleteExportArchive;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue.QueueStorageCommandQueue;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache;
    using Microsoft.PrivacyServices.CommandFeed.Service.Telemetry.Repository;
    using Microsoft.PrivacyServices.CommandFeed.Validator;
    using Microsoft.PrivacyServices.CommandFeedv2.Common.Storage;
    using Microsoft.PrivacyServices.Common.Cosmos;
    using Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor.ApiTrafficThrottling;

    /// <summary>
    /// Global variables for the static portions of the command feed API.
    /// </summary>
    public static class CommandFeedGlobals
    {
        /// <summary>
        /// The primitive asset group info reader. Most of the time you'll want to use DataAgentMap instead.
        /// </summary>
        public static IAssetGroupInfoReader AssetGroupInfoReader { get; set; }

        /// <summary>
        /// The current data agent map.
        /// </summary>
        public static IDataAgentMapFactory DataAgentMapFactory { get; set; }

        /// <summary>
        /// The current command queue factory.
        /// </summary>
        public static ICommandQueueFactory CommandQueueFactory { get; set; }

        /// <summary>
        /// The current service to service authorizer.
        /// </summary>
        public static IAuthorizer ServiceAuthorizer { get; set; }

        /// <summary>
        /// An authenticator that validate the request.
        /// </summary>
        public static IAuthenticator Authenticator { get; set; }

        /// <summary>
        /// The current lifecycle event publisher.
        /// </summary>
        public static ICommandLifecycleEventPublisher EventPublisher { get; set; }

        /// <summary>
        /// The cold storage repository.
        /// </summary>
        public static ICommandHistoryRepository CommandHistory { get; set; }

        /// <summary>
        /// The validation service that checks command verifiers.
        /// </summary>
        public static IValidationService CommandValidationService { get; set; }

        /// <summary>
        /// Factory method for creating cosmos db clients.
        /// </summary>
        public static ICosmosDbClientFactory CosmosDbClientFactory { get; set; }

        /// <summary>
        /// The app configuration.
        /// </summary>
        public static IAppConfiguration AppConfiguration { get; set; }

        /// <summary>
        /// Publishes work items that can filter and route to agents.
        /// </summary>
        public static IAzureWorkItemQueuePublisher<PublishCommandBatchWorkItem> ExpandCommandBatchWorkItemPublisher { get; set; }

        /// <summary>
        /// Publishes work items that delete commands from an Agent's queue.
        /// </summary>
        public static IAzureWorkItemQueuePublisher<DeleteFromQueueWorkItem> DeleteFromQueuePublisher { get; set; }

        /// <summary>
        /// Publishes work items for completing commands from an Agent's queue coming from the batch checkpoint complete API.
        /// </summary>
        public static IAzureWorkItemQueuePublisher<BatchCheckpointCompleteWorkItem> BatchCheckpointCompleteQueuePublisher { get; set; }

        /// <summary>
        /// Publishes work items that can batch replay request from agents.
        /// </summary>
        public static IAzureWorkItemQueuePublisher<ReplayRequestWorkItem> InsertReplayRequestWorkItemPublisher { get; set; }

        /// <summary>
        /// Publishes work items that will upsert replayed commands into agent's queue.
        /// </summary>
        public static IAzureWorkItemQueuePublisher<EnqueueBatchReplayCommandsWorkItem> EnqueueReplayCommandsWorkItemPublisher { get; set; }

        /// <summary>
        /// Publishes work items that request agent queue flush.
        /// </summary>
        public static IAzureWorkItemQueuePublisher<AgentQueueFlushWorkItem> AgentQueueFlushWorkItemPublisher { get; set; }

        /// <summary>
        /// Publishes work items that trigger ingestion recovery in Command History.
        /// </summary>
        public static IAzureWorkItemQueuePublisher<IngestionRecoveryWorkItem> IngestionRecoveryWorkItemPublisher { get; set; }

        /// <summary>
        /// Publishes work item to delete full export archive on user request
        /// </summary>
        public static IAzureWorkItemQueuePublisher<DeleteFullExportArchiveWorkItem> DeleteFullExportArchivePublisher { get; set; }
        /// <summary>
        /// Context for command queues in cosmos db.
        /// </summary>
        public static CosmosDbContext CosmosDbCommandQueueContext { get; set; }

        /// <summary>
        /// The Kusto telemetry repository
        /// </summary>
        public static ITelemetryRepository KustoTelemetryRepository { get; set; }

        /// <summary>
        /// Context for command queues in azure queue storage.
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

        public static ICosmosResourceFactory CosmosResourceFactory = new CosmosResourceFactory();

        /// <summary>
        /// The ApiTrafficHandler
        /// </summary>
        public static IApiTrafficHandler ApiTrafficHandler { get; set; }
    }
}
