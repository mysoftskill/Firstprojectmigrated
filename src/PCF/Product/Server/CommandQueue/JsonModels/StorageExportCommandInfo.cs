namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// Format of export commands in storage.
    /// </summary>
    internal class StorageExportCommandInfo
    {
        [JsonProperty("d")]
        public string[] PrivacyDataTypes { get; set; }

        [JsonProperty("u")]
        public Uri AzureBlobStorageUri { get; set; }

        [JsonProperty("p")]
        public string AzureBlobStoragePath { get; set; }
    }
}
