namespace PCF.UnitTests.Applicability
{
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.PrivacyServices.SignalApplicability;
    using Ploeh.AutoFixture.Xunit2;
    using System;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Defines the <see cref="TipAndCheckIfBlockedTest" />
    /// </summary>
    [Trait("Category", "UnitTest")]
    public class TipAndCheckIfBlockedTest : INeedDataBuilders
    {
        private readonly ITestOutputHelper outputHelper;

        public TipAndCheckIfBlockedTest(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }

        [Fact]
        public void SyntheticCommand_Drop()
        {
            var command = ApplicabilityTestHelper.CreatePrivacyCommand(this, PrivacyCommandType.AccountClose);
            var testHelper = new ApplicabilityTestHelper(this.outputHelper, this);
            testHelper.IsSyntheticCommand = true;

            testHelper.RunIsCommandActionableTest(ApplicabilityReasonCode.SyntheticTestCommand, command);
        }

        [Fact]
        public void AgentIsBlocked_Drop()
        {
            var command = ApplicabilityTestHelper.CreatePrivacyCommand(this, PrivacyCommandType.AccountClose);
            var testHelper = new ApplicabilityTestHelper(this.outputHelper, this);

            using (new FlightEnabled(FlightingNames.BlockedAgents))
            {
                testHelper.RunIsCommandActionableTest(ApplicabilityReasonCode.AgentIsBlocked, command);
            }
        }

        [Fact]
        public void AssetGroupIsBlocked_Drop()
        {
            var command = ApplicabilityTestHelper.CreatePrivacyCommand(this, PrivacyCommandType.AccountClose);
            var testHelper = new ApplicabilityTestHelper(this.outputHelper, this);

            using (new FlightEnabled(FlightingNames.BlockedAssetGroups))
            {
                testHelper.RunIsCommandActionableTest(ApplicabilityReasonCode.AssetGroupIsBlocked, command);
            }
        }

        [Fact]
        public void ProdReadyAssetGroup_NotOnline_Allow()
        {
            var command = ApplicabilityTestHelper.CreatePrivacyCommand(this, PrivacyCommandType.AccountClose);
            var testHelper = new ApplicabilityTestHelper(this.outputHelper, this);
            testHelper.IsAgentOnline = false;

            testHelper.RunIsCommandActionableTest(ApplicabilityReasonCode.None, command);
        }

        [Fact]
        public void TipAssetGroup_Offline_Block()
        {
            var command = ApplicabilityTestHelper.CreatePrivacyCommand(this, PrivacyCommandType.AccountClose);
            var testHelper = new ApplicabilityTestHelper(this.outputHelper, this);
            testHelper.IsAgentOnline = false;
            testHelper.AssetGroupInfoDocument.AgentReadiness = Enum.GetName(typeof(AgentReadinessState), AgentReadinessState.TestInProd);

            testHelper.RunIsCommandActionableTest(ApplicabilityReasonCode.TipAgentIsNotOnline, command);
        }

        [Fact]
        public void TipAssetGroup_Online_Allow()
        {
            var command = ApplicabilityTestHelper.CreatePrivacyCommand(this, PrivacyCommandType.AccountClose);
            var testHelper = new ApplicabilityTestHelper(this.outputHelper, this);
            testHelper.AssetGroupInfoDocument.AgentReadiness = Enum.GetName(typeof(AgentReadinessState), AgentReadinessState.TestInProd);

            testHelper.RunIsCommandActionableTest(ApplicabilityReasonCode.None, command);
        }

        [Fact]
        public void TipAadExportTenantId_None()
        {
            var testHelper = new ApplicabilityTestHelper(this.outputHelper, this);
            var command = ApplicabilityTestHelper.CreatePrivacyCommand(this, PrivacyCommandType.Export);
            var tid = Guid.NewGuid();
            var aadSubject = (AadSubject)ApplicabilityTestHelper.CreatePrivacySubject(this, SubjectType.Aad);
            aadSubject.TenantId = tid;
            command.Subject = aadSubject;
            command.CommandSource = "Portals.PartnerTestPage";
            testHelper.AssetGroupInfoDocument.AgentReadiness = Enum.GetName(typeof(AgentReadinessState), AgentReadinessState.TestInProd);
            testHelper.AssetGroupInfoDocument.TenantIds = new[] { tid.ToString() };

            // Flight TestInProductionByTenantIdEnabled is ENABLED
            using (new FlightEnabled(FlightingNames.TestInProductionByTenantIdEnabled))
            {
                testHelper.RunIsCommandActionableTest(ApplicabilityReasonCode.None, command);
            }
        }

