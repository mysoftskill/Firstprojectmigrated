namespace PCF.UnitTests.Pdms
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.Cosmos.Structured;
    using Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache;
    using Microsoft.PrivacyServices.Identity;
    using Microsoft.PrivacyServices.Policy;

    using Moq;
    using Xunit;

    using SubjectType = Microsoft.PrivacyServices.CommandFeed.Service.Common.SubjectType;

    /// <summary>
    /// Tests for CosmosAssetGroupInfoReader
    /// </summary>
    [Trait("Category", "UnitTest")]
    public class CosmosAssetGroupInfoReaderTests
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "reader")]
        [Fact]
        public void ConstructorThrowsIfNoCosmosReader()
        {
            bool isException = false;

            try
            {
                var variantReaderMock = new Mock<ICosmosAssetGroupVariantInfoReader>();
                var reader = new CosmosAssetGroupInfoReader(null, variantReaderMock.Object);
            }
            catch (ArgumentNullException)
            {
                isException = true;
            }

            Assert.True(isException);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "reader")]
        [Fact]
        public void ConstructorThrowsIfNoVariantReader()
        {
            bool isException = false;

            try
            {
                var streamReaderMock = new Mock<ICosmosStructuredStreamReader>();
                var reader = new CosmosAssetGroupInfoReader(streamReaderMock.Object, null);
            }
            catch (ArgumentNullException)
            {
                isException = true;
            }

            Assert.True(isException);
        }

        [Fact]
        public void ConstructorSucceeds()
        {
            var streamReaderMock = new Mock<ICosmosStructuredStreamReader>();
            var variantReaderMock = new Mock<ICosmosAssetGroupVariantInfoReader>();
            var reader = new CosmosAssetGroupInfoReader(streamReaderMock.Object, variantReaderMock.Object); 

            Assert.NotNull(reader);
        }

        [Fact]
        public async Task ReadAsyncReturnsAssetGroupInfos()
        {
            var assetGroupStream = "assetGroupStream1";
            var variantStream = "variantStream1";
            var agId1 = Guid.NewGuid();
            var agId2 = Guid.NewGuid();
            var aadAppId1 = Guid.NewGuid();
            var aadAppId2 = Guid.NewGuid();
            var agentId1 = Guid.NewGuid();
            var agentId2 = Guid.NewGuid();

            var agQualifier1 = "AssetType=CosmosStructuredStream;PhysicalCluster=cosmos15;VirtualCluster=PXSCosmos15.Prod;RelativePath=/local/Public";
            var agQualifier2 = "AssetType=AzureDocumentDB;AccountName=abcde";
            var auth1 = "someAuth";
            var auth2 = "someAuth2";
            var readinessProd = "ProdReady";
            var readinessTest = "TestInProd";

            var capabilities1 = new[] { "AccountClose", "Delete" };
            var capabilities2 = new[] { "Export" };
            var dataTypes1 = new[] { "CustomerContent", "WorkProfile" };
            var dataTypes2 = new[] { "CustomerContent" };
            var subjectTypes1 = new[] { "AADUser", "MSAUser", "AADUser2" };
            var subjectTypes2 = new[] { "DeviceOther", "Other" };

            var deploymentLocation = "Public";
            var supportedCloudInstances = new[] { "Public", "US.Azure.Fairfax" };

            var variantInfo1 = new AssetGroupVariantInfoDocument
            {
                AssetGroupId = new AssetGroupId(Guid.NewGuid()),
                AssetGroupQualifier = "AssetType=AzureDocumentDB;AccountName=pdms",
                VariantId = new VariantId(Guid.NewGuid()),
                VariantName = "name1",
                VariantDescription = "desc1",
                IsAgentApplied = false,
                Capabilities = new[] { "AccountClose", "Delete" },
                DataTypes = new[] { "CustomerContent", "WorkProfile" },
                SubjectTypes = new[] { "DemographicUser" },
            };

            var variantInfo2 = new AssetGroupVariantInfoDocument
            {
                AssetGroupId = new AssetGroupId(Guid.NewGuid()),
                AssetGroupQualifier = "AssetType=CosmosStructuredStream;PhysicalCluster=cosmos15;VirtualCluster=PXSCosmos15.Prod;RelativePath=/local/Public",
                VariantId = new VariantId(Guid.NewGuid()),
                VariantName = "name2",
                VariantDescription = "desc2",
                IsAgentApplied = false,
                Capabilities = new string[0],
                DataTypes = new string[0],
                SubjectTypes = new string[0]
            };

            var variantInfo3 = new AssetGroupVariantInfoDocument
            {
                AssetGroupId = new AssetGroupId(Guid.NewGuid()),
                AssetGroupQualifier = "AssetType=CosmosStructuredStream;PhysicalCluster=cosmos15;VirtualCluster=PXSCosmos15.Prod",
                VariantId = new VariantId(Guid.NewGuid()),
                VariantName = "name3",
                VariantDescription = "desc3",
                IsAgentApplied = false,
                Capabilities = new string[0],
                DataTypes = new string[0],
                SubjectTypes = new string[0]
            };

            var variantInfo4 = new AssetGroupVariantInfoDocument
            {
                AssetGroupId = new AssetGroupId(Guid.NewGuid()),
                AssetGroupQualifier = "AssetType=CosmosStructuredStream;PhysicalCluster=cosmos15;VirtualCluster=PXSCosmos15.Prod;RelativePath=/local/Public/Upload/DataGrid/PDMS",
                VariantId = new VariantId(Guid.NewGuid()),
                VariantName = "name4",
                VariantDescription = "desc4",
                IsAgentApplied = false,
                Capabilities = new string[0],
                DataTypes = new string[0],
                SubjectTypes = new string[0]
            };

            var variantInfo5 = new AssetGroupVariantInfoDocument
            {
                AssetGroupId = new AssetGroupId(Guid.NewGuid()),
                AssetGroupQualifier = "AssetType=CosmosStructuredStream;PhysicalCluster=cosmos15;VirtualCluster=PXSCosmos15.Prod",
                VariantId = new VariantId(Guid.NewGuid()),
                VariantName = "name5",
                VariantDescription = "desc5",
                IsAgentApplied = true,
                Capabilities = new string[0],
                DataTypes = new string[0],
                SubjectTypes = new string[0]
            };

            var variantInfos = new Dictionary<AssetQualifier, List<AssetGroupVariantInfoDocument>>
            {
                { AssetQualifier.Parse(variantInfo1.AssetGroupQualifier), new List<AssetGroupVariantInfoDocument> { variantInfo1 } },
                { AssetQualifier.Parse(variantInfo2.AssetGroupQualifier), new List<AssetGroupVariantInfoDocument> { variantInfo2 } },
                { AssetQualifier.Parse(variantInfo3.AssetGroupQualifier), new List<AssetGroupVariantInfoDocument> { variantInfo3, variantInfo5 } },
                { AssetQualifier.Parse(variantInfo4.AssetGroupQualifier), new List<AssetGroupVariantInfoDocument> { variantInfo4 } },
            };

            var streamReaderMock = new Mock<ICosmosStructuredStreamReader>();
            var variantReaderMock = new Mock<ICosmosAssetGroupVariantInfoReader>();
            var reader = new CosmosAssetGroupInfoReader(streamReaderMock.Object, variantReaderMock.Object);

            variantReaderMock.Setup(m => m.Read()).Returns(variantInfos);
            variantReaderMock.Setup(m => m.VariantInfoStream).Returns(variantStream);

            streamReaderMock.Setup(m => m.CosmosStream).Returns(assetGroupStream);
            streamReaderMock.SetupSequence(m => m.MoveNext()).Returns(true).Returns(true).Returns(false);
            streamReaderMock.SetupSequence(m => m.GetValue<Guid>("AadAppId")).Returns(aadAppId1).Returns(aadAppId2);
            streamReaderMock.SetupSequence(m => m.GetValue<Guid>("AgentId")).Returns(agentId1).Returns(agentId2);
            streamReaderMock.SetupSequence(m => m.GetValue<Guid>("AssetGroupId")).Returns(agId1).Returns(agId2);
            streamReaderMock.SetupSequence(m => m.GetValue<string>("AssetGroupQualifier")).Returns(agQualifier1).Returns(agQualifier2);
            streamReaderMock.SetupSequence(m => m.GetValue<string>("AuthenticationType")).Returns(auth1).Returns(auth2);
            streamReaderMock.SetupSequence(m => m.GetJsonValue<string[]>("Capabilities")).Returns(capabilities1).Returns(capabilities2);
            streamReaderMock.SetupSequence(m => m.GetJsonValue<string[]>("DataTypes")).Returns(dataTypes1).Returns(dataTypes2);
            streamReaderMock.SetupSequence(m => m.GetJsonValue<string[]>("SubjectTypes")).Returns(subjectTypes1).Returns(subjectTypes2);
            streamReaderMock.SetupSequence(m => m.GetJsonValue<Dictionary<string, string>>("ExtendedProps")).Returns(new Dictionary<string, string>()).Returns(new Dictionary<string, string>());
            streamReaderMock.SetupSequence(m => m.GetValue<bool>("IsDeprecated")).Returns(false).Returns(false);
            streamReaderMock.SetupSequence(m => m.GetValue<bool>("IsRealTimeStore")).Returns(true).Returns(true);
            streamReaderMock.SetupSequence(m => m.GetValue<long>("MsaSiteId")).Returns(12345).Returns(21345);
            streamReaderMock.SetupSequence(m => m.GetJsonValue<string[]>("TenantIds")).Returns(new string[0]).Returns(new string[0]);
            streamReaderMock.SetupSequence(m => m.GetValue<string>("AgentReadiness")).Returns(readinessProd).Returns(readinessTest);
            streamReaderMock.Setup(m => m.TryGetValue("DeploymentLocation", out deploymentLocation)).Returns(true);
            streamReaderMock.Setup(m => m.TryGetJsonValue("SupportedClouds", out supportedCloudInstances)).Returns(true);

            var result = await reader.ReadAsync();
            
            Assert.NotNull(result);
            Assert.NotNull(result.AssetGroupInfos);

            Assert.Equal(2, result.AssetGroupInfos.Count);
            Assert.Equal(assetGroupStream, result.AssetGroupInfoStream);
            Assert.Equal(variantStream, result.VariantInfoStream);

            var assetGroup1 = result.AssetGroupInfos[0];
            var assetGroup2 = result.AssetGroupInfos[1];

            Assert.NotNull(assetGroup1.VariantInfosAppliedByAgents);
            Assert.NotNull(assetGroup1.VariantInfosAppliedByPcf);
            Assert.NotNull(assetGroup2.VariantInfosAppliedByPcf);
            Assert.NotNull(assetGroup2.VariantInfosAppliedByAgents);

            Assert.Equal(2, assetGroup1.VariantInfosAppliedByPcf.Count);
            Assert.Equal(2, assetGroup1.VariantInfosAppliedByAgents.Count);

            Assert.Contains<AssetGroupVariantInfoDocument>(variantInfo2, assetGroup1.VariantInfosAppliedByPcf);
            Assert.Contains<AssetGroupVariantInfoDocument>(variantInfo3, assetGroup1.VariantInfosAppliedByPcf);
            Assert.Contains<AssetGroupVariantInfoDocument>(variantInfo4, assetGroup1.VariantInfosAppliedByAgents);
            Assert.Contains<AssetGroupVariantInfoDocument>(variantInfo5, assetGroup1.VariantInfosAppliedByAgents);

            Assert.Empty(assetGroup2.VariantInfosAppliedByPcf);
            Assert.Empty(assetGroup2.VariantInfosAppliedByAgents);

            Assert.Equal(new AssetGroupId(agId1), assetGroup1.AssetGroupId);
            Assert.Equal(new AssetGroupId(agId2), assetGroup2.AssetGroupId);

            Assert.Equal(agQualifier1, assetGroup1.AssetGroupQualifier);
            Assert.Equal(agQualifier2, assetGroup2.AssetGroupQualifier);

            // Verify parsing
            AssetGroupInfo assetGroupInfo1 = new AssetGroupInfo(assetGroup1, enableTolerantParsing: false);
            AssetGroupInfo assetGroupInfo2 = new AssetGroupInfo(assetGroup2, enableTolerantParsing: false);
            
            Assert.Equal(3, assetGroupInfo1.SupportedSubjectTypes.Count());
            Assert.Contains(SubjectType.Aad, assetGroupInfo1.SupportedSubjectTypes);
            Assert.Contains(SubjectType.Msa, assetGroupInfo1.SupportedSubjectTypes);
            Assert.Contains(SubjectType.Aad2, assetGroupInfo1.SupportedSubjectTypes);

            Assert.Equal(assetGroupInfo2.SupportedSubjectTypes, new[] { SubjectType.Device });
        }
    }
}
