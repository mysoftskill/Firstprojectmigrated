namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation
{
    using System.Diagnostics.Tracing;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.Telemetry;

    /// <summary>
    /// This provides a generic fallback mechanism in the event 
    /// that a specific event type is not registered in the Dependency Injection container.
    /// </summary>
    /// <typeparam name="_">This type is irrelevant. The writer will be used for all data types.</typeparam>
    public class EmptyEventWriter<_> : BaseEventWriter<_>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EmptyEventWriter{_}" /> class.
        /// </summary>
        /// <param name="log">The log to write events to.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="properties">The session properties.</param>
        public EmptyEventWriter(ILogger<Base> log, PrivacyServices.Common.Azure.ILogger logger, SessionProperties properties)
            : base(log, logger, properties)
        {
        }

        /// <summary>
        /// Logs a warning event that indicates the data type that is not mapped to a specific event writer.
        /// </summary>
        /// <param name="componentName">Name of caller component class.</param>
        /// <param name="data">The original data.</param>
        /// <param name="level">The event level.</param>
        /// <param name="options">The event options.</param>
        public override void WriteEvent(string componentName, _ data, EventLevel level = EventLevel.Informational, EventOptions options = EventOptions.None)
        {
            var sllEvent = new WriterTypeNotRegisteredEvent();
            sllEvent.writerType = typeof(_).FullName;
            sllEvent.message = "Cannot find a registered IEventWriter interface that has the requested type.";
            this.LogEvent(sllEvent, EventLevel.Warning, EventOptions.None);
            this.logger.Warning(componentName, sllEvent.message);
        }
    }
}
