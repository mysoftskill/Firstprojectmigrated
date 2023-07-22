namespace Microsoft.PrivacyServices.CommandFeed.Service.Common.Telemetry
{
    using System;

    /// <summary>
    /// TelemetryLifecycleCheckpoint SLL event info.
    /// </summary>
    public class TelemetryLifecycleCheckpointInfo
    {
        /// <summary>
        /// CheckpointFrequency.
        /// </summary>
        public TimeSpan CheckpointFrequency { get; set; }

        /// <summary>
        /// LastCheckpointTime.
        /// </summary>
        public DateTimeOffset LastCheckpointTime { get; set; }

        /// <summary>
        /// Number of events in the buffer.
        /// </summary>
        public int EventsCount { get; set; }
    }
}
