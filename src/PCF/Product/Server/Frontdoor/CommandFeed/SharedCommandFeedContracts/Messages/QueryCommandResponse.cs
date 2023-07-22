namespace Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The result of a call to query a specific command
    /// </summary>
    [JsonObject]
    internal class QueryCommandResponse
    {
        [JsonProperty("command")]
        public JObject Command
        {
            get; set;
        }
    }
}
