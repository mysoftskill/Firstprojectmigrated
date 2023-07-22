namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi
{
    using System;
    using System.Linq;

    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions;
    using Microsoft.Telemetry;

    using Newtonsoft.Json;

    /// <summary>
    /// Converts service errors that are caught by the WebAPI ExceptionHandler into SLL events.
    /// </summary>
    public class ServiceErrorSessionWriter : BaseSessionWriter<Tuple<OperationMetadata, ServiceError>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceErrorSessionWriter" /> class.
        /// </summary>
        /// <param name="log">The log object.</param>
        /// <param name="properties">The session properties.</param>
        public ServiceErrorSessionWriter(ILogger<Base> log, SessionProperties properties)
            : base(log, properties)
        {
        }

        /// <summary>
        /// Converts the error data into an SLL event and logs it.
        /// </summary>
        /// <param name="status">The session status.</param>
        /// <param name="name">The session name.</param>
        /// <param name="totalMilliseconds">The session duration.</param>
        /// <param name="cv">The snapped CV value when the session was started.</param>
        /// <param name="data">The error data.</param>
        public override void WriteDone(SessionStatus status, string name, long totalMilliseconds, string cv, Tuple<OperationMetadata, ServiceError> data)
        {
            var operationMetadata = data.Item1;
            var serviceError = data.Item2;
            Action<IncomingApiServiceErrorEvent> fillStandardFields = sllEvent =>
            {
                sllEvent.baseData.SetOperationMetadata(operationMetadata);
            };

            var innerError = serviceError.InnerError as ServiceFault.ExceptionError;
            if (innerError != null)
            {
                var sllEvent = new IncomingApiServiceFaultEvent();
                sllEvent.message = serviceError.Message;
                sllEvent.stackTrace = innerError.StackTrace;
                sllEvent.innerException = this.CreateInnerException(innerError);
                fillStandardFields(sllEvent);
                this.LogIncomingEvent(sllEvent, status, name, totalMilliseconds, cv, serviceError.ToString());
            }
            else
            {
                var sllEvent = new IncomingApiServiceErrorEvent();
                sllEvent.target = serviceError.Target;
                sllEvent.message = serviceError.Message;
                sllEvent.details = serviceError.Details?.Select(d => new Common.Configuration.Detail() { code = d.Code, message = d.Message, target = d.Target }).ToList();
                sllEvent.innerError = JsonConvert.SerializeObject(serviceError.InnerError);
                fillStandardFields(sllEvent);
                this.LogIncomingEvent(sllEvent, status, name, totalMilliseconds, cv, serviceError.ToString());
            }
        }

        /// <summary>
        /// Converts the inner exception information into corresponding SLL data.
        /// </summary>
        /// <param name="exceptionError">The exception data.</param>
        /// <returns>The SLL data.</returns>
        private InnerException CreateInnerException(ServiceFault.ExceptionError exceptionError)
        {
            if (exceptionError != null && !string.IsNullOrEmpty(exceptionError.Code))
            {
                var innerException = new InnerException();
                innerException.code = exceptionError.Code;
                innerException.message = exceptionError.Message;
                innerException.innerException = this.CreateInnerException(exceptionError.InnerException);
                return innerException;
            }
            else
            {
                return null;
            }
        }
    }
}
