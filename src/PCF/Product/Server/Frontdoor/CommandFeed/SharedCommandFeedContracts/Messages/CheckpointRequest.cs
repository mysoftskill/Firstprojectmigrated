namespace Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    using Newtonsoft.Json;

    /// <summary>
    /// The request sent to the checkpoint API.
    /// </summary>
    internal class CheckpointRequest
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("rowCount")]
        public int RowCount { get; set; }

        [JsonProperty("leaseExtension")]
        public int LeaseExtensionSeconds { get; set; }

        [JsonProperty("agentState")]
        public string AgentState { get; set; }

        [JsonProperty("commandId")]
        public string CommandId { get; set; }

        [JsonProperty("leaseReceipt")]
        public string LeaseReceipt { get; set; }

        [JsonProperty("variants")]
        public string[] Variants { get; set; }

        [JsonProperty("nonTransientFailures")]
        public string[] NonTransientFailures { get; set; }

        [JsonProperty("exportedFileSizeDetails")]
        public List<ExportedFileSizeDetails> ExportedFileSizeDetails { get; set; }
    }
}
