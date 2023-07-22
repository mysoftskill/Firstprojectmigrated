namespace Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts
{
    using System.Diagnostics.CodeAnalysis;

    using Newtonsoft.Json;

    /// <summary>
    /// The request sent to the PostExportedFileSizeRequest API
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal class PostExportedFileSizeRequest
    {
        [JsonProperty("leaseReceipt")]
        public string LeaseReceipt { get; set; }

        [JsonProperty("fileName")]
        public string FileName { get; set; }

        [JsonProperty("originalSize")]
        public long OriginalSize { get; set; }

        [JsonProperty("compressedSize")]
        public long CompressedSize { get; set; }

        [JsonProperty("isCompressed")]
        public bool IsCompressed { get; set; }
    }
}
