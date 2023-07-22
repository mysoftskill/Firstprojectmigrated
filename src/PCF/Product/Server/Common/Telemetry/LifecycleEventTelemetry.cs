namespace Microsoft.PrivacyServices.CommandFeed.Service.Common.Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Newtonsoft.Json;

    /// <summary>
    /// Command Queue checkpoint document aka Delta
    /// </summary>
    public class LifecycleEventTelemetry
    {
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
        /// AssetgroupId.
        /// </summary>
        [JsonProperty]
        public PrivacyCommandType CommandType { get; set; }

        /// <summary>
        /// CommandId.
        /// </summary>
        [JsonProperty]
        public CommandId CommandId { get; set; }

        /// <summary>
        /// Timestamp.
        /// </summary>
        [JsonProperty]
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Command lifecycle (checkpoint) event type
        /// </summary>
        [JsonProperty]
        public LifecycleEventType EventType { get; set; }

        /// <summary>
        /// Number of commands
        /// </summary>
        [JsonProperty]
        public int Count { get; set; }

        /// <summary>
        /// Aggregate lifecycle events.
        /// </summary>
        public static List<LifecycleEventTelemetry> Aggregate(List<LifecycleEventTelemetry> events)
        {
            if (!events.Any())
            {
                return new List<LifecycleEventTelemetry>();
            }

            // Aggregate
            var aggregatedEvents = (from c in events
                                    group c by new
                                    {
                                        c.AgentId,
                                        c.AssetGroupId,
                                        c.CommandId,
                                        c.CommandType,
                                        c.EventType,
                                    }
                             into gcs
                                    select new LifecycleEventTelemetry()
                                    {
                                        AgentId = gcs.Key.AgentId,
                                        AssetGroupId = gcs.Key.AssetGroupId,
                                        CommandId = gcs.Key.CommandId,
                                        CommandType = gcs.Key.CommandType,
                                        EventType = gcs.Key.EventType,
                                        Count = gcs.Select(x => x.Count).Sum(),
                                        Timestamp = DateTimeOffset.UtcNow,
                                    }).ToList();

            return aggregatedEvents;
        }
    }
}
