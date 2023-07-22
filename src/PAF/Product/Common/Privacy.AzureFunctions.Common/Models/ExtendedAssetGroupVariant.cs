namespace Microsoft.PrivacyServices.AzureFunctions.Common.Models
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    /// <summary>
    /// The extended asset group variant object which is used to capture additional variant information
    /// like datatypes, capabilities and subjecttypes along with the information in asset group variant.
    /// </summary>
    public class ExtendedAssetGroupVariant : AssetGroupVariant
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
