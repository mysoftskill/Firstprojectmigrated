namespace Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Contains the AssetGFroupInfos read and the cosmos stream from which it is being read from
    /// </summary>
    public class AssetGroupInfoCollectionReadResult
    {
        /// <summary>
        /// The cosmos stream from which the AssetGroupCollection is loaded from
        /// </summary>
        public string AssetGroupInfoStream { get; set; }

        /// <summary>
        /// The cosmos stream from which the AssetGroupVariantInfo is loaded from
        /// </summary>
        public string VariantInfoStream { get; set; }

        /// <summary>
        /// The version of the result.
        /// </summary>
        public long DataVersion { get; set; }

        /// <summary>
        /// The time at which this dataset was created.
        /// </summary>
        public DateTimeOffset CreatedTime { get; set; }

        /// <summary>
        /// List of AssetGroupInfos read
        /// </summary>
        public List<AssetGroupInfoDocument> AssetGroupInfos { get; set; }
    }
}
