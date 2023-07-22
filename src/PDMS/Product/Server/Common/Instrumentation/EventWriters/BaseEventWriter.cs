namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation
{
    using System.Diagnostics.Tracing;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.Telemetry;

    /// <summary>
    /// Base class for all event writers.
    /// </summary>
    /// <typeparam name="T">The event writer's type.</typeparam>
    public abstract class BaseEventWriter<T> : IEventWriter<T>
    {
        /// <summary>
        /// The log to write events to.
        /// </summary>
        private readonly ILogger<Base> log;

        /// <summary>
        /// The session properties.
        /// </summary>
        private readonly SessionProperties properties;

        /// <summary>
        /// The logger.
        /// </summary>
        protected readonly PrivacyServices.Common.Azure.ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseEventWriter{T}" /> class.
        /// </summary>
        /// <param name="log">The log to write events to.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="properties">The session properties.</param>
        public BaseEventWriter(ILogger<Base> log, PrivacyServices.Common.Azure.ILogger logger, SessionProperties properties)
        {
            this.log = log;
            this.logger = logger;
            this.properties = properties;
        }

        /// <summary>
        /// Given a specific payload, write a corresponding event.
        /// </summary>
        /// <param name="componentName">Name of caller component class.</param>
        /// <param name="data">The data to log.</param>
        /// <param name="level">The level to log. By default, Debug and Verbose levels are ignored.</param>
        /// <param name="options">Additional event options.</param>
        public abstract void WriteEvent(string componentName, T data, EventLevel level = EventLevel.Informational, EventOptions options = EventOptions.None);

        /// <summary>
        /// Sets common properties for incoming events and logs the event to the telemetry stream.
        /// </summary>
        /// <typeparam name="TEvent">The SLL event type.</typeparam>
        /// <param name="sllEvent">The event to log.</param>
        /// <param name="level">The level at which to log the event.</param>
        /// <param name="options">The log options.</param>
        protected void LogEvent<TEvent>(TEvent sllEvent, EventLevel level = EventLevel.Informational, EventOptions options = EventOptions.None)
            where TEvent : Microsoft.Telemetry.Base
        {
            this.log.Write(this.properties, sllEvent, level, options);
        }
    }
}