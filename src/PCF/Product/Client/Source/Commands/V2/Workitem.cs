
namespace Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts
{
    public enum WorkitemStatus
    {
        None = 0,
        Leased = 1,
        Completed = 2,
        Disabled = 3
    }

    public class Workitem : GetBatchCommandResponse
    {
        /// <summary>
        /// The unique identifier of this workitem.
        /// </summary>
        public string WorkitemId { get; set; }

        /// <summary>
        /// The workitem’s status – None, Leased, Completed
        /// </summary>
        public WorkitemStatus WorkitemStatus { get; set; }

        /// <summary>
        /// The time when the current lease will expire. In epoch format.
        /// </summary>
        public long LeaseExpires { get; set; }

        /// <summary>
        /// A custom blob of state specific to a single Agent. Limit to 1kb in size.
        /// </summary>
        public string AgentState { get; set; }

        /// <summary>
        /// ETag for updates
        /// </summary>
        public string ETag { get; set; }
    }
}
