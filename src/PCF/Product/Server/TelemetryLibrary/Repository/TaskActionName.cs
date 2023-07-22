namespace Microsoft.PrivacyServices.CommandFeed.Service.Telemetry.Repository
{
    /// <summary>
    /// Pcf baseline calculation task action name.
    /// </summary>
    public enum TaskActionName
    {
        /// <summary>
        /// Action: create a new task.
        /// </summary>
        Create,

        /// <summary>
        /// Action: a task was completed.
        /// </summary>
        Complete,

        /// <summary>
        /// Action: a task fail to run.
        /// </summary>
        Fail
    }
}
