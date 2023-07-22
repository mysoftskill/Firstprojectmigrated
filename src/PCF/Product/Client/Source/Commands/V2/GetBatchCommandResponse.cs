namespace Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts
{
    using System;

    /// <summary>
    /// The response payload for the Get Command calls.
    /// </summary>
    public class GetBatchCommandResponse
    {
        /// <summary>
        /// The agentId this workitem belongs to
        /// </summary>
        public Guid AgentId { get; set; }

        /// <summary>
        /// The assetGroupId this workitem belongs to
        /// </summary>
        public Guid AssetGroupId { get; set; }

        /// <summary>
        /// The link to the next page. Null if this is the last page.
        /// </summary>
        public string NextLink { get; set; }

        /// <summary>
        /// The link to mark all commands complete. Only available for the last page.
        /// </summary>
        public string CompletionLink { get; set; }

        /// <summary>
        /// The command completion token is generated when completion link is active.
        /// It is used to pass through the request body of CompleteCommand API.
        /// </summary>
        public string CompletionToken { get; set; }

        /// <summary>
        /// The content of a command page json file. Can be deserialized into CommandPage object
        /// </summary>
        public string CommandPage;
    }
}
