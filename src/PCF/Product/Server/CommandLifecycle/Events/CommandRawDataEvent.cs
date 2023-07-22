namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Published when a command is received from PXS
    /// </summary>
    public class CommandRawDataEvent : CommandLifecycleEvent
    {
        public const string Name = "RawCommand";

        /// <summary>
        /// Gets the name of this event.
        /// </summary>
        protected internal override string EventName => Name;

        /// <summary>
        /// The PXS command
        /// </summary>
        public JObject PxsCommand { get; set; }

        /// <summary>
        /// The set of PXS commands. Mutually exclusive with "PxsCommand".
        /// </summary>
        public IReadOnlyList<JObject> PxsCommands { get; set; } = new List<JObject>();

        /// <inheritdoc />
        public override void Process(ICommandLifecycleEventProcessor processor)
        {
            processor.Process(this);
        }
    }
}
