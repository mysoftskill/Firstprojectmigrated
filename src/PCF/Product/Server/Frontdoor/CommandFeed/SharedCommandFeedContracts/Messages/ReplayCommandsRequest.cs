namespace Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// The request sent to the ReplayCommands API
    /// </summary>
    internal class ReplayCommandsRequest
    {
        [JsonProperty("replayFromDate")]
        public DateTimeOffset? ReplayFromDate { get; set; }

        [JsonProperty("replayToDate")]
        public DateTimeOffset? ReplayToDate { get; set; }

        [JsonProperty("assetQualifiers")]
        public string[] AssetQualifiers { get; set; }

        [JsonProperty("commandIds")]
        public string[] CommandIds { get; set; }

        [JsonProperty("subjectType")]
        public string SubjectType { get; set; }

        [JsonProperty("includeExportCommands")]
        public bool? IncludeExportCommands { get; set; }
    }
}
