namespace Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts
{
    using System.Diagnostics.CodeAnalysis;

    using Newtonsoft.Json;

    /// <summary>
    /// Defines the properties of a single checkpoint complete response message.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal class CheckpointCompleteResponse
    {
        [JsonProperty("id")]
        public string CommandId { get; set; }

        [JsonProperty("err")]
        public string Error { get; set; }
    }
}
