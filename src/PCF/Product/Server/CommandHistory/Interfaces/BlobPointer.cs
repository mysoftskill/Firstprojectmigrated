namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandHistory
{
    using Newtonsoft.Json;

    /// <summary>
    /// Pointers to blobs in Azure Storage.
    /// </summary>
    internal class BlobPointer
    {
        /// <summary>
        /// The account name.
        /// </summary>
        [JsonProperty("a")]
        public string AccountName { get; set; }

        /// <summary>
        /// The container name.
        /// </summary>
        [JsonProperty("c")]
        public string ContainerName { get; set; }

        /// <summary>
        /// The blob name.
        /// </summary>
        [JsonProperty("b")]
        public string BlobName { get; set; }
    }
}
