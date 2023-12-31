namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Net.Http;
    using System.Runtime.Serialization;

    /// <summary>
    /// An exception thrown that should return a bad request error to the client.
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Usage", "CA2240:ImplementISerializableCorrectly", Justification = "Not necessary since we are not serializing this custom exception")]
    public class BadRequestException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the class <see cref="BadRequestException" />.
        /// </summary>
        public BadRequestException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the class <see cref="BadRequestException" />.
        /// </summary>
        /// <param name="message">The error message.</param>
        public BadRequestException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class <see cref="BadRequestException" />.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public BadRequestException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class <see cref="BadRequestException" />.
        /// </summary>
        /// <param name="info">Serialiation info.</param>
        /// <param name="context">The streaming context.</param>
        protected BadRequestException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
        
        /// <summary>
        /// Gets or sets the response context.
        /// </summary>
        public HttpContent ResponseContent { get; set; }
    }
}
