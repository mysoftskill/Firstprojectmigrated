namespace Microsoft.PrivacyServices.CommandFeed.Service.Common.Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Telemetry PCF Commands Lifecycle Events.
    /// </summary>
    public enum LifecycleEventType
    {
        /// <summary>
        /// Default value.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// CommandCompletedEvent.
        /// </summary>
        CommandCompletedEvent = 1,

        /// <summary>
        /// CommandSoftDeleteEvent.
        /// </summary>
        CommandSoftDeleteEvent = 2,

        /// <summary>
        /// CommandStartedEvent.
        /// </summary>
        CommandStartedEvent = 3,

        /// <summary>
        /// CommandSentToAgentEvent.
        /// </summary>
        CommandSentToAgentEvent = 4,

        /// <summary>
        /// CommandPendingEvent.
        /// </summary>
        CommandPendingEvent = 5,

        /// <summary>
        /// CommandFailedEvent.
        /// </summary>
        CommandFailedEvent = 6,

        /// <summary>
        /// CommandUnexpectedEvent.
        /// </summary>
        CommandUnexpectedEvent = 7,

        /// <summary>
        /// CommandVerificationFailedEvent.
        /// </summary>
        CommandVerificationFailedEvent = 8,

        /// <summary>
        /// CommandUnexpectedVerificationFailureEvent.
        /// </summary>
        CommandUnexpectedVerificationFailureEvent = 9,

        /// <summary>
        /// CommandCompletedByPcfEvent
        /// </summary>
        CommandCompletedByPcfEvent = 10,
    }
}
