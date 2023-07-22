namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation
{
    /// <summary>
    /// Determines how an event should be logged.
    /// </summary>
    public enum EventOptions
    {
        /// <summary>
        /// No special options.
        /// </summary>
        None,

        /// <summary>
        /// Logs the event in real time.
        /// </summary>
        RealTime,

        /// <summary>
        /// Marks the event as sensitive.
        /// </summary>
        Sensitive
    }
}
