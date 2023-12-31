namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications
{
    using System;

    /// <summary>
    /// Published when a command has first started.
    /// </summary>
    public class CommandStartedEvent : CommandLifecycleEvent
    {
        public const string Name = "CommandStarted";

        /// <summary>
        /// Gets the name of this event.
        /// </summary>
        protected internal override string EventName => Name;

        /// <summary>
        /// The final destination to write exports to.
        /// </summary>
        public Uri FinalExportDestinationUri { get; set; }

        /// <summary>
        /// The staging destination this command should export to.
        /// </summary>
        public Uri ExportStagingDestinationUri { get; set; }

        /// <summary>
        /// The staging path this command should export to.
        /// </summary>
        public string ExportStagingPath { get; set; }

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
