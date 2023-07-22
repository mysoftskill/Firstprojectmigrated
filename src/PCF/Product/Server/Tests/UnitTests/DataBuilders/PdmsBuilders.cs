namespace PCF.UnitTests
{
    using System;
    using System.Collections.Generic;
    using Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache;

    public class AssetGroupInfoDocumentBuilder : TestDataBuilder<AssetGroupInfoDocument>, INeedDataBuilders
    {
        protected override AssetGroupInfoDocument CreateNewObject()
        {
            return new AssetGroupInfoDocument
            {
                AadAppId = Guid.NewGuid(),
                MsaSiteId = 12345,
                AgentId = this.AnAgentId(),
                AssetGroupId = this.AnAssetGroupId(),
                AssetGroupQualifier = "AssetType=Kusto;ClusterName=6389ea80-0d5f-4879-9d58-abf67a8295a3;DatabaseName=6cb88704-c0fd-4818-926d-1f3704204852;TableName=6d306a21-7ad2-47c3-af78-b8a9c713a4c5",
                AuthenticationType = "MSA",
                Capabilities = new[] { "AccountClose", "Export", "Delete" },
                DataTypes = new[] { "Any" },
                ExtendedProps = new Dictionary<string, string>(),
                IsDeprecated = false,
                IsRealTimeStore = true,
                SubjectTypes = new[] { "MSAUser", "AADUser", "DemographicUser", "MicrosoftEmployee", "Windows10Device", "DeviceOther" },
                DeploymentLocation = "Public",
                SupportedCloudInstances = new[] { "Public" },
                TenantIds = new string[0],
                VariantInfosAppliedByAgents = new List<AssetGroupVariantInfoDocument>(),
                VariantInfosAppliedByPcf = new List<AssetGroupVariantInfoDocument>(),
                AgentReadiness = "ProdReady"
            };
        }

        public static implicit operator AssetGroupInfo(AssetGroupInfoDocumentBuilder builder)
        {
            return new AssetGroupInfo(builder.Build(), false);
        }
    }
    
    public class AssetGroupVariantInfoDocumentBuilder : TestDataBuilder<AssetGroupVariantInfoDocument>, INeedDataBuilders
    {
        protected override AssetGroupVariantInfoDocument CreateNewObject()
        {
            return new AssetGroupVariantInfoDocument
            {
                AssetGroupId = this.AnAssetGroupId(),
                AssetGroupQualifier = "AssetType=Kusto;ClusterName=6389ea80-0d5f-4879-9d58-abf67a8295a3;DatabaseName=6cb88704-c0fd-4818-926d-1f3704204852;TableName=6d306a21-7ad2-47c3-af78-b8a9c713a4c5",
                DataTypes = new[] { "Any" },
                SubjectTypes = new[] { "MSAUser", "AADUser", "DemographicUser", "MicrosoftEmployee", "Windows10Device", "DeviceOther" },
                Capabilities = new[] { "AccountClose", "Export", "Delete" },
                VariantDescription = "Fancy description",
                IsAgentApplied = false,
                VariantId = this.AVariantId(),
                VariantName = "Name"
            };
        }

        public static implicit operator AssetGroupVariantInfo(AssetGroupVariantInfoDocumentBuilder builder)
        {
            return new AssetGroupVariantInfo(builder.Build(), false);
        }
    }
}
