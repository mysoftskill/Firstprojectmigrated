namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    /// <summary>
    /// Enumerates status values for a privacy command.
    /// </summary>
    /// <remarks>
    /// I USUALLY DON'T LIKE TO YELL, BUT THIS IS IMPORTANT. THIS ENUM IS SENSITVE. 
    /// BOTH THE NAMES AND VALUES ARE IMPORTANT, DO NOT RENAME OR CHANGE THE VALUES OF ANY EXISTING MEMBERS.
    /// SOME OF THESE VALUES ARE USED IN DOCDB STORED PROCEDURES.
    /// </remarks>
    public enum PrivacyCommandStatus
    {
        /// <summary>
        /// The command is in progress.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// The command has been soft deleted.
        /// </summary>
        SoftDelete = 1,

        /// <summary>
        /// The command has been completed.
        /// </summary>
        Complete = 2,

        /// <summary>
        /// The data has been anonymized.
        /// </summary>
        Deidentify = 3,
        
        /// <summary>
        /// The command failed. Immediately release the lease.
        /// </summary>
        Failed = 4,

        /// <summary>
        /// The command failed due to unexpected command type for the agent.
        /// </summary>
        /// <remarks>
        /// Do not rename this enum. It is used in alerting.
        /// </remarks>
        UnexpectedCommand = 5,

        /// <summary>
        /// Indicates that the command verification failed.
        /// </summary>
        /// <remarks>
        /// Do not rename this enum. It is used in alerting.
        /// </remarks>
        VerificationFailed = 6,

        /// <summary>
        /// Indicates that the command verification failed due to unknown reasons.
        /// </summary>
        /// <remarks>
        /// Do not rename this enum. It is used in alerting.
        /// </remarks>
        UnexpectedVerificationFailure = 7,
    }
}
