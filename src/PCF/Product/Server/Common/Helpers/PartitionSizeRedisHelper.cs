namespace Microsoft.PrivacyServices.CommandFeed.Service.Common.Helpers
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A helper class for some key data structure for partition size data
    /// </summary>
    public class PartitionSizeRedisHelper
    {
        public const string LastRunRedisKey = "CosmosDbPartitionSizeWorkerLastRun";

        /// <summary>
        /// The key of partition size entry
        /// </summary>
        public class PartitionSizeEntryKey
        {
            public Guid AgentId { get; set; }

            public Guid AssetGroupId { get; set; }

            public string CollectionId { get; set; }

            public string RedisCacheKey => $"{AgentId}|{AssetGroupId}|{CollectionId}";

            public override int GetHashCode()
            {
                return AgentId.GetHashCode() ^ AssetGroupId.GetHashCode() ^ CollectionId.GetHashCode();
            }
            public override bool Equals(object obj)
            {
                if (!ReferenceEquals(null, obj) && (obj is PartitionSizeEntryKey other))
                {
                    return (AgentId == other.AgentId) && (AssetGroupId == other.AssetGroupId) && string.Equals(CollectionId, other.CollectionId);
                }

                return false;
            }
        }

        /// <summary>
        /// One partition size entry value. The value saved in Redis will be an array of PartitionSizeEntryValue
        /// </summary>
        public class PartitionSizeEntryValue
        {
            public string DbMoniker { get; set; }

            public long PartitionSizeKb { get; set; }
        }

        public class PartitionSizeRecord
        {
            public PartitionSizeEntryKey Key { get; set; }

            public List<PartitionSizeEntryValue> Sizes { get; set; }
        }
    }
}
