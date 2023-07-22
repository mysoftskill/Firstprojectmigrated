namespace Microsoft.PrivacyServices.CommandFeed.Validator
{
    using System;
    using System.Runtime.Serialization;
    using Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation;

    /// <summary>
    /// Thrown when there was a problem retrieving the certificate from the key discovery
    /// </summary>
    [Serializable]
    public class KeyDiscoveryException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeyDiscoveryException"/> class.
        /// </summary>
        public KeyDiscoveryException()
            : this("There was an problem getting the certificate to validate the verifier from the key discovery endpoint.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyDiscoveryException"/> class.
        /// </summary>
        public KeyDiscoveryException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyDiscoveryException"/> class.
        /// </summary>
        internal KeyDiscoveryException(string message, LoggableInformation loggable)
            : base($"The privacycommand ({loggable}) failed validation. {message})")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyDiscoveryException"/> class.
        /// </summary>
        public KeyDiscoveryException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyDiscoveryException"/> class.
        /// </summary>
        internal KeyDiscoveryException(string message, Exception innerException, LoggableInformation loggable)
            : base($"The privacycommand ({loggable}) failed validation. {message})", innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyDiscoveryException"/> class.
        /// </summary>
        protected KeyDiscoveryException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
