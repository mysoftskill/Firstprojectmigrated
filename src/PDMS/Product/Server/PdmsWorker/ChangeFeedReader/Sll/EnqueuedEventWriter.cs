namespace Microsoft.PrivacyServices.DataManagement.Worker.ChangeFeedReader.Sll
{
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Tracing;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.Telemetry;

    /// <summary>
    /// Logs the events.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class EnqueuedEventWriter : BaseEventWriter<EnqueuedMessageEvent>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnqueuedEventWriter" /> class.
        /// </summary>
        /// <param name="log">The log to write events to.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="properties">The session properties.</param>
        public EnqueuedEventWriter(ILogger<Base> log, PrivacyServices.Common.Azure.ILogger logger, SessionProperties properties) : base(log, logger, properties)
        {
        }

        /// <summary>
        /// Writes the event.
        /// </summary>
        /// <param name="componentName">Name of caller component class.</param>
        /// <param name="sllEvent">The event data.</param>
        /// <param name="level">The event level.</param>
        /// <param name="options">Logging options.</param>
        public override void WriteEvent(string componentName, EnqueuedMessageEvent sllEvent, EventLevel level = EventLevel.Informational, EventOptions options = EventOptions.None)
        {
            this.LogEvent(sllEvent, level, options);

            var message = $"Enqueued Id: {sllEvent.id} lsn: {sllEvent.lsn} storageUri: {sllEvent.storageUri}";
            this.logger.Log(IfxTraceLogger.GetIfxTracingLevel(level), componentName, message);
        }
    }
}
