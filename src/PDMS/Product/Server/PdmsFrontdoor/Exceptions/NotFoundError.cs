namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions
{
    using System;
    using System.Net;
    using System.Runtime.Serialization;

    /// <summary>
    /// Indicates that something was not found.
    /// </summary>
    [Serializable]
    public abstract class NotFoundError : ServiceException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotFoundError" /> class.
        /// </summary>
        /// <param name="message">A friendly error message.</param>
        /// <param name="innerError">The more specific error information.</param>
        protected NotFoundError(string message, InnerError innerError)
            : base(HttpStatusCode.NotFound, new ServiceError("NotFound", message))
        {
            this.ServiceError.InnerError = innerError;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotFoundError" /> class.
        /// Required by ISerializable.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The serialization context.</param>
        protected NotFoundError(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
