namespace Microsoft.PrivacyServices.DataManagement.Worker.DataOwner
{
    using System;
    using DocumentDB.Models;
    using Newtonsoft.Json;

    /// <summary>
    /// Contains the data that is stored in the Data owner worker lock.
    /// </summary>
    public class DataOwnerWorkerLockState
    {
        /// <summary>
        /// Gets or sets the time of the last full sync.
        /// </summary>
        [JsonProperty(PropertyName = "lastSyncTime")]
        [JsonConverter(typeof(DateTimeOffsetConverter))]
        public DateTimeOffset LastSyncTime { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the update process has started.
        /// </summary>
        [JsonProperty(PropertyName = "inProgress")]
        public bool InProgress { get; set; }
    }
}