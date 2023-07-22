namespace PCF.UnitTests.Applicability
{
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.Applicability;
    using Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache;
    using Microsoft.PrivacyServices.SignalApplicability;
    using Moq;
    using System;
    using Xunit;
    using Xunit.Abstractions;
    using PcfCommon = Microsoft.PrivacyServices.CommandFeed.Service.Common;

    public class ApplicabilityTestHelper
    {
        private readonly ITestOutputHelper outputHelper;

        public ApplicabilityTestHelper(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }

        public ApplicabilityTestHelper(ITestOutputHelper outputHelper, INeedDataBuilders dataBuilders)
        {
            this.outputHelper = outputHelper;

            var assetGroupInfoDocument = dataBuilders.AnAssetGroupInfoDocument().Build();
            assetGroupInfoDocument.IsDeprecated = false;

            this.AssetGroupInfoDocument = assetGroupInfoDocument;
            this.IsAgentOnline = true;
            this.IsSyntheticCommand = false;
        }

        public AssetGroupInfoDocument AssetGroupInfoDocument { get; set; }

        public bool IsAgentOnline { get; set; }

        public bool IsSyntheticCommand { get; set; }

        public void RunIsCommandActionableTest(
            ApplicabilityReasonCode expectedResult,
            PcfCommon.PrivacyCommand privacyCommand,
            bool enableTolerantParsing = false)
        {
            var mocAgentInfo = new Mock<IDataAgentInfo>();
            mocAgentInfo.SetupGet(a => a.IsOnline).Returns(this.IsAgentOnline);
            var assetGroupInfo = new AssetGroupInfo(this.AssetGroupInfoDocument, enableTolerantParsing)
            {
                AgentInfo = mocAgentInfo.Object
            };

            privacyCommand.IsSyntheticTestCommand = this.IsSyntheticCommand;

            // Check final error code
            assetGroupInfo.IsCommandActionable(privacyCommand, out var applicabilityResult);
            Assert.Equal(expectedResult, applicabilityResult.ReasonCode);
        }

        public static IPrivacySubject CreatePrivacySubject(INeedDataBuilders testDataBuilder, SubjectType subjectType)
        {
            switch (subjectType)
            {
                case SubjectType.Aad:
                    return testDataBuilder.AnAadSubject().Build();
                case SubjectType.Aad2:
                    return testDataBuilder.AnAadSubject2().Build();
                case SubjectType.Demographic:
                    return testDataBuilder.ADemographicSubject().Build();
                case SubjectType.MicrosoftEmployee:
                    return testDataBuilder.AMicrosoftEmployeeSubject().Build();
                case SubjectType.Device:
                    return testDataBuilder.ADeviceSubject().Build();
                case SubjectType.NonWindowsDevice:
                    return testDataBuilder.ANonWindowsDeviceSubject().Build();
                case SubjectType.EdgeBrowser:
                    return testDataBuilder.AEdgeBrowserSubject().Build();
                case SubjectType.Msa:
                    return testDataBuilder.AnMsaSubject().Build();
                default:
                    throw new ArgumentOutOfRangeException(nameof(subjectType), $"Unknown subject: {subjectType}.");
            }
        }

        public static PcfCommon.PrivacyCommand CreatePrivacyCommand(
            INeedDataBuilders testDataBuilder,
            PrivacyCommandType privacyCommandType)
        {
            switch (privacyCommandType)
            {
                case PrivacyCommandType.AccountClose:
                    return testDataBuilder.AnAccountCloseCommand().Build();
                case PrivacyCommandType.Export:
                    return testDataBuilder.AnExportCommand().Build();
                case PrivacyCommandType.Delete:
                    return testDataBuilder.ADeleteCommand().Build();
                default:
                    throw new ArgumentOutOfRangeException(nameof(privacyCommandType), $"Unknown command type: {privacyCommandType}.");
            }
        }
    }
}
