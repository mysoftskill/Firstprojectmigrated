namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications
{
    using System;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Base class for command lifecycle events.
    /// </summary>
    public abstract class CommandLifecycleEvent
    {
        internal CommandLifecycleEvent()
        {
        }

        /// <summary>
        /// Gets the type of this lifecycle event. Used for deserialization.
        /// </summary>
        [JsonIgnore]
        protected internal abstract string EventName { get; }

        /// <summary>
        /// The command ID.
        /// </summary>
        public CommandId CommandId { get; set; }

        /// <summary>
        /// The agent ID.
        /// </summary>
        public AgentId AgentId { get; set; }

        /// <summary>
        /// The asset group ID.
        /// </summary>
        public AssetGroupId AssetGroupId { get; set; }

        /// <summary>
        /// The asset group qualifier.
        /// </summary>
        public string AssetGroupQualifier { get; set; }

        /// <summary>
        /// The type of command.
        /// </summary>
        public PrivacyCommandType CommandType { get; set; }

        /// <summary>
        /// The time at which this event was created.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// The time at which the command for this event was created.
        /// This value is important to determine if the command has already expired from Command History.
        /// </summary>
        public DateTimeOffset? CommandCreationTime { get; set; }

        /// <summary>
        /// The audit log action.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public AuditLogCommandAction AuditLogCommandAction { get; set; } = AuditLogCommandAction.None;

        /// <summary>
        /// Processes the current event using the given processor.
        /// </summary>
        public abstract void Process(ICommandLifecycleEventProcessor processor);
    }
}
