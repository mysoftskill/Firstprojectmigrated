namespace PCF.UnitTests.Applicability
{
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.SignalApplicability;
    using Ploeh.AutoFixture.Xunit2;

    using System;

    using Xunit;
    using Xunit.Abstractions;

    [Trait("Category", "UnitTest")]
    public class InvalidAssetGroupInfoTest : INeedDataBuilders
    {
        private readonly ITestOutputHelper outputHelper;

        public InvalidAssetGroupInfoTest(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }

        [Theory]
        [InlineAutoData(PrivacyCommandType.AccountClose)]
        [InlineAutoData(PrivacyCommandType.Export)]
        [InlineAutoData(PrivacyCommandType.Delete)]
        public void InvalidAssetGroupInfo_CapabilityIsEmpty(PrivacyCommandType privacyCommandType)
        {
            var command = ApplicabilityTestHelper.CreatePrivacyCommand(this, privacyCommandType);
            var testHelper = new ApplicabilityTestHelper(this.outputHelper, this);

            testHelper.AssetGroupInfoDocument.Capabilities = Array.Empty<string>();
            testHelper.RunIsCommandActionableTest(ApplicabilityReasonCode.AssetGroupInfoIsInvalid, command);
        }

        [Theory]
        [InlineAutoData(PrivacyCommandType.AccountClose)]
        [InlineAutoData(PrivacyCommandType.Export)]
        [InlineAutoData(PrivacyCommandType.Delete)]
        public void InvalidAssetGroupInfo_SubjectTypesAreEmpty(PrivacyCommandType privacyCommandType)
        {
            var command = ApplicabilityTestHelper.CreatePrivacyCommand(this, privacyCommandType);
            var testHelper = new ApplicabilityTestHelper(this.outputHelper, this);

            testHelper.AssetGroupInfoDocument.SubjectTypes = Array.Empty<string>();
            testHelper.RunIsCommandActionableTest(ApplicabilityReasonCode.AssetGroupInfoIsInvalid, command);
        }

        [Theory]
        [InlineAutoData(PrivacyCommandType.AccountClose)]
        [InlineAutoData(PrivacyCommandType.Export)]
        [InlineAutoData(PrivacyCommandType.Delete)]
        public void InvalidAssetGroupInfo_DataTypesAreEmpty(PrivacyCommandType privacyCommandType)
        {
            var command = ApplicabilityTestHelper.CreatePrivacyCommand(this, privacyCommandType);
            var testHelper = new ApplicabilityTestHelper(this.outputHelper, this);

            testHelper.AssetGroupInfoDocument.DataTypes = Array.Empty<string>();
            testHelper.RunIsCommandActionableTest(ApplicabilityReasonCode.AssetGroupInfoIsInvalid, command);
        }

        
        [Theory]
        [InlineAutoData(PrivacyCommandType.AccountClose)]
        [InlineAutoData(PrivacyCommandType.Export)]
        [InlineAutoData(PrivacyCommandType.Delete)]
        public void InvalidAssetGroupInfo_DeploymentLocationIsNull(PrivacyCommandType privacyCommandType)
        {
            using (new FlightEnabled(FlightingNames.CloudInstanceConfigMissingFallbackDisabled))
            {
                var command = ApplicabilityTestHelper.CreatePrivacyCommand(this, privacyCommandType);
                var testHelper = new ApplicabilityTestHelper(this.outputHelper, this);

                testHelper.AssetGroupInfoDocument.DeploymentLocation = null;
                testHelper.RunIsCommandActionableTest(ApplicabilityReasonCode.None, command, true);
            }

            using (new FlightDisabled(FlightingNames.CloudInstanceConfigMissingFallbackDisabled))
            {
                var command = ApplicabilityTestHelper.CreatePrivacyCommand(this, privacyCommandType);
                var testHelper = new ApplicabilityTestHelper(this.outputHelper, this);

                testHelper.AssetGroupInfoDocument.DeploymentLocation = null;
                testHelper.RunIsCommandActionableTest(ApplicabilityReasonCode.None, command, true);
            }

            using (new FlightEnabled(FlightingNames.CloudInstanceConfigMissingFallbackDisabled))
            {
                var command = ApplicabilityTestHelper.CreatePrivacyCommand(this, privacyCommandType);
                var testHelper = new ApplicabilityTestHelper(this.outputHelper, this);

                testHelper.AssetGroupInfoDocument.DeploymentLocation = null;
                Assert.Throws<InvalidOperationException>(() => testHelper.RunIsCommandActionableTest(ApplicabilityReasonCode.None, command, false));
            }

            using (new FlightDisabled(FlightingNames.CloudInstanceConfigMissingFallbackDisabled))
            {
                var command = ApplicabilityTestHelper.CreatePrivacyCommand(this, privacyCommandType);
                var testHelper = new ApplicabilityTestHelper(this.outputHelper, this);

                testHelper.AssetGroupInfoDocument.DeploymentLocation = null;
                testHelper.RunIsCommandActionableTest(ApplicabilityReasonCode.None, command, false);
            }
        }

        [Theory]
        [InlineAutoData(PrivacyCommandType.AccountClose)]
        [InlineAutoData(PrivacyCommandType.Export)]
        [InlineAutoData(PrivacyCommandType.Delete)]
        public void InvalidAssetGroupInfo_SupportedCloudInstancesIsNullOrEmpty(PrivacyCommandType privacyCommandType)
        {
            using (new FlightEnabled(FlightingNames.CloudInstanceConfigMissingFallbackDisabled))
            {
                var command = ApplicabilityTestHelper.CreatePrivacyCommand(this, privacyCommandType);
                var testHelper = new ApplicabilityTestHelper(this.outputHelper, this);

                testHelper.AssetGroupInfoDocument.SupportedCloudInstances = null;
                testHelper.RunIsCommandActionableTest(ApplicabilityReasonCode.None, command, true);

                testHelper.AssetGroupInfoDocument.SupportedCloudInstances = new string[0];
                testHelper.RunIsCommandActionableTest(ApplicabilityReasonCode.None, command, true);
            }

            using (new FlightDisabled(FlightingNames.CloudInstanceConfigMissingFallbackDisabled))
            {
                var command = ApplicabilityTestHelper.CreatePrivacyCommand(this, privacyCommandType);
                var testHelper = new ApplicabilityTestHelper(this.outputHelper, this);

                testHelper.AssetGroupInfoDocument.SupportedCloudInstances = null;
                testHelper.RunIsCommandActionableTest(ApplicabilityReasonCode.None, command, true);

                testHelper.AssetGroupInfoDocument.SupportedCloudInstances = new string[0];
                testHelper.RunIsCommandActionableTest(ApplicabilityReasonCode.None, command, true);
            }

            using (new FlightEnabled(FlightingNames.CloudInstanceConfigMissingFallbackDisabled))
            {
                var command = ApplicabilityTestHelper.CreatePrivacyCommand(this, privacyCommandType);
                var testHelper = new ApplicabilityTestHelper(this.outputHelper, this);

                testHelper.AssetGroupInfoDocument.SupportedCloudInstances = null;
                Assert.Throws<InvalidOperationException>(() => testHelper.RunIsCommandActionableTest(ApplicabilityReasonCode.None, command, false));

                testHelper.AssetGroupInfoDocument.SupportedCloudInstances = new string[0];
                Assert.Throws<InvalidOperationException>(() => testHelper.RunIsCommandActionableTest(ApplicabilityReasonCode.None, command, false));
            }

            using (new FlightDisabled(FlightingNames.CloudInstanceConfigMissingFallbackDisabled))
            {
                var command = ApplicabilityTestHelper.CreatePrivacyCommand(this, privacyCommandType);
                var testHelper = new ApplicabilityTestHelper(this.outputHelper, this);

                testHelper.AssetGroupInfoDocument.SupportedCloudInstances = null;
                testHelper.RunIsCommandActionableTest(ApplicabilityReasonCode.None, command, false);

                testHelper.AssetGroupInfoDocument.SupportedCloudInstances = new string[0];
                testHelper.RunIsCommandActionableTest(ApplicabilityReasonCode.None, command, false);
            }
        }

        [Theory]
        [InlineData(SubjectType.Msa, "Public", new[] { "CN.Azure.Mooncake", "US.Azure.Fairfax" }, ApplicabilityReasonCode.None)]
        [InlineData(SubjectType.Msa, "Public", new[] { "US.Azure.Fairfax" }, ApplicabilityReasonCode.None)]
        [InlineData(SubjectType.Device, "Public", new[] { "CN.Azure.Mooncake", "US.Azure.Fairfax" }, ApplicabilityReasonCode.None)]
        [InlineData(SubjectType.Device, "Public", new[] { "US.Azure.Fairfax" }, ApplicabilityReasonCode.None)]
        [InlineData(SubjectType.Demographic, "Public", new[] { "CN.Azure.Mooncake", "US.Azure.Fairfax" }, ApplicabilityReasonCode.None)]
        [InlineData(SubjectType.Demographic, "Public", new[] { "US.Azure.Fairfax" }, ApplicabilityReasonCode.None)]
        [InlineData(SubjectType.MicrosoftEmployee, "Public", new[] { "CN.Azure.Mooncake", "US.Azure.Fairfax" }, ApplicabilityReasonCode.None)]
        [InlineData(SubjectType.MicrosoftEmployee, "Public", new[] { "US.Azure.Fairfax" }, ApplicabilityReasonCode.None)]
        [InlineData(SubjectType.Aad, "Public", new[] { "Public" }, ApplicabilityReasonCode.None)]
        [InlineData(SubjectType.Aad, "Public", new[] { "Public", "CN.Azure.Mooncake", "US.Azure.Fairfax" }, ApplicabilityReasonCode.None)]
        [InlineData(SubjectType.Aad, "Public", new[] { "CN.Azure.Mooncake", "US.Azure.Fairfax" }, ApplicabilityReasonCode.DoesNotMatchAssetGroupSupportedCloudInstances)]
        [InlineData(SubjectType.Aad, "Public", new[] { "US.Azure.Fairfax" }, ApplicabilityReasonCode.DoesNotMatchAssetGroupSupportedCloudInstances)]
        [InlineData(SubjectType.Aad, "Public", new[] { "All" }, ApplicabilityReasonCode.None)]
        [InlineData(SubjectType.Aad, "US.Azure.Fairfax", new[] { "US.Azure.Fairfax" }, ApplicabilityReasonCode.None)]
        [InlineData(SubjectType.Aad, "US.Azure.Fairfax", new[] { "All" }, ApplicabilityReasonCode.AssetGroupInfoIsInvalid)]
        [InlineData(SubjectType.Aad, "US.Azure.Fairfax", new[] { "Public" }, ApplicabilityReasonCode.AssetGroupInfoIsInvalid)]
        [InlineData(SubjectType.Aad, "US.Azure.Fairfax", new[] { "Public", "US.Azure.Fairfax" }, ApplicabilityReasonCode.AssetGroupInfoIsInvalid)]
        [InlineData(SubjectType.Aad, "CN.Azure.Mooncake", new[] { "US.Azure.Fairfax" }, ApplicabilityReasonCode.AssetGroupInfoIsInvalid)]
        [InlineData(SubjectType.Aad, "US.Azure.Fairfax", new[] { "CN.Azure.Mooncake" }, ApplicabilityReasonCode.AssetGroupInfoIsInvalid)]
        [InlineData(SubjectType.Aad, "US.Azure.Fairfax", new[] { "Public", "US.Azure.Fairfax", "CN.Azure.Mooncake" }, ApplicabilityReasonCode.AssetGroupInfoIsInvalid)]
        public void InvalidAssetGroupInfo_CloudInstanceConfiguration(SubjectType subjectType, string deploymentLocation, string[] supportedCloudInstances, ApplicabilityReasonCode expectedResult)
        {
            var command = ApplicabilityTestHelper.CreatePrivacyCommand(this, PrivacyCommandType.AccountClose);
            command.CloudInstance = deploymentLocation;
            command.Subject = ApplicabilityTestHelper.CreatePrivacySubject(this, subjectType);
            var testHelper = new ApplicabilityTestHelper(this.outputHelper, this);

            testHelper.AssetGroupInfoDocument.DeploymentLocation = deploymentLocation;
            testHelper.AssetGroupInfoDocument.SupportedCloudInstances = supportedCloudInstances;

            testHelper.RunIsCommandActionableTest(expectedResult, command);
        }
    }
}
