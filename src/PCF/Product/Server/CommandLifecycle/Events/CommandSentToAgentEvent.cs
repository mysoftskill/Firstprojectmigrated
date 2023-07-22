namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications
{
    /// <summary>
    /// Published when a command was sent to agent
    /// </summary>
    public class CommandSentToAgentEvent : CommandLifecycleEvent
    {
        public const string Name = "CommandSentToAgent";

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
