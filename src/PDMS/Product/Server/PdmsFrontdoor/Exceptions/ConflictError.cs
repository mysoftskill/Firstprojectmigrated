namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions
{
    using System;
    using System.Net;
    using System.Runtime.Serialization;

    /// <summary>
    /// Indicates a logical flaw with the data in the request.
    /// </summary>
    [Serializable]
    public abstract class ConflictError : ServiceException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConflictError" /> class.
        /// </summary>
        /// <param name="message">A friendly error message.</param>
        /// <param name="innerError">The inner error data that contains more specific error information.</param>
        protected ConflictError(string message, InnerError innerError)
            : base(HttpStatusCode.Conflict, new ServiceError("Conflict", message))
        {
            this.ServiceError.InnerError = innerError;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConflictError" /> class.
        /// Required by ISerializable.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The serialization context.</param>
        protected ConflictError(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
