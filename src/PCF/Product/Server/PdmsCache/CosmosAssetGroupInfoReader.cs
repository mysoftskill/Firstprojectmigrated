namespace Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.Cosmos.Structured;
    using Microsoft.PrivacyServices.Identity;

    /// <summary>
    /// Loads asset group information from cosmos streams.
    /// </summary>
    public sealed class CosmosAssetGroupInfoReader : IAssetGroupInfoReader
    {
        private readonly ICosmosStructuredStreamReader cosmosStructuredStreamReader;
        private readonly ICosmosAssetGroupVariantInfoReader cosmosAssetGroupVariantInfoReader;

        /// <summary>
        ///  Initializes a new <see cref="CosmosAssetGroupInfoReader"/>
        /// </summary>
        /// <param name="cosmosStructuredStreamReader">Reader to read from Cosmos stream</param>
        /// <param name="cosmosAssetGroupVariantInfoReader">Reader to read the variantInfo from Cosmos</param>
        public CosmosAssetGroupInfoReader(ICosmosStructuredStreamReader cosmosStructuredStreamReader, ICosmosAssetGroupVariantInfoReader cosmosAssetGroupVariantInfoReader)
        {
            this.cosmosStructuredStreamReader = cosmosStructuredStreamReader ?? throw new ArgumentNullException(nameof(cosmosStructuredStreamReader));
            this.cosmosAssetGroupVariantInfoReader = cosmosAssetGroupVariantInfoReader ?? throw new ArgumentNullException(nameof(cosmosAssetGroupVariantInfoReader));
        }

        /// <summary>
        /// Gets the Cosmos stream for the AssetGroupInfo
        /// </summary>
        public static string AssetGroupInfoStreamFormat => Config.Instance.PdmsCache.Cosmos.AssetGroupCosmosStreamTemplate;

        /// <summary>
        /// Gets the Cosmos hourly stream for the AssetGroupInfo
        /// </summary>
        public static string AssetGroupInfoHourlyStreamFormat => Config.Instance.PdmsCache.Cosmos.AssetGroupHourlyCosmosStreamTemplate;

        /// <inheritdoc/>
        public Task<AssetGroupInfoCollectionReadResult> ReadAsync()
        {
            Dictionary<AssetQualifier, List<AssetGroupVariantInfoDocument>> variantInfos = this.cosmosAssetGroupVariantInfoReader.Read();

            List<AssetGroupInfoDocument> items = new List<AssetGroupInfoDocument>();

            while (this.cosmosStructuredStreamReader.MoveNext())
            {
                AssetGroupInfoDocument assetGroupInfo = new AssetGroupInfoDocument
                {
                    AadAppId = this.cosmosStructuredStreamReader.GetValue<Guid?>("AadAppId"),
                    AgentId = new AgentId(this.cosmosStructuredStreamReader.GetValue<Guid>("AgentId")),
                    AssetGroupId = new AssetGroupId(this.cosmosStructuredStreamReader.GetValue<Guid>("AssetGroupId")),
                    AssetGroupQualifier = this.cosmosStructuredStreamReader.GetValue<string>("AssetGroupQualifier"),
                    AuthenticationType = this.cosmosStructuredStreamReader.GetValue<string>("AuthenticationType"),
                    Capabilities = this.cosmosStructuredStreamReader.GetJsonValue<string[]>("Capabilities"),
                    DataTypes = this.cosmosStructuredStreamReader.GetJsonValue<string[]>("DataTypes"),
                    ExtendedProps = this.cosmosStructuredStreamReader.GetJsonValue<Dictionary<string, string>>("ExtendedProps"),
                    IsDeprecated = this.cosmosStructuredStreamReader.GetValue<bool>("IsDeprecated"),
                    IsRealTimeStore = this.cosmosStructuredStreamReader.GetValue<bool>("IsRealTimeStore"),
                    MsaSiteId = this.cosmosStructuredStreamReader.GetValue<long?>("MsaSiteId"),
                    TenantIds = this.cosmosStructuredStreamReader.GetJsonValue<string[]>("TenantIds"),
                    SubjectTypes = this.cosmosStructuredStreamReader.GetJsonValue<string[]>("SubjectTypes"),
                    AgentReadiness = this.cosmosStructuredStreamReader.GetValue<string>("AgentReadiness")
                };

                if (this.cosmosStructuredStreamReader.TryGetJsonValue("SupportedClouds", out string[] supportedCloudInstances))
                {
                    assetGroupInfo.SupportedCloudInstances = supportedCloudInstances;
                }

                if (this.cosmosStructuredStreamReader.TryGetValue("DeploymentLocation", out string deploymentLocation))
                {
                    assetGroupInfo.DeploymentLocation = deploymentLocation;
                }

                AssociateVariantsWithAssetGroup(assetGroupInfo, variantInfos);

                items.Add(assetGroupInfo);
            }

            var result = new AssetGroupInfoCollectionReadResult
            {
                AssetGroupInfoStream = this.cosmosStructuredStreamReader.CosmosStream,
                VariantInfoStream = this.cosmosAssetGroupVariantInfoReader.VariantInfoStream,
                CreatedTime = this.cosmosStructuredStreamReader.LastModifiedTime,
                AssetGroupInfos = items
            };

            return Task.FromResult(result);
        }

        private static void AssociateVariantsWithAssetGroup(AssetGroupInfoDocument assetGroupInfo, Dictionary<AssetQualifier, List<AssetGroupVariantInfoDocument>> variantInfos)
        {
            var variantInfosAppliedByPcf = new List<AssetGroupVariantInfoDocument>();
            var variantInfosAppliedByAgents = new List<AssetGroupVariantInfoDocument>();

            var assetGroupQualifier = AssetQualifier.Parse(assetGroupInfo.AssetGroupQualifier);

            foreach (var variantAssetQualifier in variantInfos.Keys)
            {
                if (variantAssetQualifier == assetGroupQualifier || variantAssetQualifier.Contains(assetGroupQualifier))
                {
                    // if AssetGroup.AssetQualifier = Variant.AssetQualifier || AssetGroup.AssetQualifier is more restrictive than Variant.AssetQualifier 
                    // && IsAgentApplied == false, pcf applies the variants to the command to check if the command is actionable.
                    // if IsAgentApplied is true, pcf passes it to the agent to apply.
                    // eg: AssetGroup Qualifier: 
                    // AssetType = CosmosUnstructuredStream;PhysicalCluster=cosmos15;VirtualCluster=asimov.prod.data;RelativePath=/local/Public/Upload/DataGrid/PDMS
                    // Variant asset qualifier: 
                    // AssetType = CosmosUnstructuredStream;PhysicalCluster=cosmos15;VirtualCluster=asimov.prod.data;RelativePath=/local/Public/Upload/DataGrid
                    var agentAppliedVariantInfos = variantInfos[variantAssetQualifier].Where(v => v.IsAgentApplied);
                    if (agentAppliedVariantInfos.Any())
                    {
                        variantInfosAppliedByAgents.AddRange(agentAppliedVariantInfos);
                        variantInfosAppliedByPcf.AddRange(variantInfos[variantAssetQualifier].Where(v => !v.IsAgentApplied));
                    }
                    else
                    {
                        variantInfosAppliedByPcf.AddRange(variantInfos[variantAssetQualifier]);
                    }
                }
                else if (assetGroupQualifier.Contains(variantAssetQualifier))
                {
                    // if the asset group qualifier less restrictive than the variant qualifier then 
                    // pcf just passes the variant with the command for the agent to check if the command is actionable.
                    // eg: AssetGroup Qualifier: 
                    // AssetType = CosmosUnstructuredStream;PhysicalCluster=cosmos15;VirtualCluster=asimov.prod.data;RelativePath=/local/Public/Upload/DataGrid/
                    // Variant asset qualifier: 
                    // AssetType = CosmosUnstructuredStream;PhysicalCluster=cosmos15;VirtualCluster=asimov.prod.data;RelativePath=/local/Public/Upload/DataGrid/PDMS
                    variantInfosAppliedByAgents.AddRange(variantInfos[variantAssetQualifier]);
                }
            }

            assetGroupInfo.VariantInfosAppliedByPcf = variantInfosAppliedByPcf;
            assetGroupInfo.VariantInfosAppliedByAgents = variantInfosAppliedByAgents;
        }

        /// <summary>
        /// Cosmos does not support this.
        /// </summary>
        public Task<AssetGroupInfoCollectionReadResult> ReadVersionAsync(long version)
        {
            // Cosmos doesn't support reading historical data.
            throw new NotSupportedException($"{nameof(CosmosAssetGroupInfoReader)} does not support reading versioned data.");
        }

        /// <summary>
        /// Cosmos does not support this.
        /// </summary>
        public Task<long> GetLatestVersionAsync()
        {
            // Cosmos doesn't support querying the latest version.
            throw new NotSupportedException($"{nameof(CosmosAssetGroupInfoReader)} does not support query the latest version.");
        }
    }
}
