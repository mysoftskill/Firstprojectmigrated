namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    /// <summary>
    /// Enumerates valid values for Command Status.
    /// </summary>
    public enum CommandStatus
    {
        /// <summary>
        /// Indicates that the work is pending.
        /// </summary>
        Pending = 0,
        
        /// <summary>
        /// Indicates that the work is complete. For Delete, this indicates that the data is hard deleted.
        /// </summary>
        Complete = 1,
        
        /// <summary>
        /// Indicates that the work has failed. The command will be retried.
        /// </summary>
        Failed = 2,

        /// <summary>
        /// Indicates that the agent has chosen to anonymize the data instead of deleting. This status requires explicit approval from CELA.
        /// </summary>
        Deidentify = 3,

        /// <summary>
        /// Valid for Delete only, this status indicates that the data is not viewable, but further processing is necessary to remove it completely.
        /// </summary>
        SoftDelete = 4,

        /// <summary>
        /// Indicates that the command is not supported by the agent.
        /// </summary>
        UnexpectedCommand = 5,

        /// <summary>
        /// Indicates that the command verification failed.
        /// </summary>
        VerificationFailed = 6,

        /// <summary>
        /// Indicates that the command verification failed due to unknown reasons.
        /// </summary>
        UnexpectedVerificationFailure = 7,

        /// <summary>
        /// Indicates that the command is for a cloud (public or sovereign) which the agent does not support.
        /// </summary>
        UnsupportedCloudInstance = 8,
    }
}
