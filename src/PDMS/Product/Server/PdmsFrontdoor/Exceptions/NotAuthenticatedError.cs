namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions
{
    using System;
    using System.Net;
    using System.Runtime.Serialization;

    /// <summary>
    /// Indicates an issue with authentication.
    /// </summary>
    [Serializable]
    public class NotAuthenticatedError : ServiceException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotAuthenticatedError" /> class.
        /// </summary>
        /// <param name="message">A friendly error message.</param>        
        public NotAuthenticatedError(string message)
            : base(HttpStatusCode.Unauthorized, new ServiceError("NotAuthenticated", message))
        {            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotAuthenticatedError" /> class.
        /// Required by ISerializable.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The serialization context.</param>
        protected NotAuthenticatedError(SerializationInfo info, StreamingContext context)
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