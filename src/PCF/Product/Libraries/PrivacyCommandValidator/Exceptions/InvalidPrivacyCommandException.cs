namespace Microsoft.PrivacyServices.CommandFeed.Validator
{
    using System;
    using System.Runtime.Serialization;
    using Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation;
    
    /// <summary>
    /// Thrown when the Privacy command is invalid
    /// </summary>
    [Serializable]
    public class InvalidPrivacyCommandException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the InvalidPrivacyCommandException class.
        /// </summary>
        public InvalidPrivacyCommandException()
            : this("The PrivacyCommand failed validation")
        {
        }

        /// <summary>
        /// Initializes a new instance of the InvalidPrivacyCommandException class.
        /// </summary>
        public InvalidPrivacyCommandException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the InvalidPrivacyCommandException class.
        /// </summary>
        public InvalidPrivacyCommandException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the InvalidPrivacyCommandException class.
        /// </summary>
        internal InvalidPrivacyCommandException(string message, LoggableInformation loggable)
            : base($"The privacycommand ({loggable}) failed validation. {message})")
        {
        }

        /// <summary>
        /// Initializes a new instance of the InvalidPrivacyCommandException class.
        /// </summary>
        internal InvalidPrivacyCommandException(string message, Exception innerException, LoggableInformation loggable)
            : base($"The privacycommand ({loggable}) failed validation. {message})", innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the InvalidPrivacyCommandException class.
        /// </summary>
        protected InvalidPrivacyCommandException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
