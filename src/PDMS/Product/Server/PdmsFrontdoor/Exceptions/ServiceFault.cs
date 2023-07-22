namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions
{
    using System;
    using System.Net;
    using System.Runtime.Serialization;
    using System.Web;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Newtonsoft.Json;

    /// <summary>
    /// Indicates an unhandled exception in the service.
    /// </summary>
    [Serializable]
    public sealed class ServiceFault : ServiceException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceFault" /> class.
        /// </summary>
        /// <param name="exn">The original exception that was issued.</param>
        public ServiceFault(Exception exn)
            : base(HttpStatusCode.InternalServerError, new ServiceError("ServiceFault", "An unhandled service exception occurred."))
        {
            this.ServiceError.InnerError = new ExceptionError(exn);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceFault" /> class.
        /// Required by ISerializable.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The serialization context.</param>
        public ServiceFault(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// An inner error that contains exception specific data.
        /// </summary>
        public sealed class ExceptionError : InnerError
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ExceptionError" /> class.
            /// </summary>
            /// <param name="exn">
            /// </param>
            public ExceptionError(Exception exn) : base(exn.GetName())
            {
                this.Message = HttpUtility.HtmlEncode(exn.Message); // Encode the error message to avoid XSS attack as this message is sent to end user.

                if (exn.InnerException != null)
                {
                    this.InnerException = new ExceptionError(exn.InnerException);
                }
                else
                {
                    this.StackTrace = exn.StackTrace;
                }
            }

            /// <summary>
            /// Gets or sets the error message.
            /// </summary>
            [JsonProperty(PropertyName = "message", NullValueHandling = NullValueHandling.Ignore)]
            public string Message { get; set; }

            /// <summary>
            /// Gets or sets the stack trace. Will not be serialized in the response, 
            /// but the data is persisted so that it can be logged to the telemetry stream.
            /// </summary>
            [JsonIgnore]
            public string StackTrace { get; set; }

            /// <summary>
            /// Gets or sets the inner exception data if available.
            /// </summary>
            [JsonProperty(PropertyName = "innerException", NullValueHandling = NullValueHandling.Ignore)]
            public ExceptionError InnerException { get; set; }
        }
    }
}
