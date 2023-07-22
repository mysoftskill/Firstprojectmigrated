namespace Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Newtonsoft.Json;

    /// <summary>
    /// Builds an agent and a set of asset groups for functional tests that test the applicability logic.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal static class ApplicabilityTestAgentBuilder
    {
        private const string AgentConfigFileName = "ApplicabilityTestAgentConfig.json";

        private static readonly Guid AppId = new Guid("7819dd7c-2f73-4787-9557-0e342743f34b");
        private static readonly AgentAssetGroups AgentConfig;

        [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static ApplicabilityTestAgentBuilder()
        {
            ProductionSafetyHelper.EnsureNotInProduction();

            Assembly current = typeof(ApplicabilityTestAgentBuilder).Assembly;
            string configFile = current.GetManifestResourceNames().Single(x => x.IndexOf(AgentConfigFileName, StringComparison.OrdinalIgnoreCase) >= 0);

            using (var streamReader = new StreamReader(current.GetManifestResourceStream(configFile)))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                JsonSerializer serializer = new JsonSerializer();
                AgentConfig = serializer.Deserialize<AgentAssetGroups>(jsonReader);
            }

            ApplicabilityTestAgent = new TestAgentInfo(new AgentId(AgentConfig.AgentId));

            foreach (var ag in AgentConfig.AssetGroups)
            {
                ApplicabilityTestAgent.Add(ag.GetDocument(ApplicabilityTestAgent.AgentId));
            }
        }

        public static TestAgentInfo ApplicabilityTestAgent { get; set; }

        [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
        private class AgentAssetGroups
        {
            [JsonProperty]
            public string AgentId { get; set; }

            [JsonProperty]
            public List<AssetGroup> AssetGroups { get; set; }
        }

        [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
        private class AssetGroup
        {
            [JsonProperty]
            public string AssetGroupId { get; set; }

            [JsonProperty]
            public string AssetGroupQualifier { get; set; }

            [JsonProperty]
            public string[] Capabilities { get; set; }

            [JsonProperty]
            public string[] DataTypes { get; set; }

            [JsonProperty]
            public string[] SubjectTypes { get; set; }

            [JsonProperty]
            public Variant VariantInfosAppliedByPcf { get; set; }

            [JsonProperty]
            public string[] SupportedCloudInstances { get; set; }

            [JsonProperty]
            public Dictionary<string, string> ExtendedProps { get; set; }

            public AssetGroupInfoDocument GetDocument(AgentId agentId)
            {
                var assetGroupId = new AssetGroupId(this.AssetGroupId);
                var document = new AssetGroupInfoDocument()
                {
                    AgentId = agentId,
                    AssetGroupId = assetGroupId,
                    AssetGroupQualifier = this.AssetGroupQualifier,
                    AuthenticationType = "AadAppBasedAuth",
                    AadAppId = AppId,
                    AgentReadiness = "ProdReady",
                    Capabilities = this.Capabilities,
                    DataTypes = this.DataTypes,
                    SubjectTypes = this.SubjectTypes,
                    SupportedCloudInstances = this.SupportedCloudInstances,
                    DeploymentLocation = "Public",
                    IsDeprecated = false,
                    IsRealTimeStore = false,
                    TenantIds = new string[0],
                    VariantInfosAppliedByAgents = new AssetGroupVariantInfoDocument[0],
                    VariantInfosAppliedByPcf = (this.VariantInfosAppliedByPcf == null) 
                        ? new AssetGroupVariantInfoDocument[0] : 
                        new[] { this.VariantInfosAppliedByPcf.GetDocument(assetGroupId, this.AssetGroupQualifier) },
                    ExtendedProps = this.ExtendedProps
                };

                return document;
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
        private class Variant
        {
            [JsonProperty]
            public string VariantId { get; set; }

            [JsonProperty]
            public string VariantName { get; set; }

            [JsonProperty]
            public string[] Capabilities { get; set; }

            [JsonProperty]
            public string[] SubjectTypes { get; set; }

            [JsonProperty]
            public string[] DataTypes { get; set; }

            public AssetGroupVariantInfoDocument GetDocument(AssetGroupId assetGroupId, string assetGroupQualifier)
            {
                var document = new AssetGroupVariantInfoDocument()
                {
                    AssetGroupId = assetGroupId,
                    AssetGroupQualifier = assetGroupQualifier,
                    VariantId = new VariantId(this.VariantId),
                    VariantName = this.VariantName,
                    VariantDescription = "Applicability Test Variant",
                    Capabilities = this.Capabilities,
                    DataTypes = this.DataTypes,
                    SubjectTypes = this.SubjectTypes,
                    IsAgentApplied = false
                };

                return document;
            }
        }
    }
}
