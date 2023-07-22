namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents the stats for an assetQualifier/commandType for a specific agent
    /// </summary>
    [JsonObject]
    public class QueueStats
    {
        /// <summary>
        /// AssetQualifier for the stats
        /// </summary>
        [JsonProperty("assetQualifier")]
        public string AssetGroupQualifier { get; set; }

        /// <summary>
        /// CommandType
        /// </summary>
        [JsonProperty("commandType")]
        public string CommandType { get; set; }

        /// <summary>
        /// Timestamp of the stats
        /// </summary>
        [JsonProperty("timeStamp")]
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Count of commands pending for this assetgroup/commandtype pair
        /// </summary>
        [JsonProperty("count")]
        public long PendingCommandCount { get; set; }
    }
}