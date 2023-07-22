namespace PCF.UnitTests.Applicability
{
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.SignalApplicability;
    using PCF.UnitTests;
    using Ploeh.AutoFixture.Xunit2;
    using System;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// TeanantId AAD subject applicability tests.
    /// </summary>
    [Trait("Category", "UnitTest")]
    public class AadTenantIdApplicabilityTest : INeedDataBuilders
    {
        private readonly ITestOutputHelper outputHelper;

        private const string HomeTenantId = "3CBD2892-03DD-4CBC-9E55-033740AD66C0";

        private const string ResourceTenantId = "08DB5B59-F96B-4E35-B8A1-91159B48D3FD";

        public AadTenantIdApplicabilityTest(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }

        [Theory]
        [InlineAutoData(PrivacyCommandType.AccountClose)]
        [InlineAutoData(PrivacyCommandType.Delete)]
        [InlineAutoData(PrivacyCommandType.Export)]
        public void AadSubjectTenantId_Match(PrivacyCommandType privacyCommandType)
        {
            var expected = ApplicabilityReasonCode.None;
            var tenantIdGuid = Guid.NewGuid();

            var command = ApplicabilityTestHelper.CreatePrivacyCommand(this, privacyCommandType);
            var aadSubject = (AadSubject)ApplicabilityTestHelper.CreatePrivacySubject(this, SubjectType.Aad);
            aadSubject.TenantId = tenantIdGuid;
            command.Subject = aadSubject;

            var testHelper = new ApplicabilityTestHelper(this.outputHelper, this);
            testHelper.AssetGroupInfoDocument.TenantIds = new[] { tenantIdGuid.ToString() };

            testHelper.RunIsCommandActionableTest(expected, command);
        }

        [Theory]
        [InlineAutoData(PrivacyCommandType.AccountClose)]
        [InlineAutoData(PrivacyCommandType.Delete)]
        [InlineAutoData(PrivacyCommandType.Export)]
        public void AadSubjectTenantId_DoesNotMatch(PrivacyCommandType privacyCommandType)
        {
            var expected = ApplicabilityReasonCode.DoesNotMatchAadSubjectTenantId;

            var command = ApplicabilityTestHelper.CreatePrivacyCommand(this, privacyCommandType);
            var aadSubject = (AadSubject)ApplicabilityTestHelper.CreatePrivacySubject(this, SubjectType.Aad);
            aadSubject.TenantId = Guid.NewGuid();
            command.Subject = aadSubject;

            var testHelper = new ApplicabilityTestHelper(this.outputHelper, this);
            testHelper.AssetGroupInfoDocument.TenantIds = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };

            testHelper.RunIsCommandActionableTest(expected, command);
        }

        [Theory]
        [InlineAutoData(PrivacyCommandType.AccountClose)]
        [InlineAutoData(PrivacyCommandType.Delete)]
        [InlineAutoData(PrivacyCommandType.Export)]
        public void AadSubjectTenantId_Pass_EmptyTenantIdsInAsset(PrivacyCommandType privacyCommandType)
        {
            var expected = ApplicabilityReasonCode.None;
            var tenantIdGuid = Guid.NewGuid();

            var command = ApplicabilityTestHelper.CreatePrivacyCommand(this, privacyCommandType);
            var aadSubject = (AadSubject)ApplicabilityTestHelper.CreatePrivacySubject(this, SubjectType.Aad);
            aadSubject.TenantId = tenantIdGuid;
            command.Subject = aadSubject;

            var testHelper = new ApplicabilityTestHelper(this.outputHelper, this);
            testHelper.AssetGroupInfoDocument.TenantIds = new string[] { };

            testHelper.RunIsCommandActionableTest(expected, command);
        }

        [Theory]
        [InlineAutoData(PrivacyCommandType.AccountClose, HomeTenantId, HomeTenantId, HomeTenantId, ApplicabilityReasonCode.None)]
        [InlineAutoData(PrivacyCommandType.AccountClose, HomeTenantId, HomeTenantId, ResourceTenantId, ApplicabilityReasonCode.DoesNotMatchAadSubjectTenantId)]
        [InlineAutoData(PrivacyCommandType.Export, ResourceTenantId, HomeTenantId, HomeTenantId, ApplicabilityReasonCode.DoesNotMatchAadSubjectTenantId)]
        [InlineAutoData(PrivacyCommandType.Export, ResourceTenantId, HomeTenantId, ResourceTenantId, ApplicabilityReasonCode.None)]
        public void AadSubject2TenantIds(PrivacyCommandType privacyCommandType, string tenantId, string homeTenantId, string assetTenantId, ApplicabilityReasonCode expectedCode)
        {
            var command = ApplicabilityTestHelper.CreatePrivacyCommand(this, privacyCommandType);
            var aadSubject2 = (AadSubject2)ApplicabilityTestHelper.CreatePrivacySubject(this, SubjectType.Aad2);
            aadSubject2.TenantId = Guid.Parse(tenantId);
            aadSubject2.HomeTenantId = Guid.Parse(homeTenantId);
            aadSubject2.TenantIdType = (aadSubject2.TenantId == aadSubject2.HomeTenantId) ? TenantIdType.Home : TenantIdType.Resource;
            command.Subject = aadSubject2;

            var testHelper = new ApplicabilityTestHelper(this.outputHelper, this);
            testHelper.AssetGroupInfoDocument.TenantIds = new string[] { assetTenantId };
            testHelper.AssetGroupInfoDocument.SubjectTypes = new[] { "MSAUser", "AADUser", "AADUser2" };

            testHelper.RunIsCommandActionableTest(expectedCode, command);
        }
    }
}
