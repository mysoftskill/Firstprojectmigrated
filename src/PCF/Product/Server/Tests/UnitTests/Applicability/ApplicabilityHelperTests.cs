namespace PCF.UnitTests.Applicability
{
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.Applicability;
    using Microsoft.PrivacyServices.SignalApplicability;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;

    [Trait("Category", "UnitTest")]
    public class ApplicabilityHelperTests
    {
        [Theory]
        [InlineAutoData(ApplicabilityReasonCode.AssetGroupInfoIsDeprecated)]
        [InlineAutoData(ApplicabilityReasonCode.AssetGroupInfoIsInvalid)]
        [InlineAutoData(ApplicabilityReasonCode.AssetGroupNotOptInToMsaAgeOut)]
        [InlineAutoData(ApplicabilityReasonCode.AssetGroupStartTimeLaterThanRequestTimeStamp)]
        [InlineAutoData(ApplicabilityReasonCode.DoesNotMatchAadSubjectTenantId)]
        [InlineAutoData(ApplicabilityReasonCode.DoesNotMatchAssetGroupCapability)]
        [InlineAutoData(ApplicabilityReasonCode.DoesNotMatchAssetGroupDataTypes)]
        [InlineAutoData(ApplicabilityReasonCode.DoesNotMatchAssetGroupSubjects)]
        [InlineAutoData(ApplicabilityReasonCode.DoesNotMatchAssetGroupSupportedCloudInstances)]
        [InlineAutoData(ApplicabilityReasonCode.IgnoreAccountCloseForEmployeeControllerDataTypes)]
        public void IsApplicabilityResultTagDependentReturnTrueForTagDependentResultCodes(ApplicabilityReasonCode applicabilityReasonCode)
        {
            Assert.True(ApplicabilityHelper.IsApplicabilityResultTagDependent(applicabilityReasonCode));
        }

        [Theory]
        [InlineAutoData(ApplicabilityReasonCode.AgentIsBlocked)]
        [InlineAutoData(ApplicabilityReasonCode.AssetGroupIsBlocked)]
        [InlineAutoData(ApplicabilityReasonCode.FilteredByVariant)]
        [InlineAutoData(ApplicabilityReasonCode.GreaterThanMaxDataAgeWindow)]
        [InlineAutoData(ApplicabilityReasonCode.None)]
        [InlineAutoData(ApplicabilityReasonCode.RequestTypeDoesNotMatchBulkOnlyAssetGroup)]
        [InlineAutoData(ApplicabilityReasonCode.SyntheticTestCommand)]
        [InlineAutoData(ApplicabilityReasonCode.TipAgentIsNotOnline)]
        [InlineAutoData(ApplicabilityReasonCode.TipAgentNotInTestTenantIdFlight)]
        [InlineAutoData(ApplicabilityReasonCode.TipAgentShouldNotReceiveProdCommands)]
        public void IsApplicabilityResultTagDependentReturnFalseForTagNondependentResultCodes(ApplicabilityReasonCode applicabilityReasonCode)
        {
            Assert.False(ApplicabilityHelper.IsApplicabilityResultTagDependent(applicabilityReasonCode));
        }
    }
}
