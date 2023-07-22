namespace Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache
{
    using System.Collections.Generic;
    using Microsoft.PrivacyServices.Identity;

    /// <summary>
    /// Defines Reader for AssetGroupVariantInfo
    /// </summary>
    public interface ICosmosAssetGroupVariantInfoReader
    {
        /// <summary>
        /// Get VariantInfo Cosmos stream path.
        /// </summary>
        string VariantInfoStream { get; }

        /// <summary>
        /// Read VariantInfo from Cosmos stream
        /// </summary>
        /// <returns>Dictionary of AssetQualifier and applicable VariantInfos</returns>
        Dictionary<AssetQualifier, List<AssetGroupVariantInfoDocument>> Read();
    }
}
