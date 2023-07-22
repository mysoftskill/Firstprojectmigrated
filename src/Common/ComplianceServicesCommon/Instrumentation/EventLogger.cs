namespace Microsoft.Azure.ComplianceServices.Common.Instrumentation
{
    /// <summary>
    /// Static event logger class.
    /// </summary>
    public static class EventLogger
    {
        /// <summary>
        /// Gets or sets the current logger.
        /// </summary>
        public static IEventLogger Instance
        {
            get;
            set;
        }
   }
}
