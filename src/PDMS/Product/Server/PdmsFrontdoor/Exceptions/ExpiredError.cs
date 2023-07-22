namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions
{
    using System;
    using System.Net;
    using System.Runtime.Serialization;

    /// <summary>
    /// Indicates an issue with a single argument.
    /// </summary>
    [Serializable]
    public abstract class ExpiredError : ServiceException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExpiredError" /> class.
        /// </summary>
        /// <param name="message">A friendly error message.</param>
        /// <param name="innerError">The inner error data that contains more specific error information.</param>
        protected ExpiredError(string message, InnerError innerError)
            : base(HttpStatusCode.PreconditionFailed, new ServiceError("Expired", message))
        {
            this.ServiceError.InnerError = innerError;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpiredError" /> class.
        /// Required by ISerializable.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The serialization context.</param>
        protected ExpiredError(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Converts the error data into a Detail object.
        /// </summary>
        /// <returns>The converted value.</returns>
        public abstract Detail ToDetail();
    }    
}
