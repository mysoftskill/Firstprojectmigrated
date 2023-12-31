namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications
{
    /// <summary>
    /// Published when an agent has reported soft delete (or non-use).
    /// </summary>
    public class CommandSoftDeleteEvent : CommandLifecycleEvent
    {
        public const string Name = "CommandSoftDelete";

        /// <summary>
        /// Gets the name of this event.
        /// </summary>
        protected internal override string EventName => Name;

        /// <summary>
        /// List of fatal exception information
        /// </summary>
        public string NonTransientExceptions { get; set; }

        /// <inheritdoc />
        public override void Process(ICommandLifecycleEventProcessor processor)
        {
            processor.Process(this);
        }
    }
}
