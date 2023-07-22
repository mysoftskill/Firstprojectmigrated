namespace Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts
{
    using System.Diagnostics.CodeAnalysis;
    using Newtonsoft.Json;

    /// <summary>
    /// The request sent to the QueryCommand API.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal class QueryCommandRequest
    {
        [JsonProperty("leaseReceipt")]
        public string LeaseReceipt { get; set; }
    }
}
