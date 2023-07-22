namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using Newtonsoft.Json;

    public class DeleteExportArchiveParameters
    {
        /// <summary>
        /// Command Id of the request needs to be deleted
        /// </summary>
        [JsonProperty("commandId")]
        public string CommandId { get; set; }

        /// <summary>
        /// Puid of the user who requested this delete request
        /// </summary>
        [JsonProperty("puid")]
        public long RequesterPuid { get; set; }

    }
}