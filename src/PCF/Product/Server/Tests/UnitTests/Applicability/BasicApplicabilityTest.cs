namespace PCF.UnitTests.Applicability
{
    using System.Linq;

    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.SignalApplicability;
    using PCF.UnitTests;

    using Ploeh.AutoFixture.Xunit2;

    using Xunit;
    using Xunit.Abstractions;

    using PrivacyPolicy = Microsoft.PrivacyServices.Policy;
    using SubjectType = Microsoft.PrivacyServices.CommandFeed.Service.Common.SubjectType;

    /// <summary>
    /// Basic applicability test cases:
    /// - subject types match
    /// - data types match
    /// - capability
    /// </summary>
    [Trait("Category", "UnitTest")]
    public class ApplicabilityMatchTest : INeedDataBuilders
    {
        private readonly ITestOutputHelper outputHelper;

        public ApplicabilityMatchTest(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }

        [Theory]
        [InlineAutoData(PrivacyCommandType.AccountClose)]
        [InlineAutoData(PrivacyCommandType.Export)]
        [InlineAutoData(PrivacyCommandType.Delete)]
        public void ApplicabilityReasonCode_None(PrivacyCommandType privacyCommandType)
        {
            var command = ApplicabilityTestHelper.CreatePrivacyCommand(this, privacyCommandType);
            var testHelper = new ApplicabilityTestHelper(this.outputHelper, this);

            testHelper.RunIsCommandActionableTest(ApplicabilityReasonCode.None, command);
        }

        [Theory]
        [InlineAutoData(PrivacyCommandType.AccountClose, new string[] { }, new[] { "Any" })]
        [InlineAutoData(PrivacyCommandType.AccountClose, new string[] { }, new[] { "BrowsingHistory", "CustomerContact" })]
        [InlineAutoData(PrivacyCommandType.Delete, new[] { "Credentials" }, new[] { "Any" })]
        [InlineAutoData(PrivacyCommandType.Export, new[] { "Credentials" }, new[] { "Any" })]
        [InlineAutoData(PrivacyCommandType.Delete, new[] { "BrowsingHistory" }, new[] { "BrowsingHistory", "CustomerContact", "Credentials" })]
        [InlineAutoData(PrivacyCommandType.Export, new[] { "BrowsingHistory" }, new[] { "BrowsingHistory" })]
        public void CommandMatchDataTypes(PrivacyCommandType privacyCommandType, string[] commandDataTypeIds, string[] assetDataTypeIds)
        {
            var expected = ApplicabilityReasonCode.None;

            var command = ApplicabilityTestHelper.CreatePrivacyCommand(this, privacyCommandType);
            command.DataTypeIds = commandDataTypeIds.Select(x => PrivacyPolicy.Policies.Current.DataTypes.CreateId(x));

            var testHelper = new ApplicabilityTestHelper(this.outputHelper, this);
            testHelper.AssetGroupInfoDocument.DataTypes = assetDataTypeIds;

            testHelper.RunIsCommandActionableTest(expected, command);
        }

        [Theory]
        [InlineAutoData(PrivacyCommandType.AccountClose, new[] { "Delete", "Export" })]
        [InlineAutoData(PrivacyCommandType.Delete, new[] { "Export" })]
        [InlineAutoData(PrivacyCommandType.Export, new[] { "AccountClose" })]
        public void CommandDoesNotMatchCapability(PrivacyCommandType privacyCommandType, string[] assetCapabilityIds)
        {
            var expected = ApplicabilityReasonCode.DoesNotMatchAssetGroupCapability;

            var command = ApplicabilityTestHelper.CreatePrivacyCommand(this, privacyCommandType);
            var testHelper = new ApplicabilityTestHelper(this.outputHelper, this);

            testHelper.AssetGroupInfoDocument.Capabilities = assetCapabilityIds;

            testHelper.RunIsCommandActionableTest(expected, command);
        }
        [Theory]
        [InlineAutoData(PrivacyCommandType.AccountClose, SubjectType.Aad, new[] { "AADUser" })]
        [InlineAutoData(PrivacyCommandType.AccountClose, SubjectType.Aad2, new[] { "AADUser" })]
        [InlineAutoData(PrivacyCommandType.AccountClose, SubjectType.Aad2, new[] { "AADUser", "AADUser2" })]
        public void CommandDoesMatchSubjectTypes(PrivacyCommandType privacyCommandType, SubjectType subjectType, string[] assetSubjectTypeIds)
        {
            var expected = ApplicabilityReasonCode.None;

            var command = ApplicabilityTestHelper.CreatePrivacyCommand(this, privacyCommandType);
            command.Subject = ApplicabilityTestHelper.CreatePrivacySubject(this, subjectType);
            var testHelper = new ApplicabilityTestHelper(this.outputHelper, this);
            testHelper.AssetGroupInfoDocument.SubjectTypes = assetSubjectTypeIds;

            testHelper.RunIsCommandActionableTest(expected, command);
        }

        [Theory]
        [InlineAutoData(PrivacyCommandType.AccountClose, SubjectType.Aad, new[] { "MSAUser" })]
        [InlineAutoData(PrivacyCommandType.Delete, SubjectType.Demographic, new[] { "MSAUser", "AADUser" })]
        [InlineAutoData(PrivacyCommandType.Delete, SubjectType.MicrosoftEmployee, new[] { "MSAUser", "AADUser" })]
        [InlineAutoData(PrivacyCommandType.Export, SubjectType.Device, new[] { "DemographicUser" })]
        [InlineAutoData(PrivacyCommandType.Delete, SubjectType.Msa, new[] { "AADUser" })]
        [InlineAutoData(PrivacyCommandType.AccountClose, SubjectType.Aad, new[] { "AADUser2" })]
        [InlineAutoData(PrivacyCommandType.Delete, SubjectType.Msa, new[] { "Other" })]
        [InlineAutoData(PrivacyCommandType.Export, SubjectType.Aad2, new[] { "MSAUser" })]
        public void CommandDoesNotMatchSubjectTypes(PrivacyCommandType privacyCommandType, SubjectType subjectType, string[] assetSubjectTypeIds)
        {
            var expected = ApplicabilityReasonCode.DoesNotMatchAssetGroupSubjects;

            var command = ApplicabilityTestHelper.CreatePrivacyCommand(this, privacyCommandType);
            command.Subject = ApplicabilityTestHelper.CreatePrivacySubject(this, subjectType);
            var testHelper = new ApplicabilityTestHelper(this.outputHelper, this);
            testHelper.AssetGroupInfoDocument.SubjectTypes = assetSubjectTypeIds;

            testHelper.RunIsCommandActionableTest(expected, command);
        }

        [Theory]
        [InlineAutoData(PrivacyCommandType.Delete, new[] { "Credentials" }, new[] { "BrowsingHistory", "CustomerContact" })]
        [InlineAutoData(PrivacyCommandType.Export, new[] { "BrowsingHistory" }, new[] { "Credentials", "CustomerContact" })]
        public void CommandDoesNotMatchDataTypes(PrivacyCommandType privacyCommandType, string[] commandDataTypeIds, string[] assetDataTypeIds)
        {
            var expected = ApplicabilityReasonCode.DoesNotMatchAssetGroupDataTypes;

            var command = ApplicabilityTestHelper.CreatePrivacyCommand(this, privacyCommandType);
            command.DataTypeIds = commandDataTypeIds.Select(x => PrivacyPolicy.Policies.Current.DataTypes.CreateId(x));

            var testHelper = new ApplicabilityTestHelper(this.outputHelper, this);
            testHelper.AssetGroupInfoDocument.DataTypes = assetDataTypeIds;

            testHelper.RunIsCommandActionableTest(expected, command);
        }

        [Theory]
        [InlineAutoData(PrivacyCommandType.Delete, SubjectType.Aad, new[] { "MSAUser" }, new[] { "Credentials" }, new[] { "BrowsingHistory", "CustomerContact" })]
        [InlineAutoData(PrivacyCommandType.Export, SubjectType.Msa, new[] { "AADUser" }, new[] { "BrowsingHistory" }, new[] { "Credentials", "CustomerContact" })]
        public void CommandDoesNotMatchSubjectAndDataTypes(
            PrivacyCommandType privacyCommandType,
            SubjectType subjectType, 
            string[] assetSubjectTypeIds,
            string[] commandDataTypeIds, 
            string[] assetDataTypeIds)
        {
            var expected = ApplicabilityReasonCode.DoesNotMatchAssetGroupSubjects;

            var command = ApplicabilityTestHelper.CreatePrivacyCommand(this, privacyCommandType);
            command.Subject = ApplicabilityTestHelper.CreatePrivacySubject(this, subjectType);
            command.DataTypeIds = commandDataTypeIds.Select(x => PrivacyPolicy.Policies.Current.DataTypes.CreateId(x));

            var testHelper = new ApplicabilityTestHelper(this.outputHelper, this);
            testHelper.AssetGroupInfoDocument.DataTypes = assetDataTypeIds;
            testHelper.AssetGroupInfoDocument.SubjectTypes = assetSubjectTypeIds;

            testHelper.RunIsCommandActionableTest(expected, command);
        }

        [Fact]
        public void AssetGroupIsDeprecated_Drop()
        {
            var command = ApplicabilityTestHelper.CreatePrivacyCommand(this, PrivacyCommandType.AccountClose);
            var testHelper = new ApplicabilityTestHelper(this.outputHelper, this);
            testHelper.AssetGroupInfoDocument.IsDeprecated = true;

            testHelper.RunIsCommandActionableTest(ApplicabilityReasonCode.AssetGroupInfoIsDeprecated, command);
        }
    }
}
