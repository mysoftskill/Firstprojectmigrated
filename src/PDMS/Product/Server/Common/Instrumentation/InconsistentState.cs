namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation
{
    /// <summary>
    /// Defines an event payload that should be logged whenever the storage state does not match an expected set of assertions.
    /// </summary>
    public class InconsistentState
    {
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        public string Type { get; set; }
    
        /// <summary>
        /// Gets or sets a friendly message that explains what this event means.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets any custom data that should be logged with this event.
        /// This should be data that is needed to help debug/investigate the issue.
        /// The object will ultimately be serialized to JSON and trimmed if it is too large,
        /// so avoid storing large data sets.
        /// </summary>
        public object Data { get; set; }
    }
}