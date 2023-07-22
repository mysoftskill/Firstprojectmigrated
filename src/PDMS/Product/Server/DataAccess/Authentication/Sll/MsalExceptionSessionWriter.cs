namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Authentication
{
    using Microsoft.Identity.Client;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.Telemetry;

    /// <summary>
    /// Logs an SLL event for <c>AdalExceptions</c> from Active Directory authentication.
    /// </summary>
    public class MsalExceptionSessionWriter : BaseSessionWriter<MsalException>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MsalExceptionSessionWriter" /> class.
        /// </summary>
        /// <param name="log">The log object.</param>
        /// <param name="properties">The session properties.</param>
        public MsalExceptionSessionWriter(ILogger<Base> log, SessionProperties properties) : base(log, properties)
        {
        }

        /// <summary>
        /// The required function from the ISessionWriter interface.
        /// Derived classes must implement the appropriate mapping from the TData type into an SLL data type.
        /// </summary>
        /// <param name="status">The session status.</param>
        /// <param name="name">The session name.</param>
        /// <param name="totalMilliseconds">The session duration.</param>
        /// <param name="cv">The snapped CV value when the session was started.</param>
        /// <param name="data">The original data.</param>
        public override void WriteDone(SessionStatus status, string name, long totalMilliseconds, string cv, MsalException data)
        {
            var sllEvent = new MsalExceptionEvent();
            sllEvent.message = data.Message;
            sllEvent.stackTrace = data.StackTrace;

            this.LogOutGoingEvent(sllEvent, status, name, totalMilliseconds, cv, data.ErrorCode);
        }        
    }
}