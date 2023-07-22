namespace Microsoft.Azure.ComplianceServices.Common.DistributedLocking
{
    using System;

    using Newtonsoft.Json;

    /// <summary>
    /// Information about the distributed lock.
    /// </summary>
    public class DistributedLockStatus<T>
    {
        /// <summary>
        /// The time at which this lock expires.
        /// </summary>
        public DateTimeOffset ExpirationTime { get; set; }

        /// <summary>
        /// The most recent owner of the lock.
        /// </summary>
        public string OwnerId { get; set; }

        /// <summary>
        /// The last state of the lock.
        /// </summary>
        public T State { get; set; }

        /// <summary>
        /// The etag (used to update).
        /// </summary>
        [JsonIgnore]
        public string ETag { get; set; }
    }
}
