namespace PCF.UnitTests.Applicability
{
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.SignalApplicability;
    using Xunit;
    using Xunit.Abstractions;

    [Trait("Category", "UnitTest")]
    public class CloudInstanceApplicabilityTest : INeedDataBuilders
    {
        private readonly ITestOutputHelper outputHelper;

        public CloudInstanceApplicabilityTest(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }

        [Theory]
        //// MSA
        [InlineData(SubjectType.Msa, null, new[] { "Public" }, ApplicabilityReasonCode.None)]
        [InlineData(SubjectType.Msa, "Public", new[] { "Public" }, ApplicabilityReasonCode.None)]
        [InlineData(SubjectType.Msa, "Public", new[] { "Public", "CN.Azure.Mooncake", "US.Azure.Fairfax" }, ApplicabilityReasonCode.None)]
        [InlineData(SubjectType.Msa, "Public", new[] { "CN.Azure.Mooncake", "US.Azure.Fairfax" }, ApplicabilityReasonCode.None)]
        [InlineData(SubjectType.Msa, "Public", new[] { "All" }, ApplicabilityReasonCode.None)]
        //// Demographic
        [InlineData(SubjectType.Demographic, null, new[] { "Public" }, ApplicabilityReasonCode.None)]
        [InlineData(SubjectType.Demographic, "Public", new[] { "Public" }, ApplicabilityReasonCode.None)]
        [InlineData(SubjectType.Demographic, "Public", new[] { "Public", "CN.Azure.Mooncake", "US.Azure.Fairfax" }, ApplicabilityReasonCode.None)]
        [InlineData(SubjectType.Demographic, "Public", new[] { "CN.Azure.Mooncake", "US.Azure.Fairfax" }, ApplicabilityReasonCode.None)]
        [InlineData(SubjectType.Demographic, "Public", new[] { "All" }, ApplicabilityReasonCode.None)]
        //// MicrosoftEmployee
        [InlineData(SubjectType.MicrosoftEmployee, null, new[] { "Public" }, ApplicabilityReasonCode.None)]
        [InlineData(SubjectType.MicrosoftEmployee, "Public", new[] { "Public" }, ApplicabilityReasonCode.None)]
        [InlineData(SubjectType.MicrosoftEmployee, "Public", new[] { "Public", "CN.Azure.Mooncake", "US.Azure.Fairfax" }, ApplicabilityReasonCode.None)]
        [InlineData(SubjectType.MicrosoftEmployee, "Public", new[] { "CN.Azure.Mooncake", "US.Azure.Fairfax" }, ApplicabilityReasonCode.None)]
        [InlineData(SubjectType.MicrosoftEmployee, "Public", new[] { "All" }, ApplicabilityReasonCode.None)]
        //// Device
        [InlineData(SubjectType.Device, null, new[] { "Public" }, ApplicabilityReasonCode.None)]
        [InlineData(SubjectType.Device, "Public", new[] { "Public" }, ApplicabilityReasonCode.None)]
        [InlineData(SubjectType.Device, "Public", new[] { "Public", "CN.Azure.Mooncake", "US.Azure.Fairfax" }, ApplicabilityReasonCode.None)]
        [InlineData(SubjectType.Device, "Public", new[] { "CN.Azure.Mooncake", "US.Azure.Fairfax" }, ApplicabilityReasonCode.None)]
        [InlineData(SubjectType.Device, "Public", new[] { "All" }, ApplicabilityReasonCode.None)]
        //// AAD
        [InlineData(SubjectType.Aad, null, new[] { "Public" }, ApplicabilityReasonCode.None)]
        [InlineData(SubjectType.Aad, "Public", new[] { "Public" }, ApplicabilityReasonCode.None)]
        [InlineData(SubjectType.Aad, "Public", new[] { "Public", "CN.Azure.Mooncake", "US.Azure.Fairfax" }, ApplicabilityReasonCode.None)]
        [InlineData(SubjectType.Aad, "Public", new[] { "CN.Azure.Mooncake", "US.Azure.Fairfax" }, ApplicabilityReasonCode.DoesNotMatchAssetGroupSupportedCloudInstances)]
        [InlineData(SubjectType.Aad, "Public", new[] { "All" }, ApplicabilityReasonCode.None)]
        [InlineData(SubjectType.Aad, "US.Azure.Fairfax", new[] { "US.Azure.Fairfax" }, ApplicabilityReasonCode.None)]
        [InlineData(SubjectType.Aad, "US.Azure.Fairfax", new[] { "All" }, ApplicabilityReasonCode.None)]
        [InlineData(SubjectType.Aad, "US.Azure.Fairfax", new[] { "Public" }, ApplicabilityReasonCode.DoesNotMatchAssetGroupSupportedCloudInstances)]
        [InlineData(SubjectType.Aad, "US.Azure.Fairfax", new[] { "Public", "US.Azure.Fairfax" }, ApplicabilityReasonCode.None)]
        [InlineData(SubjectType.Aad, "US.Azure.Fairfax", new[] { "US.Azure.Fairfax", "US.Azure.Fairfax" }, ApplicabilityReasonCode.None)]
        [InlineData(SubjectType.Aad, "CN.Azure.Mooncake", new[] { "US.Azure.Fairfax" }, ApplicabilityReasonCode.DoesNotMatchAssetGroupSupportedCloudInstances)]
        [InlineData(SubjectType.Aad, "US.Azure.Fairfax", new[] { "Public", "US.Azure.Fairfax", "CN.Azure.Mooncake" }, ApplicabilityReasonCode.None)]
        public void SignalCloudInstanceConfiguration(SubjectType subjectType, string signalCloudInstance, string[] supportedCloudInstances, ApplicabilityReasonCode expectedResult)
        {
            var command = ApplicabilityTestHelper.CreatePrivacyCommand(this, PrivacyCommandType.AccountClose);
            command.CloudInstance = signalCloudInstance;
            command.Subject = ApplicabilityTestHelper.CreatePrivacySubject(this, subjectType);

            var testHelper = new ApplicabilityTestHelper(this.outputHelper, this);

            testHelper.AssetGroupInfoDocument.DeploymentLocation = "Public";
            testHelper.AssetGroupInfoDocument.SupportedCloudInstances = supportedCloudInstances;

            testHelper.RunIsCommandActionableTest(expectedResult, command);
        }
    }
}
