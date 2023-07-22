namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions
{
    using System;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;
    
    /// <summary>
    /// Indicates an argument is malformed.
    /// </summary>
    [Serializable]
    public sealed class InvalidArgumentError : BadArgumentError
    {
        /// <summary>
        /// The invalid data.
        /// </summary>
        [NonSerialized]
        private readonly string value;

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidArgumentError" /> class.
        /// </summary>
        /// <param name="param">The name of the parameter that was null.</param>
        /// <param name="value">The invalid data.</param>
        public InvalidArgumentError(string param, string value)
            : this(param, value, "The given value is invalid.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidArgumentError" /> class.
        /// </summary>
        /// <param name="param">The name of the parameter that was null.</param>
        /// <param name="value">The invalid data.</param>
        /// <param name="customMessage">A custom error message.</param>
        public InvalidArgumentError(string param, string value, string customMessage)
            : base(param, customMessage, new ArgumentInnerError("InvalidValue", value))
        {
            this.value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidArgumentError" /> class.
        /// Required by ISerializable.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The serialization context.</param>
        public InvalidArgumentError(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Converts the error data into a Detail object.
        /// </summary>
        /// <returns>The converted value.</returns>
        public override Detail ToDetail()
        {
            return new Detail(this.ServiceError.InnerError.Code, string.Format($"{this.ServiceError.Message} Value: {this.value}")) { Target = this.ServiceError.Target };
        }

        /// <summary>
        /// A custom inner error that includes the value of the bad argument.
        /// </summary>
        public class ArgumentInnerError : InnerError
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ArgumentInnerError" /> class.
            /// </summary>
            /// <param name="code">The more specific error code.</param>
            /// <param name="value">The invalid data.</param>
            public ArgumentInnerError(string code, string value) : base(code)
            {
                this.Value = value;
            }

            /// <summary>
            /// Gets or sets the value of the bad argument.
            /// </summary>
            [JsonProperty(PropertyName = "value")]
            public string Value { get; set; }
        }
    }
}
