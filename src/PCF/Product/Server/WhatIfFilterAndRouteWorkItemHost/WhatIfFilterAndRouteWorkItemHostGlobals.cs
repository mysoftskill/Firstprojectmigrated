namespace Microsoft.PrivacyServices.CommandFeed.Service.PcfWorker
{
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandReplay;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Validator;

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
        /// The command replay job repository.
        /// </summary>
        public static ICommandReplayJobRepository ReplayJobRepo { get; set; }

        /// <summary>
        /// The validation service that checks command verifiers.
        /// </summary>
        public static IValidationService CommandValidationService { get; set; }
    }
}