        [Fact]
        public void TipAadExportTenantId_FlightDisabled_TipAgentNotInTestTenantIdFlight()
        {
            var testHelper = new ApplicabilityTestHelper(this.outputHelper, this);
            var command = ApplicabilityTestHelper.CreatePrivacyCommand(this, PrivacyCommandType.Export);
            var tid = Guid.NewGuid();
            var aadSubject = (AadSubject)ApplicabilityTestHelper.CreatePrivacySubject(this, SubjectType.Aad);
            aadSubject.TenantId = tid;
            command.Subject = aadSubject;
            command.CommandSource = "Portals.PartnerTestPage";
            testHelper.AssetGroupInfoDocument.AgentReadiness = Enum.GetName(typeof(AgentReadinessState), AgentReadinessState.TestInProd);
            testHelper.AssetGroupInfoDocument.TenantIds = new[] { tid.ToString() };

            // Flight TestInProductionByTenantIdEnabled is DISABLED
            using (new FlightDisabled(FlightingNames.TestInProductionByTenantIdEnabled))
            {
                testHelper.RunIsCommandActionableTest(ApplicabilityReasonCode.TipAgentNotInTestTenantIdFlight, command);
            }
        }

        [Fact]
        public void TipNotAadExportTenantId_FromTestPage_None()
        {
            var testHelper = new ApplicabilityTestHelper(this.outputHelper, this);
            var command = ApplicabilityTestHelper.CreatePrivacyCommand(this, PrivacyCommandType.Export);
            
            // Command issued from test page
            command.CommandSource = Portals.PartnerTestPage;
            var tid = Guid.NewGuid();
            command.Subject = ApplicabilityTestHelper.CreatePrivacySubject(this, SubjectType.Msa);
            testHelper.AssetGroupInfoDocument.AgentReadiness = Enum.GetName(typeof(AgentReadinessState), AgentReadinessState.TestInProd);
            testHelper.AssetGroupInfoDocument.TenantIds = new[] { tid.ToString() };

            using (new FlightEnabled(FlightingNames.TestInProductionByTenantIdEnabled))
            {
                testHelper.RunIsCommandActionableTest(ApplicabilityReasonCode.None, command);
            }
        }

        [Fact]
        public void TipNotAadExportTenantId_NotFromTestPage_TipAgentShouldNotReceiveProdCommands()
        {
            var testHelper = new ApplicabilityTestHelper(this.outputHelper, this);
            var command = ApplicabilityTestHelper.CreatePrivacyCommand(this, PrivacyCommandType.Export);
            
            // Command issued from test page
            command.CommandSource = "FakeXXX";
            var tid = Guid.NewGuid();
            command.Subject = ApplicabilityTestHelper.CreatePrivacySubject(this, SubjectType.Msa);

            testHelper.AssetGroupInfoDocument.AgentReadiness = Enum.GetName(typeof(AgentReadinessState), AgentReadinessState.TestInProd);
            testHelper.AssetGroupInfoDocument.TenantIds = new[] { tid.ToString() };

            using (new FlightEnabled(FlightingNames.TestInProductionByTenantIdEnabled))
            {
                testHelper.RunIsCommandActionableTest(ApplicabilityReasonCode.TipAgentShouldNotReceiveProdCommands, command);
            }
        }

        [Fact]
        public void TipAadExport_TenantIdNotMatch_TipAgentNotInTestTenantIdFlight()
        {
            var testHelper = new ApplicabilityTestHelper(this.outputHelper, this);
            var command = ApplicabilityTestHelper.CreatePrivacyCommand(this, PrivacyCommandType.Export);
            var tid = Guid.NewGuid();
            var aadSubject = (AadSubject)ApplicabilityTestHelper.CreatePrivacySubject(this, SubjectType.Aad);
            aadSubject.TenantId = tid;
            command.Subject = aadSubject;
            command.CommandSource = "Portals.PartnerTestPage";
            testHelper.AssetGroupInfoDocument.AgentReadiness = Enum.GetName(typeof(AgentReadinessState), AgentReadinessState.TestInProd);
            testHelper.AssetGroupInfoDocument.TenantIds = new[] { tid.ToString() };

            using (new FlightDisabled(FlightingNames.TestInProductionByTenantIdEnabled))
            {
                testHelper.RunIsCommandActionableTest(ApplicabilityReasonCode.TipAgentNotInTestTenantIdFlight, command);
            }
        }
    }
}
