
namespace Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts
{
    public class UpdateWorkitemRequest
    {
        /// <summary>
        /// The workitem’s status – None, Leased, Completed
        /// </summary>
        public WorkitemStatus? WorkitemStatus { get; set; }

        /// <summary>
        /// For how long the current lease should be extended. In seconds.
        /// </summary>
        public int? LeaseExtension { get; set; }

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
