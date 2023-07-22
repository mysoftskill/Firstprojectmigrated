namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation
{
    using System.Diagnostics.Tracing;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.Telemetry;

    /// <summary>
    /// Logs strings as generic trace messages.
    /// </summary>
    public class SuppressedExceptionEventWriter : BaseEventWriter<SuppressedException>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SuppressedExceptionEventWriter" /> class.
        /// </summary>
        /// <param name="log">The log to write events to.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="properties">The session properties.</param>
        public SuppressedExceptionEventWriter(ILogger<Base> log, PrivacyServices.Common.Azure.ILogger logger, SessionProperties properties)
            : base(log, logger, properties)
        {
        }

        /// <summary>
        /// Logs a suppressed exception event that contains the given exception data.
        /// </summary>
        /// <param name="componentName">Name of caller component class.</param>
        /// <param name="data">The original data.</param>
        /// <param name="level">The event level.</param>
        /// <param name="options">The event options.</param>
        public override void WriteEvent(string componentName, SuppressedException data, EventLevel level = EventLevel.Informational, EventOptions options = EventOptions.None)
        {
            var sllEvent = new SuppressedExceptionEvent();
            sllEvent.action = data.Name;
            sllEvent.code = data.Value.GetName();
            sllEvent.message = data.Value.Message;
            sllEvent.stackTrace = data.Value.StackTrace;
            sllEvent.innerException = ExceptionSessionWriter.CreateInnerException(data.Value.InnerException);

            this.LogEvent(sllEvent, level, options);

            var message = $"Message: {sllEvent.message} StackTrace: {sllEvent.stackTrace} InnerException: {sllEvent.innerException}";
            this.logger.Log(IfxTraceLogger.GetIfxTracingLevel(level), componentName, message);
        }
    }
}
