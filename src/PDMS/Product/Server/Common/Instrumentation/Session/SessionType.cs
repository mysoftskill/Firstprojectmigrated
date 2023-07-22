namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation
{
    /// <summary>
    /// Classifies the type of a session.
    /// </summary>
    public enum SessionType
    {
        /// <summary>
        /// An incoming session.
        /// </summary>
        Incoming,

        /// <summary>
        /// An outgoing session.
        /// </summary>
        Outgoing,

        /// <summary>
        /// An internal session.
        /// </summary>
        Internal
    }
}
