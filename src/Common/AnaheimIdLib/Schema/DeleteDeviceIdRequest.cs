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
        /// AuthorizationId.
        /// </summary>
        [JsonProperty(PropertyName = "authorizationId")]
        public string AuthorizationId { get; set; }

        /// <summary>
        /// Correlation Vector
        /// </summary>
        [JsonProperty(PropertyName = "cV")]
        public string CorrelationVector { get; set; }

        /// <summary>
        /// Request Id.
        /// </summary>
        [JsonProperty(PropertyName = "requestId")]
        public Guid RequestId { get; set; }

        /// <summary>
        /// Windows 10 Global Device Id.
        /// </summary>
        [JsonProperty(PropertyName = "globalDeviceId")]
        public long GlobalDeviceId { get; set; }

        /// <summary>
        /// Request create time.
        /// </summary>
        [JsonProperty(PropertyName = "createTime")]
        public DateTimeOffset CreateTime { get; set; }

        /// <summary>
        /// Is it a test signal?
        /// </summary>
        [JsonProperty(PropertyName = "testSignal")]
        public bool TestSignal { get; set; }
    }
}
