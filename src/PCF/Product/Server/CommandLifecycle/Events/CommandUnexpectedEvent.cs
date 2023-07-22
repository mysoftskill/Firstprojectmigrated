namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications
{
    /// <summary>
    /// Published when an agent has marked a command as 'Unexpected'.
    /// </summary>
    public class CommandUnexpectedEvent : CommandLifecycleEvent
    {
        public const string Name = "CommandUnexpected";

        /// <summary>
        /// Gets the name of this event.
        /// </summary>
        protected internal override string EventName => Name;

        /// <inheritdoc />
        public override void Process(ICommandLifecycleEventProcessor processor)
        {
            processor.Process(this);
        }
    }
}
