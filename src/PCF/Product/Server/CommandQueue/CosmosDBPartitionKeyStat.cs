namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue
{
    using Microsoft.Azure.Documents;

    /// <summary>
    /// Azure Cosmos DB Partition Key Statistics
    /// </summary>
    public class CosmosDBPartitionKeyStat
    {
        private readonly PartitionKey partitionKey;
        private readonly long partitionKeySizeInKB;
        private readonly string partitionKeyRangeId;
        private readonly long partitionRangeSizeInKB;
        private readonly long partitionRangeDocumentCount;
        private readonly double partitionRangeDocumentAverageSizeInKB;
        private readonly long partitionKeyAverageDocumentCount;

        public CosmosDBPartitionKeyStat(
            PartitionKey partitionKey,
            long partitionKeySizeInKB, 
            string partitionKeyRangeId, 
            long partitionRangeSizeInKB, 
            long partitionRangeDocumentCount)
        {
            this.partitionKey = partitionKey;
            this.partitionKeySizeInKB = partitionKeySizeInKB;
            this.partitionKeyRangeId = partitionKeyRangeId;
            this.partitionRangeSizeInKB = partitionRangeSizeInKB;
            this.partitionRangeDocumentCount = partitionRangeDocumentCount;
            this.partitionRangeDocumentAverageSizeInKB = (double)this.partitionRangeSizeInKB / this.partitionRangeDocumentCount;
            this.partitionKeyAverageDocumentCount = (long)(this.partitionKeySizeInKB / this.partitionRangeDocumentAverageSizeInKB);
        }

        /// <summary>
        /// Partition Key
        /// </summary>
        public PartitionKey PartitionKey => this.partitionKey;

        /// <summary>
        /// Partition Key size in KB
        /// </summary>
        public long PartitionKeySizeInKB => this.partitionKeySizeInKB;

        /// <summary>
        /// Phisical partition Id
        /// </summary>
        public string PartitionKeyRangeId => this.partitionKeyRangeId;

        /// <summary>
        /// Total phisical partition size in KB
        /// </summary>
        public long PartitionRangeSizeInKB => this.partitionRangeSizeInKB;

        /// <summary>
        /// Total documents in phisical partition
        /// </summary>
        public long PartitionRangeDocumentCount => this.partitionRangeDocumentCount;

        /// <summary>
        /// Average size of the document in phisical partition
        /// </summary>
        public double PartitionRangeDocumentAverageSizeInKB => this.partitionRangeDocumentAverageSizeInKB;

        /// <summary>
        /// Estimated number of documents in partition key
        /// </summary>
        public long PartitionKeyApproximateDocumentCount => this.partitionKeyAverageDocumentCount;
    }
}
