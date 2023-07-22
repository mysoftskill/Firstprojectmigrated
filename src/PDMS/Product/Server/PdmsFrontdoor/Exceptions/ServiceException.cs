namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions
{
    using System;
    using System.Net;
    using System.Runtime.Serialization;

    /// <summary>
    /// Defines a base exception type that all API exceptions derive from.
    /// </summary>
    [Serializable]
    public abstract class ServiceException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceException" /> class.
        /// </summary>
        /// <param name="statusCode">The http status code.</param>
        /// <param name="serviceError">The specific service error.</param>
        protected ServiceException(HttpStatusCode statusCode, ServiceError serviceError)
            : base()
        {
            if (serviceError == null)
            {
                throw new ArgumentNullException(nameof(serviceError));
            }

            this.StatusCode = statusCode;
            this.ServiceError = serviceError;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceException" /> class.
        /// Required by ISerializable.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The serialization context.</param>
        protected ServiceException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Gets or sets the http status code.
        /// </summary>
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the service error.
        /// </summary>
        public ServiceError ServiceError { get; set; }

        /// <summary>
        /// Required by ISerializable.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The serialization context.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}