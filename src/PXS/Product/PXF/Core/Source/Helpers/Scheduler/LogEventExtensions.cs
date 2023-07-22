// -------------------------------------------------------------------------
// <copyright>
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Scheduler
{
    using System;
    using Ms.Qos;
    using Common.Logging;

    /// <summary>
    /// Extension methods for SLL events
    /// </summary>
    public static class LogEventExtensions
    {
        /// <summary>
        /// Logs exception details
        /// </summary>
        /// <param name="logEvent">Log event instance</param>
        /// <param name="ex">Exception</param>
        /// <param name="callerError">True if this is a caller error (4xx class HTTP status), false if this is a service issue</param>
        /// <param name="unhandledError">True if this error is NOT a handled/expected exception type for this request (i.e. true if this was caught in the last chance exception handler)</param>
        public static void LogException(this SchedulerIncomingServiceRequest logEvent, Exception ex, bool callerError, bool unhandledError)
        {
            logEvent.ErrorDetails = ExceptionToErrorDetails(ex, unhandledError);
            logEvent.baseData.requestStatus = callerError ? ServiceRequestStatus.CallerError : ServiceRequestStatus.ServiceError;
        }

        /// <summary>
        /// Logs exception details
        /// </summary>
        /// <param name="logEvent">Log event instance</param>
        /// <param name="ex">Exception</param>
        /// <param name="unhandledError">True if this error is NOT a handled/expected exception type for this request (i.e. true if this was caught in the last chance exception handler)</param>
        public static void LogException(this SchedulerOutgoingRequestEvent logEvent, Exception ex, bool unhandledError)
        {
            logEvent.ErrorDetails = ExceptionToErrorDetails(ex, unhandledError);
            logEvent.baseData.requestStatus = ServiceRequestStatus.ServiceError;
        }

        private static ErrorDetails ExceptionToErrorDetails(Exception ex, bool unhandledError)
        {
            return new ErrorDetails
            {
                ExceptionType = ex.GetType().ToString(),
                StackTrace = ex.StackTrace,
                ErrorMessage = ex.Message,
                InnerExceptionDetail = (ex.InnerException != null) ? ex.InnerException.ToString() : string.Empty,
                UnexpectedException = unhandledError,
            };
        }
    }
}
