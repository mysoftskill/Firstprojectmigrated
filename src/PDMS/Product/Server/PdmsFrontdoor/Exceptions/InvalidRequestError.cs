namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents an error thrown by the web API pipeline due to an invalid model or request query.
    /// </summary>
    [Serializable]
    public class InvalidRequestError : BadArgumentError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidRequestError" /> class.
        /// </summary>
        /// <param name="message">The name of the parameter that was null.</param>
        public InvalidRequestError(string message = "The given request is not a valid query.")
            : base(null, message, new StandardInnerError("InvalidRequest"))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidRequestError" /> class.
        /// Required by ISerializable.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The serialization context.</param>
        public InvalidRequestError(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidRequestError" /> class.
        /// This is used for derived classes only.
        /// </summary>
        /// <param name="message">The message for the nested error.</param>
        /// <param name="innerError">A nested error.</param>
        protected InvalidRequestError(string message, InnerError innerError)
            : base(null, message, new StandardInnerError("InvalidRequest", innerError))
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
    }
}