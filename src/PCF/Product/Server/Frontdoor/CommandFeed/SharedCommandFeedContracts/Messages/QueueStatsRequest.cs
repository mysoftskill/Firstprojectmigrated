namespace Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts
{
    using Newtonsoft.Json;

    internal class QueueStatsRequest
    {
        [JsonProperty("assetQualifier")]
        public string AssetGroupQualifier { get; set; }

        [JsonProperty("commandType")]
        public string CommandType { get; set; }
    }
}