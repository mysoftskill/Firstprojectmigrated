namespace Microsoft.PrivacyServices.DataManagement.Worker.ChangeFeedReader
{
    using System;
    using Microsoft.PrivacyServices.DocumentDB.Models;
    using Newtonsoft.Json;

    /// <summary>
    /// Contains the data that is stored on the worker lock.
    /// </summary>
    public class ChangeFeedReaderLockState
    {
        /// <summary>
        /// Gets or sets the timestamp of the last processed configuration.
        /// </summary>
        [JsonProperty(PropertyName = "continuationToken")]
        public string ContinuationToken { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether or not a full sync is in progress.
        /// </summary>
        [JsonProperty(PropertyName = "fullSyncInProgress")]
        public bool FullSyncInProgress { get; set; }

        /// <summary>
        /// Gets or sets the continuation token for the full sync.
        /// </summary>
        [JsonProperty(PropertyName = "syncContinuationToken")]
        public string SyncContinuationToken { get; set; }

        /// <summary>
        /// Gets or sets the time of the last full sync.
        /// </summary>
        [JsonProperty(PropertyName = "lastSyncTime")]
        [JsonConverter(typeof(DateTimeOffsetConverter))]
        public DateTimeOffset LastSyncTime { get; set; }
    }
}