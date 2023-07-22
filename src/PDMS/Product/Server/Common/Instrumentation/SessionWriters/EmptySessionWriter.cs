namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation
{
    using System.Diagnostics.Tracing;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.Cloud.InstrumentationFramework;
    using Microsoft.Telemetry;

    /// <summary>
    /// This provides a generic fallback mechanism in the event 
    /// that a specific session type is not registered in the Dependency Injection container.
    /// </summary>
    /// <typeparam name="_">This type is irrelevant. The writer will be used for all data types.</typeparam>
    public class EmptySessionWriter<_> : ISessionWriter<_>
    {
        /// <summary>
        /// The log to write events to.
        /// </summary>
        private readonly ILogger<Base> log;

        /// <summary>
        /// The logger.
        /// </summary>
        private readonly PrivacyServices.Common.Azure.ILogger logger;

        /// <summary>
        /// The session properties.
        /// </summary>
        private readonly SessionProperties properties;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmptySessionWriter{_}" /> class.
        /// </summary>
        /// <param name="log">The log to write events to.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="properties">The session properties.</param>
        public EmptySessionWriter(ILogger<Base> log, PrivacyServices.Common.Azure.ILogger logger, SessionProperties properties)
        {
            this.log = log;
            this.logger = logger;
            this.properties = properties;
        }

        /// <summary>
        /// Logs a warning event that indicates the data type that is not mapped to a specific session writer.
        /// </summary>
        /// <param name="status">The session status.</param>
        /// <param name="name">The session name.</param>
        /// <param name="totalMilliseconds">The session duration.</param>
        /// <param name="cv">The snapped CV value when the session was started.</param>
        /// <param name="data">The original data.</param>
        public void WriteDone(SessionStatus status, string name, long totalMilliseconds, string cv, _ data)
        {
            var sllEvent = new WriterTypeNotRegisteredEvent();
            sllEvent.writerType = typeof(_).FullName;
            sllEvent.message = "Cannot find a registered ISessionWriter interface that has the requested type.";
            this.log.Write(this.properties, sllEvent, EventLevel.Warning, EventOptions.None, cv);
            this.logger.Warning(name, sllEvent.message);
        }
    }
}
