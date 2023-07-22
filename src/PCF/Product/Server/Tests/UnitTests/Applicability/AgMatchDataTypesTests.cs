namespace PCF.UnitTests.Applicability
{
    using System.Linq;

    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache;
    using PCF.UnitTests.Pdms;
    using Xunit;
    using Xunit.Abstractions;

    [Trait("Category", "UnitTest")]
    public class AgMatchDataTypesTests : INeedDataBuilders
    {
        private readonly ITestOutputHelper outputHelper;

        public AgMatchDataTypesTests(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }
        
        // Delete: the data type in the Command is present in the AssetGroup Data Type list 
        [Fact]
        public void CommandInAssetGroupDataTypeList()
        {
            this.CommandMatchesApplicableAssetGroupDataTypes(
                true,
                PcfTestCapability.Delete,
                new[] { PcfTestDataType.BrowsingHistory },
                new[] { PcfTestDataType.BrowsingHistory },
                commandSubjectType: PdmsSubjectType.MSAUser);
        }
        
        // Delete: the data type in the Command is NOT present in the AssetGroup Data Type list 
        [Fact]
        public void CommandNotInAssetGroupDataTypeList()
        {
            this.CommandMatchesApplicableAssetGroupDataTypes(
                false,
                PcfTestCapability.Delete,
                new[] { PcfTestDataType.ContentConsumption, PcfTestDataType.PreciseUserLocation },
                new[] { PcfTestDataType.BrowsingHistory },
                commandSubjectType: PdmsSubjectType.DeviceOther);
        }
        
        // AccountClose: Always TRUE 
        [Fact]
        public void AccountCloseAlwaysMatches_1()
        {
            this.CommandMatchesApplicableAssetGroupDataTypes(
                true,
                PcfTestCapability.AccountClose,
                new[] { PcfTestDataType.ContentConsumption, PcfTestDataType.PreciseUserLocation },
                new[] { PcfTestDataType.BrowsingHistory });
        }

        [Fact]
        public void AccountCloseAlwaysMatches_2()
        {
            this.CommandMatchesApplicableAssetGroupDataTypes(
                true,
                PcfTestCapability.AccountClose,
                new[] { PcfTestDataType.BrowsingHistory },
                new[] { PcfTestDataType.BrowsingHistory });
        }

        [Fact]
        public void AccountCloseAlwaysMatches_3()
        {
            this.CommandMatchesApplicableAssetGroupDataTypes(
                true,
                PcfTestCapability.AccountClose);
        }
        
        // Export: the data type in the Command is present in the AssetGroup Data Type list 
        [Fact]
        public void ExportDataTypeInAssetGroupList()
        {
            this.CommandMatchesApplicableAssetGroupDataTypes(
                true,
                PcfTestCapability.Export,
                new[] { PcfTestDataType.BrowsingHistory },
                new[] { PcfTestDataType.BrowsingHistory });
        }

        // Export: the data type in the Command is NOT present in the AssetGroup Data Type list 
        [Fact]
        public void ExportDataTypeNotInAssetGroupList()
        {
            this.CommandMatchesApplicableAssetGroupDataTypes(
                false,
                PcfTestCapability.Export,
                new[] { PcfTestDataType.BrowsingHistory },
                new[] { PcfTestDataType.ContentConsumption, PcfTestDataType.PreciseUserLocation });
        }

        // AssetGroup DataType: Any
        [Fact]
        public void DeleteMatchesAny()
        {
            this.CommandMatchesApplicableAssetGroupDataTypes(
                true,
                PcfTestCapability.Delete,
                new[] { PcfTestDataType.Any },
                commandSubjectType: PdmsSubjectType.DemographicUser);
        }
        
        [Fact]
        public void ExportMatchesAny()
        {
            this.CommandMatchesApplicableAssetGroupDataTypes(
                true,
                PcfTestCapability.Export,
                new[] { PcfTestDataType.Any });
        }

        [Fact]
        public void AccountCloseMatchesAny()
        {
            this.CommandMatchesApplicableAssetGroupDataTypes(
                true,
                PcfTestCapability.AccountClose,
                new[] { PcfTestDataType.Any });
        }

        private void CommandMatchesApplicableAssetGroupDataTypes(
            bool expectedResult,
            PcfTestCapability commandCapability,
            PcfTestDataType[] assetGroupDataTypes = null,
            PcfTestDataType[] commandDataTypes = null,
            PcfTestCapability[] assetGroupCapability = null,
            PdmsSubjectType[] assetGroupSubjectTypes = null,
            PdmsSubjectType? commandSubjectType = null)
        {
            assetGroupDataTypes = assetGroupDataTypes ?? this.AnInstanceOf<PcfTestDataType[]>();
            commandDataTypes = commandDataTypes ?? this.AnInstanceOf<PcfTestDataType[]>();
            assetGroupCapability = assetGroupCapability ?? this.AnInstanceOf<PcfTestCapability[]>();
            assetGroupSubjectTypes = assetGroupSubjectTypes ?? new[]  { PdmsSubjectType.AADUser, PdmsSubjectType.MSAUser };
            commandSubjectType = commandSubjectType ?? PdmsSubjectType.MSAUser;

            var assetGroupInfoDocument = this.AnAssetGroupInfoDocument().Build();

            this.outputHelper.WriteLine($"CommandCapability: ({commandCapability})");
            this.outputHelper.WriteLine($"CommandSubjectType: ({commandSubjectType})");
            this.outputHelper.WriteLine($"CommandDataTypes: ({string.Join(",", commandDataTypes)})");

            this.outputHelper.WriteLine($"AssetGroupCapability: ({string.Join(",", assetGroupCapability)})");
            this.outputHelper.WriteLine($"AssetGroupSubjectTypes: ({string.Join(",", assetGroupSubjectTypes)})");
            this.outputHelper.WriteLine($"AssetGroupDataTypes: ({string.Join(",", assetGroupDataTypes)})");

            this.outputHelper.WriteLine($"Expected ({expectedResult}): command data types match asset group data types.");

            PdmsTestHelpers.SetValueToAssetGroupInfo(
                assetGroupInfoDocument,
                "AssetType=AzureTable;AccountName=pcfTest;TableName=pcfactionable",
                assetGroupCapability.Select(o => o.ToString()).ToArray(),
                assetGroupSubjectTypes.Select(o => o.ToString()).ToArray(),
                assetGroupDataTypes.Select(o => o.ToString()).ToArray());

            var assetGroupInfo = new AssetGroupInfo(assetGroupInfoDocument, true);

            PrivacyCommand command = PdmsTestHelpers.CreatePrivacyCommand(
                commandCapability,
                commandSubjectType.Value,
                commandDataTypes,
                assetGroupInfo,
                null);

            Assert.True(command.CommandType == commandCapability.GetPrivacyCommandType());
            Assert.True(command.AreDataTypesApplicable(assetGroupInfo.SupportedDataTypes) == expectedResult);
        }
    }
}
