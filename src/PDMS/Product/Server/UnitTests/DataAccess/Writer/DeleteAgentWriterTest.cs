namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Writer.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Common;
    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.ActiveDirectory;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authorization;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Reader;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.UnitTest;
    using Microsoft.PrivacyServices.DataManagement.Models;
    using Microsoft.PrivacyServices.DataManagement.Models.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;

    public class DeleteAgentWriterTest
    {
        [Theory(DisplayName = "When CreateAsync is called, then the storage layer is called and returned."), ValidData]
        public async Task When_CreateAsync_Then_CallAndReturnStorageValue(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            DeleteAgent deleteAgent,
            DeleteAgent storageDeleteAgent,
            DeleteAgentWriter writer)
        {
            storageWriter.Setup(m => m.CreateDataAgentAsync(deleteAgent)).ReturnsAsync(storageDeleteAgent); // Frozen doesn't work for this.

            var result = await writer.CreateAsync(deleteAgent).ConfigureAwait(false);

            Assert.Equal(storageDeleteAgent, result);

            storageWriter.Verify(m => m.CreateDataAgentAsync(deleteAgent), Times.Once);
        }

        [Theory(DisplayName = "When CreateAsync is called without an owner id, then fail."), ValidData]
        public async Task When_CreateAsyncWithoutOwner_Then_Fail(
            DeleteAgent deleteAgent,
            DeleteAgentWriter writer)
        {
            deleteAgent.OwnerId = Guid.Empty;

            var exn = await Assert.ThrowsAsync<MissingPropertyException>(() => writer.CreateAsync(deleteAgent)).ConfigureAwait(false);

            Assert.Equal("ownerId", exn.ParamName);
        }

        [Theory(DisplayName = "When CreateAsync is called with an owner that has no write security groups, then fail."), ValidData]
        public async Task When_CreateAsyncWithoutWriteSecurityGroups_Then_Fail(
            [Frozen] DataOwner dataOwner,
            DeleteAgent deleteAgent,
            DeleteAgentWriter writer)
        {
            dataOwner.WriteSecurityGroups = null;

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.CreateAsync(deleteAgent)).ConfigureAwait(false);

            Assert.Equal("dataOwner.writeSecurityGroups", exn.Target);
            Assert.Equal(ConflictType.DoesNotExist, exn.ConflictType);
        }

        [Theory(DisplayName = "When CreateAsync is called with the same site id between PreProd and Prod, then fail."), ValidData]
        public async Task When_CreateAsyncWithSharedSiteId_Then_Fail(
            DeleteAgent deleteAgent,
            DeleteAgentWriter writer)
        {
            deleteAgent.ConnectionDetails[ReleaseState.PreProd] = new ConnectionDetail
            {
                ReleaseState = ReleaseState.PreProd,
                AgentReadiness = AgentReadiness.ProdReady,
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                AuthenticationType = AuthenticationType.MsaSiteBasedAuth,
                MsaSiteId = 10
            };

            deleteAgent.ConnectionDetails[ReleaseState.Prod] = new ConnectionDetail
            {
                ReleaseState = ReleaseState.Prod,
                AgentReadiness = AgentReadiness.TestInProd,
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                AuthenticationType = AuthenticationType.MsaSiteBasedAuth,
                MsaSiteId = 10
            };

            var exn = await Assert.ThrowsAsync<MutuallyExclusiveException>(() => writer.CreateAsync(deleteAgent)).ConfigureAwait(false);

            Assert.Equal("connectionDetails[PreProd].msaSiteId", exn.Source);
            Assert.Equal("connectionDetails[Prod].msaSiteId", exn.ParamName);
            Assert.Equal("10", exn.Value);
        }

        [Theory(DisplayName = "When CreateAsync is called with the same app id between PreProd and Prod, then fail."), ValidData]
        public async Task When_CreateAsyncWithSharedAppId_Then_Fail(
            Guid appId,
            DeleteAgent deleteAgent,
            DeleteAgentWriter writer)
        {
            deleteAgent.ConnectionDetails[ReleaseState.PreProd] = new ConnectionDetail
            {
                ReleaseState = ReleaseState.PreProd,
                AgentReadiness = AgentReadiness.ProdReady,
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                AadAppId = appId
            };

            deleteAgent.ConnectionDetails[ReleaseState.Prod] = new ConnectionDetail
            {
                ReleaseState = ReleaseState.Prod,
                AgentReadiness = AgentReadiness.TestInProd,
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                AadAppId = appId
            };

            var exn = await Assert.ThrowsAsync<MutuallyExclusiveException>(() => writer.CreateAsync(deleteAgent)).ConfigureAwait(false);

            Assert.Equal("connectionDetails[PreProd].aadAppId(s)", exn.Source);
            Assert.Equal("connectionDetails[Prod].aadAppId(s)", exn.ParamName);
            Assert.Equal(appId.ToString(), exn.Value);
        }

        [Theory(DisplayName = "When CreateAsync is called with empty capabilities, then fail."), ValidData]
        public async Task When_CreateAsyncWithEmptyCapabilities_Then_Fail(
            DeleteAgent deleteAgent,
            DeleteAgentWriter writer)
        {
            deleteAgent.Capabilities = new CapabilityId[0];

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(deleteAgent)).ConfigureAwait(false);

            Assert.Equal("capabilities", exn.ParamName);
        }

        [Theory(DisplayName = "When CreateAsync is called with capabilities, then fail."), ValidData]
        public async Task When_CreateAsyncWithCapabilities_Then_Fail(
            DeleteAgent deleteAgent,
            DeleteAgentWriter writer)
        {
            deleteAgent.Capabilities = new[] { Policies.Current.Capabilities.Ids.Delete };

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(deleteAgent)).ConfigureAwait(false);

            Assert.Equal("capabilities", exn.ParamName);
        }

        [Theory(DisplayName = "When CreateAsync is called with null deploymentLocation, then fail."), ValidData]
        public async Task When_CreateAsyncWithEmptyDeploymentLocation_Then_Fail(
            DeleteAgent deleteAgent,
            DeleteAgentWriter writer)
        {
            deleteAgent.DeploymentLocation = null;
            var exn = await Assert.ThrowsAsync<MissingPropertyException>(() => writer.CreateAsync(deleteAgent)).ConfigureAwait(false);
            Assert.Equal("deploymentLocation", exn.ParamName);
        }

        [Theory(DisplayName = "When CreateAsync is called with null supportedCloud, then fail."), ValidData]
        public async Task When_CreateAsyncWithEmptySupportedClouds_Then_Fail(
            DeleteAgent deleteAgent,
            DeleteAgentWriter writer)
        {
            deleteAgent.DeploymentLocation = Policies.Current.CloudInstances.Ids.US_Azure_Fairfax;
            deleteAgent.SupportedClouds = null;
            var exn = await Assert.ThrowsAsync<MissingPropertyException>(() => writer.CreateAsync(deleteAgent)).ConfigureAwait(false);
            Assert.Equal("supportedClouds", exn.ParamName);
        }

        [Theory(DisplayName = "When CreateAsync is called with a public deploymentLocation and duplicate supported clouds, then fail."), ValidData]
        public async Task When_CreateAsyncWithPublicDelpoymentLocationAndDuplicateCloudInstances_Then_Fail(
            DeleteAgent deleteAgent,
            DeleteAgentWriter writer)
        {
            deleteAgent.SupportedClouds = new[] 
            {
                Policies.Current.CloudInstances.Ids.Public,
                Policies.Current.CloudInstances.Ids.Public
            };
            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(deleteAgent)).ConfigureAwait(false);
            Assert.Equal("supportedClouds", exn.ParamName);
        }

        [Theory(DisplayName = "When CreateAsync is called with a public deploymentLocation and all supported clouds with additional clouds, then fail."), ValidData]
        public async Task When_CreateAsyncWithPublicDelpoymentLocationAndAllSupportedCloudsWithAdditionalClouds_Then_Fail(
            DeleteAgent deleteAgent,
            DeleteAgentWriter writer)
        {
            deleteAgent.SupportedClouds = new[]
            {
                Policies.Current.CloudInstances.Ids.All,
                Policies.Current.CloudInstances.Ids.US_Azure_Fairfax
            };
            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(deleteAgent)).ConfigureAwait(false);
            Assert.Equal("supportedClouds", exn.ParamName);
        }

        [Theory(DisplayName = "When CreateAsync is called with a sovereign cloud deploymentLocation and list of sovereign clouds does not sole contain the deploymentLocation, then fail."), ValidData]
        public async Task When_CreateAsyncWithSovereignCloudDelpoymentLocationAndMismatchSupportedClouds_Then_Fail(
            DeleteAgent deleteAgent,
            DeleteAgentWriter writer)
        {
            deleteAgent.DeploymentLocation = Policies.Current.CloudInstances.Ids.US_Azure_Fairfax;
            deleteAgent.SupportedClouds = new[] { Policies.Current.CloudInstances.Ids.Public };
            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(deleteAgent)).ConfigureAwait(false);
            Assert.Equal("supportedClouds", exn.ParamName);
            
            deleteAgent.SupportedClouds = new[] 
            {
                Policies.Current.CloudInstances.Ids.Public,
                Policies.Current.CloudInstances.Ids.US_Azure_Fairfax
            };
            exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(deleteAgent)).ConfigureAwait(false);
            Assert.Equal("supportedClouds", exn.ParamName);
        }

        [Theory(DisplayName = "When CreateAsync is called without connection details, then fail."), ValidData]
        public async Task When_CreateAsyncWithoutConnectionDetails_Then_Fail(
            DeleteAgent deleteAgent,
            DeleteAgentWriter writer)
        {
            deleteAgent.ConnectionDetails = null;

            var exn = await Assert.ThrowsAsync<MissingPropertyException>(() => writer.CreateAsync(deleteAgent)).ConfigureAwait(false);

            Assert.Equal("connectionDetails", exn.ParamName);
        }

        [Theory(DisplayName = "When CreateAsync is called with empty connection details, then fail."), ValidData]
        public async Task When_CreateAsyncWithEmptyConnectionDetails_Then_Fail(
            DeleteAgent deleteAgent,
            DeleteAgentWriter writer)
        {
            deleteAgent.ConnectionDetails = new ConnectionDetail[0].ToDictionary(v => v.ReleaseState);

            var exn = await Assert.ThrowsAsync<MissingPropertyException>(() => writer.CreateAsync(deleteAgent)).ConfigureAwait(false);

            Assert.Equal("connectionDetails", exn.ParamName);
        }

        [Theory(DisplayName = "When CreateAsync is called with invalid release, then fail.")]
        [InlineValidData(WriteAction.Create, ReleaseState.Ring1)]
        [InlineValidData(WriteAction.Create, ReleaseState.Ring2)]
        [InlineValidData(WriteAction.Create, ReleaseState.Ring3)]
        public async Task When_CreateAsyncWithInvalidRelease_Then_Fail(
            ReleaseState releaseState,
            DeleteAgent deleteAgent,
            DeleteAgentWriter writer)
        {
            deleteAgent.ConnectionDetails[releaseState] = deleteAgent.ConnectionDetails.First().Value;
            deleteAgent.ConnectionDetails[releaseState].ReleaseState = releaseState;

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(deleteAgent)).ConfigureAwait(false);

            Assert.Equal($"connectionDetails[{releaseState}]", exn.ParamName);
        }

        [Theory(DisplayName = "When CreateAsync is called with non-existant owner, then fail."), ValidData]
        public async Task When_CreateAsyncAndOwnerDoesNotExist_Then_Fail(
            [Frozen] Mock<IDataOwnerReader> ownerReader,
            DeleteAgent deleteAgent,
            DeleteAgentWriter writer)
        {
            ownerReader.Setup(m => m.ReadByIdAsync(deleteAgent.OwnerId, ExpandOptions.ServiceTree)).ReturnsAsync(null as DataOwner);

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.CreateAsync(deleteAgent)).ConfigureAwait(false);

            Assert.Equal(ConflictType.DoesNotExist, exn.ConflictType);
            Assert.Equal("ownerId", exn.Target);
        }

        [Theory(DisplayName = "When CreateAsync is called with sharing enabled and owner has null share contacts, then fail."), ValidData]
        public async Task When_CreateAsyncWithSharingEnabledAndOwnerHasNullContacts_Then_Fail(
            [Frozen] DataOwner existingOwner,
            DeleteAgent deleteAgent,
            DeleteAgentWriter writer)
        {
            existingOwner.SharingRequestContacts = null;
            deleteAgent.SharingEnabled = true;

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.CreateAsync(deleteAgent)).ConfigureAwait(false);

            Assert.Equal(ConflictType.NullValue, exn.ConflictType);
            Assert.Equal("owner.sharingRequestContacts", exn.Target);
        }

        [Theory(DisplayName = "When CreateAsync is called with sharing enabled and owner has empty share contacts, then fail."), ValidData]
        public async Task When_CreateAsyncWithSharingEnabledAndOwnerHasEmptyContacts_Then_Fail(
            [Frozen] DataOwner existingOwner,
            DeleteAgent deleteAgent,
            DeleteAgentWriter writer)
        {
            existingOwner.SharingRequestContacts = new System.Net.Mail.MailAddress[0];
            deleteAgent.SharingEnabled = true;

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.CreateAsync(deleteAgent)).ConfigureAwait(false);

            Assert.Equal(ConflictType.NullValue, exn.ConflictType);
            Assert.Equal("owner.sharingRequestContacts", exn.Target);
        }

        [Theory(DisplayName = "When CreateAsync is called with sharing enabled and owner has some share contacts, then pass."), ValidData]
        public async Task When_CreateAsyncWithSharingEnabledAndOwnerSomeContacts_Then_Pass(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            IEnumerable<System.Net.Mail.MailAddress> addresses,
            [Frozen] DataOwner existingOwner,
            DeleteAgent deleteAgent,
            DeleteAgentWriter writer)
        {
            storageWriter.Setup(m => m.CreateDataAgentAsync(deleteAgent)).ReturnsAsync(deleteAgent); // Frozen doesn't work for this.

            existingOwner.SharingRequestContacts = addresses;
            deleteAgent.SharingEnabled = true;

            await writer.CreateAsync(deleteAgent).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When CreateAsync is called with sharing disabled and owner has no share contacts, then pass."), ValidData]
        public async Task When_CreateAsyncWithSharingDisabledAndOwnerNoContacts_Then_Pass(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            [Frozen] DataOwner existingOwner,
            DeleteAgent deleteAgent,
            DeleteAgentWriter writer)
        {
            storageWriter.Setup(m => m.CreateDataAgentAsync(deleteAgent)).ReturnsAsync(deleteAgent); // Frozen doesn't work for this.

            existingOwner.SharingRequestContacts = null;
            deleteAgent.SharingEnabled = false;

            await writer.CreateAsync(deleteAgent).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When CreateAsync is called ReleaseState.Prod and AgentReadiness.ProdReady, then the InProdDate value is set."), ValidData]
        public async Task When_CreateAsyncWithProdReady_Then_SetInProdDateValue(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            [Frozen] IDateFactory dateFactoryMock,
            Guid appId,
            DeleteAgent deleteAgent,
            DeleteAgentWriter writer)
        {
            storageWriter.Setup(m => m.CreateDataAgentAsync(deleteAgent)).ReturnsAsync(deleteAgent); // Frozen doesn't work for this.
            deleteAgent.ConnectionDetails[ReleaseState.Prod] = new ConnectionDetail
            {
                ReleaseState = ReleaseState.Prod,
                AgentReadiness = AgentReadiness.ProdReady,
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                AadAppId = appId
            };

            var result = await writer.CreateAsync(deleteAgent).ConfigureAwait(false);
            Assert.Equal(deleteAgent.InProdDate, dateFactoryMock.GetCurrentTime());
        }

        [Theory(DisplayName = "When CreateAsync is called with an In Prod Date, then fail."), ValidData]
        public async Task When_CreateAsyncWithInProdDate_Then_Fail(
            DeleteAgent deleteAgent,
            DeleteAgentWriter writer)
        {
            deleteAgent.InProdDate = DateTime.UtcNow;

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(deleteAgent)).ConfigureAwait(false);

            Assert.Equal("inProdDate", exn.ParamName);
        }

        [Theory(DisplayName = "When UpdateAsync is called with sharing enabled and owner has null share contacts, then fail."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithSharingEnabledAndOwnerHasNullContacts_Then_Fail(
            [Frozen] DataOwner existingOwner,
            DeleteAgent deleteAgent,
            DeleteAgentWriter writer)
        {
            existingOwner.SharingRequestContacts = null;
            deleteAgent.SharingEnabled = true;

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.UpdateAsync(deleteAgent)).ConfigureAwait(false);

            Assert.Equal(ConflictType.NullValue, exn.ConflictType);
            Assert.Equal("owner.sharingRequestContacts", exn.Target);
        }

        [Theory(DisplayName = "When UpdateAsync is called with sharing enabled and owner has empty share contacts, then fail."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithSharingEnabledAndOwnerHasEmptyContacts_Then_Fail(
            [Frozen] DataOwner existingOwner,
            DeleteAgent deleteAgent,
            DeleteAgentWriter writer)
        {
            existingOwner.SharingRequestContacts = new System.Net.Mail.MailAddress[0];
            deleteAgent.SharingEnabled = true;

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.UpdateAsync(deleteAgent)).ConfigureAwait(false);

            Assert.Equal(ConflictType.NullValue, exn.ConflictType);
            Assert.Equal("owner.sharingRequestContacts", exn.Target);
        }

        [Theory(DisplayName = "When UpdateAsync is called with sharing enabled and owner has some share contacts, then pass."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithSharingEnabledAndOwnerSomeContacts_Then_Pass(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            IEnumerable<System.Net.Mail.MailAddress> addresses,
            [Frozen] DataOwner existingOwner,
            DeleteAgent deleteAgent,
            [Frozen] DeleteAgent existingDeleteAgent,
            DeleteAgentWriter writer,
            TrackingDetails trackingDetails)
        {
            storageWriter.Setup(m => m.UpdateDataAgentAsync(existingDeleteAgent)).ReturnsAsync(existingDeleteAgent);
            existingDeleteAgent.TrackingDetails = trackingDetails;

            existingOwner.SharingRequestContacts = addresses;
            deleteAgent.SharingEnabled = true;

            await writer.UpdateAsync(deleteAgent).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When UpdateAsync is called with a preprod detail, then default to prod ready."), ValidData(WriteAction.Update)]
        public async Task When_UpdateWithNewPreProdDetail_Then_SetToProdReady(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            DeleteAgent deleteAgent,
            [Frozen] DeleteAgent existingDeleteAgent,
            DeleteAgentWriter writer,
            TrackingDetails trackingDetails)
        {
            deleteAgent.ConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>();
            deleteAgent.ConnectionDetails[ReleaseState.Prod] = existingDeleteAgent.ConnectionDetails[ReleaseState.Prod];
            deleteAgent.ConnectionDetails[ReleaseState.PreProd] = new ConnectionDetail
            {
                AgentReadiness = AgentReadiness.TestInProd,
                Protocol = Policies.Current.Protocols.Ids.CosmosDeleteSignalV2
            };

            storageWriter.Setup(m => m.UpdateDataAgentAsync(existingDeleteAgent)).ReturnsAsync(existingDeleteAgent);
            existingDeleteAgent.TrackingDetails = trackingDetails;
            
            var actual = await writer.UpdateAsync(deleteAgent).ConfigureAwait(false);
            Assert.Equal(AgentReadiness.ProdReady, actual.ConnectionDetails[ReleaseState.PreProd].AgentReadiness);
        }

        [Theory(DisplayName = "When UpdateAsync is called with an existing preprod detail, then do not change."), ValidData(WriteAction.Update)]
        public async Task When_UpdateWithExistingPreProdDetail_Then_LeaveAsIs(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            DeleteAgent deleteAgent,
            [Frozen] DeleteAgent existingDeleteAgent,
            DeleteAgentWriter writer,
            TrackingDetails trackingDetails)
        {
            deleteAgent.ConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>();
            deleteAgent.ConnectionDetails[ReleaseState.Prod] = existingDeleteAgent.ConnectionDetails[ReleaseState.Prod];
            deleteAgent.ConnectionDetails[ReleaseState.PreProd] = new ConnectionDetail
            {
                AgentReadiness = AgentReadiness.TestInProd,
                Protocol = Policies.Current.Protocols.Ids.CosmosDeleteSignalV2
            };

            existingDeleteAgent.ConnectionDetails[ReleaseState.PreProd] = new ConnectionDetail
            {
                AgentReadiness = AgentReadiness.TestInProd,
                Protocol = Policies.Current.Protocols.Ids.CosmosDeleteSignalV2
            };

            storageWriter.Setup(m => m.UpdateDataAgentAsync(existingDeleteAgent)).ReturnsAsync(existingDeleteAgent);
            existingDeleteAgent.TrackingDetails = trackingDetails;

            var actual = await writer.UpdateAsync(deleteAgent).ConfigureAwait(false);
            Assert.Equal(AgentReadiness.TestInProd, actual.ConnectionDetails[ReleaseState.PreProd].AgentReadiness);
        }

        [Theory(DisplayName = "When UpdateAsync is called with sharing disabled and owner has no share contacts, then pass."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithSharingDisabledAndOwnerNoContacts_Then_Pass(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            [Frozen] DataOwner existingOwner,
            DeleteAgent deleteAgent,
            [Frozen] DeleteAgent existingDeleteAgent,
            DeleteAgentWriter writer,
            TrackingDetails trackingDetails)
        {
            storageWriter.Setup(m => m.UpdateDataAgentAsync(existingDeleteAgent)).ReturnsAsync(existingDeleteAgent);
            existingDeleteAgent.TrackingDetails = trackingDetails;

            existingOwner.SharingRequestContacts = null;
            deleteAgent.SharingEnabled = false;

            await writer.UpdateAsync(deleteAgent).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When UpdateAsync is called, then the storage layer is called and returned."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsync_Then_CallAndReturnStorageValue(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            DeleteAgent deleteAgent,
            [Frozen] DeleteAgent existingDeleteAgent,
            DeleteAgentWriter writer,
            TrackingDetails trackingDetails)
        {
            storageWriter.Setup(m => m.UpdateDataAgentAsync(existingDeleteAgent)).ReturnsAsync(existingDeleteAgent);

            existingDeleteAgent.TrackingDetails = trackingDetails;

            var result = await writer.UpdateAsync(deleteAgent).ConfigureAwait(false);

            Assert.Equal(existingDeleteAgent, result);

            storageWriter.Verify(m => m.UpdateDataAgentAsync(existingDeleteAgent), Times.Once);
        }

        [Theory(DisplayName = "When UpdateAsync is called with the same site id between PreProd and Prod and the details have changed, then fail."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithChangedSharedSiteId_Then_Fail(
            DeleteAgent deleteAgent,
            [Frozen] DeleteAgent storageDeleteAgent,
            DeleteAgentWriter writer)
        {
            deleteAgent.ConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>();
            deleteAgent.ConnectionDetails[ReleaseState.PreProd] = new ConnectionDetail
            {
                ReleaseState = ReleaseState.PreProd,
                AgentReadiness = AgentReadiness.ProdReady,
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                AuthenticationType = AuthenticationType.MsaSiteBasedAuth,
                MsaSiteId = 10
            };

            deleteAgent.ConnectionDetails[ReleaseState.Prod] = new ConnectionDetail
            {
                ReleaseState = ReleaseState.Prod,
                AgentReadiness = AgentReadiness.TestInProd,
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                AuthenticationType = AuthenticationType.MsaSiteBasedAuth,
                MsaSiteId = 10
            };

            storageDeleteAgent.ConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>();
            storageDeleteAgent.ConnectionDetails[ReleaseState.PreProd] = new ConnectionDetail
            {
                ReleaseState = ReleaseState.Prod,
                AgentReadiness = AgentReadiness.TestInProd,
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                AuthenticationType = AuthenticationType.MsaSiteBasedAuth,
                MsaSiteId = 20
            };

            storageDeleteAgent.ConnectionDetails[ReleaseState.Prod] = new ConnectionDetail
            {
                ReleaseState = ReleaseState.Prod,
                AgentReadiness = AgentReadiness.TestInProd,
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                AuthenticationType = AuthenticationType.MsaSiteBasedAuth,
                MsaSiteId = 10
            };

            var exn = await Assert.ThrowsAsync<MutuallyExclusiveException>(() => writer.UpdateAsync(deleteAgent)).ConfigureAwait(false);

            Assert.Equal("connectionDetails[PreProd].msaSiteId", exn.Source);
            Assert.Equal("connectionDetails[Prod].msaSiteId", exn.ParamName);
            Assert.Equal("10", exn.Value);
        }

        [Theory(DisplayName = "When UpdateAsync is called with the same app id between PreProd and Prod and the details have changed, then fail."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithChangedSharedAppId_Then_Fail(
            Guid appId,
            Guid oldAppId,
            DeleteAgent deleteAgent,
            [Frozen] DeleteAgent storageDeleteAgent,
            DeleteAgentWriter writer)
        {
            deleteAgent.ConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>();
            deleteAgent.ConnectionDetails[ReleaseState.PreProd] = new ConnectionDetail
            {
                ReleaseState = ReleaseState.PreProd,
                AgentReadiness = AgentReadiness.ProdReady,
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                AadAppId = appId
            };

            deleteAgent.ConnectionDetails[ReleaseState.Prod] = new ConnectionDetail
            {
                ReleaseState = ReleaseState.Prod,
                AgentReadiness = AgentReadiness.TestInProd,
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                AadAppId = appId
            };

            storageDeleteAgent.ConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>();
            storageDeleteAgent.ConnectionDetails[ReleaseState.PreProd] = new ConnectionDetail
            {
                ReleaseState = ReleaseState.PreProd,
                AgentReadiness = AgentReadiness.ProdReady,
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                AadAppId = oldAppId
            };

            storageDeleteAgent.ConnectionDetails[ReleaseState.Prod] = new ConnectionDetail
            {
                ReleaseState = ReleaseState.Prod,
                AgentReadiness = AgentReadiness.TestInProd,
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                AadAppId = appId
            };

            var exn = await Assert.ThrowsAsync<MutuallyExclusiveException>(() => writer.UpdateAsync(deleteAgent)).ConfigureAwait(false);

            Assert.Equal("connectionDetails[PreProd].aadAppId(s)", exn.Source);
            Assert.Equal("connectionDetails[Prod].aadAppId(s)", exn.ParamName);
            Assert.Equal(appId.ToString(), exn.Value);
        }

        [Theory(DisplayName = "When UpdateAsync is called with the same app id between PreProd and Prod in App ID list, then fail."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithChangedSharedAppIdInList_Then_Fail(
            Guid preProdAppId,
            Guid prodAppId,
            DeleteAgent deleteAgent,
            [Frozen] DeleteAgent storageDeleteAgent,
            DeleteAgentWriter writer)
        {

            // Scenario: Accidentally update Delete agent Prod with same AppId as in Preprod
            deleteAgent.ConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>();
            deleteAgent.ConnectionDetails[ReleaseState.PreProd] = new ConnectionDetail
            {
                ReleaseState = ReleaseState.PreProd,
                AgentReadiness = AgentReadiness.ProdReady,
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                AadAppId = preProdAppId,
            };
            deleteAgent.ConnectionDetails[ReleaseState.Prod] = new ConnectionDetail
            {
                ReleaseState = ReleaseState.Prod,
                AgentReadiness = AgentReadiness.TestInProd,
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                AuthenticationType = AuthenticationType.AadAppBasedAuth,   
                AadAppIds = Enumerable.Empty<Guid>().Append(prodAppId).Append(preProdAppId)
            };

            // Data Agent previously existing in PDMS
            storageDeleteAgent.ConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>();
            storageDeleteAgent.ConnectionDetails[ReleaseState.PreProd] = new ConnectionDetail
            {
                ReleaseState = ReleaseState.PreProd,
                AgentReadiness = AgentReadiness.ProdReady,
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                AadAppId = preProdAppId,

            };

            storageDeleteAgent.ConnectionDetails[ReleaseState.Prod] = new ConnectionDetail
            {
                ReleaseState = ReleaseState.Prod,
                AgentReadiness = AgentReadiness.TestInProd,
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                AadAppId = prodAppId,
            };

            var exn = await Assert.ThrowsAsync<MutuallyExclusiveException>(() => writer.UpdateAsync(deleteAgent)).ConfigureAwait(false);

            Assert.Equal("connectionDetails[PreProd].aadAppId(s)", exn.Source);
            Assert.Equal("connectionDetails[Prod].aadAppId(s)", exn.ParamName);
            Assert.Equal(preProdAppId.ToString(), exn.Value);
        }

        [Theory(DisplayName = "When UpdateAsync is called with the same site id between PreProd and Prod and the details have not changed, then pass."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithUnchangedSharedSiteId_Then_Pass(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            DeleteAgent deleteAgent,
            [Frozen] DeleteAgent storageDeleteAgent,
            TrackingDetails trackingDetails,
            DeleteAgentWriter writer)
        {
            deleteAgent.ConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>();
            deleteAgent.ConnectionDetails[ReleaseState.PreProd] = new ConnectionDetail
            {
                ReleaseState = ReleaseState.PreProd,
                AgentReadiness = AgentReadiness.ProdReady,
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                AuthenticationType = AuthenticationType.MsaSiteBasedAuth,
                MsaSiteId = 10
            };

            deleteAgent.ConnectionDetails[ReleaseState.Prod] = new ConnectionDetail
            {
                ReleaseState = ReleaseState.Prod,
                AgentReadiness = AgentReadiness.TestInProd,
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                AuthenticationType = AuthenticationType.MsaSiteBasedAuth,
                MsaSiteId = 10
            };

            storageDeleteAgent.ConnectionDetails = deleteAgent.ConnectionDetails;
            storageDeleteAgent.TrackingDetails = trackingDetails;
            storageWriter.Setup(m => m.UpdateDataAgentAsync(storageDeleteAgent)).ReturnsAsync(storageDeleteAgent);

            await writer.UpdateAsync(deleteAgent).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When UpdateAsync is called with the same app id between PreProd and Prod and the details have not changed, then pass."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithUnchangedSharedAppId_Then_Pass(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            Guid appId,
            DeleteAgent deleteAgent,
            [Frozen] DeleteAgent storageDeleteAgent,
            TrackingDetails trackingDetails,
            DeleteAgentWriter writer)
        {
            deleteAgent.ConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>();
            deleteAgent.ConnectionDetails[ReleaseState.PreProd] = new ConnectionDetail
            {
                ReleaseState = ReleaseState.PreProd,
                AgentReadiness = AgentReadiness.ProdReady,
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                AadAppId = appId
            };

            deleteAgent.ConnectionDetails[ReleaseState.Prod] = new ConnectionDetail
            {
                ReleaseState = ReleaseState.Prod,
                AgentReadiness = AgentReadiness.TestInProd,
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                AadAppId = appId
            };

            storageDeleteAgent.ConnectionDetails = deleteAgent.ConnectionDetails;
            storageDeleteAgent.TrackingDetails = trackingDetails;
            storageWriter.Setup(m => m.UpdateDataAgentAsync(storageDeleteAgent)).ReturnsAsync(storageDeleteAgent);

            await writer.UpdateAsync(deleteAgent).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When UpdateAsync is called and capabilities have changed, then do not copy."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithChangedCapabilities_Then_Fail(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            DeleteAgent deleteAgent,
            [Frozen] DeleteAgent existingDeleteAgent,
            TrackingDetails trackingDetails,
            IFixture fixture)
        {
            // Do this so that the mapper logic is triggered.
            fixture.FreezeMapper();
            var writer = fixture.Create<DeleteAgentWriter>();

            var originalCapabilities = new[] { Policies.Current.Capabilities.Ids.Export };
            existingDeleteAgent.Capabilities = originalCapabilities;
            existingDeleteAgent.TrackingDetails = trackingDetails;

            storageWriter.Setup(m => m.UpdateDataAgentAsync(existingDeleteAgent)).ReturnsAsync(existingDeleteAgent);

            await writer.UpdateAsync(deleteAgent).ConfigureAwait(false);

            Assert.Equal(originalCapabilities, existingDeleteAgent.Capabilities);
        }

        [Theory(DisplayName = "When UpdateAsync is called and Prod connection details are removed, then fail.")]
        [ValidData(WriteAction.Update, disableAuthFixtures: true)]
        public async Task When_UpdateAsyncWithProdConnectionDetailsRemoved_Then_Fail(
            [Frozen] Mock<IAuthorizationProvider> authProvider,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            DeleteAgent deleteAgent,
            [Frozen] DeleteAgent existingDeleteAgent,
            DeleteAgentWriter writer)
        {
            authProvider.Setup(m => m.TryAuthorizeAsync(AuthorizationRole.ServiceAdmin, null)).ReturnsAsync(false);

            existingDeleteAgent.ConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>
            {
                {
                    ReleaseState.PreProd,
                    new ConnectionDetail
                    {
                        Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                        ReleaseState = ReleaseState.PreProd,
                        AgentReadiness = AgentReadiness.ProdReady,
                        AuthenticationType = AuthenticationType.AadAppBasedAuth,
                        AadAppId = Guid.NewGuid()
                    }
                },
                {
                    ReleaseState.Prod,
                    new ConnectionDetail
                    {
                        Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                        ReleaseState = ReleaseState.Prod,
                        AgentReadiness = AgentReadiness.TestInProd,
                        AuthenticationType = AuthenticationType.AadAppBasedAuth,
                        AadAppId = Guid.NewGuid()
                    }
                }
            };

            deleteAgent.ConnectionDetails = 
                existingDeleteAgent.ConnectionDetails
                .Where(x => x.Key != ReleaseState.Prod)
                .ToDictionary(x => x.Key, x => x.Value);

            storageWriter.Setup(m => m.UpdateDataAgentAsync(existingDeleteAgent)).ReturnsAsync(existingDeleteAgent);

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.UpdateAsync(deleteAgent)).ConfigureAwait(false);

            Assert.Equal(ConflictType.InvalidValue_Immutable, exn.ConflictType);
            Assert.Equal("connectionDetails[Prod]", exn.Target);
        }

        [Theory(DisplayName = "When UpdateAsync is called and Prod connection details are removed as a ServiceAdmin, then pass.")]
        [ValidData(WriteAction.Update, disableAuthFixtures: true)]
        public async Task When_UpdateAsyncWithProdConnectionDetailsRemovedAsServiceAdmin_Then_Pass(
            [Frozen] Mock<IAuthorizationProvider> authProvider,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            DeleteAgent deleteAgent,
            [Frozen] DeleteAgent existingDeleteAgent,
            DeleteAgentWriter writer,
            TrackingDetails trackingDetails)
        {
            authProvider.Setup(m => m.TryAuthorizeAsync(AuthorizationRole.ServiceAdmin, null)).ReturnsAsync(true);

            var connectionDetail = deleteAgent.ConnectionDetails.First().Value;
            connectionDetail.ReleaseState = ReleaseState.PreProd;
            connectionDetail.AgentReadiness = AgentReadiness.ProdReady;

            deleteAgent.ConnectionDetails = DataAgentWriterTest.CreateConnectionDetails(connectionDetail);

            storageWriter.Setup(m => m.UpdateDataAgentAsync(existingDeleteAgent)).ReturnsAsync(existingDeleteAgent);
            existingDeleteAgent.TrackingDetails = trackingDetails;

            await writer.UpdateAsync(deleteAgent).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When UpdateAsync is called and it already has prod details, then prod details are immutable.")]
        [ValidData(WriteAction.Update, disableAuthFixtures: true)]
        public async Task When_UpdateAsyncWithProdConnectionDetails_Then_DetailsAreImmutable(
            [Frozen] Mock<IAuthorizationProvider> authProvider,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            DeleteAgent deleteAgent,
            [Frozen] DeleteAgent existingDeleteAgent,
            DeleteAgentWriter writer)
        {
            authProvider.Setup(m => m.TryAuthorizeAsync(AuthorizationRole.ServiceAdmin, null)).ReturnsAsync(false);
            storageWriter.Setup(m => m.UpdateDataAgentAsync(existingDeleteAgent)).ReturnsAsync(existingDeleteAgent);

            existingDeleteAgent.ConnectionDetails = DataAgentWriterTest.CreateConnectionDetails(new ConnectionDetail
            {
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                ReleaseState = ReleaseState.Prod,
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                AadAppId = Guid.NewGuid()
            });

            deleteAgent.ConnectionDetails = DataAgentWriterTest.CreateConnectionDetails(new ConnectionDetail
            {
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                ReleaseState = ReleaseState.Prod,
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                AadAppId = Guid.NewGuid()
            });

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.UpdateAsync(deleteAgent)).ConfigureAwait(false);

            Assert.Equal(ConflictType.InvalidValue_Immutable, exn.ConflictType);
            Assert.Equal("connectionDetails[Prod].aadAppId", exn.Target);
        }

        [Theory(DisplayName = "When UpdateAsync is called and it already has prod details, then prod details are mutable for service admins.")]
        [ValidData(WriteAction.Update, disableAuthFixtures: true)]
        public async Task When_UpdateAsyncWithProdConnectionDetails_Then_DetailsAreMutableForServiceAdmins(
            [Frozen] Mock<IAuthorizationProvider> authProvider,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            DeleteAgent deleteAgent,
            [Frozen] DeleteAgent existingDeleteAgent,
            DeleteAgentWriter writer,
            TrackingDetails trackingDetails)
        {
            authProvider.Setup(m => m.TryAuthorizeAsync(AuthorizationRole.ServiceAdmin, null)).ReturnsAsync(true);
            storageWriter.Setup(m => m.UpdateDataAgentAsync(existingDeleteAgent)).ReturnsAsync(existingDeleteAgent);

            existingDeleteAgent.TrackingDetails = trackingDetails;
            existingDeleteAgent.ConnectionDetails = DataAgentWriterTest.CreateConnectionDetails(new ConnectionDetail
            {
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                ReleaseState = ReleaseState.Prod,
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                AadAppId = Guid.NewGuid()
            });

            deleteAgent.ConnectionDetails = DataAgentWriterTest.CreateConnectionDetails(new ConnectionDetail
            {
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                ReleaseState = ReleaseState.Prod,
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                AadAppId = Guid.NewGuid()
            });

            await writer.UpdateAsync(deleteAgent).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When UpdateAsync is called and it already has prod details, then preprod details are still mutable.")]
        [ValidData(WriteAction.Update, disableAuthFixtures: true)]
        public async Task When_UpdateAsyncWithProdConnectionDetails_Then_PreProdDetailsAreMutable(
            [Frozen] Mock<IAuthorizationProvider> authProvider,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            DeleteAgent deleteAgent,
            [Frozen] DeleteAgent existingDeleteAgent,
            TrackingDetails trackingDetails,
            DeleteAgentWriter writer)
        {
            authProvider.Setup(m => m.TryAuthorizeAsync(AuthorizationRole.ServiceAdmin, null)).ReturnsAsync(false);
            storageWriter.Setup(m => m.UpdateDataAgentAsync(existingDeleteAgent)).ReturnsAsync(existingDeleteAgent);

            existingDeleteAgent.TrackingDetails = trackingDetails;

            var prodDetails = new ConnectionDetail
            {
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                ReleaseState = ReleaseState.Prod,
                AgentReadiness = AgentReadiness.TestInProd,
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                AadAppId = Guid.NewGuid()
            };

            existingDeleteAgent.ConnectionDetails = DataAgentWriterTest.CreateConnectionDetails(new ConnectionDetail
            {
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                ReleaseState = ReleaseState.PreProd,
                AgentReadiness = AgentReadiness.ProdReady,
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                AadAppId = Guid.NewGuid()
            });
            existingDeleteAgent.ConnectionDetails[ReleaseState.Prod] = prodDetails;

            deleteAgent.ConnectionDetails = DataAgentWriterTest.CreateConnectionDetails(new ConnectionDetail
            {
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                ReleaseState = ReleaseState.PreProd,
                AgentReadiness = AgentReadiness.ProdReady,
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                AadAppId = Guid.NewGuid()
            });
            deleteAgent.ConnectionDetails[ReleaseState.Prod] = prodDetails;

            await writer.UpdateAsync(deleteAgent).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When UpdateAsync is called and user is in at least one incoming and one existing sg, then pass."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWitPartialSg_Then_Pass(
            [Frozen] ICoreConfiguration config,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            [Frozen] Mock<ICachedActiveDirectory> cachedActiveDirectory,
            [Frozen] Mock<IDataOwnerReader> dataOwnerReader,
            DeleteAgentWriter writer,
            DeleteAgent newAgent,
            [Frozen] DeleteAgent existingAgent,
            DataOwner newOwner,
            DataOwner existingOwner,
            TrackingDetails trackingDetails)
        {
            EntityWriterTest.PopulateIds(existingAgent, newAgent, existingOwner, newOwner);

            existingAgent.Id = newAgent.Id;
            existingAgent.TrackingDetails = trackingDetails;
            existingAgent.OwnerId = existingOwner.Id;
            newAgent.OwnerId = newOwner.Id;

            storageWriter.Setup(m => m.UpdateDataAgentAsync(existingAgent)).ReturnsAsync(existingAgent);

            cachedActiveDirectory
                .Setup(m => m.GetSecurityGroupIdsAsync(It.IsAny<AuthenticatedPrincipal>()))
                .ReturnsAsync(new[] { existingOwner.WriteSecurityGroups.First(), newOwner.WriteSecurityGroups.First(), Guid.Parse(config.ServiceAdminSecurityGroups.First()) });

            dataOwnerReader.Setup(m => m.ReadByIdAsync(newOwner.Id, ExpandOptions.None)).ReturnsAsync(newOwner);
            dataOwnerReader.Setup(m => m.ReadByIdAsync(existingOwner.Id, ExpandOptions.None)).ReturnsAsync(existingOwner);
            newAgent.InProdDate = null;

            await writer.UpdateAsync(newAgent).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When UpdateAsync is called and user is not in incoming SG, then fail."), ValidData(WriteAction.Update, false)]
        public async Task When_UpdateAsyncWithMissingIncomingSg_Then_Fail(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            [Frozen] Mock<ICachedActiveDirectory> cachedActiveDirectory,
            [Frozen] Mock<IDataOwnerReader> dataOwnerReader,
            DeleteAgentWriter writer,
            DeleteAgent newAgent,
            [Frozen] DeleteAgent existingAgent,
            DataOwner newOwner,
            DataOwner existingOwner,
            TrackingDetails trackingDetails)
        {
            EntityWriterTest.PopulateIds(existingAgent, newAgent, existingOwner, newOwner);

            existingAgent.Id = newAgent.Id;
            existingAgent.TrackingDetails = trackingDetails;
            existingAgent.OwnerId = existingOwner.Id;
            newAgent.OwnerId = newOwner.Id;

            storageWriter.Setup(m => m.UpdateDataAgentAsync(existingAgent)).ReturnsAsync(existingAgent);

            cachedActiveDirectory
                .Setup(m => m.GetSecurityGroupIdsAsync(It.IsAny<AuthenticatedPrincipal>()))
                .ReturnsAsync(new[] { existingOwner.WriteSecurityGroups.First(), Guid.Parse("00000000-0000-0000-0000-000000000001") });

            dataOwnerReader.Setup(m => m.ReadByIdAsync(newOwner.Id, ExpandOptions.None)).ReturnsAsync(newOwner);
            dataOwnerReader.Setup(m => m.ReadByIdAsync(existingOwner.Id, ExpandOptions.None)).ReturnsAsync(existingOwner);

            await Assert.ThrowsAsync<SecurityGroupMissingWritePermissionException>(() => writer.UpdateAsync(newAgent)).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When UpdateAsync is called and user is not in existing SG, then fail."), ValidData(WriteAction.Update, false)]
        public async Task When_UpdateAsyncWithMissingExistingSg_Then_Fail(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            [Frozen] Mock<ICachedActiveDirectory> cachedActiveDirectory,
            [Frozen] Mock<IDataOwnerReader> dataOwnerReader,
            DeleteAgentWriter writer,
            DeleteAgent newAgent,
            [Frozen] DeleteAgent existingAgent,
            DataOwner newOwner,
            DataOwner existingOwner,
            TrackingDetails trackingDetails)
        {
            EntityWriterTest.PopulateIds(existingAgent, newAgent, existingOwner, newOwner);

            existingAgent.Id = newAgent.Id;
            existingAgent.TrackingDetails = trackingDetails;
            existingAgent.OwnerId = existingOwner.Id;
            newAgent.OwnerId = newOwner.Id;

            storageWriter.Setup(m => m.UpdateDataAgentAsync(existingAgent)).ReturnsAsync(existingAgent);

            cachedActiveDirectory
                .Setup(m => m.GetSecurityGroupIdsAsync(It.IsAny<AuthenticatedPrincipal>()))
                .ReturnsAsync(new[] { newOwner.WriteSecurityGroups.First(), Guid.Parse("00000000-0000-0000-0000-000000000001") });

            dataOwnerReader.Setup(m => m.ReadByIdAsync(newOwner.Id, ExpandOptions.None)).ReturnsAsync(newOwner);
            dataOwnerReader.Setup(m => m.ReadByIdAsync(existingOwner.Id, ExpandOptions.None)).ReturnsAsync(existingOwner);

            await Assert.ThrowsAsync<SecurityGroupMissingWritePermissionException>(() => writer.UpdateAsync(newAgent)).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When UpdateAsync is called with same in-Prod date, then pass.")]
        [ValidData(WriteAction.Update, disableAuthFixtures: true)]
        public async Task When_UpdateAsyncWithSameInProdDate_Then_Pass(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            DeleteAgent deleteAgent,
            [Frozen] DeleteAgent existingDeleteAgent,
            TrackingDetails trackingDetails,
            DeleteAgentWriter writer)
        {
            existingDeleteAgent.InProdDate = DateTime.Parse("09/01/2018 01:00:00");
            existingDeleteAgent.ConnectionDetails[ReleaseState.Prod] = new ConnectionDetail
            {
                ReleaseState = ReleaseState.Prod,
                AgentReadiness = AgentReadiness.ProdReady,
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                AadAppId = Guid.NewGuid()
            };
            existingDeleteAgent.TrackingDetails = trackingDetails;
            storageWriter.Setup(m => m.UpdateDataAgentAsync(existingDeleteAgent)).ReturnsAsync(existingDeleteAgent);

            deleteAgent.InProdDate = existingDeleteAgent.InProdDate;
            await writer.UpdateAsync(deleteAgent).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When UpdateAsync is called with differet in Prod date, then fail.")]
        [ValidData(WriteAction.Update, disableAuthFixtures: true)]
        public async Task When_UpdateAsyncWithDifferentInProdDate_Then_Fail(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            DeleteAgent deleteAgent,
            [Frozen] DeleteAgent existingDeleteAgent,
            DeleteAgentWriter writer)
        {
            existingDeleteAgent.InProdDate = DateTime.Parse("09/01/2018 01:00:00");
            existingDeleteAgent.ConnectionDetails[ReleaseState.Prod] = new ConnectionDetail
            {
                ReleaseState = ReleaseState.Prod,
                AgentReadiness = AgentReadiness.ProdReady,
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                AadAppId = Guid.NewGuid()
            };

            storageWriter.Setup(m => m.UpdateDataAgentAsync(existingDeleteAgent)).ReturnsAsync(existingDeleteAgent);

            deleteAgent.InProdDate = DateTime.Parse("09/01/2018 02:00:00");
            deleteAgent.ConnectionDetails[ReleaseState.Prod] = existingDeleteAgent.ConnectionDetails[ReleaseState.Prod];

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.UpdateAsync(deleteAgent)).ConfigureAwait(false);

            Assert.Equal(ConflictType.InvalidValue_Immutable, exn.ConflictType);
            Assert.Equal("deleteAgent.InProdDate", exn.Target);
        }

        [Theory(DisplayName = "When UpdateAsync is called without an Agent or  owner Icm Connector and agent is ProdReady, then fail."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithNoAgentIcmForProdReady_Then_Fail(
            [Frozen] DataOwner incomingOwner,
            DeleteAgent deleteAgent,
            DeleteAgentWriter writer)
        {
            deleteAgent.ConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>();
            deleteAgent.ConnectionDetails[ReleaseState.Prod] = new ConnectionDetail
            {
                ReleaseState = ReleaseState.Prod,
                AgentReadiness = AgentReadiness.ProdReady,
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                AadAppId = Guid.NewGuid()
            };
            deleteAgent.Icm = null;
            incomingOwner.Icm = null;

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.UpdateAsync(deleteAgent)).ConfigureAwait(false);

            Assert.Equal(ConflictType.NullValue, exn.ConflictType);
        }

        [Theory(DisplayName = "When UpdateAsync is called with an agent Icm Connector and agent is ProdReady, then succeed."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithAgentIcmConnectorForProdReady_Then_Succeed(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            [Frozen] DataOwner incomingOwner,
            DeleteAgent deleteAgent,
            [Frozen] DeleteAgent existingDeleteAgent,
            DeleteAgentWriter writer,
            TrackingDetails trackingDetails)
        {
            deleteAgent.ConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>();
            deleteAgent.ConnectionDetails[ReleaseState.Prod] = new ConnectionDetail
            {
                ReleaseState = ReleaseState.Prod,
                AgentReadiness = AgentReadiness.ProdReady,
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                AadAppId = Guid.NewGuid()
            };

            incomingOwner.Icm = null;
            Assert.NotNull(deleteAgent.Icm);

            storageWriter.Setup(m => m.UpdateDataAgentAsync(existingDeleteAgent)).ReturnsAsync(existingDeleteAgent);
            existingDeleteAgent.TrackingDetails = trackingDetails;

            await writer.UpdateAsync(deleteAgent).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When UpdateAsync is called with an data owner Icm Connector and agent is ProdReady, then succeed."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithOwnerIcmConnectorForProdReady_Then_Succeed(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            [Frozen] DataOwner incomingOwner,
            DeleteAgent deleteAgent,
            [Frozen] DeleteAgent existingDeleteAgent,
            DeleteAgentWriter writer,
            TrackingDetails trackingDetails)
        {
            deleteAgent.ConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>();
            deleteAgent.ConnectionDetails[ReleaseState.Prod] = new ConnectionDetail
            {
                ReleaseState = ReleaseState.Prod,
                AgentReadiness = AgentReadiness.ProdReady,
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                AadAppId = Guid.NewGuid()
            };

            deleteAgent.Icm = null;
            Assert.NotNull(incomingOwner.Icm);

            storageWriter.Setup(m => m.UpdateDataAgentAsync(existingDeleteAgent)).ReturnsAsync(existingDeleteAgent);
            existingDeleteAgent.TrackingDetails = trackingDetails;

            await writer.UpdateAsync(deleteAgent).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When DeleteAsync is called for DeleteAgent, then the storage layer is called."), ValidData(WriteAction.Update)]
        public async Task When_DeleteAsync_Then_CallStorageLayer(
            [Frozen] Mock<IDeleteAgentReader> agentReader,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            [Frozen] Mock<IDeleteAgentReader> entityReader,
            Guid id,
            string etag,
            [Frozen] DeleteAgent agent,
            DeleteAgentWriter writer,
            TrackingDetails trackingDetails)
        {
            agent.TrackingDetails = trackingDetails;
            agent.ETag = etag;

            agentReader.Setup(m => m.IsLinkedToAnyOtherEntities(It.IsAny<Guid>())).ReturnsAsync(false);
            entityReader.Setup(m => m.HasPendingCommands(It.IsAny<Guid>())).Returns(Task.FromResult(false));

            await writer.DeleteAsync(id, etag).ConfigureAwait(false);

            storageWriter.Verify(m => m.UpdateDataAgentAsync(It.Is<DeleteAgent>(x => x.IsDeleted == true)), Times.Once);
        }

        [Theory(DisplayName = "When DeleteAsync is called for DeleteAgent With Kusto Query returning pending commands > 0, then the storage layer call should not happen."), ValidData(WriteAction.Update)]
        public async Task When_DeleteAsync_PendingCommandsFound_ThrowException(
            [Frozen] Mock<IDeleteAgentReader> agentReader,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            [Frozen] Mock<IDeleteAgentReader> entityReader,
            Guid id,
            string etag,
            [Frozen] DeleteAgent agent,
            DeleteAgentWriter writer,
            TrackingDetails trackingDetails)
        {
            agent.TrackingDetails = trackingDetails;
            agent.ETag = etag;

            agentReader.Setup(m => m.IsLinkedToAnyOtherEntities(It.IsAny<Guid>())).ReturnsAsync(false);
            entityReader.Setup(m => m.HasPendingCommands(It.IsAny<Guid>())).Returns(Task.FromResult(true));

            var exception = await Assert.ThrowsAsync<ConflictException>(() => writer.DeleteAsync(id, etag)).ConfigureAwait(false);

            Assert.NotNull(exception);
            Assert.Equal(ConflictType.PendingCommandsExists, exception.ConflictType);
            Assert.Equal("Unable to perform delete. Pending commands were found.", exception.Message);

            // Should not call storage layer when pending commands found.
            storageWriter.Verify(m => m.UpdateDataAgentAsync(It.Is<DeleteAgent>(x => x.IsDeleted == true)), Times.Never);
        }

        [Theory(DisplayName = "When DeleteAsync is called for DeleteAgent with no pending commands, then the call storage layer."), ValidData(WriteAction.Update)]
        public async Task When_DeleteAsync_ZeroPendingCommandsFound_CallStorageLayer(
            [Frozen] Mock<IDeleteAgentReader> agentReader,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            [Frozen] Mock<IDeleteAgentReader> entityReader,
            Guid id,
            string etag,
            [Frozen] DeleteAgent agent,
            DeleteAgentWriter writer,
            TrackingDetails trackingDetails)
        {
            agent.TrackingDetails = trackingDetails;
            agent.ETag = etag;

            agentReader.Setup(m => m.IsLinkedToAnyOtherEntities(It.IsAny<Guid>())).ReturnsAsync(false);
            entityReader.Setup(m => m.HasPendingCommands(It.IsAny<Guid>())).Returns(Task.FromResult(false));

            await writer.DeleteAsync(id, etag).ConfigureAwait(false);

            storageWriter.Verify(m => m.UpdateDataAgentAsync(It.Is<DeleteAgent>(x => x.IsDeleted == true)), Times.Once);
        }        

        [Theory(DisplayName = "When DeleteAsync is called for Prod enabled DeleteAgent, then require service editor role."), ValidData(WriteAction.Update, disableAuthFixtures: true)]
        public async Task When_DeleteAsyncForProdEnabledAgent_Then_RequireServiceEditor(
            [Frozen] Mock<IDeleteAgentReader> agentReader,
            [Frozen] Mock<IDeleteAgentReader> entityReader,
            Guid id,
            string etag,
            [Frozen] DeleteAgent agent,
            [Frozen] Mock<IAuthorizationProvider> authorizationProvider,
            DeleteAgentWriter writer,
            TrackingDetails trackingDetails)
        {
            agent.TrackingDetails = trackingDetails;
            agent.ETag = etag;
            agent.ConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>
            {
                { ReleaseState.Prod, null }
            };

            agentReader.Setup(m => m.IsLinkedToAnyOtherEntities(It.IsAny<Guid>())).ReturnsAsync(false);
            entityReader.Setup(m => m.HasPendingCommands(It.IsAny<Guid>())).Returns(Task.FromResult(false));

            await writer.DeleteAsync(id, etag).ConfigureAwait(false);

            authorizationProvider.Verify(m => m.AuthorizeAsync(AuthorizationRole.ServiceEditor, It.IsAny<Func<Task<IEnumerable<DataOwner>>>>()), Times.Once);
        }

        [Theory(DisplayName = "When DeleteAsync is called for non-prod enabled DeleteAgent, then require service editor role."), ValidData(WriteAction.Update, disableAuthFixtures: true)]
        public async Task When_DeleteAsyncForNonProdEnabledAgent_Then_DoNotRequireServiceEditor(
            [Frozen] Mock<IDeleteAgentReader> agentReader,
            [Frozen] Mock<IDeleteAgentReader> entityReader,
            Guid id,
            string etag,
            [Frozen] DeleteAgent agent,
            [Frozen] Mock<IAuthorizationProvider> authorizationProvider,
            DeleteAgentWriter writer,
            TrackingDetails trackingDetails)
        {
            agent.TrackingDetails = trackingDetails;
            agent.ETag = etag;
            agent.ConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>();

            agentReader.Setup(m => m.IsLinkedToAnyOtherEntities(It.IsAny<Guid>())).ReturnsAsync(false);
            entityReader.Setup(m => m.HasPendingCommands(It.IsAny<Guid>())).Returns(Task.FromResult(false));

            await writer.DeleteAsync(id, etag).ConfigureAwait(false);

            authorizationProvider.Verify(m => m.AuthorizeAsync(AuthorizationRole.ServiceEditor, It.IsAny<Func<Task<IEnumerable<DataOwner>>>>()), Times.Once);
        }

        [Theory(DisplayName = "When DeleteAsync is called with override, do not check for pending commands."), ValidData(WriteAction.SoftDelete)]
        public async Task When_DeleteAsyncCalled_WithOverride_Then_DoNotCallHasPendingCommands(
            [Frozen] Mock<IDeleteAgentReader> agentReader,
            Guid id,
            string etag,
            [Frozen] DeleteAgent agent,
            DeleteAgentWriter writer,
            TrackingDetails trackingDetails)
        {
            agent.TrackingDetails = trackingDetails;
            agent.ETag = etag;
            agent.ConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>();

            agentReader.Setup(m => m.IsLinkedToAnyOtherEntities(It.IsAny<Guid>())).ReturnsAsync(false);
            agentReader.Setup(m => m.HasPendingCommands(It.IsAny<Guid>())).Returns(Task.FromResult(false));

            // Delete with override = true
            await writer.DeleteAsync(id, etag, true, false).ConfigureAwait(false); 
            
            // Should check for Linked entities
            agentReader.Verify(m => m.IsLinkedToAnyOtherEntities(It.IsAny<Guid>()), Times.Once);

            // Should not check for Pending Commands
            agentReader.Verify(m => m.HasPendingCommands(It.IsAny<Guid>()), Times.Never);
        }

        [Theory(DisplayName = "When DeleteAsync is called with force flag set, should still check linked entities."), ValidData(WriteAction.SoftDelete)]
        public async Task When_DeleteAsyncCalled_WithForce_Then_StillCheckLinkedEntity(
            [Frozen] Mock<IDeleteAgentReader> agentReader,
            Guid id,
            string etag,
            [Frozen] DeleteAgent agent,
            DeleteAgentWriter writer,
            TrackingDetails trackingDetails)
        {
            agent.TrackingDetails = trackingDetails;
            agent.ETag = etag;
            agent.ConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>();

            agentReader.Setup(m => m.IsLinkedToAnyOtherEntities(It.IsAny<Guid>())).ReturnsAsync(true);

            // Should still throw since "force" can only be used with VariantDefinition
            await Assert.ThrowsAsync<ConflictException>(() => writer.DeleteAsync(id, etag, false, true)).ConfigureAwait(false);
        }

        public class ValidDataAttribute : AutoMoqDataAttribute
        {
            public ValidDataAttribute(WriteAction action = WriteAction.Create, bool treatAsAdmin = true, bool disableAuthFixtures = false) : base(true)
            {
                var connectionDetail = new ConnectionDetail();
                connectionDetail.Protocol = Policies.Current.Protocols.Ids.CosmosDeleteSignalV2;
                connectionDetail.ReleaseState = ReleaseState.Prod;

                this.Fixture.FreezeMapper();
                this.Fixture.Inject<IValidator>(this.Fixture.Create<Validator>());
                this.Fixture.Inject(DataAgentWriterTest.CreateConnectionDetails(connectionDetail));

                this.Fixture.Customize<Icm>(obj =>
                    obj
                    .Without(x => x.Source)
                    .Without(x => x.TenantId));

                this.Fixture.Customize<Entity>(obj =>
                    obj
                    .Without(x => x.Id)
                    .Without(x => x.ETag)
                    .Without(x => x.TrackingDetails));

                this.Fixture.Customize<DataOwner>(obj =>
                    obj
                    .Without(x => x.ServiceTree));

                if (action == WriteAction.Create)
                {
                    this.Fixture.Customize<DeleteAgent>(obj =>
                    obj
                    .Without(x => x.Owner)
                    .Without(x => x.AssetGroups)
                    .Without(x => x.Capabilities)
                    .Without(x => x.InProdDate)
                    .Without(x => x.MigratingConnectionDetails));
                }
                else
                {
                    var id = this.Fixture.Create<Guid>();

                    this.Fixture.Customize<DeleteAgent>(obj =>
                    obj
                    .With(x => x.Id, id)
                    .With(x => x.ETag, "ETag")
                    .Without(x => x.Owner)
                    .Without(x => x.AssetGroups)
                    .Without(x => x.InProdDate)
                    .Without(x => x.MigratingConnectionDetails));
                }

                this.Fixture.Customize<FilterResult<DeleteAgent>>(obj =>
                    obj
                    .With(x => x.Values, Enumerable.Empty<DeleteAgent>()));

                if (!disableAuthFixtures)
                {
                    // Ensure all calls are preformed as an admin.
                    var writeSecurityGroups = this.Fixture.Create<IEnumerable<Guid>>();

                    this.Fixture.RegisterAuthorizationClasses(writeSecurityGroups, treatAsAdmin: treatAsAdmin);
                }
            }
        }

        public class InlineValidDataAttribute : InlineAutoMoqDataAttribute
        {
            public InlineValidDataAttribute(WriteAction action = WriteAction.Create, params object[] values) : base(new ValidDataAttribute(action), values)
            {
            }
        }
    }
}