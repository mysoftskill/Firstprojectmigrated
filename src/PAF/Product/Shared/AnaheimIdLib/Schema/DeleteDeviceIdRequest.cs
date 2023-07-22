namespace Microsoft.ComplianceServices.AnaheimIdLib.Schema
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// DeleteDeviceIdRequest schema.
    /// </summary>
    public class DeleteDeviceIdRequest
    {
        /// <summary>
        /// Device Id 
        /// </summary>
        [JsonProperty(PropertyName = "requestId")]
        public Guid RequestId { get; set; }

        /// <summary>
        /// Windows 10 Global Device Id
        /// </summary>
        [JsonProperty(PropertyName = "globalDeviceId")]
        public long GlobalDeviceId { get; set; }

        /// <summary>
        /// Global device id issue time
        /// </summary>
        [JsonProperty(PropertyName = "issueTime")]
        public DateTimeOffset IssueTime { get; set; }
    }
}
