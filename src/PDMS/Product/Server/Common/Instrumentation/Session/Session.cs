namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation
{
    using System;

    /// <summary>
    /// The SLL implementation of the ISession interface.
    /// It logs data agnostic Done events using the EmptyEvent type.
    /// </summary>
    public class Session : BaseSession
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Session" /> class.
        /// </summary>
        /// <param name="sessionWriterFactory">A factory to find session writer instances.</param>
        /// <param name="name">The name of the session.</param>
        /// <param name="sessionType">The type of the session.</param>
        /// <param name="sessionProperties">The session properties.</param>
        public Session(ISessionWriterFactory sessionWriterFactory, string name, SessionType sessionType, SessionProperties sessionProperties) : base(sessionWriterFactory, sessionType, name, sessionProperties)
        {
        }
        
        /// <summary>
        /// Logs the completed session information.
        /// </summary>
        /// <param name="status">The session status.</param>
        /// <param name="name">The session name.</param>
        /// <param name="totalMilliseconds">The session duration.</param>
        protected override void Done(SessionStatus status, string name, long totalMilliseconds)
        {
            SessionWriterFactory.WriteDone<Exception>(this.SessionType, status, name, totalMilliseconds, this.CorrelationVector, null);
        }       
    }
}
