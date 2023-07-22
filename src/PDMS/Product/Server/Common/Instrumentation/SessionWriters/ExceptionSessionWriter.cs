namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation
{
    using System;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.Telemetry;

    /// <summary>
    /// Logs any generic exception as an SLL event.
    /// Covers all session types and session statuses.
    /// </summary>
    public class ExceptionSessionWriter : BaseSessionWriter<Exception>
    {
        /// <summary>
        /// The session type that needs to be logged.
        /// </summary>
        private readonly SessionType sessionType;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionSessionWriter" /> class.
        /// </summary>
        /// <param name="log">The log object.</param>
        /// <param name="properties">The session properties.</param>
        /// <param name="sessionType">The session type.</param>
        public ExceptionSessionWriter(ILogger<Base> log, SessionProperties properties, SessionType sessionType)
            : base(log, properties)
        {
            this.sessionType = sessionType;
        }

        /// <summary>
        /// Writes an appropriate SLL exception event with the given exception data.
        /// </summary>
        /// <param name="status">The session status.</param>
        /// <param name="name">The session name.</param>
        /// <param name="totalMilliseconds">The session duration.</param>
        /// <param name="cv">The snapped CV value when the session was started.</param>
        /// <param name="data">The exception data to log.</param>
        public override void WriteDone(SessionStatus status, string name, long totalMilliseconds, string cv, Exception data)
        {
            switch (this.sessionType)
            {
                case SessionType.Incoming:
                    this.LogIncomingEvent(status, name, totalMilliseconds, cv, data);
                    break;
                case SessionType.Internal:
                    this.LogInternalEvent(status, name, totalMilliseconds, cv, data);
                    break;
                case SessionType.Outgoing:
                    this.LogOutGoingEvent(status, name, totalMilliseconds, cv, data);
                    break;
            }
        }

        /// <summary>
        /// Creates and sets the inner exception data of the SLL event.        
        /// </summary>
        /// <remarks>
        /// This function is recursive, but that is ok because exceptions do not typically have a large number of inner exceptions.
        /// </remarks>
        /// <param name="data">The exception data to store.</param>
        /// <param name="depth">The recursive depth.</param>
        /// <returns>The mapped object.</returns>
        internal static InnerException CreateInnerException(Exception data, int depth = 0)
        {
            if (data != null && depth < 5)
            {
                var innerException = new InnerException();
                innerException.code = data.GetName();
                innerException.message = data.Message;
                innerException.innerException = CreateInnerException(data.InnerException, depth++);
                return innerException;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Logs the exception data as an incoming event.
        /// </summary>
        /// <param name="status">The session status.</param>
        /// <param name="name">The session name.</param>
        /// <param name="totalMilliseconds">The session duration.</param>
        /// <param name="cv">The snapped CV value when the session was started.</param>
        /// <param name="data">The exception data to log.</param>
        private void LogIncomingEvent(SessionStatus status, string name, long totalMilliseconds, string cv, Exception data)
        {
            if (status == SessionStatus.Success)
            {
                var sllEvent = new BaseIncomingSucessEvent();
                base.LogIncomingEvent(sllEvent, status, name, totalMilliseconds, cv, "Success");
            }
            else
            {
                var sllEvent = new BaseIncomingExceptionEvent();
                sllEvent.message = data?.Message;
                sllEvent.stackTrace = data?.StackTrace;
                sllEvent.innerException = CreateInnerException(data?.InnerException);
                base.LogIncomingEvent(sllEvent, status, name, totalMilliseconds, cv, data.GetName());
            }
        }

        /// <summary>
        /// Logs the exception data as an internal event.
        /// </summary>
        /// <param name="status">The session status.</param>
        /// <param name="name">The session name.</param>
        /// <param name="totalMilliseconds">The session duration.</param>
        /// <param name="cv">The snapped CV value when the session was started.</param>
        /// <param name="data">The exception data to log.</param>
        private void LogInternalEvent(SessionStatus status, string name, long totalMilliseconds, string cv, Exception data)
        {
            if (status == SessionStatus.Success)
            {
                var sllEvent = new BaseIncomingSucessEvent();
                base.LogInternalEvent(sllEvent, status, name, totalMilliseconds, cv, "Success");
            }
            else
            {
                var sllEvent = new BaseIncomingExceptionEvent();
                sllEvent.message = data?.Message;
                sllEvent.stackTrace = data?.StackTrace;
                sllEvent.innerException = CreateInnerException(data?.InnerException);
                base.LogInternalEvent(sllEvent, status, name, totalMilliseconds, cv, data.GetName());
            }
        }

        /// <summary>
        /// Logs the exception data as an outgoing event.
        /// </summary>
        /// <param name="status">The session status.</param>
        /// <param name="name">The session name.</param>
        /// <param name="totalMilliseconds">The session duration.</param>
        /// <param name="cv">The snapped CV value when the session was started.</param>
        /// <param name="data">The exception data to log.</param>
        private void LogOutGoingEvent(SessionStatus status, string name, long totalMilliseconds, string cv, Exception data)
        {
            if (status == SessionStatus.Success)
            {
                var sllEvent = new BaseOutgoingSucessEvent();
                base.LogOutGoingEvent(sllEvent, status, name, totalMilliseconds, cv, "Success");
            }
            else
            {
                var sllEvent = new BaseOutgoingExceptionEvent();
                sllEvent.message = data?.Message;
                sllEvent.stackTrace = data?.StackTrace;
                sllEvent.innerException = CreateInnerException(data?.InnerException);
                base.LogOutGoingEvent(sllEvent, status, name, totalMilliseconds, cv, data.GetName());
            }
        }
    }
}
