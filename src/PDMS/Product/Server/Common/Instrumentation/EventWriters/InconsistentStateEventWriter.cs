namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation
{
    using System.Diagnostics.Tracing;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.Telemetry;

    /// <summary>
    /// Logs the InconsistentState event as an SLL event.
    /// </summary>
    public class InconsistentStateEventWriter : BaseEventWriter<InconsistentState>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InconsistentStateEventWriter" /> class.
        /// </summary>
        /// <param name="log">The log to write events to.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="properties">The session properties.</param>
        public InconsistentStateEventWriter(ILogger<Base> log, PrivacyServices.Common.Azure.ILogger logger, SessionProperties properties) : base(log, logger, properties)
        {
        }

        /// <summary>
        /// Given a specific payload, write a corresponding event.
        /// </summary>
        /// <param name="componentName">Name of caller component class.</param>
        /// <param name="data">The data to log.</param>
        /// <param name="level">The level to log. By default, Debug and Verbose levels are ignored.</param>
        /// <param name="options">Additional event options.</param>
        public override void WriteEvent(string componentName, InconsistentState data, EventLevel level = EventLevel.Informational, EventOptions options = EventOptions.None)
        {
            var sllEvent = new InconsistentStateEvent();
            sllEvent.instanceType = data.Type;
            sllEvent.message = data.Message;
            sllEvent.data = Serialization.Serialize(data.Data);

            this.LogEvent(sllEvent, level, options);
            this.logger.Information(componentName, sllEvent.message);
        }
    }
}