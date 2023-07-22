namespace PCF.UnitTests.Applicability
{
    using System.Linq;

    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache;
    using PCF.UnitTests.Pdms;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Tests AssetGroupVariantInfo 
    /// </summary>
    [Trait("Category", "UnitTest")]
    public class AssetGroupVariantInfoTests : INeedDataBuilders
    {
        private readonly ITestOutputHelper outputHelper;

        public AssetGroupVariantInfoTests(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }

        [Fact]
        public void IsApplicableToCommandTest_1()
        {
            this.IsApplicableToCommandTest(
                true,
                PcfTestCapability.Delete,
                PdmsSubjectType.DemographicUser,
                new PcfTestDataType[] { PcfTestDataType.BrowsingHistory },
                new PcfTestCapability[] { },
                new PdmsSubjectType[] { },
                new PcfTestDataType[] { });
        }

        [Fact]
        public void IsApplicableToCommandTest_2()
        {
            this.IsApplicableToCommandTest(
                true,
                PcfTestCapability.Delete,
                PdmsSubjectType.DemographicUser,
                new PcfTestDataType[] { PcfTestDataType.BrowsingHistory },
                new PcfTestCapability[] { PcfTestCapability.Delete },
                new PdmsSubjectType[] { },
                new PcfTestDataType[] { });
        }

        [Fact]
        public void IsApplicableToCommandTest_3()
        {
            this.IsApplicableToCommandTest(
                true,
                PcfTestCapability.Delete,
                PdmsSubjectType.DemographicUser,
                new PcfTestDataType[] { PcfTestDataType.BrowsingHistory },
                new PcfTestCapability[] { },
                new PdmsSubjectType[] { PdmsSubjectType.DemographicUser },
                new PcfTestDataType[] { });
        }

        [Fact]
        public void IsApplicableToCommandTest_4()
        {
            this.IsApplicableToCommandTest(
                true,
                PcfTestCapability.Delete,
                PdmsSubjectType.DemographicUser,
                new PcfTestDataType[] { PcfTestDataType.BrowsingHistory },
                new PcfTestCapability[] { },
                new PdmsSubjectType[] { },
                new PcfTestDataType[] { PcfTestDataType.BrowsingHistory });
        }

        [Fact]
        public void IsApplicableToCommandTest_5()
        {
            this.IsApplicableToCommandTest(
                false,
                PcfTestCapability.Delete,
                PdmsSubjectType.DemographicUser,
                new PcfTestDataType[] { PcfTestDataType.BrowsingHistory },
                new PcfTestCapability[] { PcfTestCapability.Export },
                new PdmsSubjectType[] { },
                new PcfTestDataType[] { });
        }

        [Fact]
        public void IsApplicableToCommandTest_6()
        {
            this.IsApplicableToCommandTest(
                false,
                PcfTestCapability.Delete,
                PdmsSubjectType.DemographicUser,
                new PcfTestDataType[] { PcfTestDataType.BrowsingHistory },
                new PcfTestCapability[] { },
                new PdmsSubjectType[] { PdmsSubjectType.AADUser },
                new PcfTestDataType[] { });
        }

        [Fact]
        public void IsApplicableToCommandTest_7()
        {
            this.IsApplicableToCommandTest(
                false,
                PcfTestCapability.Delete,
                PdmsSubjectType.DemographicUser,
                new PcfTestDataType[] { PcfTestDataType.BrowsingHistory },
                new PcfTestCapability[] { },
                new PdmsSubjectType[] { },
                new PcfTestDataType[] { PcfTestDataType.ProductAndServicePerformance });
        }

        [Fact]
        public void IsApplicableToCommandTest_8()
        {
            this.IsApplicableToCommandTest(
                expectedResult: false,
                commandCapability: PcfTestCapability.Delete,
                commandSubjectType: PdmsSubjectType.Windows10Device,
                commandDataTypes: new PcfTestDataType[] { PcfTestDataType.InkingTypingAndSpeechUtterance },
                variantCapabilities: new PcfTestCapability[] { },
                variantSubjectTypes: new PdmsSubjectType[] { },
                variantDataTypes: new PcfTestDataType[] { PcfTestDataType.ProductAndServicePerformance });
        }

        [Fact]
        public void IsApplicableToCommandTest_9()
        {
            this.IsApplicableToCommandTest(
                expectedResult: true,
                commandCapability: PcfTestCapability.Delete,
                commandSubjectType: PdmsSubjectType.NonWindowsDevice,
                commandDataTypes: new PcfTestDataType[] { PcfTestDataType.InkingTypingAndSpeechUtterance },
                variantCapabilities: new PcfTestCapability[] { PcfTestCapability.Delete , PcfTestCapability.AccountClose },
                variantSubjectTypes: new PdmsSubjectType[] { PdmsSubjectType.NonWindowsDevice },
                variantDataTypes: new PcfTestDataType[] { PcfTestDataType.InkingTypingAndSpeechUtterance });
        }

        [Fact]
        public void IsApplicableToCommandTest_10()
        {
            this.IsApplicableToCommandTest(
                expectedResult: true,
                commandCapability: PcfTestCapability.Delete,
                commandSubjectType: PdmsSubjectType.EdgeBrowser,
                commandDataTypes: new PcfTestDataType[] { PcfTestDataType.InkingTypingAndSpeechUtterance },
                variantCapabilities: new PcfTestCapability[] { PcfTestCapability.Delete, PcfTestCapability.AccountClose },
                variantSubjectTypes: new PdmsSubjectType[] { PdmsSubjectType.EdgeBrowser },
                variantDataTypes: new PcfTestDataType[] { PcfTestDataType.InkingTypingAndSpeechUtterance });
        }

        private void IsApplicableToCommandTest(
            bool expectedResult,
            PcfTestCapability commandCapability,
            PdmsSubjectType commandSubjectType,
            PcfTestDataType[] commandDataTypes,
            PcfTestCapability[] variantCapabilities,
            PdmsSubjectType[] variantSubjectTypes,
            PcfTestDataType[] variantDataTypes)
        {
            AssetGroupVariantInfoDocument variantInfoDocument = this.AnAssetGroupVariantInfoDocument()
                .With(x => x.DataTypes, variantDataTypes.Select(o => o.ToString()).ToArray())
                .With(x => x.SubjectTypes, variantSubjectTypes.Select(o => o.ToString()).ToArray())
                .With(x => x.Capabilities, variantCapabilities.Select(x => x.ToString()).ToArray())
                .Build();

            var variantInfo = new AssetGroupVariantInfo(variantInfoDocument, false);
            
            var assetGroupInfoDocument = this.AnAssetGroupInfoDocument().With(x => x.VariantInfosAppliedByAgents, new[] { variantInfoDocument }).Build();
            var assetGroupInfo = new AssetGroupInfo(assetGroupInfoDocument, false);
            
            this.outputHelper.WriteLine($"CommandCapability: ({commandCapability})");
            this.outputHelper.WriteLine($"CommandSubjectType: ({commandSubjectType})");
            this.outputHelper.WriteLine($"CommandDataTypes: ({string.Join(",", commandDataTypes)})");

            this.outputHelper.WriteLine($"AssetGroupCapability: ({string.Join(",", variantCapabilities)})");
            this.outputHelper.WriteLine($"AssetGroupSubjectTypes: ({string.Join(",", variantSubjectTypes)})");
            this.outputHelper.WriteLine($"AssetGroupDataTypes: ({string.Join(",", variantDataTypes)})");
            
            PrivacyCommand command = PdmsTestHelpers.CreatePrivacyCommand(
                commandCapability,
                commandSubjectType,
                commandDataTypes,
                assetGroupInfo,
                null);

            Assert.True(variantInfo.IsApplicableToCommand(command, false) == expectedResult);
        }
    }
}
