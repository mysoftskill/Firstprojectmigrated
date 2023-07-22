namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions
{
    using System;
    using System.Net;
    using System.Runtime.Serialization;

    /// <summary>
    /// Indicates an issue with authorization.
    /// </summary>
    [Serializable]
    public class NotAuthorizedError : ServiceException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotAuthorizedError" /> class.
        /// </summary>
        /// <param name="message">A friendly error message.</param>        
        public NotAuthorizedError(string message)
            : base(HttpStatusCode.Forbidden, new ServiceError("NotAuthorized", message))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotAuthorizedError" /> class.
        /// </summary>
        /// <param name="message">A friendly error message.</param>
        /// <param name="innerError">The more specific error information.</param>
        protected NotAuthorizedError(string message, InnerError innerError)
            : base(HttpStatusCode.Forbidden, new ServiceError("NotAuthorized", message))
        {
            this.ServiceError.InnerError = innerError;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotAuthorizedError" /> class.
        /// Required by ISerializable.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The serialization context.</param>
        protected NotAuthorizedError(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Converts the error data into a Detail object.
        /// </summary>
        /// <returns>The converted value.</returns>
        public virtual Detail ToDetail()
        {
            return new Detail(this.ServiceError.Code, this.ServiceError.Message);
        }
    }
}