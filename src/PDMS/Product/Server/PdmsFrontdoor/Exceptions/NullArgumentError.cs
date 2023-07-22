namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions
{
    using System;    
    using System.Runtime.Serialization;
        
    /// <summary>
    /// Indicates a missing argument.
    /// </summary>
    [Serializable]
    public sealed class NullArgumentError : BadArgumentError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NullArgumentError" /> class.
        /// </summary>
        /// <param name="param">The name of the parameter that was null.</param>
        public NullArgumentError(string param)
            : this(param, "The given value is null.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NullArgumentError" /> class.
        /// </summary>
        /// <param name="param">The name of the parameter that was null.</param>
        /// <param name="customMessage">A custom error message.</param>
        public NullArgumentError(string param, string customMessage)
            : base(param, customMessage, new StandardInnerError("NullValue"))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NullArgumentError" /> class.
        /// Required by ISerializable.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The serialization context.</param>
        public NullArgumentError(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Converts the error data into a Detail object.
        /// </summary>
        /// <returns>The converted value.</returns>
        public override Detail ToDetail()
        {
            return new Detail(this.ServiceError.InnerError.Code, this.ServiceError.Message) { Target = this.ServiceError.Target };
        }
    }
}
