namespace Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts
{
    using System.Diagnostics.CodeAnalysis;
    using Newtonsoft.Json;

    /// <summary>
    /// The result of a call to the checkopint API from command feed.
    /// </summary>
    internal class CheckpointResponse
    {
        [JsonProperty("leaseReceipt")]
        public string LeaseReceipt { get; set; }
    }
}
