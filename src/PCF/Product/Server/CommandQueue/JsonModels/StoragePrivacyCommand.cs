namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue
{
    using System;
    using Microsoft.Azure.Documents;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Schema of privacy commands in storage.
    /// </summary>
    internal class StoragePrivacyCommand : Document
    {
        [JsonProperty("agq")]
        public string AssetGroupQualifier { get; set; }

        [JsonProperty("ver")]
        public string Verifier { get; set; }

        [JsonProperty("ver3")]
        public string VerifierV3 { get; set; }

        [JsonProperty("nvt")]
        public long UnixNextVisibleTimeSeconds { get; set; }

        [JsonProperty("ct")]
        public PrivacyCommandType CommandType { get; set; }

        [JsonProperty("bid")]
        public string RequestBatchId { get; set; }

        [JsonProperty("as", DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
        public string AgentState { get; set; }

        [JsonProperty("s")]
        public IPrivacySubject Subject { get; set; }

        [JsonProperty("pk")]
        public string PartitionKey { get; set; }

        [JsonProperty("cv")]
        public string CorrelationVector { get; set; }

        [JsonProperty("cld")]
        public string CloudInstance { get; set; }

        [JsonProperty("src")]
        public string CommandSource { get; set; }

        [JsonProperty("ts")]
        public DateTimeOffset CreatedTime { get; set; }
        
        // Information specific to this type of command. Parsed differently depending on command type.
        [JsonProperty("ci", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public JObject CommandInfo { get; set; }

        [JsonProperty("pa")]
        public bool? ProcessorApplicable { get; set; }

        [JsonProperty("ca")]
        public bool? ControllerApplicable { get; set; }

        /// <summary>
        /// A compound key that concatenates (Partition Key) + (Next Visible time). Used to create a composite index that 
        /// supports efficient range queries for a known partition key.
        /// </summary>
        /// <remarks>
        /// This property is computed on demand each time. The value is stored, but we never set it separately from the data that is the NVT and PK properties.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "value")]
        [JsonProperty("ck")]
        private string PartitionKeyNvtCompoundKey
        {
            get { return CreateCompoundKey(this.PartitionKey, DateTimeOffset.FromUnixTimeSeconds(this.UnixNextVisibleTimeSeconds)); }
            set { }
        }

        public static string CreateCompoundKey(string partitionKey, DateTimeOffset timestamp)
        {
            // Important: We are using string comparisons for our ordering,
            // so it's critical that all timestamps have the same number of digits in the string.
            // DateTimeOffset.MaxValue uses 12 digits, so we pad everything to 12 using 'D12' encoding.
            return $"{partitionKey}.{timestamp.ToUnixTimeSeconds():D12}";
        }
    }
}
