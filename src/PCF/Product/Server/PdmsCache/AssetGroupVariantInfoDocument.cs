namespace Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Newtonsoft.Json;

    /// <summary>
    /// Asset group variant JSON schema
    /// </summary>
    public sealed class AssetGroupVariantInfoDocument
    {
        /// <summary>
        /// Id of the AssetGroup to which the variant information applies
        /// </summary>
        [JsonProperty]
        public AssetGroupId AssetGroupId { get; set; }

        /// <summary>
        /// Id of the VariantInfo
        /// </summary>
        [JsonProperty]
        public VariantId VariantId { get; set; }

        /// <summary>
        /// AssetGroupQualifier for the Variant
        /// </summary>
        [JsonProperty]
        public string AssetGroupQualifier { get; set; }

        /// <summary>
        /// Variant name
        /// </summary>
        [JsonProperty]
        public string VariantName { get; set; }

        /// <summary>
        /// Description for the variant
        /// </summary>
        [JsonProperty]
        public string VariantDescription { get; set; }

        /// <summary>
        /// If true, we always add it to the variant list that is applied by agent
        /// and pcf never applies this
        /// </summary>
        [JsonProperty]
        public bool IsAgentApplied { get; set; }

        /// <summary>
        /// Capabilities to which the variant applies, all capabilities if null
        /// </summary>
        [JsonProperty]
        public string[] Capabilities { get; set; }

        /// <summary>
        /// SubjectTypes to which the variant applies, all subject types if null
        /// </summary>
        [JsonProperty]
        public string[] SubjectTypes { get; set; }

        /// <summary>
        /// DataTypes to which the variant applies, all datatypes if null
        /// </summary>
        [JsonProperty]
        public string[] DataTypes { get; set; }
    }
}
