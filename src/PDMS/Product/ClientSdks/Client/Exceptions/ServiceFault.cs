namespace Microsoft.PrivacyServices.DataManagement.Client
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    using Newtonsoft.Json;

    /// <summary>
    /// An exception that is thrown when there is an unexpected issue with the service.
    /// </summary>
    [Serializable]
    public class ServiceFault : BaseException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceFault" /> class.
        /// </summary>
        /// <param name="result">The result that had this error.</param>
        public ServiceFault(IHttpResult result)
            : this(result, BaseException.ParseResponse(result))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceFault" /> class.
        /// </summary>
        /// <param name="result">The result that had this error.</param>
        /// <param name="responseError">The parsed response error data.</param>       
        public ServiceFault(IHttpResult result, ResponseError responseError)
            : base(result, responseError)
        {
            if (responseError.InnerError != null)
            {
                this.InnerException = new ExceptionError(responseError.InnerError);
            }
        }

        /// <summary>
        /// Gets the strongly typed InnerError data.
        /// </summary>
        [JsonProperty]
        public new ExceptionError InnerException { get; private set; }

        /// <summary>
        /// Required by ISerializable.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The serialization context.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// An inner error that contains exception specific data.
        /// </summary>
        public sealed class ExceptionError : ResponseError.SubError
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ExceptionError" /> class.
            /// </summary>
            /// <param name="error">The parsed error data.</param>
            internal ExceptionError(ResponseError.SubError error) : base(error)
            {
                if (error.Data.TryGetValue("message", out object message))
                {
                    this.Message = message as string;
                }

                if (error.Data.TryGetValue("innerException", out object value))
                {
                    if (value is IDictionary<string, object> exn)
                    {
                        this.InnerException = new ExceptionError(new ResponseError.SubError(exn));
                    }
                }
            }

            /// <summary>
            /// Gets the error message.
            /// </summary>
            [JsonProperty]
            public string Message { get; private set; }

            /// <summary>
            /// Gets the inner exception data if available.
            /// </summary>
            [JsonProperty]
            public ExceptionError InnerException { get; private set; }
        }
    }
}