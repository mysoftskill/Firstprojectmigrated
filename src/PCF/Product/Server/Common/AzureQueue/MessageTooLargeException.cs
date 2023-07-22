namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// An exception raised when a message too large is published to an Azure Queue.
    /// </summary>
    [Serializable]
    public class MessageTooLargeException : Exception
    {
        public MessageTooLargeException()
        {
        }

        public MessageTooLargeException(string message) : base(message)
        {
        }

        public MessageTooLargeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MessageTooLargeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
