namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2
{
    using Newtonsoft.Json;

    /// <summary>
    /// Defines additional response payloads.
    /// </summary>
    public class IncidentResponseMetadata
    {
        /// <summary>
        /// Gets or sets the ICM incident status. This is a response from ICM.
        /// </summary>
        [JsonProperty(PropertyName = "status")]
        public int Status { get; set; }

        /// <summary>
        /// Gets or sets the ICM incident sub-status. This is a response from ICM.
        /// </summary>
        [JsonProperty(PropertyName = "substatus")]
        public int Substatus { get; set; }
    }
}
