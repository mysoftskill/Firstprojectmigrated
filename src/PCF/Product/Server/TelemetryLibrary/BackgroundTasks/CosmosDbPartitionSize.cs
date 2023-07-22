namespace Microsoft.PrivacyServices.CommandFeed.Service.Telemetry.BackgroundTasks
{
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using System;

    public class CosmosDbPartitionSize
    {
        /// <summary>
        /// Timestamp when the snapshot was taken
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Agent id
        /// </summary>
        public Guid AgentId { get; set; }

        /// <summary>
        /// Assetgroup Id.
        /// </summary>
        public Guid AssetGroupId { get; set; }

        /// <summary>
        /// The friendly name of the database within the account. e.g. pcfprod-west-04.db25
        /// </summary>
        public string DbMoniker { get; set; }

        /// <summary>
        /// Database collection id. e.g. aadQueueCollection
        /// </summary>
        public string CollectionId { get; set; }

        /// <summary>
        /// Partition size in KB
        /// </summary>
        public long PartitionSizeKb { get; set; }
    }
}
