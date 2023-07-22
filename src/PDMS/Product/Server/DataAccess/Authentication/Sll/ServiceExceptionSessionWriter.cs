namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Authentication
{
    using Microsoft.Graph;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.Telemetry;

    using Error = DataManagement.Common.Configuration.Error;

    /// <summary>
    /// Logs an SLL event for ServiceExceptions from the Microsoft graph.
    /// </summary>
    public class ServiceExceptionSessionWriter : BaseSessionWriter<ServiceException>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceExceptionSessionWriter" /> class.
        /// </summary>
        /// <param name="log">The log object.</param>
        /// <param name="properties">The session properties.</param>
        public ServiceExceptionSessionWriter(ILogger<Base> log, SessionProperties properties) : base(log, properties)
        {
        }

        /// <summary>
        /// Logs the result and duration of the session.
        /// </summary>
        /// <param name="status">How the state should be classified.</param>
        /// <param name="name">The name of the operation.</param>
        /// <param name="totalMilliseconds">How long it took for the operation to complete.</param>
        /// <param name="cv">The snapped CV value when the session was started.</param>
        /// <param name="data">The state data that may be logged for debug purposes.</param>
        public override void WriteDone(SessionStatus status, string name, long totalMilliseconds, string cv, ServiceException data)
        {
            var sllEvent = new MicrosoftGraphExceptionEvent();
            sllEvent.message = data.Message;
            sllEvent.stackTrace = data.StackTrace;
            sllEvent.error = this.CreateError(data.Error);
            
            var innerError = sllEvent.error;

            for (var error = data.Error.InnerError; error != null; error = error.InnerError)
            {
                innerError.innerError = this.CreateError(error);
                innerError = innerError.innerError;
            }
            
            this.LogOutGoingEvent(sllEvent, status, name, totalMilliseconds, cv, data.Error.Code);
        }

        private Error CreateError(Graph.Error error)
        {
            if (error == null)
            {
                return null;
            }
            else
            {
                return new Error
                {
                    code = error.Code,
                    message = error.Message
                };
            }
        }
    }
}