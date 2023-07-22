namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation
{
    using System.Diagnostics.Tracing;

    /// <summary>
    /// The log interface is the generic interface that wraps a specific instrumentation library.
    /// EventWriters and SessionWriters will describe the business data types that they receive.
    /// Internally these writers convert that into an instrumentation event and pass that event
    /// to the ILog functions.
    /// </summary>
    /// <remarks>
    /// TBase is required due to how SLL works. 
    /// SLL will only log the properties associated with the explicit compile time type.
    /// For this reason, you cannot call Write{T} with a base type. If you do, it will only log properties for the base type.
    /// By including TBase in the ILog interface, we can avoid a direct dependency on SLL,
    /// but we can also ensure that the SLL implementation of the interface is restricted to only SLL base types.
    /// </remarks>
    /// <typeparam name="TBase">The base event type for all instrumentation events.</typeparam>
    public interface ILogger<TBase>
    {
        /// <summary>
        /// Write a specific instrumentation type event to the telemetry stream.
        /// </summary>
        /// <typeparam name="T">The specific event type to log.</typeparam>
        /// <param name="properties">Any additional session properties that were set.</param>
        /// <param name="event">The event data.</param>
        /// <param name="level">The level to log.</param>
        /// <param name="options">Additional event options.</param>
        /// <param name="cv">An override to the cv value available on the session properties.</param>
        void Write<T>(SessionProperties properties, T @event, EventLevel level, EventOptions options, string cv = null) where T : TBase;
    }
}
