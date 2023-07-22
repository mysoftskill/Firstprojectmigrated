namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;

    /// <summary>
    /// Defines a property bag representing an outgoing event.
    /// </summary>
    public class OutgoingEvent : OperationEvent
    {
        /// <summary>
        /// Creates a new OutgoingEvent from the given location.
        /// </summary>
        public OutgoingEvent(SourceLocation sourceLocation) : base(sourceLocation)
        {
            this.IncomingOperationName = IncomingEvent.Current?.OperationName ?? "Unknown";
        }

        /// <summary>
        /// The name of the incoming operation that triggered this outgoing event.
        /// </summary>
        public string IncomingOperationName { get; set; }

        /// <summary>
        /// The status code of the outgoing Event
        /// </summary>
        public string StatusCode { get; set; }

        /// <summary>
        /// Logs this event.
        /// </summary>
        public override void Log(ILogger logger)
        {
            logger.Log(this);
        }
    }
}