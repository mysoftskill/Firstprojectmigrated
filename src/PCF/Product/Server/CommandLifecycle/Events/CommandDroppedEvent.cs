namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications
{
    /// <summary>
    /// Published when a command is dropped due to not appliable
    /// </summary>
    public class CommandDroppedEvent : CommandLifecycleEvent
    {
        public const string Name = "CommandDropped";

        /// <inheritdoc />
        protected internal override string EventName => Name;

        /// <summary>
        /// The reason why the command is not applicable
        /// </summary>
        public string NotApplicableReasonCode { get; set; }

        /// <summary>
        /// The AssetGroup info config cosmos stream name.
        /// </summary>
        public string AssetGroupStreamName { get; set; }

        /// <summary>
        /// The Variant info config cosmos stream name.
        /// </summary>
        public string VariantStreamName { get; set; }

        /// <inheritdoc />
        public override void Process(ICommandLifecycleEventProcessor processor)
        {
            processor.Process(this);
        }
    }
}
