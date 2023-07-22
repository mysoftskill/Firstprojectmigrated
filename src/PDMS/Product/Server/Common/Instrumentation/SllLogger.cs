namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Tracing;

    using Microsoft.CommonSchema.Services;
    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Telemetry;

    /// <summary>
    /// A concrete implementation of the ILog interface that logs events to SLL.
    /// </summary>
    /// <remarks>
    /// This class should be created as a singleton. It initializes the cosmos event listener
    /// and that can only happen once or else an exception is thrown.
    /// It is excluded from code coverage because it deals with an external dependency.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    public sealed class SllLogger : ILogger<Base>
    {
        private readonly string cloudLocation;

        /// <summary>
        /// Initializes a new instance of the <see cref="SllLogger" /> class.
        /// </summary>
        public SllLogger()
        {
            cloudLocation = Environment.GetEnvironmentVariable("MONITORING_DATACENTER");
        }

        /// <summary>
        /// Writes the given event data to the telemetry stream.
        /// </summary>
        /// <typeparam name="T">The event data type. This MUST be the most specific concrete type.</typeparam>
        /// <param name="properties">The session properties.</param>
        /// <param name="event">The event data.</param>
        /// <param name="level">The event level.</param>
        /// <param name="options">The event options.</param>
        /// <param name="cv">An override to the cv value available on the session properties.</param>
        public void Write<T>(SessionProperties properties, T @event, EventLevel level = EventLevel.Informational, EventOptions options = EventOptions.None, string cv = null) where T : Base
        {
            @event.Log(level, this.GetLogOption(options), this.FillEnvelope(properties, cv));
        }

        /// <summary>
        /// This adds common properties to the Part A schema of all events.
        /// </summary>
        /// <param name="properties">The session properties.</param>
        /// <param name="cv">An override to the cv value available on the session properties.</param>
        /// <returns>An action that will fill the envelope.</returns>
        private Action<Envelope> FillEnvelope(SessionProperties properties, string cv = null)
        {
            return e =>
            {
                e.cV = cv ?? properties.CV.Get();
                e.SafeUser().id = properties.User;
                e.SafeCloud().location = cloudLocation;
            };
        }

        /// <summary>
        /// Converts the internal EventOptions type into the SLL specific LogOption type.
        /// </summary>
        /// <param name="options">The event options.</param>
        /// <returns>The converted type.</returns>
        private LogOption GetLogOption(EventOptions options)
        {
            switch (options)
            {
                case EventOptions.RealTime:
                    return LogOption.Realtime;
                case EventOptions.Sensitive:
                    return LogOption.Sensitive;
                case EventOptions.None:
                default:
                    return LogOption.None;
            }
        }
    }
}