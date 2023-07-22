namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Scheduler
{
    using System;

    using Microsoft.PrivacyServices.DocumentDB.Models;

    using Newtonsoft.Json;    

    /// <summary>
    /// Lock entity stored in persistent store.
    /// </summary>
    /// <typeparam name="T">Custom state specific to lock worker instance.</typeparam>
    public class Lock<T> : DocumentBase<string>
    {
        /// <summary>
        /// Gets or sets lock expiry time.
        /// </summary>
        [JsonProperty(PropertyName = "expiryTime")]
        [JsonConverter(typeof(DateTimeOffsetConverter))]
        public DateTimeOffset ExpiryTime { get; set; }

        /// <summary>
        /// Gets or sets worker id.
        /// </summary>
        [JsonProperty(PropertyName = "workerId")]
        public Guid WorkerId { get; set; }

        /// <summary>
        /// Gets or sets state.
        /// </summary>
        [JsonProperty(PropertyName = "state")]
        public T State { get; set; }

        /// <summary>
        /// Gets or sets failure count.
        /// </summary>
        [JsonProperty(PropertyName = "failureCount")]
        public int FailureCount { get; set; }
    }
}
