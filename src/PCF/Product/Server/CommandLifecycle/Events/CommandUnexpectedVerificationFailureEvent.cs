namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications
{
    /// <summary>
    /// Published when an agent has marked a command as 'UnexpectedVerificationFailure'.
    /// </summary>
    public class CommandUnexpectedVerificationFailureEvent : CommandLifecycleEvent
    {
        public const string Name = "CommandUnexpectedVerificationFailure";

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
