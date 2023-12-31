namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.Serialization;

    /// <summary>
    /// Enumerates broad classes of PCF downstream dependency errors.
    /// </summary>
    public enum CommandFeedInternalErrorCode
    {
        /// <summary>
        /// Default value.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Indicates throttling from a downstream provider.
        /// </summary>
        Throttle = 1,

        /// <summary>
        /// Indicates a conflict from a downstream provider.
        /// </summary>
        Conflict = 2,
        
        /// <summary>
        /// Indicates the resource from the downstream provider was not found.
        /// </summary>
        NotFound = 3,

        /// <summary>
        /// Indicates lease receipt is not supported.
        /// </summary>
        InvalidLeaseReceipt = 4,

        /// <summary>
        /// Indicates the resource is not supported.
        /// </summary>
        NotSupported = 5
    }

    /// <summary>
    /// A specialized PCF exception that contains metadata about whether the exception we're handling is normal or not.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors", Justification = "Default constructor doesn't provide enough information")]
    [Serializable]
    [ExcludeFromCodeCoverage]  // Justifcation: Constructors that aren't used.
    public class CommandFeedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of command feed exception based on an existing exception.
        /// </summary>
        public CommandFeedException(Exception innerException)
            : base(innerException.Message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the CommandFeedException class with the given message.
        /// </summary>
        public CommandFeedException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the CommandFeedException class with the given message and inner exception.
        /// </summary>
        public CommandFeedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected CommandFeedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        /// <summary>
        /// Indicates if this exception is something we exepct in the normal flow of operation.
        /// </summary>
        public bool IsExpected { get; set; }

        /// <summary>
        /// The error code we've mapped to this exception.
        /// </summary>
        public CommandFeedInternalErrorCode ErrorCode { get; set; }
        
        /// <inheritdoc />
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}
