namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation
{
    using System.Diagnostics.Tracing;
    
    /// <summary>
    /// Extension functions for the ServiceRequest class.
    /// </summary>
    public static class ServiceRequestExtensions
    {
        /// <summary>
        /// Sets the appropriate event fields based on the session status.
        /// </summary>
        /// <param name="baseData">The event.</param>
        /// <param name="status">The session status.</param>
        public static void SetSessionStatus(this Ms.Qos.IncomingServiceRequest baseData, SessionStatus status)
        {
            baseData.requestStatus = ConvertStatus(status);
            baseData.succeeded = HasSucceeded(status);
        }

        /// <summary>
        /// Sets the appropriate event fields based on the session status.
        /// </summary>
        /// <param name="baseData">The event.</param>
        /// <param name="status">The session status.</param>
        public static void SetSessionStatus(this Ms.Qos.OutgoingServiceRequest baseData, SessionStatus status)
        {
            baseData.requestStatus = ConvertStatus(status);
            baseData.succeeded = HasSucceeded(status);
        }

        /// <summary>
        /// Converts the session status into a corresponding event level.
        /// </summary>
        /// <param name="status">The original value.</param>
        /// <returns>The converted value.</returns>
        public static EventLevel ToEventLevel(this SessionStatus status)
        {
            switch (status)
            {
                case SessionStatus.Success:
                    return EventLevel.Informational;
                case SessionStatus.Error:
                    return EventLevel.Warning;
                case SessionStatus.Fault:
                    return EventLevel.Error;
                default:
                    return EventLevel.Verbose;
            }
        }

        /// <summary>
        /// Converts the session status into the SLL data type.
        /// </summary>
        /// <param name="status">The original value.</param>
        /// <returns>The converted value.</returns>
        private static Ms.Qos.ServiceRequestStatus ConvertStatus(SessionStatus status)
        {
            switch (status)
            {
                case SessionStatus.Success:
                    return Ms.Qos.ServiceRequestStatus.Success;
                case SessionStatus.Error:
                    return Ms.Qos.ServiceRequestStatus.CallerError;
                case SessionStatus.Fault:
                    return Ms.Qos.ServiceRequestStatus.ServiceError;
                default:
                    return Ms.Qos.ServiceRequestStatus.Unknown;
            }
        }

        /// <summary>
        /// Categorizes the session status as a success or failure.
        /// </summary>
        /// <param name="status">The original value.</param>
        /// <returns>The converted value.</returns>
        private static bool HasSucceeded(SessionStatus status)
        {
            switch (status)
            {
                case SessionStatus.Success:
                    return true;
                default:
                    return false;
            }
        }
    }
}
