namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation
{
    using System.Diagnostics.Tracing;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.Telemetry;

    /// <summary>
    /// Logs strings as generic trace messages.
    /// </summary>
    public class StringEventWriter : BaseEventWriter<string>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StringEventWriter" /> class.
        /// </summary>
        /// <param name="log">The log to write events to.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="properties">The session properties.</param>
        public StringEventWriter(ILogger<Base> log, PrivacyServices.Common.Azure.ILogger logger, SessionProperties properties)
            : base(log, logger, properties)
        {
        }

        /// <summary>
        /// Logs a trace event that contains the given string data.
        /// </summary>
        /// <param name="componentName">Name of caller component class.</param>
        /// <param name="data">The original data.</param>
        /// <param name="level">The event level.</param>
        /// <param name="options">The event options.</param>
        public override void WriteEvent(string componentName, string data, EventLevel level = EventLevel.Informational, EventOptions options = EventOptions.None)
        {
            var sllEvent = new TraceEvent();
            sllEvent.message = data;
            this.LogEvent(sllEvent, level, options);
            this.logger.Log(IfxTraceLogger.GetIfxTracingLevel(level), componentName, sllEvent.message);
        }
    }
}
