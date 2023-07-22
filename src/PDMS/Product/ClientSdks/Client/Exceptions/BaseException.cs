namespace Microsoft.PrivacyServices.DataManagement.Client
{
    using System;
    using System.Runtime.Serialization;

    using Newtonsoft.Json;

    /// <summary>
    /// An exception that contains core data, which is common to all service responses.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    [Serializable]   
    public abstract class BaseException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseException" /> class.
        /// </summary>
        /// <param name="result">The result that had this error.</param>        
        public BaseException(IHttpResult result)
            : this(result, ParseResponse(result))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseException" /> class.
        /// </summary>
        /// <param name="result">The result that had this error.</param>   
        /// <param name="responseError">The parsed response error data.</param>       
        public BaseException(IHttpResult result, ResponseError responseError)
           : base(responseError == null ? "null" : responseError.Message)
        {
            this.ResponseError = responseError;
            this.Result = result;
            this.Code = this.ResponseError?.ToString();
        }

        /// <summary>
        /// Gets the deserialized response data.
        /// This data is loosely typed and it is not recommended to use it directly.
        /// </summary>
        public ResponseError ResponseError { get; private set; }

        /// <summary>
        /// Gets the fully expanded error code.
        /// </summary>
        [JsonProperty]
        public string Code { get; private set; }

        /// <summary>
        /// Gets the friendly error message. Callers should not rely on this field. It is for debugging purposes only.
        /// </summary>
        [JsonProperty]
        public override string Message
        {
            get
            {
                return base.Message;
            }
        }

        /// <summary>
        /// Gets or sets the source.
        /// </summary>
        [JsonProperty]
        public override string Source
        {
            get
            {
                return base.Source;
            }

            set
            {
                base.Source = value;
            }
        }

        /// <summary>
        /// Gets the stack trace.
        /// </summary>
        [JsonProperty]
        public override string StackTrace
        {
            get
            {
                return base.StackTrace;
            }
        }

        /// <summary>
        /// Gets the raw result information.
        /// </summary>
        public IHttpResult Result { get; private set; }

        /// <summary>
        /// Parses the response content from the result.
        /// </summary>
        /// <param name="result">The http result with the response.</param>
        /// <returns>The parsed data.</returns>
        public static ResponseError ParseResponse(IHttpResult result)
        {
            return JsonConvert.DeserializeObject<ResponseError>(result.ResponseContent, SerializerSettings.Instance);
        }

        /// <summary>
        /// Required by ISerializable.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The serialization context.</param>
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Converts the object to a string.
        /// </summary>
        /// <returns>The string value.</returns>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented, SerializerSettings.Instance);
        }
    }
}