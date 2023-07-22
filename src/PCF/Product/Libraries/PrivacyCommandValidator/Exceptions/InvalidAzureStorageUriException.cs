namespace Microsoft.PrivacyServices.CommandFeed.Validator
{
    using System;
    using System.Runtime.Serialization;
    using Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation;

    /// <summary>
    /// Thrown when the Azure storage URI is not secured
    /// </summary>
    [Serializable]
    internal class InvalidAzureStorageUriException : InvalidPrivacyCommandException
    {
        /// <summary>
        /// Initializes a new instance of the InvalidAzureStorageUriException class.
        /// </summary>
        public InvalidAzureStorageUriException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the InvalidAzureStorageUriException class.
        /// </summary>
        public InvalidAzureStorageUriException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the InvalidAzureStorageUriException class.
        /// </summary>
        public InvalidAzureStorageUriException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the InvalidAzureStorageUriException class.
        /// </summary>
        protected InvalidAzureStorageUriException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        /// <summary>
        /// Initializes a new instance of the InvalidAzureStorageUriException class.
        /// </summary>
        internal InvalidAzureStorageUriException(string message, LoggableInformation loggable) : base(message, loggable)
        {
        }

        /// <summary>
        /// Initializes a new instance of the InvalidAzureStorageUriException class.
        /// </summary>
        internal InvalidAzureStorageUriException(string message, Exception innerException, LoggableInformation loggable) : base(message, innerException, loggable)
        {
        }
    }
}
