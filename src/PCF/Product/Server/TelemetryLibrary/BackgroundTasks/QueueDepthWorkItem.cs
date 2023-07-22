namespace Microsoft.PrivacyServices.CommandFeed.Service.Telemetry.BackgroundTasks
{
    using System;
    using System.Collections.Generic;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Newtonsoft.Json;

    /// <summary>
    /// Azure Queue queue depth workitem
    /// </summary>
    public class QueueDepthWorkItem
    {
        public static readonly TimeSpan BaselineQueueLeaseTime = TimeSpan.FromMinutes(15);
        public const string BaselineTasksQueueName = "BaselineTasks";

        public QueueDepthWorkItem()
        {
        }

        /// <summary>
        /// Work item id.
        /// </summary>
        public Guid TaskId { get; set; }

        /// <summary>
        /// The friendly name of the database within the account.
        /// </summary>
        public string DbMoniker { get; set; }

        /// <summary>
        /// Database id from the config.
        /// </summary>
        public string DatabaseId { get; set; }

        /// <summary>
        /// Agent id
        /// </summary>
        public AgentId AgentId { get; set; }

        /// <summary>
        /// Assetgroup Id.
        /// </summary>
        [JsonProperty]
        public AssetGroupId AssetGroupId { get; set; }

        /// <summary>
        /// SubjectType.
        /// </summary>
        [JsonProperty]
        public SubjectType SubjectType { get; set; }

        /// <summary>
        /// Database collection id.
        /// </summary>
        [JsonProperty]
        public string CollectionId { get; set; }

        /// <summary>
        /// CommandTypeCount dictionary.
        /// </summary>
        [JsonProperty]
        public Dictionary<PrivacyCommandType, int> CommandTypeCountDictionary { get; set; }

        /// <summary>
        /// When work item was created.
        /// </summary>
        [JsonProperty]
        public DateTimeOffset CreateTime { get; set; }

        /// <summary>
        /// When work item started.
        /// </summary>
        [JsonProperty]
        public DateTimeOffset StartTime { get; set; }

        /// <summary>
        /// Work item completed time.
        /// </summary>
        [JsonProperty]
        public DateTimeOffset EndTime { get; set; }

        /// <summary>
        /// Current iteration.
        /// </summary>
        [JsonProperty]
        public int Iteration { get; set; }

        /// <summary>
        /// Number of retries.
        /// </summary>
        [JsonProperty]
        public int Retries { get; set; }

        /// <summary>
        /// Continuation token.
        /// </summary>
        [JsonProperty]
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Page size.
        /// </summary>
        [JsonProperty]
        public int MaxItemsCount { get; set; }

        /// <summary>
        /// Batch size. How many times run it in worker.
        /// </summary>
        [JsonProperty]
        public int BatchSize { get; set; }

        /// <summary>
        /// Convert to string
        /// </summary>
        public override string ToString()
        {
            return $"{this.GetType().Name}: {JsonConvert.SerializeObject(this, Formatting.Indented)}";
        }
    }
}
