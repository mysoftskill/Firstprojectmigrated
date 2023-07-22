namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// An exception raised when the agent attempts to checkpoint a command with an expired lease.
    /// </summary>
    [Serializable]
    public class CheckpointConflictException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the CheckpointConflictException class.
        /// </summary>
        public CheckpointConflictException() : this("Conflict encountered when issing a checkpoint request.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the CheckpointConflictException class.
        /// </summary>
        public CheckpointConflictException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the CheckpointConflictException class.
        /// </summary>
        public CheckpointConflictException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the CheckpointConflictException class.
        /// </summary>
        protected CheckpointConflictException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
