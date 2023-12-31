namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Published when a command has finished.
    /// </summary>
    public class CommandCompletedEvent : CommandLifecycleEvent
    {
        public const string Name = "CommandCompleted";

        /// <summary>
        /// Gets the name of this event.
        /// </summary>
        protected internal override string EventName => Name;

        /// <summary>
        /// A set of variants claimed for the given command.
        /// </summary>
        public string[] ClaimedVariantIds { get; set; }

        /// <summary>
        /// Indicates if command is suppressed by a variant
        /// </summary>
        public bool IgnoredByVariant { get; set; }

        /// <summary>
        /// Indicates if the agent anonymized the data instead of deleting it.
        /// </summary>
        public bool Delinked { get; set; }

        /// <summary>
        /// The reported row count.
        /// </summary>
        public int AffectedRows { get; set; }

        /// <summary>
        /// List of fatal exception information
        /// </summary>
        public string NonTransientExceptions { get; set; }

        /// <summary>
        /// Indicates whether this command was force-completed before the agent acknowledged it as such.
        /// </summary>
        public bool ForceCompleted => this.ForceCompleteReasonCode != null;

        /// <summary>
        /// The force complete reason code.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public ForceCompleteReasonCode? ForceCompleteReasonCode { get; set; }

        /// <summary>
        /// True if command completed by PCF
        /// </summary>
        public bool CompletedByPcf { get; set; }

        /// <inheritdoc />
        public override void Process(ICommandLifecycleEventProcessor processor)
        {
            processor.Process(this);
        }
    }

    /// <summary>
    /// Enumerates the reasons a command is forced to a complete state.
    /// </summary>
    public enum ForceCompleteReasonCode
    {
        ForceCompleteFromPartnerTestPage = 0,
        ForceCompleteFromAgeoutTimer = 1
    }
}
