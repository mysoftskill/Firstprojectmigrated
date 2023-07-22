namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions
{
    using System;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents an error thrown by the web API pipeline due to mismatch ETag during update/delete operation.
    /// </summary>
    [Serializable]
    public class ETagMismatchError : ExpiredError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ETagMismatchError" /> class.
        /// </summary>
        /// <param name="message">The name of the parameter that was null.</param>
        /// <param name="value">The mismatched ETag.</param>
        public ETagMismatchError(string message = "ETag mismatch with existing entity.", string value = null)
            : base(message, new ETagMismatchInnerError("ETagMismatch", value))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ETagMismatchError" /> class.
        /// Required by ISerializable.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The serialization context.</param>
        public ETagMismatchError(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Converts this object to a detail object.
        /// </summary>
        /// <returns>The converted value.</returns>
        public override Detail ToDetail()
        {
            return new Detail(this.ServiceError.InnerError.Code, this.ServiceError.Message);
        }

        /// <summary>
        /// A custom inner error that includes the value of the mismatched ETag.
        /// </summary>
        public class ETagMismatchInnerError : InnerError
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ETagMismatchInnerError" /> class.
            /// </summary>
            /// <param name="code">The more specific error code.</param>
            /// <param name="value">The mismatched ETag.</param>
            public ETagMismatchInnerError(string code, string value) : base(code)
            {
                this.Value = value;
            }

            /// <summary>
            /// Gets or sets the value of the mismatched ETag.
            /// </summary>
            [JsonProperty(PropertyName = "value")]
            public string Value { get; set; }
        }
    }
}