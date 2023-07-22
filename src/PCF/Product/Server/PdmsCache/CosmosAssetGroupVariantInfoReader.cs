namespace Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache
{
    using System;
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.Cosmos.Structured;
    using Microsoft.PrivacyServices.Identity;

    /// <summary>
    /// Loads asset group variant information from cosmos streams.
    /// </summary>
    public sealed class CosmosAssetGroupVariantInfoReader : ICosmosAssetGroupVariantInfoReader
    {
        private readonly ICosmosStructuredStreamReader cosmosStructuredStreamReader;

        /// <summary>
        ///  Initializes a new <see cref="CosmosAssetGroupVariantInfoReader"/>
        /// </summary>
        /// <param name="cosmosStructuredStreamReader">Reader to read from Cosmos stream</param>
        public CosmosAssetGroupVariantInfoReader(ICosmosStructuredStreamReader cosmosStructuredStreamReader)
        {
            this.cosmosStructuredStreamReader = cosmosStructuredStreamReader ?? throw new ArgumentNullException(nameof(cosmosStructuredStreamReader));
        }

        /// <summary>
        /// Gets the Cosmos stream format template for the AssetGroupVariantInfo
        /// </summary>
        public static string AssetGroupVariantInfoStreamFormat => Config.Instance.PdmsCache.Cosmos.AssetGroupVariantInfoCosmosStreamTemplate;

        /// <summary>
        /// VariantInfo Cosmos stream path.
        /// </summary>
        public string VariantInfoStream => this.cosmosStructuredStreamReader.CosmosStream;

        /// <summary>
        /// Read VariantInfo from Cosmos stream
        /// </summary>
        /// <returns>Dictionary of AssetQualifier and applicable VariantInfos</returns>
        public Dictionary<AssetQualifier, List<AssetGroupVariantInfoDocument>> Read()
        {
            var assetGroupVariantInfos = new Dictionary<AssetQualifier, List<AssetGroupVariantInfoDocument>>();

            while (this.cosmosStructuredStreamReader.MoveNext())
            {
                AssetGroupVariantInfoDocument variantInfo = new AssetGroupVariantInfoDocument
                {
                    AssetGroupId = new AssetGroupId(this.cosmosStructuredStreamReader.GetValue<Guid>("AssetGroupId")),
                    AssetGroupQualifier = this.cosmosStructuredStreamReader.GetValue<string>("AssetGroupQualifier"),
                    VariantId = new VariantId(this.cosmosStructuredStreamReader.GetValue<Guid>("VariantId")),
                    VariantName = this.cosmosStructuredStreamReader.GetValue<string>("VariantName"),
                    VariantDescription = this.cosmosStructuredStreamReader.GetValue<string>("VariantDescription"),
                    IsAgentApplied = this.cosmosStructuredStreamReader.GetValue<bool>("DisableSignalFiltering"),
                    Capabilities = this.cosmosStructuredStreamReader.GetJsonValue<string[]>("Capabilities"),
                    DataTypes = this.cosmosStructuredStreamReader.GetJsonValue<string[]>("DataTypes"),
                    SubjectTypes = this.cosmosStructuredStreamReader.GetJsonValue<string[]>("SubjectTypes")
                };

                var qualifier = AssetQualifier.Parse(variantInfo.AssetGroupQualifier);
                
                if (assetGroupVariantInfos.ContainsKey(qualifier))
                {
                    assetGroupVariantInfos[qualifier].Add(variantInfo);
                }
                else
                {
                    assetGroupVariantInfos.Add(qualifier, new List<AssetGroupVariantInfoDocument> { variantInfo });
                }
            }

            return assetGroupVariantInfos;
        }
    }
}
