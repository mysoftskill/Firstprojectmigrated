namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation
{
    using System.Diagnostics.Tracing;

    /// <summary>
    /// Determines how to log events to the telemetry stream.
    /// </summary>
    /// <typeparam name="TData">The specific data type that the concrete writer can log.</typeparam>
    public interface IEventWriter<TData>
    {
        /// <summary>
        /// Given a specific payload, write a corresponding event.
        /// </summary>
        /// <param name="componentName">Name of caller component class.</param>
        /// <param name="data">The data to log.</param>
        /// <param name="level">The level to log. By default, Debug and Verbose levels are ignored.</param>
        /// <param name="options">Additional event options.</param>
        void WriteEvent(string componentName, TData data, EventLevel level = EventLevel.Informational, EventOptions options = EventOptions.None);
    }
}
