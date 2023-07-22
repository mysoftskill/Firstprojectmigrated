namespace PCF.UnitTests.Pdms
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.Cosmos.Structured;
    using Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache;
    using Microsoft.PrivacyServices.Identity;
    using Microsoft.PrivacyServices.Policy;

    using Moq;
    using Xunit;

    using PcfCommon = Microsoft.PrivacyServices.CommandFeed.Service.Common;

    /// <summary>
    /// Tests for CosmosAssetGroupVariantInfoReader
    /// </summary>
    [Trait("Category", "UnitTest")]
    public class CosmosAssetGroupVariantInfoReaderTests
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "reader")]
        [Fact]
        public void ConstructorThrowsIfNoCosmosReader()
        {
            bool isException = false;

            try
            {
                var reader = new CosmosAssetGroupVariantInfoReader(null);
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
            var reader = new CosmosAssetGroupVariantInfoReader(streamReaderMock.Object);

            Assert.NotNull(reader);
        }

        [Fact]
        public void BadQualifierRejected()
        {
            var agId1 = Guid.NewGuid();
            var varId1 = Guid.NewGuid();
            
            var agQualifierBad = "AssetType=AzureDocumentDB;AccounfdstName=pdms"; // bad qualifier should not get added
            var varName1 = "someVariant1";

            var capabilities1 = new[] { "AccountClose", "Delete" };
            var dataTypes1 = new[] { "CustomerContent", "WorkProfile" };
            var subjectTypes1 = new[] { "AADUser", "MSAUser" };

            var streamReaderMock = new Mock<ICosmosStructuredStreamReader>();
            var variantReader = new CosmosAssetGroupVariantInfoReader(streamReaderMock.Object);

            streamReaderMock.SetupSequence(m => m.MoveNext()).Returns(true).Returns(false);
            streamReaderMock.SetupSequence(m => m.GetValue<Guid>("AssetGroupId")).Returns(agId1);
            streamReaderMock.SetupSequence(m => m.GetValue<string>("AssetGroupQualifier")).Returns(agQualifierBad);
            streamReaderMock.SetupSequence(m => m.GetValue<Guid>("VariantId")).Returns(varId1);
            streamReaderMock.SetupSequence(m => m.GetValue<string>("VariantName")).Returns(varName1);
            streamReaderMock.SetupSequence(m => m.GetValue<string>("VariantDescription")).Returns("description");
            streamReaderMock.SetupSequence(m => m.GetJsonValue<string[]>("Capabilities")).Returns(capabilities1);
            streamReaderMock.SetupSequence(m => m.GetJsonValue<string[]>("DataTypes")).Returns(dataTypes1);
            streamReaderMock.SetupSequence(m => m.GetJsonValue<string[]>("SubjectTypes")).Returns(subjectTypes1);

            Assert.Throws<ArgumentNullException>(() => variantReader.Read());
        }

        [Fact]
        public void BadSubjectRejected()
        {
            var agId1 = Guid.NewGuid();
            var varId1 = Guid.NewGuid();

            var agQualifier = "AssetType=CosmosStructuredStream;PhysicalCluster=cosmos15;VirtualCluster=PXSCosmos15.Prod"; 
            var varName1 = "someVariant1";

            var capabilities1 = new[] { "AccountClose", "Delete" };
            var dataTypes1 = new[] { "CustomerContent", "WorkProfile" };
            var subjectTypes1 = new[] { "bananaphone", "MSAUser" };

            var streamReaderMock = new Mock<ICosmosStructuredStreamReader>();
            var variantReader = new CosmosAssetGroupVariantInfoReader(streamReaderMock.Object);

            streamReaderMock.SetupSequence(m => m.MoveNext()).Returns(true).Returns(false);
            streamReaderMock.SetupSequence(m => m.GetValue<Guid>("AssetGroupId")).Returns(agId1);
            streamReaderMock.SetupSequence(m => m.GetValue<string>("AssetGroupQualifier")).Returns(agQualifier);
            streamReaderMock.SetupSequence(m => m.GetValue<Guid>("VariantId")).Returns(varId1);
            streamReaderMock.SetupSequence(m => m.GetValue<string>("VariantName")).Returns(varName1);
            streamReaderMock.SetupSequence(m => m.GetValue<string>("VariantDescription")).Returns("description");
            streamReaderMock.SetupSequence(m => m.GetJsonValue<string[]>("Capabilities")).Returns(capabilities1);
            streamReaderMock.SetupSequence(m => m.GetJsonValue<string[]>("DataTypes")).Returns(dataTypes1);
            streamReaderMock.SetupSequence(m => m.GetJsonValue<string[]>("SubjectTypes")).Returns(subjectTypes1);

            var items = variantReader.Read();

            Assert.Single(items);
            Assert.Single(items.Single().Value);

            AssetGroupVariantInfoDocument document = items.Single().Value[0];
            Assert.Throws<InvalidOperationException>(() => new AssetGroupVariantInfo(document, false));

            var tolerantParse = new AssetGroupVariantInfo(document, true);
            Assert.Single(tolerantParse.ApplicableSubjectTypes, x => x == PcfCommon.SubjectType.Msa);
        }

        [Fact]
        public void BadDataTypeRejected()
        {
            var agId1 = Guid.NewGuid();
            var varId1 = Guid.NewGuid();

            var agQualifier = "AssetType=CosmosStructuredStream;PhysicalCluster=cosmos15;VirtualCluster=PXSCosmos15.Prod";
            var varName1 = "someVariant1";

            var capabilities1 = new[] { "AccountClose", "Delete" };
            var dataTypes1 = new[] { "bananaphone", "WorkProfile" };
            var subjectTypes1 = new[] { "MSAUser" };

            var streamReaderMock = new Mock<ICosmosStructuredStreamReader>();
            var variantReader = new CosmosAssetGroupVariantInfoReader(streamReaderMock.Object);

            streamReaderMock.SetupSequence(m => m.MoveNext()).Returns(true).Returns(false);
            streamReaderMock.SetupSequence(m => m.GetValue<Guid>("AssetGroupId")).Returns(agId1);
            streamReaderMock.SetupSequence(m => m.GetValue<string>("AssetGroupQualifier")).Returns(agQualifier);
            streamReaderMock.SetupSequence(m => m.GetValue<Guid>("VariantId")).Returns(varId1);
            streamReaderMock.SetupSequence(m => m.GetValue<string>("VariantName")).Returns(varName1);
            streamReaderMock.SetupSequence(m => m.GetValue<string>("VariantDescription")).Returns("description");
            streamReaderMock.SetupSequence(m => m.GetJsonValue<string[]>("Capabilities")).Returns(capabilities1);
            streamReaderMock.SetupSequence(m => m.GetJsonValue<string[]>("DataTypes")).Returns(dataTypes1);
            streamReaderMock.SetupSequence(m => m.GetJsonValue<string[]>("SubjectTypes")).Returns(subjectTypes1);

            var items = variantReader.Read();

            Assert.Single(items);
            Assert.Single(items.Single().Value);

            AssetGroupVariantInfoDocument document = items.Single().Value[0];
            Assert.Throws<InvalidOperationException>(() => new AssetGroupVariantInfo(document, false));

            var tolerantParse = new AssetGroupVariantInfo(document, true);
            Assert.Single(tolerantParse.ApplicableDataTypeIds, x => x == Policies.Current.DataTypes.Ids.WorkProfile);
        }

        [Fact]
        public void BadCapabilitiesRejected()
        {
            var agId1 = Guid.NewGuid();
            var varId1 = Guid.NewGuid();

            var agQualifier = "AssetType=CosmosStructuredStream;PhysicalCluster=cosmos15;VirtualCluster=PXSCosmos15.Prod";
            var varName1 = "someVariant1";

            var capabilities1 = new[] { "foobar", "Delete" };
            var dataTypes1 = new[] { "WorkProfile" };
            var subjectTypes1 = new[] { "MSAUser" };

            var streamReaderMock = new Mock<ICosmosStructuredStreamReader>();
            var variantReader = new CosmosAssetGroupVariantInfoReader(streamReaderMock.Object);

            streamReaderMock.SetupSequence(m => m.MoveNext()).Returns(true).Returns(false);
            streamReaderMock.SetupSequence(m => m.GetValue<Guid>("AssetGroupId")).Returns(agId1);
            streamReaderMock.SetupSequence(m => m.GetValue<string>("AssetGroupQualifier")).Returns(agQualifier);
            streamReaderMock.SetupSequence(m => m.GetValue<Guid>("VariantId")).Returns(varId1);
            streamReaderMock.SetupSequence(m => m.GetValue<string>("VariantName")).Returns(varName1);
            streamReaderMock.SetupSequence(m => m.GetValue<string>("VariantDescription")).Returns("description");
            streamReaderMock.SetupSequence(m => m.GetJsonValue<string[]>("Capabilities")).Returns(capabilities1);
            streamReaderMock.SetupSequence(m => m.GetJsonValue<string[]>("DataTypes")).Returns(dataTypes1);
            streamReaderMock.SetupSequence(m => m.GetJsonValue<string[]>("SubjectTypes")).Returns(subjectTypes1);

            var items = variantReader.Read();

            Assert.Single(items);
            Assert.Single(items.Single().Value);

            AssetGroupVariantInfoDocument document = items.Single().Value[0];
            Assert.Throws<InvalidOperationException>(() => new AssetGroupVariantInfo(document, false));

            var tolerantParse = new AssetGroupVariantInfo(document, true);
            Assert.Single(tolerantParse.ApplicableCapabilities, x => x == PrivacyCommandType.Delete);
        }

        [Fact]
        public void ReadReturnsVariantInfos()
        {
            var agId1 = Guid.NewGuid();
            var agId2 = Guid.NewGuid();
            var agId3 = Guid.NewGuid();
            var varId1 = Guid.NewGuid();
            var varId2 = Guid.NewGuid();
            var varId3 = Guid.NewGuid();

            var agQualifier1 = "AssetType=CosmosStructuredStream;PhysicalCluster=cosmos15;VirtualCluster=PXSCosmos15.Prod";
            var agQualifier3 = "AssetType=AzureDocumentDB;AccountName=pdms";
            var varName1 = "someVariant1";
            var varName2 = "someVariant2";
            var varName3 = "someVariant3";
            var varDesc1 = "someVariant1";
            var varDesc2 = "someVariant2";
            var varDesc3 = "someVariant3";

            var capabilities1 = new[] { "AccountClose", "Delete" };
            var capabilities2 = new string[0];
            var capabilities3 = new[] { "AccountClose" };
            var dataTypes1 = new[] { "CustomerContent", "WorkProfile" };
            var dataTypes2 = new string[0];
            var dataTypes3 = new[] { "WorkProfile" };
            var subjectTypes1 = new[] { "AADUser", "MSAUser" };
            var subjectTypes2 = new string[0];
            var subjectTypes3 = new[] { "DemographicUser", "AADUser2" };

            var streamReaderMock = new Mock<ICosmosStructuredStreamReader>();
            var variantReader = new CosmosAssetGroupVariantInfoReader(streamReaderMock.Object);

            streamReaderMock.SetupSequence(m => m.MoveNext()).Returns(true).Returns(true).Returns(true).Returns(false);
            streamReaderMock.SetupSequence(m => m.GetValue<Guid>("AssetGroupId")).Returns(agId1).Returns(agId2).Returns(agId3);
            streamReaderMock.SetupSequence(m => m.GetValue<string>("AssetGroupQualifier")).Returns(agQualifier1).Returns(agQualifier1).Returns(agQualifier3);
            streamReaderMock.SetupSequence(m => m.GetValue<Guid>("VariantId")).Returns(varId1).Returns(varId2).Returns(varId3);
            streamReaderMock.SetupSequence(m => m.GetValue<string>("VariantName")).Returns(varName1).Returns(varName2).Returns(varName3);
            streamReaderMock.SetupSequence(m => m.GetValue<string>("VariantDescription")).Returns(varDesc1).Returns(varDesc2).Returns(varDesc3);
            streamReaderMock.SetupSequence(m => m.GetJsonValue<string[]>("Capabilities")).Returns(capabilities1).Returns(capabilities2).Returns(capabilities3);
            streamReaderMock.SetupSequence(m => m.GetJsonValue<string[]>("DataTypes")).Returns(dataTypes1).Returns(dataTypes2).Returns(dataTypes3);
            streamReaderMock.SetupSequence(m => m.GetJsonValue<string[]>("SubjectTypes")).Returns(subjectTypes1).Returns(subjectTypes2).Returns(subjectTypes3);

            var result = variantReader.Read();
            
            Assert.NotNull(result);

            // There should be two items in the dictionary, since there are two unique assetQualifiers
            Assert.Equal(2, result.Count);

            // There are two variants with agQualifier1
            var variantInfos1 = result[AssetQualifier.Parse(agQualifier1)];
            Assert.Equal(2, variantInfos1.Count);

            // There are one variant with agQualifier3
            var variantInfos2 = result[AssetQualifier.Parse(agQualifier3)];
            Assert.Single(variantInfos2);
            
            var variantDocument1 = variantInfos1[0];
            var variantDocument2 = variantInfos1[1];
            var variantDocument3 = variantInfos2[0];

            var variantInfo1 = new AssetGroupVariantInfo(variantDocument1, false);
            var variantInfo2 = new AssetGroupVariantInfo(variantDocument2, false);
            var variantInfo3 = new AssetGroupVariantInfo(variantDocument3, false);

            Assert.Equal(new AssetGroupId(agId1), variantInfo1.AssetGroupId);
            Assert.Equal(new AssetGroupId(agId2), variantInfo2.AssetGroupId);
            Assert.Equal(new AssetGroupId(agId3), variantInfo3.AssetGroupId);

            Assert.Equal(agQualifier1, variantInfo1.AssetGroupQualifier);
            Assert.Equal(agQualifier1, variantInfo2.AssetGroupQualifier);
            Assert.Equal(agQualifier3, variantInfo3.AssetGroupQualifier);

            Assert.Equal(AssetQualifier.Parse(agQualifier1), variantInfo1.ApplicableAssetQualifier);
            Assert.Equal(AssetQualifier.Parse(agQualifier1), variantInfo2.ApplicableAssetQualifier);
            Assert.Equal(AssetQualifier.Parse(agQualifier3), variantInfo3.ApplicableAssetQualifier);

            Assert.Equal(new VariantId(varId1), variantInfo1.VariantId);
            Assert.Equal(new VariantId(varId2), variantInfo2.VariantId);
            Assert.Equal(new VariantId(varId3), variantInfo3.VariantId);

            Assert.Equal(varName1, variantInfo1.VariantName);
            Assert.Equal(varName2, variantInfo2.VariantName);
            Assert.Equal(varName3, variantInfo3.VariantName);

            Assert.Equal(varDesc1, variantInfo1.VariantDescription);
            Assert.Equal(varDesc2, variantInfo2.VariantDescription);
            Assert.Equal(varDesc3, variantInfo3.VariantDescription);

            Assert.Equal(capabilities1, variantDocument1.Capabilities);
            Assert.Equal(capabilities2, variantDocument2.Capabilities);
            Assert.Equal(capabilities3, variantDocument3.Capabilities);

            var applicableCapabilites1 = new List<PrivacyCommandType> { PrivacyCommandType.AccountClose, PrivacyCommandType.Delete };
            Assert.Equal(applicableCapabilites1, variantInfo1.ApplicableCapabilities);
            
            Assert.Equal(new List<PrivacyCommandType>(), variantInfo2.ApplicableCapabilities);

            var applicableCapabilites3 = new List<PrivacyCommandType> { PrivacyCommandType.AccountClose };
            Assert.Equal(applicableCapabilites3, variantInfo3.ApplicableCapabilities);

            Assert.Equal(dataTypes1, variantDocument1.DataTypes);
            Assert.Equal(dataTypes2, variantDocument2.DataTypes);
            Assert.Equal(dataTypes3, variantDocument3.DataTypes);

            var applicableDatatypes1 = new List<DataTypeId> { Policies.Current.DataTypes.CreateId("CustomerContent"), Policies.Current.DataTypes.CreateId("WorkProfile") };
            Assert.Equal(applicableDatatypes1, variantInfo1.ApplicableDataTypeIds);

            Assert.Equal(new List<DataTypeId>(), variantInfo2.ApplicableDataTypeIds);

            var applicableDatatypes3 = new List<DataTypeId> { Policies.Current.DataTypes.CreateId("WorkProfile") };
            Assert.Equal(applicableDatatypes3, variantInfo3.ApplicableDataTypeIds);

            Assert.Equal(subjectTypes1, variantDocument1.SubjectTypes);
            Assert.Equal(subjectTypes2, variantDocument2.SubjectTypes);
            Assert.Equal(subjectTypes3, variantDocument3.SubjectTypes);

            var applicableSubjectTypes1 = new List<PcfCommon.SubjectType> { PcfCommon.SubjectType.Aad, PcfCommon.SubjectType.Msa };
            Assert.Equal(applicableSubjectTypes1, variantInfo1.ApplicableSubjectTypes);

            Assert.Equal(new List<PcfCommon.SubjectType>(), variantInfo2.ApplicableSubjectTypes);

            var applicableDSubjectTypes3 = new List<PcfCommon.SubjectType> { PcfCommon.SubjectType.Demographic, PcfCommon.SubjectType.Aad2 };
            Assert.Equal(applicableDSubjectTypes3, variantInfo3.ApplicableSubjectTypes);
        }
    }
}
