namespace Microsoft.PrivacyServices.AzureFunctions.Common.Models
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    /// <summary>
    /// The definition for each NGP variant that needs to be approved by CELA or authorized persons.
    /// </summary>
    public class VariantDefinition
    {
        /// <summary>
        /// The variant definition data types.
        /// </summary>
        [JsonProperty(PropertyName = "dataTypes")]
        public IEnumerable<string> DataTypes { get; set; }

        /// <summary>
        /// The variant definition capabilities.
        /// </summary>
        [JsonProperty(PropertyName = "capabilities")]
        public IEnumerable<string> Capabilities { get; set; }

        /// <summary>
        /// The variant definition subject types.
        /// </summary>
        [JsonProperty(PropertyName = "subjectTypes")]
        public IEnumerable<string> SubjectTypes { get; set; }
    }
}