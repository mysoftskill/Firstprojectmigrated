namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    /// <summary>
    /// Enumerates audit log command action.
    /// </summary>
    public enum AuditLogCommandAction
    {
        /// <summary>
        /// No action.
        /// </summary>
        None = -1,

        /// <summary>
        /// A Delete or AccountClose command that marked as "SoftDelete" through checkpoint.
        /// </summary>
        SoftDelete = 0,

        /// <summary>
        /// A Delete or AccountClose command that marked as "Complete" through checkpoint.
        /// </summary>
        HardDelete = 1,

        /// <summary>
        /// An Export command that marked as "Complete" through checkpoint.
        /// </summary>
        ExportComplete = 2,

        /// <summary>
        /// A command that got ignored due to a Variant.
        /// </summary>
        IgnoredByVariant = 3,

        /// <summary>
        /// A permanent failure when a command is older than 30 days and has not been completed.
        /// </summary>
        Expired = 4,

        /// <summary>
        /// An Export command that is inserted into an agent queue.
        /// </summary>
        ExportStart = 5,

        /// <summary>
        /// An Export command that was forcefully and manually marked as complete through a partner test page.
        /// </summary>
        ExportFailedByManualComplete = 6,

        /// <summary>
        /// An Export command that was forcefully marked as complete due to an automated scheduled task.
        /// </summary>
        ExportFailedByAutoComplete = 7,

        /// <summary>
        /// A Delete or AccountClose command that is inserted into an agent queue.
        /// </summary>
        DeleteStart = 8,

        /// <summary>
        /// A command that is dropped due to filtering logic.
        /// </summary>
        NotApplicable = 9
    }
}
