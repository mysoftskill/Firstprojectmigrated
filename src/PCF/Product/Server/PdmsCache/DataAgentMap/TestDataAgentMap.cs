namespace Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    /// <summary>
    /// A Data agent map hard coded with information for our functional tests to run.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class TestDataAgentMap : IDataAgentMap
    {
        /// <summary>
        /// Used in AgentQueueFlushFunctionalTests and PDMSFunctionalTests FCTs
        /// </summary>
        private static readonly AgentId TestAgent1Id = new AgentId("14b1e8de19ad41329344abfe28f37d04");

        private static readonly TestAgentInfo TestAgent1 = new TestAgentInfo(TestAgent1Id)
        {
            {
                new AssetGroupInfoDocument
                {
                    AgentId = TestAgent1Id,
                    TenantIds = Array.Empty<string>(),
                    AssetGroupId = new AssetGroupId("dbc33a6b4abe4d0e810467294992e072"),
                    AssetGroupQualifier = "AssetType=AzureTable; AccountName=cf3f402d-aa79-48d8-8fcd-8cb8bef4b4f1;",
                    AadAppId = Guid.Parse("7819dd7c-2f73-4787-9557-0e342743f34b"),
                    MsaSiteId = 296170,
                    SubjectTypes = new[] { "MsaUser", "AadUser" },
                    DataTypes = new[] { "BrowsingHistory", "SearchRequestsAndQuery" },
                    Capabilities = new[] { "Delete", "Export", "AccountClose" },
                    AgentReadiness = "ProdReady",
                    DeploymentLocation = "Public",
                    SupportedCloudInstances = new[] { "Public" },
                    VariantInfosAppliedByAgents = new List<AssetGroupVariantInfoDocument>()
                    {
                        new AssetGroupVariantInfoDocument
                        {
                            VariantId = new VariantId(Guid.NewGuid()),
                            AssetGroupId = new AssetGroupId("dbc33a6b4abe4d0e810467294992e072"),
                            AssetGroupQualifier = "AssetType=AzureTable; AccountName=cf3f402d-aa79-48d8-8fcd-8cb8bef4b4f1;TableName=dc5a1a10-b4f2-49a2-b7c5-0f429e5ef80e;",
                            VariantName = "name",
                            VariantDescription = "desc",
                            IsAgentApplied = false,
                            SubjectTypes = new string[0],
                            DataTypes = new string[0],
                            Capabilities = new string[0],
                        }
                    },
                    VariantInfosAppliedByPcf = new List<AssetGroupVariantInfoDocument>
                    {
                        new AssetGroupVariantInfoDocument
                        {
                            VariantId = new VariantId(Guid.NewGuid()),
                            AssetGroupId = new AssetGroupId("dbc33a6b4abe4d0e810467294992e072"),
                            AssetGroupQualifier = "AssetType=AzureTable; AccountName=cf3f402d-aa79-48d8-8fcd-8cb8bef4b4f1;",
                            VariantName = "name1",
                            VariantDescription = "desc1",
                            IsAgentApplied = false,
                            SubjectTypes = new[] { "AadUser" },
                            DataTypes = new[] { "BrowsingHistory",  },
                            Capabilities = new[] { "Delete" },
                        }
                    }
                }
            },
            {
                new AssetGroupInfoDocument
                {
                    AgentId = TestAgent1Id,
                    TenantIds = Array.Empty<string>(),
                    AssetGroupId = new AssetGroupId("0ac13e25a1bc48729b601dcf24bf3203"),
                    AssetGroupQualifier = "AssetType=Kusto; ClusterName=6389ea80-0d5f-4879-9d58-abf67a8295a3;DatabaseName=6cb88704-c0fd-4818-926d-1f3704204852;TableName=6d306a21-7ad2-47c3-af78-b8a9c713a4c5",
                    AadAppId = Guid.Parse("7819dd7c-2f73-4787-9557-0e342743f34b"),
                    MsaSiteId = 296170,
                    SubjectTypes = new[] { "AadUser" },
                    DataTypes = new[] { "BrowsingHistory", "InkingTypingAndSpeechUtterance" },
                    Capabilities = new[] { "Delete", "Export", "AccountClose" },
                    DeploymentLocation = "Public",
                    SupportedCloudInstances = new[] { "Public" },
                    VariantInfosAppliedByAgents = new List<AssetGroupVariantInfoDocument>(),
                    VariantInfosAppliedByPcf = new List<AssetGroupVariantInfoDocument>(),
                    AgentReadiness = "ProdReady"
                }
            }
        };

        /// <summary>
        /// Used in InsertCommandTests and PdmsFunctionalTests FCTs
        /// </summary>
        private static readonly AgentId TestAgent2Id = new AgentId("9c14b08ff06448baa221be13f015ccba");

        private static readonly TestAgentInfo TestAgent2 = new TestAgentInfo(TestAgent2Id)
        {
            {
                new AssetGroupInfoDocument
                {
                    AgentId = TestAgent2Id,
                    TenantIds = Array.Empty<string>(),
                    AssetGroupId = new AssetGroupId("18bdf5729b494c6bb5a03b5681d80ad8"),
                    AssetGroupQualifier = "AssetType=AzureTable;AccountName=cf3f402d-aa79-48d8-8fcd-8cb8bef4b4f1;TableName=dc5a1a10-b4f2-49a2-b7c5-0f429e5ef80e",
                    AadAppId = Guid.Parse("7819dd7c-2f73-4787-9557-0e342743f34b"),
                    MsaSiteId = 296170,
                    SubjectTypes = new[] { "MsaUser", "AadUser", "Windows10Device", "DemographicUser", "DeviceOther", "NonWindowsDevice", "EdgeBrowser", "AadUser2" },
                    DataTypes = new[] { "BrowsingHistory", "SearchRequestsAndQuery", "ContentConsumption", "ProductAndServiceUsage", "InkingTypingAndSpeechUtterance" },
                    Capabilities = new[] { "Delete", "Export", "AccountClose", "AgeOut" },
                    ExtendedProps = new Dictionary<string, string>() { { "MaxDataAgeMonths", "9999" }, { "OptionalFeatures", "[ \"MsaAgeOutOptIn\" ]" } },
                    DeploymentLocation = "Public",
                    SupportedCloudInstances = new[] { "Public" },
                    VariantInfosAppliedByAgents = new List<AssetGroupVariantInfoDocument>(),
                    VariantInfosAppliedByPcf = new List<AssetGroupVariantInfoDocument>(),
                    AgentReadiness = "ProdReady"
                }
            },
            {
                new AssetGroupInfoDocument
                {
                    AgentId = TestAgent2Id,
                    TenantIds = Array.Empty<string>(),
                    AssetGroupId = new AssetGroupId("ffbce331f09747feae4ce461cd870933"),
                    AssetGroupQualifier = "AssetType=Kusto;ClusterName=6389ea80-0d5f-4879-9d58-abf67a8295a3;DatabaseName=6cb88704-c0fd-4818-926d-1f3704204852;TableName=6d306a21-7ad2-47c3-af78-b8a9c713a4c5",
                    AadAppId = Guid.Parse("7819dd7c-2f73-4787-9557-0e342743f34b"),
                    MsaSiteId = 296170,
                    SubjectTypes = new[] { "AadUser" },
                    DataTypes = new[] { "BrowsingHistory", "InkingTypingAndSpeechUtterance" },
                    Capabilities = new[] { "Delete", "Export", "AccountClose" },
                    DeploymentLocation = "Public",
                    SupportedCloudInstances = new[] { "Public" },
                    VariantInfosAppliedByAgents = new List<AssetGroupVariantInfoDocument>(),
                    VariantInfosAppliedByPcf = new List<AssetGroupVariantInfoDocument>(),
                    AgentReadiness = "ProdReady"
                }
            }
        };

        /// <summary>
        /// Used in BasicFrontDoorTests and PdmsFunctionalTests FCTs
        /// </summary>
        private static readonly AgentId TestAgent3Id = new AgentId("9a7f42842f844733ac276f7a99725d1f");

        private static readonly TestAgentInfo TestAgent3 = new TestAgentInfo(TestAgent3Id)
        {
            {
                // MSA and Microsoft auth
                new AssetGroupInfoDocument
                {
                    AgentId = TestAgent3Id,
                    TenantIds = Array.Empty<string>(),
                    AssetGroupId = new AssetGroupId("8d00c2b556b0434b827e583c5097677d"),
                    AssetGroupQualifier = "AssetType=AzureTable; AccountName=cf3f402d-aa79-48d8-8fcd-8cb8bef4b4f1;TableName=dc5a1a10-b4f2-49a2-b7c5-0f429e5ef80e",
                    AadAppId = Guid.Parse("7819dd7c-2f73-4787-9557-0e342743f34b"),
                    MsaSiteId = 296170,
                    SubjectTypes = new[] { "MsaUser" },
                    DataTypes = new[] { "BrowsingHistory" },
                    Capabilities = new[] { "Delete" },
                    DeploymentLocation = "Public",
                    SupportedCloudInstances = new[] { "Public" },
                    VariantInfosAppliedByAgents = new List<AssetGroupVariantInfoDocument>(),
                    VariantInfosAppliedByPcf = new List<AssetGroupVariantInfoDocument>(),
                    AgentReadiness = "ProdReady"
                }
            },
            {
                // AME AAD app ID
                new AssetGroupInfoDocument
                {
                    AgentId = TestAgent3Id,
                    TenantIds = Array.Empty<string>(),
                    AssetGroupId = new AssetGroupId("8d00c2b556b0434b827e583c5097677e"),
                    AssetGroupQualifier = "AssetType=AzureTable; AccountName=cf3f402d-aa79-48d8-8fcd-8cb8bef4b4f1;TableName=dc5a1a10-b4f2-49a2-b7c5-0f429e5ef80f",
                    AadAppId = Guid.Parse("061be1ab-f7cb-4d44-bc8e-c0dfb357b7fc"),
                    MsaSiteId = null,
                    SubjectTypes = new[] { "MsaUser" },
                    DataTypes = new[] { "BrowsingHistory" },
                    Capabilities = new[] { "Delete" },
                    DeploymentLocation = "Public",
                    SupportedCloudInstances = new[] { "Public" },
                    VariantInfosAppliedByAgents = new List<AssetGroupVariantInfoDocument>(),
                    VariantInfosAppliedByPcf = new List<AssetGroupVariantInfoDocument>(),
                    AgentReadiness = "ProdReady"
                }
            }
        };

        /// <summary>
        /// Used in PxsInsertCommandTests and PdmsFunctionalTests FCTs
        /// </summary>
        private static readonly AgentId TestAgent4Id = new AgentId("f3d89dc9428e4823a64ca243b459de53");
        private static readonly AssetGroupId TestAssetGroupId4 = new AssetGroupId("7baa875b2d9440089d0fccbffcfad856");

        private static readonly TestAgentInfo TestAgent4 = new TestAgentInfo(TestAgent4Id)
        {
            new AssetGroupInfoDocument
            {
                AgentId = TestAgent4Id,
                TenantIds = Array.Empty<string>(),
                AssetGroupId = TestAssetGroupId4,
                AssetGroupQualifier = "AssetType=AzureTable; AccountName=cf3f402d-aa79-48d8-8fcd-8cb8bef4b4f1;TableName=dc5a1a10-b4f2-49a2-b7c5-0f429e5ef80e",
                AadAppId = Guid.Parse("7819dd7c-2f73-4787-9557-0e342743f34b"),
                MsaSiteId = 296170,
                SubjectTypes = new[] { "MsaUser", "AadUser" },
                DataTypes = new[] { "BrowsingHistory", "ProductAndServiceUsage", "InkingTypingAndSpeechUtterance" },
                Capabilities = new[] { "Delete", "Export", "AccountClose", "AgeOut" },
                ExtendedProps = new Dictionary<string, string>() { { "MaxDataAgeMonths", "9999" }, { "OptionalFeatures", "[ \"MsaAgeOutOptIn\" ]" } },
                DeploymentLocation = "Public",
                SupportedCloudInstances = new[] { "Public" },
                AgentReadiness = "ProdReady",
                VariantInfosAppliedByAgents = new List<AssetGroupVariantInfoDocument>
                {
                    new AssetGroupVariantInfoDocument
                    {
                        VariantId = new VariantId("1331F955-6A1E-4FFF-9F38-472B76BCDA1A"),
                        VariantName = "agentVariant",
                        VariantDescription = "variant for agent",
                        AssetGroupId = TestAssetGroupId4,
                        IsAgentApplied = true,
                        AssetGroupQualifier = "AssetType=AzureTable; AccountName=cf3f402d-aa79-48d8-8fcd-8cb8bef4b4f1;TableName=dc5a1a10-b4f2-49a2-b7c5-0f429e5ef80e",
                        SubjectTypes = new[] { "MsaUser" },
                        DataTypes = new[] { "InkingTypingAndSpeechUtterance" },
                        Capabilities = new[] { "Delete" }
                    }
                },
                VariantInfosAppliedByPcf = new List<AssetGroupVariantInfoDocument>
                {
                    new AssetGroupVariantInfoDocument
                    {
                        VariantId = new VariantId("2561D4F9-7AED-47FD-A3D5-4C9672615B41"),
                        VariantName = "PcfVariant",
                        VariantDescription = "variant for Pcf",
                        AssetGroupId = TestAssetGroupId4,
                        IsAgentApplied = false,
                        AssetGroupQualifier = "AssetType=AzureTable; AccountName=cf3f402d-aa79-48d8-8fcd-8cb8bef4b4f1;TableName=dc5a1a10-b4f2-49a2-b7c5-0f429e5ef80e",
                        SubjectTypes = new[] { "AadUser" },
                        DataTypes = new[] { "ProductAndServiceUsage" },
                        Capabilities = new[] { "Delete" }
                    }
                }
            }
        };

        /// <summary>
        /// Used in PdmsFunctionalTest FCT
        /// </summary>
        private static readonly AgentId TestAgent5Id = new AgentId("144c01cbf725406dbb60ed8a3702f290");
        private static readonly AssetGroupId TestAssetGroupId5 = new AssetGroupId("59171531283141f0a7496c22d41bc04c");

        private static readonly TestAgentInfo TestAgent5 = new TestAgentInfo(TestAgent5Id)
        {
            new AssetGroupInfoDocument
            {
                AgentId = TestAgent5Id,
                TenantIds = Array.Empty<string>(),
                AssetGroupId = TestAssetGroupId5,
                AssetGroupQualifier = "AssetType=AzureTable;AccountName=02c68653-e8a1-49bb-b0e9-401139f226f3;TableName=be253c65-2e5d-47fe-96f2-09638b164743",
                AadAppId = Guid.Parse("7819dd7c-2f73-4787-9557-0e342743f34b"),
                MsaSiteId = 296170,
                SubjectTypes = new[] { "MsaUser", "AadUser" },
                DataTypes = new[] { "BrowsingHistory", "ProductAndServiceUsage", "InkingTypingAndSpeechUtterance" },
                Capabilities = new[] { "Delete", "Export", "AccountClose" },
                AgentReadiness = "ProdReady",
                DeploymentLocation = "Public",
                SupportedCloudInstances = new[] { "Public" },
                VariantInfosAppliedByAgents = new List<AssetGroupVariantInfoDocument>
                {
                    new AssetGroupVariantInfoDocument
                    {
                        VariantId = new VariantId("1331F955-6A1E-4FFF-9F38-472B76BCDA1A"),
                        VariantName = "agentVariant",
                        VariantDescription = "variant for agent",
                        AssetGroupId = TestAssetGroupId5,
                        IsAgentApplied = true,
                        AssetGroupQualifier = "AssetType=AzureTable; AccountName=02c68653-e8a1-49bb-b0e9-401139f226f3;TableName=be253c65-2e5d-47fe-96f2-09638b164743",
                        SubjectTypes = new[] { "MsaUser" },
                        DataTypes = new[] { "InkingTypingAndSpeechUtterance" },
                        Capabilities = new[] { "Delete" }
                    }
                },
                VariantInfosAppliedByPcf = new List<AssetGroupVariantInfoDocument>
                {
                    new AssetGroupVariantInfoDocument
                    {
                        VariantId = new VariantId("DB8B5BCB-3E2C-41B1-A76E-DAAB0CC2F03C"),
                        VariantName = "PcfVariant",
                        VariantDescription = "variant for Pcf",
                        AssetGroupId = TestAssetGroupId5,
                        IsAgentApplied = false,
                        AssetGroupQualifier = "AssetType=AzureTable; AccountName=02c68653-e8a1-49bb-b0e9-401139f226f3;TableName=be253c65-2e5d-47fe-96f2-09638b164743",
                        SubjectTypes = new[] { "AadUser" },
                        DataTypes = new[] { "ProductAndServiceUsage" },
                        Capabilities = new[] { "Delete" }
                    }
                }
            }
        };

        /// <summary>
        /// Not used in FCT
        /// </summary>
        private static readonly AgentId TestAgent6Id = new AgentId("DA316070-F639-4A16-AE88-4DFC2B8700C5");
        private static readonly AssetGroupId TestAssetGroupId6 = new AssetGroupId("6FF33B1F-79B1-49D7-B0C4-1FF61C33EAE8");

        private static readonly TestAgentInfo TestAgent6 = new TestAgentInfo(TestAgent6Id)
        {
            new AssetGroupInfoDocument
            {
                AgentId = TestAgent6Id,
                TenantIds = Array.Empty<string>(),
                AssetGroupId = TestAssetGroupId6,
                AssetGroupQualifier = "AssetType=AzureTable; AccountName=cf3f402d-aa79-48d8-8fcd-8cb8bef4b4f1;TableName=dc5a1a10-b4f2-49a2-b7c5-0f429e5ef80e",
                AadAppId = Guid.Parse("7819dd7c-2f73-4787-9557-0e342743f34b"),
                MsaSiteId = 296170,
                SubjectTypes = new[] { "MsaUser", "AadUser" },
                DataTypes = new[] { "BrowsingHistory", "ProductAndServiceUsage", "InkingTypingAndSpeechUtterance" },
                Capabilities = new[] { "Delete", "Export", "AccountClose" },
                AgentReadiness = "ProdReady",
                DeploymentLocation = "Public",
                SupportedCloudInstances = new[] { "Public" },
                VariantInfosAppliedByAgents = new List<AssetGroupVariantInfoDocument>
                {
                    new AssetGroupVariantInfoDocument
                    {
                        VariantId = new VariantId("1331F955-6A1E-4FFF-9F38-472B76BCDA1A"),
                        VariantName = "agentVariant",
                        VariantDescription = "variant for agent",
                        AssetGroupId = TestAssetGroupId6,
                        IsAgentApplied = true,
                        AssetGroupQualifier = "AssetType=AzureTable; AccountName=cf3f402d-aa79-48d8-8fcd-8cb8bef4b4f1;TableName=dc5a1a10-b4f2-49a2-b7c5-0f429e5ef80e",
                        SubjectTypes = new[] { "MsaUser" },
                        DataTypes = new[] { "InkingTypingAndSpeechUtterance" },
                        Capabilities = new[] { "Delete" }
                    }
                },
                VariantInfosAppliedByPcf = new List<AssetGroupVariantInfoDocument>
                {
                    new AssetGroupVariantInfoDocument
                    {
                        VariantId = new VariantId("2561D4F9-7AED-47FD-A3D5-4C9672615B41"),
                        VariantName = "PcfVariant",
                        VariantDescription = "variant for Pcf",
                        AssetGroupId = TestAssetGroupId6,
                        IsAgentApplied = false,
                        AssetGroupQualifier = "AssetType=AzureTable; AccountName=cf3f402d-aa79-48d8-8fcd-8cb8bef4b4f1;TableName=dc5a1a10-b4f2-49a2-b7c5-0f429e5ef80e",
                        SubjectTypes = new[] { "AadUser" },
                        DataTypes = new[] { "ProductAndServiceUsage" },
                        Capabilities = new[] { "Delete" }
                    }
                }
            }
        };

        /// <summary>
        /// Not used in FCT
        /// </summary>
        private static readonly AgentId TestAgent7Id = new AgentId("96890793-dd67-4c23-b3b2-bc62dfe364ff");

        private static readonly TestAgentInfo TestAgent7 = new TestAgentInfo(TestAgent7Id)
        {
            {
                new AssetGroupInfoDocument
                {
                    AgentId = TestAgent7Id,
                    TenantIds = Array.Empty<string>(),
                    AssetGroupId = new AssetGroupId("a9ee6cac2a15484184a24cca9a310e54"),
                    AssetGroupQualifier = "AssetType=AzureTable;AccountName=cf3f402d-aa79-48d8-8fcd-8cb8bef4b4f1;TableName=dc5a1a10-b4f2-49a2-b7c5-0f429e5ef80e",
                    AadAppId = Guid.Parse("7819dd7c-2f73-4787-9557-0e342743f34b"),
                    MsaSiteId = 296170,
                    SubjectTypes = new[] { "MsaUser", "AadUser", "Windows10Device", "DemographicUser", "DeviceOther" },
                    DataTypes = new[] { "BrowsingHistory", "SearchRequestsAndQuery", "ContentConsumption", "ProductAndServiceUsage", "InkingTypingAndSpeechUtterance" },
                    Capabilities = new[] { "Delete", "Export", "AccountClose" },
                    VariantInfosAppliedByAgents = new List<AssetGroupVariantInfoDocument>(),
                    VariantInfosAppliedByPcf = new List<AssetGroupVariantInfoDocument>(),
                    AgentReadiness = "ProdReady",
                    DeploymentLocation = "Public",
                    SupportedCloudInstances = new[] { "Public" }
                }
            },
            {
                new AssetGroupInfoDocument
                {
                    AgentId = TestAgent7Id,
                    TenantIds = Array.Empty<string>(),
                    AssetGroupId = new AssetGroupId("8ed68390f1404b609d010fcfb7ce94a3"),
                    AssetGroupQualifier = "AssetType=Kusto;ClusterName=6389ea80-0d5f-4879-9d58-abf67a8295a3;DatabaseName=6cb88704-c0fd-4818-926d-1f3704204852;TableName=6d306a21-7ad2-47c3-af78-b8a9c713a4c5",
                    AadAppId = Guid.Parse("7819dd7c-2f73-4787-9557-0e342743f34b"),
                    MsaSiteId = 296170,
                    SubjectTypes = new[] { "AadUser" },
                    DataTypes = new[] { "BrowsingHistory", "InkingTypingAndSpeechUtterance" },
                    Capabilities = new[] { "Delete", "Export", "AccountClose" },
                    VariantInfosAppliedByAgents = new List<AssetGroupVariantInfoDocument>(),
                    VariantInfosAppliedByPcf = new List<AssetGroupVariantInfoDocument>(),
                    AgentReadiness = "ProdReady",
                    DeploymentLocation = "Public",
                    SupportedCloudInstances = new[] { "Public" }
                }
            }
        };

        /// <summary>
        /// Used in IngestionRecoveryTests FCT
        /// </summary>
        private static readonly AgentId TestAgent8Id = new AgentId("30d4f43210a54284a8de7dec2435ae46");

        private static readonly TestAgentInfo TestAgent8 = new TestAgentInfo(TestAgent8Id)
        {
            {
                new AssetGroupInfoDocument
                {
                    AgentId = TestAgent8Id,
                    TenantIds = Array.Empty<string>(),
                    AssetGroupId = new AssetGroupId("c992ee4799854378940b859ecc6e6ce1"),
                    AssetGroupQualifier = "AssetType=AzureTable;AccountName=cf3f402d-aa79-48d8-8fcd-8cb8bef4b4f1;TableName=dc5a1a10-b4f2-49a2-b7c5-0f429e5ef80e",
                    AadAppId = Guid.Parse("7819dd7c-2f73-4787-9557-0e342743f34b"),
                    MsaSiteId = 296170,
                    SubjectTypes = new[] { "MsaUser", "AadUser", "Windows10Device", "DemographicUser", "DeviceOther" },
                    DataTypes = new[] { "BrowsingHistory", "SearchRequestsAndQuery", "ContentConsumption", "ProductAndServiceUsage", "InkingTypingAndSpeechUtterance" },
                    Capabilities = new[] { "Delete", "Export", "AccountClose" },
                    DeploymentLocation = "Public",
                    SupportedCloudInstances = new[] { "Public" },
                    VariantInfosAppliedByAgents = new List<AssetGroupVariantInfoDocument>(),
                    VariantInfosAppliedByPcf = new List<AssetGroupVariantInfoDocument>(),
                    AgentReadiness = "ProdReady"
                }
            }
        };

        /// <summary>
        /// NonWindowsDevice AgentId
        /// </summary>
        private static readonly AgentId TestAgent9Id = new AgentId("6E5A7DCE-6FF8-4BFC-A224-3235D702DABA");
        private static readonly TestAgentInfo TestAgent9 = new TestAgentInfo(TestAgent9Id)
        {
            {
                new AssetGroupInfoDocument
                {
                    AgentId = TestAgent9Id,
                    TenantIds = Array.Empty<string>(),
                    AssetGroupId = new AssetGroupId("E6BE0AA6-2EC4-417E-B69B-A17A20490684"),
                    AssetGroupQualifier = "AssetType=AzureTable;AccountName={E6BE0AA6-2EC4-417E-B69B-A17A20490684};TableName=dc5a1a10-b4f2-49a2-b7c5-0f429e5ef80e",
                    AadAppId = Guid.Parse("7819dd7c-2f73-4787-9557-0e342743f34b"),
                    MsaSiteId = 296170,
                    SubjectTypes = new[] { "Windows10Device", "NonWindowsDevice", "EdgeBrowser" },
                    DataTypes = new[] { "BrowsingHistory", "SearchRequestsAndQuery", "ContentConsumption", "ProductAndServiceUsage", "InkingTypingAndSpeechUtterance" },
                    Capabilities = new[] { "Delete", "Export", "AccountClose", "AgeOut" },
                    ExtendedProps = new Dictionary<string, string>() { { "MaxDataAgeMonths", "9999" }, { "OptionalFeatures", "[ \"MsaAgeOutOptIn\" ]" } },
                    DeploymentLocation = "Public",
                    SupportedCloudInstances = new[] { "Public" },
                    VariantInfosAppliedByAgents = new List<AssetGroupVariantInfoDocument>(),
                    VariantInfosAppliedByPcf = new List<AssetGroupVariantInfoDocument>(),
                    AgentReadiness = "ProdReady"
                }
            }
        };

        /// <summary>
        /// Xbox AgentId
        /// </summary>
        private static readonly AgentId TestAgent10Id = new AgentId("08F8D795-0AAD-41A9-9D00-3EE6D41DCDB3");
        private static readonly TestAgentInfo TestAgent10 = new TestAgentInfo(TestAgent10Id)
        {
            {
                new AssetGroupInfoDocument
                {
                    AgentId = TestAgent10Id,
                    TenantIds = Array.Empty<string>(),
                    AssetGroupId = new AssetGroupId("F0182920-A465-4AD2-AA86-E5649E3238FE"),
                    AssetGroupQualifier = "AssetType=AzureTable;AccountName={17F2E8DA-AF66-4E5F-88FD-506C3C2D3E38};TableName=784E2C12-AF45-45B8-A1C6-E3870735D9C8",
                    AadAppId = Guid.Parse("B3796F70-F2F9-496F-988D-3DC75FCD0437"),
                    MsaSiteId = 296170,
                    SubjectTypes = new[] { "MsaUser", "Xbox" },
                    DataTypes = new[] { "BrowsingHistory", "SearchRequestsAndQuery", "ContentConsumption", "ProductAndServiceUsage", "InkingTypingAndSpeechUtterance" },
                    Capabilities = new[] { "Delete", "Export", "AccountClose", "AgeOut" },
                    ExtendedProps = new Dictionary<string, string>() { { "MaxDataAgeMonths", "9999" }, { "OptionalFeatures", "[ \"MsaAgeOutOptIn\" ]" } },
                    DeploymentLocation = "Public",
                    SupportedCloudInstances = new[] { "Public" },
                    VariantInfosAppliedByAgents = new List<AssetGroupVariantInfoDocument>(),
                    VariantInfosAppliedByPcf = new List<AssetGroupVariantInfoDocument>(),
                    AgentReadiness = "ProdReady"
                }
            },
            {
                new AssetGroupInfoDocument
                {
                    AgentId = TestAgent10Id,
                    TenantIds = Array.Empty<string>(),
                    AssetGroupId = new AssetGroupId("1B9685F9-3016-46FF-97EA-B42A04508144"),
                    AssetGroupQualifier = "AssetType=Kusto;ClusterName=CA397241-AADA-4735-BEDE-873B589731FE;DatabaseName=DAC22F58-38BB-4837-866B-AA63C5424E0A;TableName=99D0F577-838A-4661-99C1-A015355BCBB1",
                    AadAppId = Guid.Parse("0BB2ED09-7CAE-48C7-992A-990961C92C57"),
                    MsaSiteId = 296170,
                    SubjectTypes = new[] { "Xbox" },
                    DataTypes = new[] { "ContentConsumption", "ProductAndServiceUsage", "InkingTypingAndSpeechUtterance" },
                    Capabilities = new[] { "Delete", "Export", "AccountClose", "AgeOut" },
                    ExtendedProps = new Dictionary<string, string>() { { "MaxDataAgeMonths", "9999" }, { "OptionalFeatures", "[ \"MsaAgeOutOptIn\" ]" } },
                    DeploymentLocation = "Public",
                    SupportedCloudInstances = new[] { "Public" },
                    VariantInfosAppliedByAgents = new List<AssetGroupVariantInfoDocument>(),
                    VariantInfosAppliedByPcf = new List<AssetGroupVariantInfoDocument>(),
                    AgentReadiness = "ProdReady"
                }
            }
        };

        /// <summary>
        /// Cross-Tenant enabled AgentId
        /// </summary>
        private static readonly AgentId TestAgent11Id = new AgentId("7E1148E5-073B-4AB7-94AC-E550093F1A26");
        private static readonly TestAgentInfo TestAgent11 = new TestAgentInfo(TestAgent11Id)
        {
            {
                new AssetGroupInfoDocument
                {
                    AgentId = TestAgent11Id,
                    TenantIds = Array.Empty<string>(),
                    AssetGroupId = new AssetGroupId("5695F1A6-C434-4D48-8BE2-0397B3DED56D"),
                    AssetGroupQualifier = "AssetType=AzureTable;AccountName={17F2E8DA-AF66-4E5F-88FD-506C3C2D3E38};TableName=784E2C12-AF45-45B8-A1C6-E3870735D9C8",
                    AadAppId = Guid.Parse("B3796F70-F2F9-496F-988D-3DC75FCD0437"),
                    MsaSiteId = 296170,
                    SubjectTypes = new[] { "AadUser", "AadUser2" },
                    DataTypes = new[] { "Any" },
                    Capabilities = new[] { "Delete", "Export", "AccountClose" },
                    ExtendedProps = new Dictionary<string, string>() { { "MaxDataAgeMonths", "9999" } },
                    DeploymentLocation = "Public",
                    SupportedCloudInstances = new[] { "Public" },
                    VariantInfosAppliedByAgents = new List<AssetGroupVariantInfoDocument>(),
                    VariantInfosAppliedByPcf = new List<AssetGroupVariantInfoDocument>(),
                    AgentReadiness = "ProdReady"
                }
            }
        };

        /// <summary>
        /// Used in PAF.AID FCT
        /// </summary>
        private static readonly AgentId TestAgent12Id = new AgentId("db99e11a06a7403f8cf1c5bfd79f95af");

        private static readonly TestAgentInfo TestAgent12 = new TestAgentInfo(TestAgent12Id)
        {
            {
                new AssetGroupInfoDocument
                {
                    AgentId = TestAgent12Id,
                    TenantIds = Array.Empty<string>(),
                    AssetGroupId = new AssetGroupId("764db0a339924f1eaa1b1daa1516dada"),
                    AssetGroupQualifier = "AssetType=AzureTable;AccountName=cf3f402d-aa79-48d8-8fcd-8cb8bef4b4f1;TableName=dc5a1a10-b4f2-49a2-b7c5-0f429e5ef80e",
                    AadAppId = Guid.Parse("7819dd7c-2f73-4787-9557-0e342743f34b"),
                    MsaSiteId = 296170,
                    SubjectTypes = new[] {"EdgeBrowser", "Windows10Device"},
                    DataTypes = new[] { "Any" },
                    Capabilities = new[] { "Delete", "Export", "AccountClose", "AgeOut" },
                    ExtendedProps = new Dictionary<string, string>() { { "MaxDataAgeMonths", "9999" }, { "OptionalFeatures", "[ \"MsaAgeOutOptIn\" ]" } },
                    DeploymentLocation = "Public",
                    SupportedCloudInstances = new[] { "Public" },
                    VariantInfosAppliedByAgents = new List<AssetGroupVariantInfoDocument>(),
                    VariantInfosAppliedByPcf = new List<AssetGroupVariantInfoDocument>(),
                    AgentReadiness = "ProdReady"
                }
            }
        };

        private static readonly TestAgentInfo[] TestAgents =
        {
            TestAgent1, TestAgent2, TestAgent3, TestAgent4, TestAgent5, TestAgent6, TestAgent7, TestAgent8, TestAgent9, TestAgent10, TestAgent11, TestAgent12, ApplicabilityTestAgentBuilder.ApplicabilityTestAgent
        };

        public TestDataAgentMap()
        {
            ProductionSafetyHelper.EnsureNotInProduction();
        }

        public IDataAgentInfo this[AgentId agentId]
        {
            get
            {
                if (this.TryGetAgent(agentId, out var info))
                {
                    return info;
                }

                return null;
            }
        }

        public long Version => 12;

        public string AssetGroupInfoStreamName => string.Empty;

        public string VariantInfoStreamName => string.Empty;

        public bool TryGetAgent(AgentId agentId, out IDataAgentInfo dataAgentInfo)
        {
            dataAgentInfo = TestAgents.FirstOrDefault(x => x.AgentId == agentId);
            return dataAgentInfo != null;
        }

        public IEnumerable<AgentId> GetAgentIds()
        {
            IEnumerable<AgentId> ids = TestAgents.Select(x => x.AgentId);
            return ids;
        }

        /// <inheritdoc />
        public IEnumerable<IAssetGroupInfo> AssetGroupInfos
        {
            get
            {
                return TestAgents.Select(x => x.AssetGroupInfos).SelectMany(y => y).Distinct();
            }
        }
    }
}
