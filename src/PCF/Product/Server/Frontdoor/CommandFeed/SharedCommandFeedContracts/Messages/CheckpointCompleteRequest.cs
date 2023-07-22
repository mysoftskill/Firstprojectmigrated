namespace Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    using Newtonsoft.Json;

    /// <summary>
    /// Defines the properties of a single checkpoint complete request message.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal class CheckpointCompleteRequest
    {
        [JsonRequired]
        [JsonProperty("id")]
        public string CommandId { get; set; }

        [JsonRequired]
        [JsonProperty("lr")]
        public string LeaseReceipt { get; set; }

        [JsonRequired]
        [JsonProperty("rc")]
        public int RowCount { get; set; }

        [JsonProperty("vids")]
        public IEnumerable<string> VariantIds { get; set; }

        [JsonProperty("ntfls")]
        public IEnumerable<string> NonTransientFailures { get; set; }

        [JsonProperty("efsd")]
        public List<ExportedFileSizeDetails> ExportedFileSizeDetails { get; set; }
    }
}
