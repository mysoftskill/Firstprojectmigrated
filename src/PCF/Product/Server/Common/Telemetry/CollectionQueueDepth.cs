namespace Microsoft.PrivacyServices.CommandFeed.Service.Common.Telemetry
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Newtonsoft.Json;

    /// <summary>
    /// Azure Cosmos DB collection command queue depth.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public sealed class CollectionQueueDepth
    {
        /// <summary>
        /// Baseline version
        /// </summary>
        [JsonProperty]
        public string BaselineVersion { get; set; }

        /// <summary>
        /// AgentId
        /// </summary>
        [JsonProperty]
        public AgentId AgentId { get; set; }

        /// <summary>
        /// Assetgroup Id
        /// </summary>
        [JsonProperty]
        public AssetGroupId AssetGroupId { get; set; }

        /// <summary>
        /// Assetgroup Id
        /// </summary>
        [JsonProperty]
        public PrivacyCommandType CommandType { get; set; }

        /// <summary>
        /// Commands count
        /// </summary>
        [JsonProperty]
        public int CommandsCount { get; set; }

        /// <summary>
        /// Document creation date time.
        /// </summary>
        [JsonProperty]
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Query start time
        /// </summary>
        [JsonProperty]
        public DateTimeOffset StartTime { get; set; }

        /// <summary>
        /// Query completed time
        /// </summary>
        [JsonProperty]
        public DateTimeOffset EndTime { get; set; }

        /// <summary>
        /// Query duration
        /// </summary>
        [JsonProperty]
        public double DurationSeconds => (this.EndTime - this.StartTime).TotalSeconds;

        /// <summary>
        /// DbMoniker
        /// </summary>
        [JsonProperty]
        public string DbMoniker { get; set; }

        /// <summary>
        /// Queue collection id
        /// </summary>
        [JsonProperty]
        public string CollectionId { get; set; }

        /// <summary>
        /// Query request charge
        /// </summary>
        [JsonProperty]
        public double RequestCharge { get; set; }

        /// <summary>
        /// Request charge per second (average)
        /// </summary>
        [JsonProperty]
        public double RequestChargePerSecond => this.RequestCharge / this.DurationSeconds;

        /// <summary>
        /// Number of db throttling retries
        /// </summary>
        [JsonProperty]
        public int Retries { get; set; }

        /// <summary>
        /// GetMaxItem iterations
        /// </summary>
        [JsonProperty]
        public int Iterations { get; set; }
    }
}
