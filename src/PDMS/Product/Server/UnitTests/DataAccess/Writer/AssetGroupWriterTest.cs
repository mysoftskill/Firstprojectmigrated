namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Writer.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.DataAccess.Reader;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.UnitTest;
    using Microsoft.PrivacyServices.DataManagement.Models;
    using Microsoft.PrivacyServices.DataManagement.Models.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;
    using Microsoft.PrivacyServices.Identity;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;

    public class AssetGroupWriterTest
    {
        [Theory(DisplayName = "When CreateAsync is called with an owner that has no write security groups, then fail."), ValidData]
        public async Task When_CreateAsyncWithoutWriteSecurityGroups_Then_Fail(
            [Frozen] DataOwner dataOwner,
            AssetGroup assetGroup,
            AssetGroupWriter writer)
        {
            dataOwner.WriteSecurityGroups = null;

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.CreateAsync(assetGroup)).ConfigureAwait(false);

            Assert.Equal("dataOwner.writeSecurityGroups", exn.Target);
            Assert.Equal(ConflictType.DoesNotExist, exn.ConflictType);
        }

        [Theory(DisplayName = "When CreateAsync is called, then the storage layer is called and returned."), ValidData]
        public async Task When_CreateAsync_Then_CallAndReturnStorageValue(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            AssetGroup assetGroup,
            [Frozen] AssetGroup storageAssetGroup,
            AssetGroupWriter writer)
        {
            var result = await writer.CreateAsync(assetGroup).ConfigureAwait(false);

            Assert.Equal(storageAssetGroup, result);

            storageWriter.Verify(m => m.CreateAssetGroupAsync(assetGroup), Times.Once);
        }

        [Theory(DisplayName = "When CreateAsync is called without an owner id, then pass."), ValidData]
        public async Task When_CreateAsyncWithoutOwnerId_Then_Pass(
            AssetGroup assetGroup,
            AssetGroupWriter writer)
        {
            assetGroup.OwnerId = Guid.Empty;
            assetGroup.InventoryId = Guid.Empty;
            await writer.CreateAsync(assetGroup).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When CreateAsync is called with empty owner id and empty agent ids, then fail."), ValidData]
        public async Task When_CreateAsyncWithEmptyOwnerIdAndAgentIds_Then_Fail(
            AssetGroup assetGroup,
            AssetGroupWriter writer)
        {
            assetGroup.OwnerId = Guid.Empty;
            assetGroup.DeleteAgentId = Guid.Empty;
            assetGroup.ExportAgentId = Guid.Empty;
            assetGroup.AccountCloseAgentId = Guid.Empty;

            var exn = await Assert.ThrowsAsync<MissingPropertyException>(() => writer.CreateAsync(assetGroup)).ConfigureAwait(false);

            Assert.Equal("ownerId,deleteAgentId", exn.ParamName);
        }

        [Theory(DisplayName = "When CreateAsync is called with an empty owner id and null agent ids, then fail."), ValidData]
        public async Task When_CreateAsyncWithEmptyOwnerIdAndNullAgentIds_Then_Fail(
            AssetGroup assetGroup,
            AssetGroupWriter writer)
        {
            assetGroup.OwnerId = Guid.Empty;
            assetGroup.DeleteAgentId = null;
            assetGroup.ExportAgentId = null;
            assetGroup.AccountCloseAgentId = null;

            var exn = await Assert.ThrowsAsync<MissingPropertyException>(() => writer.CreateAsync(assetGroup)).ConfigureAwait(false);

            Assert.Equal("ownerId,deleteAgentId", exn.ParamName);
        }

        [Theory(DisplayName = "When CreateAsync is called with a variant, then fail."), ValidData]
        public async Task When_CreateAsyncWithVariant_Then_Fail(
            AssetGroup assetGroup,
            AssetGroupVariant assetGroupVariant,
            AssetGroupWriter writer)
        {
            assetGroup.Variants = new[] { assetGroupVariant };

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(assetGroup)).ConfigureAwait(false);

            Assert.Equal("variants", exn.ParamName);
        }

        [Theory(DisplayName = "When CreateAsync is called with an owner, then fail."), ValidData]
        public async Task When_CreateAsyncWithOwner_Then_Fail(
            DataOwner owner,
            AssetGroup assetGroup,
            AssetGroupWriter writer)
        {
            assetGroup.Owner = owner;

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(assetGroup)).ConfigureAwait(false);

            Assert.Equal("owner", exn.ParamName);
            Assert.Null(exn.Value);
        }

        [Theory(DisplayName = "When CreateAsync is called with an owner id that does not exist, then fail."), ValidData]
        public async Task When_CreateAsyncWithUnknownOwner_Then_Fail(
            [Frozen] Mock<IDataOwnerReader> dataOwnerReader,
            AssetGroup assetGroup,
            AssetGroupWriter writer)
        {
            dataOwnerReader.Setup(m => m.ReadByIdAsync(assetGroup.OwnerId, ExpandOptions.None)).ReturnsAsync(null as DataOwner);

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.CreateAsync(assetGroup)).ConfigureAwait(false);

            Assert.Equal("ownerId", exn.Target);
            Assert.Equal(ConflictType.DoesNotExist, exn.ConflictType);
        }

        [Theory(DisplayName = "When CreateAsync is called without a owner id, then do not query storage."), ValidData]
        public async Task When_CreateAsyncWithoutOwnertId_Then_SkipStorageCall(
            [Frozen] Mock<IDataOwnerReader> dataOwnerReader,
            [Frozen] Mock<IDeleteAgentReader> deleteAgentReader,
            AssetGroup assetGroup,
            AssetGroupWriter writer,
            DeleteAgent deleteAgent)
        {
            assetGroup.OwnerId = Guid.Empty;
            assetGroup.InventoryId = Guid.Empty;
            deleteAgentReader.Setup(m => m.ReadByIdAsync(assetGroup.DeleteAgentId.Value, ExpandOptions.WriteProperties)).ReturnsAsync(deleteAgent);

            await writer.CreateAsync(assetGroup).ConfigureAwait(false);

            dataOwnerReader.Verify(m => m.ReadByIdAsync(It.IsAny<Guid>(), It.IsAny<ExpandOptions>()), Times.Once);
            dataOwnerReader.Verify(m => m.ReadByIdAsync(deleteAgent.OwnerId, ExpandOptions.None), Times.Once);
        }

        [Theory(DisplayName = "When CreateAsync is called with a delete agent id that does not exist, then fail."), ValidData]
        public async Task When_CreateAsyncWithUnknownDeleteAgent_Then_Fail(
            [Frozen] Mock<IDeleteAgentReader> deleteAgentReader,
            AssetGroup assetGroup,
            AssetGroupWriter writer)
        {
            deleteAgentReader.Setup(m => m.ReadByIdAsync(assetGroup.DeleteAgentId.Value, ExpandOptions.WriteProperties)).ReturnsAsync(null as DeleteAgent);

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.CreateAsync(assetGroup)).ConfigureAwait(false);

            Assert.Equal("deleteAgentId", exn.Target);
            Assert.Equal(ConflictType.DoesNotExist, exn.ConflictType);
        }

        [Theory(DisplayName = "When CreateAsync is called with a delete agent id that does not have valid protocols, then fail."), ValidData]
        public async Task When_CreateAsyncWithDeleteAgentNotHaveValidProtocols_Then_Fail(
            [Frozen] Mock<IDeleteAgentReader> deleteAgentReader,
            AssetGroup assetGroup,
            AssetGroupWriter writer,
            DeleteAgent deleteAgent,
            ConnectionDetail connectionDetail)
        {
            connectionDetail.Protocol = null;
            deleteAgent.ConnectionDetails = DataAgentWriterTest.CreateConnectionDetails(connectionDetail);

            deleteAgentReader.Setup(m => m.ReadByIdAsync(assetGroup.DeleteAgentId.Value, ExpandOptions.WriteProperties)).ReturnsAsync(deleteAgent);

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(assetGroup)).ConfigureAwait(false);

            Assert.Equal($"connectionDetails[{connectionDetail.ReleaseState}].protocol", exn.ParamName);
            Assert.Null(exn.Value);
        }

        [Theory(DisplayName = "When CreateAsync is called with an export agent id that does not exist, then fail."), ValidData]
        public async Task When_CreateAsyncWithUnknownExportAgent_Then_Fail(
            [Frozen] Mock<IDeleteAgentReader> deleteAgentReader,
            AssetGroup assetGroup,
            AssetGroupWriter writer)
        {
            deleteAgentReader.Setup(m => m.ReadByIdAsync(assetGroup.ExportAgentId.Value, ExpandOptions.WriteProperties)).ReturnsAsync(null as DeleteAgent);

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.CreateAsync(assetGroup)).ConfigureAwait(false);

            Assert.Equal("exportAgentId", exn.Target);
            Assert.Equal(ConflictType.DoesNotExist, exn.ConflictType);
        }

        [Theory(DisplayName = "When CreateAsync is called with an account close agent id that does not exist, then fail."), ValidData]
        public async Task When_CreateAsyncWithUnknownAccountCloseAgent_Then_Fail(
            [Frozen] Mock<IDeleteAgentReader> deleteAgentReader,
            AssetGroup assetGroup,
            AssetGroupWriter writer)
        {
            deleteAgentReader.Setup(m => m.ReadByIdAsync(assetGroup.AccountCloseAgentId.Value, ExpandOptions.WriteProperties)).ReturnsAsync(null as DeleteAgent);

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.CreateAsync(assetGroup)).ConfigureAwait(false);

            Assert.Equal("accountCloseAgentId", exn.Target);
            Assert.Equal(ConflictType.DoesNotExist, exn.ConflictType);
        }

        [Theory(DisplayName = "When CreateAsync is called without a delete agent id, then succeed."), ValidData]
        public async Task When_CreateAsyncWithoutDeleteAgentId_Then_Succeed(
            AssetGroup assetGroup,
            AssetGroupWriter writer)
        {
            assetGroup.DeleteAgentId = Guid.Empty;

            var result = await writer.CreateAsync(assetGroup).ConfigureAwait(false);

            Assert.NotNull(result);
        }

        [Theory(DisplayName = "When CreateAsync is called with a delete agent, then fail."), ValidData]
        public async Task When_CreateAsyncWithDeleteAgent_Then_Fail(
            DeleteAgent deleteAgent,
            AssetGroup assetGroup,
            AssetGroupWriter writer)
        {
            assetGroup.DeleteAgent = deleteAgent;

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(assetGroup)).ConfigureAwait(false);

            Assert.Equal("deleteAgent", exn.ParamName);
            Assert.Null(exn.Value);
        }

        [Theory(DisplayName = "When CreateAsync is called with an export agent, then fail."), ValidData]
        public async Task When_CreateAsyncWithExportAgent_Then_Fail(
            DeleteAgent exportAgent,
            AssetGroup assetGroup,
            AssetGroupWriter writer)
        {
            assetGroup.ExportAgent = exportAgent;

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(assetGroup)).ConfigureAwait(false);

            Assert.Equal("exportAgent", exn.ParamName);
            Assert.Null(exn.Value);
        }

        [Theory(DisplayName = "When CreateAsync is called with an account close agent, then fail."), ValidData]
        public async Task When_CreateAsyncWithAccountCloseAgent_Then_Fail(
            DeleteAgent accountCloseAgent,
            AssetGroup assetGroup,
            AssetGroupWriter writer)
        {
            assetGroup.AccountCloseAgent = accountCloseAgent;

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(assetGroup)).ConfigureAwait(false);

            Assert.Equal("accountCloseAgent", exn.ParamName);
            Assert.Null(exn.Value);
        }

        [Theory(DisplayName = "When CreateAsync is called with an inventory, then fail."), ValidData]
        public async Task When_CreateAsyncWithInventory_Then_Fail(
            Inventory inventory,
            AssetGroup assetGroup,
            AssetGroupWriter writer)
        {
            assetGroup.Inventory = inventory;

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(assetGroup)).ConfigureAwait(false);

            Assert.Equal("inventory", exn.ParamName);
            Assert.Null(exn.Value);
        }

        [Theory(DisplayName = "When CreateAsync is called without any agent id, then do not query storage."), ValidData]
        public async Task When_CreateAsyncWithoutAnyAgentId_Then_SkipStorageCall(
            [Frozen] Mock<IDeleteAgentReader> deleteAgentReader,
            AssetGroup assetGroup,
            AssetGroupWriter writer)
        {
            assetGroup.DeleteAgentId = Guid.Empty;
            assetGroup.ExportAgentId = Guid.Empty;
            assetGroup.AccountCloseAgentId = Guid.Empty;

            await writer.CreateAsync(assetGroup).ConfigureAwait(false);

            deleteAgentReader.Verify(m => m.ReadByIdAsync(It.IsAny<Guid>(), It.IsAny<ExpandOptions>()), Times.Never);
        }

        [Theory(DisplayName = "When CreateAsync is called with null agent ids, then do not query storage."), ValidData]
        public async Task When_CreateAsyncWithNullDeleteAgentId_Then_SkipStorageCall(
            [Frozen] Mock<IDeleteAgentReader> deleteAgentReader,
            AssetGroup assetGroup,
            AssetGroupWriter writer)
        {
            assetGroup.DeleteAgentId = null;
            assetGroup.ExportAgentId = null;
            assetGroup.AccountCloseAgentId = null;

            await writer.CreateAsync(assetGroup).ConfigureAwait(false);

            deleteAgentReader.Verify(m => m.ReadByIdAsync(It.IsAny<Guid>(), It.IsAny<ExpandOptions>()), Times.Never);
        }

        [Theory(DisplayName = "When CreateAsync is called without a qualifier, then fail."), ValidData]
        public async Task When_CreateAsyncWithoutQualifier_Then_Fail(
            AssetGroup assetGroup,
            AssetGroupWriter writer)
        {
            assetGroup.Qualifier = null;

            var exn = await Assert.ThrowsAsync<MissingPropertyException>(() => writer.CreateAsync(assetGroup)).ConfigureAwait(false);

            Assert.Equal("qualifier", exn.ParamName);
        }

        [Theory(DisplayName = "When CreateAsync is called with a qualifier that already has an asset group, then fail."), ValidData]
        public async Task When_CreateAsyncWithExistingQualifier_Then_Fail(
            IEnumerable<AssetGroup> results,
            FilterResult<AssetGroup> queryResult,
            [Frozen] Mock<IAssetGroupReader> assetGroupReader,
            AssetGroup assetGroup,
            AssetGroupWriter writer)
        {
            results.First().Qualifier = assetGroup.Qualifier;
            results.First().OwnerId = assetGroup.OwnerId;
            queryResult.Values = results;

            Action<AssetGroupFilterCriteria> filter = f =>
            {
                f.Qualifier.SortedSequenceAssert(
                    assetGroup.QualifierParts,
                    a => a.Key,
                    b => b.Key,
                    (a, b) =>
                    {
                        Assert.Equal(a.Key, b.Key);
                        Assert.Equal(a.Value.Value, b.Value);
                    });
            };

            assetGroupReader.Setup(m => m.ReadByFiltersAsync(Is.Value(filter), ExpandOptions.None)).ReturnsAsync(queryResult);

            var exn = await Assert.ThrowsAsync<AlreadyOwnedException>(() => writer.CreateAsync(assetGroup)).ConfigureAwait(false);

            Assert.Equal(assetGroup.Qualifier.Value, exn.Value);
            Assert.Equal(assetGroup.OwnerId.ToString(), exn.OwnerId);
            Assert.Equal("qualifier", exn.Target);
            Assert.Equal(ConflictType.AlreadyExists_ClaimedByOwner, exn.ConflictType);
        }

        [Theory(DisplayName = "When CreateAsync is called with a qualifier that is more specific, then pass."), ValidData]
        public async Task When_CreateAsyncWithExistingLessSpecificQualifier_Then_Succeed(
            IEnumerable<AssetGroup> results,
            [Frozen] FilterResult<AssetGroup> queryResult,
            AssetGroup assetGroup,
            AssetGroupWriter writer)
        {
            assetGroup.Qualifier = AssetQualifier.CreateForAzureTable("a");
            results.First().Qualifier = AssetQualifier.CreateForAzureTable("a", "b");

            queryResult.Values = results;

            await writer.CreateAsync(assetGroup).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When CreateAsync is called and user is not in the owner security group, then fail."), ValidData]
        public async Task When_CreateAsyncWithMissingSecurityGroup_Then_Fail(
            IEnumerable<Guid> securityGroups,
            [Frozen] DataOwner existingOwner,
            AssetGroup assetGroup,
            AssetGroupWriter writer)
        {
            existingOwner.WriteSecurityGroups = securityGroups;

            await Assert.ThrowsAsync<SecurityGroupMissingWritePermissionException>(() => writer.CreateAsync(assetGroup)).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When CreateAsync is called without owner and user is not in the delete agent owner security group, then fail."), ValidData]
        public async Task When_CreateAsyncWithoutOwnerAndMissingSecurityGroup_Then_Fail(
            IEnumerable<Guid> securityGroups,
            [Frozen] Mock<IDataOwnerReader> dataOwnerReader,
            [Frozen] DeleteAgent deleteAgent,
            AssetGroup assetGroup,
            AssetGroupWriter writer,
            DataOwner dataOwner)
        {
            assetGroup.OwnerId = Guid.Empty;

            dataOwner.WriteSecurityGroups = securityGroups;

            dataOwnerReader.Setup(m => m.ReadByIdAsync(deleteAgent.OwnerId, ExpandOptions.None)).ReturnsAsync(dataOwner);

            await Assert.ThrowsAsync<SecurityGroupMissingWritePermissionException>(() => writer.CreateAsync(assetGroup)).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When CreateAsync is called without variants and user does not have variant editor role, then succeed."), ValidData]
        public async Task When_CreateAsyncWithoutVariantsAndUserNotHavingVariantEditorRole_Then_Succeed(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            AssetGroup assetGroup,
            AssetGroupWriter writer)
        {
            assetGroup.Variants = null;

            await writer.CreateAsync(assetGroup).ConfigureAwait(false);

            storageWriter.Verify(m => m.CreateAssetGroupAsync(assetGroup), Times.Once);
        }

        [Theory(DisplayName = "When CreateAsync is called with delete sharing request id set, then fail."), ValidData(WriteAction.Create)]
        public async Task When_CreateWithDeleteAgentAndRequestSet_Then_Fail(
            AssetGroup assetGroup,
            AssetGroupWriter writer)
        {
            assetGroup.DeleteSharingRequestId = Guid.NewGuid();

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(assetGroup)).ConfigureAwait(false);

            Assert.Equal("deleteSharingRequestId", exn.ParamName);
        }

        [Theory(DisplayName = "When CreateAsync is called with export sharing request id set, then fail."), ValidData(WriteAction.Create)]
        public async Task When_CreateWithExportAgentAndRequestSet_Then_Fail(
            AssetGroup assetGroup,
            AssetGroupWriter writer)
        {
            assetGroup.ExportSharingRequestId = Guid.NewGuid();

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(assetGroup)).ConfigureAwait(false);
            
            Assert.Equal("exportSharingRequestId", exn.ParamName);
        }

        [Theory(DisplayName = "When creating an asset group, then set its compliance state to the default value."), ValidData(WriteAction.Create)]
        public async Task When_CreateAnAssetGroup_Then_SetItToDefaultValue(
            AssetGroup assetGroup,
            AssetGroupWriter writer)
        {
            await writer.CreateAsync(assetGroup).ConfigureAwait(false);

            Assert.True(assetGroup.ComplianceState.IsCompliant);
            Assert.Null(assetGroup.ComplianceState.IncompliantReason);
        }

        [Theory(DisplayName = "When UpdateAsync is called, then the storage layer is called and returned."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsync_Then_CallAndReturnStorageValue(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            AssetGroup assetGroup,
            [Frozen] AssetGroup existingAssetGroup,
            AssetGroupWriter writer,
            TrackingDetails trackingDetails)
        {
            existingAssetGroup.TrackingDetails = trackingDetails;

            var result = await writer.UpdateAsync(assetGroup).ConfigureAwait(false);

            Assert.Equal(existingAssetGroup, result);

            storageWriter.Verify(m => m.UpdateAssetGroupAsync(existingAssetGroup), Times.Once);
        }

        [Theory(DisplayName = "When UpdateAsync is called and compliance state has changed, then do not copy."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithChangedCapabilities_Then_Fail(
            AssetGroup assetGroup,
            [Frozen] AssetGroup existingAssetGroup,
            TrackingDetails trackingDetails,
            IFixture fixture)
        {
            // Do this so that the mapper logic is triggered.
            fixture.FreezeMapper();
            var writer = fixture.Create<AssetGroupWriter>();

            assetGroup.ComplianceState = null;
            var originalComplianceState = new DataManagement.Models.V2.ComplianceState { IsCompliant = false, IncompliantReason = IncompliantReason.AssetGroupNotFound };
            existingAssetGroup.ComplianceState = originalComplianceState;
            existingAssetGroup.TrackingDetails = trackingDetails;

            await writer.UpdateAsync(assetGroup).ConfigureAwait(false);

            Assert.Equal(originalComplianceState, existingAssetGroup.ComplianceState);
        }

        [Theory(DisplayName = "When UpdateAsync is called without delete agent id, then the storage layer is called and returned."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithoutDeleteAgentId_Then_CallAndReturnStorageValue(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            AssetGroup assetGroup,
            [Frozen] AssetGroup existingAssetGroup,
            AssetGroupWriter writer,
            TrackingDetails trackingDetails)
        {
            assetGroup.DeleteAgentId = Guid.Empty;
            existingAssetGroup.DeleteAgentId = Guid.Empty;
            existingAssetGroup.TrackingDetails = trackingDetails;

            var result = await writer.UpdateAsync(assetGroup).ConfigureAwait(false);

            Assert.Equal(existingAssetGroup, result);

            storageWriter.Verify(m => m.UpdateAssetGroupAsync(existingAssetGroup), Times.Once);
        }

        [Theory(DisplayName = "When UpdateAsync is called with initial delete agent id, then the storage layer is called and returned."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithInitialDeleteAgentId_Then_CallAndReturnStorageValue(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            AssetGroup assetGroup,
            [Frozen] AssetGroup existingAssetGroup,
            AssetGroupWriter writer,
            TrackingDetails trackingDetails)
        {
            assetGroup.DeleteAgentId = Guid.NewGuid();
            existingAssetGroup.DeleteAgentId = Guid.Empty;
            existingAssetGroup.TrackingDetails = trackingDetails;

            var result = await writer.UpdateAsync(assetGroup).ConfigureAwait(false);

            Assert.Equal(existingAssetGroup, result);

            storageWriter.Verify(m => m.UpdateAssetGroupAsync(existingAssetGroup), Times.Once);
        }

        [Theory(DisplayName = "When UpdateAsync is called with a delete agent id that does not exist, then fail."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithUnknownDeleteAgent_Then_Fail(
            [Frozen] Mock<IDeleteAgentReader> deleteAgentReader,
            AssetGroup assetGroup,
            AssetGroupWriter writer,
            Guid newDeleteAgentId)
        {
            assetGroup.DeleteAgentId = newDeleteAgentId;

            deleteAgentReader.Setup(m => m.ReadByIdAsync(assetGroup.DeleteAgentId.Value, ExpandOptions.WriteProperties)).ReturnsAsync(null as DeleteAgent);

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.UpdateAsync(assetGroup)).ConfigureAwait(false);

            Assert.Equal("deleteAgentId", exn.Target);
            Assert.Equal(ConflictType.DoesNotExist, exn.ConflictType);
        }

        [Theory(DisplayName = "When UpdateAsync is called with a delete agent id that does not have valid protocols, then fail."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithDeleteAgentNotHaveValidProtocols_Then_Fail(
            [Frozen] Mock<IDeleteAgentReader> deleteAgentReader,
            AssetGroup assetGroup,
            AssetGroupWriter writer,
            Guid newDeleteAgentId,
            DeleteAgent deleteAgent,
            ConnectionDetail connectionDetail1,
            ConnectionDetail connectionDetail2)
        {
            assetGroup.DeleteAgentId = newDeleteAgentId;

            connectionDetail1.ReleaseState = ReleaseState.PreProd;
            connectionDetail1.Protocol = Policies.Current.Protocols.Ids.CommandFeedV1;

            connectionDetail1.ReleaseState = ReleaseState.Prod;
            connectionDetail2.Protocol = null;

            var connectionDetails = new Dictionary<ReleaseState, ConnectionDetail> { { connectionDetail1.ReleaseState, connectionDetail1 }, { connectionDetail2.ReleaseState, connectionDetail2 } };

            deleteAgent.ConnectionDetails = connectionDetails;

            deleteAgentReader.Setup(m => m.ReadByIdAsync(assetGroup.DeleteAgentId.Value, ExpandOptions.WriteProperties)).ReturnsAsync(deleteAgent);

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.UpdateAsync(assetGroup)).ConfigureAwait(false);

            Assert.Equal($"connectionDetails[{connectionDetail2.ReleaseState}].protocol", exn.ParamName);
            Assert.Null(exn.Value);
        }

        [Theory(DisplayName = "When UpdateAsync is called with an export agent id that does not exist, then fail."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithUnknownExportAgent_Then_Fail(
            [Frozen] Mock<IDeleteAgentReader> deleteAgentReader,
            AssetGroup assetGroup,
            AssetGroupWriter writer,
            Guid newExportAgentId)
        {
            assetGroup.ExportAgentId = newExportAgentId;

            deleteAgentReader.Setup(m => m.ReadByIdAsync(assetGroup.ExportAgentId.Value, ExpandOptions.WriteProperties)).ReturnsAsync(null as DeleteAgent);

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.UpdateAsync(assetGroup)).ConfigureAwait(false);

            Assert.Equal("exportAgentId", exn.Target);
            Assert.Equal(ConflictType.DoesNotExist, exn.ConflictType);
        }

        [Theory(DisplayName = "When UpdateAsync is called with an account close agent id that does not exist, then fail."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithUnknownAccountCloseAgent_Then_Fail(
            [Frozen] Mock<IDeleteAgentReader> deleteAgentReader,
            AssetGroup assetGroup,
            AssetGroupWriter writer,
            Guid newAccountCloseAgentId)
        {
            assetGroup.AccountCloseAgentId = newAccountCloseAgentId;

            deleteAgentReader.Setup(m => m.ReadByIdAsync(assetGroup.AccountCloseAgentId.Value, ExpandOptions.WriteProperties)).ReturnsAsync(null as DeleteAgent);

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.UpdateAsync(assetGroup)).ConfigureAwait(false);

            Assert.Equal("accountCloseAgentId", exn.Target);
            Assert.Equal(ConflictType.DoesNotExist, exn.ConflictType);
        }

        [Theory(DisplayName = "When UpdateAsync is called with an account close agent id that does not have valid protocols, then fail."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithAccountCloseAgentNotHaveValidProtocols_Then_Fail(
            [Frozen] Mock<IDeleteAgentReader> deleteAgentReader,
            AssetGroup assetGroup,
            AssetGroupWriter writer,
            Guid newAccountCloseAgentId,
            DeleteAgent accountCloseAgent,
            ConnectionDetail connectionDetail1,
            ConnectionDetail connectionDetail2)
        {
            assetGroup.AccountCloseAgentId = newAccountCloseAgentId;

            connectionDetail1.ReleaseState = ReleaseState.PreProd;
            connectionDetail1.Protocol = Policies.Current.Protocols.Ids.CommandFeedV1;

            connectionDetail1.ReleaseState = ReleaseState.Prod;
            connectionDetail2.Protocol = Policies.Current.Protocols.Ids.CosmosDeleteSignalV2;

            var connectionDetails = new Dictionary<ReleaseState, ConnectionDetail> { { connectionDetail1.ReleaseState, connectionDetail1 }, { connectionDetail2.ReleaseState, connectionDetail2 } };

            accountCloseAgent.ConnectionDetails = connectionDetails;

            deleteAgentReader.Setup(m => m.ReadByIdAsync(assetGroup.AccountCloseAgentId.Value, ExpandOptions.WriteProperties)).ReturnsAsync(accountCloseAgent);

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.UpdateAsync(assetGroup)).ConfigureAwait(false);

            Assert.Equal($"connectionDetails[{connectionDetail2.ReleaseState}].protocol", exn.ParamName);
            Assert.Equal(Policies.Current.Protocols.Ids.CosmosDeleteSignalV2.Value, exn.Value);
        }

        [Theory(DisplayName = "When UpdateAsync is called with a variant that does not exist, then fail."), ValidData(WriteAction.Update, treatAsAdmin: true)]
        public async Task When_UpdateAsyncWithUnknownVariant_Then_Fail(
            [Frozen] Mock<IVariantDefinitionReader> variantDefinitionReader,
            AssetGroup assetGroup,
            AssetGroupWriter writer,
            IEnumerable<AssetGroupVariant> variants)
        {
            assetGroup.Variants = variants;

            variantDefinitionReader.Setup(m => m.ReadByIdAsync(assetGroup.Variants.First().VariantId, ExpandOptions.None)).ReturnsAsync(null as VariantDefinition);

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.UpdateAsync(assetGroup)).ConfigureAwait(false);

            Assert.Equal("variantId", exn.Target);
            Assert.Equal(ConflictType.DoesNotExist, exn.ConflictType);
        }

        [Theory(DisplayName = "When UpdateAsync is called with existing asset group having same variants as incoming asset group, then skip the existence check for the variant."), ValidData(WriteAction.Update, treatAsAdmin: true)]
        public async Task When_UpdateAsyncWithSameSetOfVariants_Then_SkipTheVariantExistenceCheck(
            [Frozen] Mock<IVariantDefinitionReader> variantDefinitionReader,
            AssetGroup assetGroup,
            [Frozen] AssetGroup existingAssetGroup,
            AssetGroupWriter writer,
            TrackingDetails trackingDetails,
            IEnumerable<AssetGroupVariant> variants)
        {
            assetGroup.Variants = variants;
            existingAssetGroup.Variants = variants;
            existingAssetGroup.TrackingDetails = trackingDetails;

            await writer.UpdateAsync(assetGroup).ConfigureAwait(false);

            variantDefinitionReader.Verify(m => m.ReadByIdAsync(It.IsAny<Guid>(), It.IsAny<ExpandOptions>()), Times.Never);
        }

        [Theory(DisplayName = "When UpdateAsync is called with an asset group having null variants, then skip existence for each variant."), ValidData(WriteAction.Update, treatAsAdmin: true)]
        public async Task When_UpdateAsyncWithAssetGroupHavingNullVariants_Then_SkipTheVariantExistenceCheck(
            [Frozen] Mock<IVariantDefinitionReader> variantDefinitionReader,
            AssetGroup assetGroup,
            [Frozen] AssetGroup existingAssetGroup,
            AssetGroupWriter writer,
            TrackingDetails trackingDetails)
        {
            assetGroup.Variants = null;

            existingAssetGroup.TrackingDetails = trackingDetails;

            await writer.UpdateAsync(assetGroup).ConfigureAwait(false);

            variantDefinitionReader.Verify(m => m.ReadByIdAsync(It.IsAny<Guid>(), It.IsAny<ExpandOptions>()), Times.Never);
        }

        [Theory(DisplayName = "When UpdateAsync is called with existing asset group having empty variants, then check existence for each variant."), ValidData(WriteAction.Update, treatAsAdmin: true)]
        public async Task When_UpdateAsyncWithEmptyExistingVariants_Then_CheckExistenceForEachVariant(
            [Frozen] Mock<IVariantDefinitionReader> variantDefinitionReader,
            AssetGroup assetGroup,
            [Frozen] AssetGroup existingAssetGroup,
            AssetGroupWriter writer,
            TrackingDetails trackingDetails,
            IEnumerable<AssetGroupVariant> variants)
        {
            assetGroup.Variants = variants;

            existingAssetGroup.Variants = Enumerable.Empty<AssetGroupVariant>();
            existingAssetGroup.TrackingDetails = trackingDetails;

            await writer.UpdateAsync(assetGroup).ConfigureAwait(false);

            variantDefinitionReader.Verify(m => m.ReadByIdAsync(It.IsAny<Guid>(), It.IsAny<ExpandOptions>()), Times.Exactly(assetGroup.Variants.Count()));
        }

        [Theory(DisplayName = "When UpdateAsync is called with variants containing duplicate values, then fail."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncCalledWithVariantsContainingDuplicateValues_Then_Fail(
            AssetGroup assetGroup,
            AssetGroupVariant assetGroupVariant1,
            AssetGroupVariant assetGroupVariant2,
            AssetGroupWriter writer)
        {
            var variantId = Guid.NewGuid();
            assetGroupVariant1.VariantId = variantId;
            assetGroupVariant2.VariantId = variantId;

            assetGroup.Variants = new[] { assetGroupVariant1, assetGroupVariant2 };
            
            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.UpdateAsync(assetGroup)).ConfigureAwait(false);

            Assert.Equal("variants", exn.ParamName);
        }

        [Theory(DisplayName = "When UpdateAsync is called with the same delete agent ids, then skip delete agent existence check."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithSameDeleteAgent_Then_SkipDeleteAgentExistenceCheck(
            [Frozen] Mock<IDeleteAgentReader> deleteAgentReader,
            AssetGroup assetGroup,
            [Frozen] AssetGroup existingAssetGroup,
            AssetGroupWriter writer,
            TrackingDetails trackingDetails)
        {
            existingAssetGroup.TrackingDetails = trackingDetails;

            var result = await writer.UpdateAsync(assetGroup).ConfigureAwait(false);

            deleteAgentReader.Verify(m => m.ReadByIdAsync(It.IsAny<Guid>(), It.IsAny<ExpandOptions>()), Times.Never);
        }

        [Theory(DisplayName = "When UpdateAsync is called with null agent ids, then skip delete agent existence check."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithNullDeleteAgent_Then_SkipDeleteAgentExistenceCheck(
            [Frozen] Mock<IDeleteAgentReader> deleteAgentReader,
            AssetGroup assetGroup,
            [Frozen] AssetGroup existingAssetGroup,
            AssetGroupWriter writer,
            TrackingDetails trackingDetails)
        {
            assetGroup.DeleteAgentId = null;
            assetGroup.ExportAgentId = null;
            assetGroup.AccountCloseAgentId = null;

            existingAssetGroup.TrackingDetails = trackingDetails;

            var result = await writer.UpdateAsync(assetGroup).ConfigureAwait(false);

            deleteAgentReader.Verify(m => m.ReadByIdAsync(It.IsAny<Guid>(), It.IsAny<ExpandOptions>()), Times.Never);
        }

        [Theory(DisplayName = "When UpdateAsync is called with empty agent ids, then skip delete agent existence check."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithEmptyDeleteAgent_Then_SkipDeleteAgentExistenceCheck(
            [Frozen] Mock<IDeleteAgentReader> deleteAgentReader,
            AssetGroup assetGroup,
            [Frozen] AssetGroup existingAssetGroup,
            AssetGroupWriter writer,
            TrackingDetails trackingDetails)
        {
            assetGroup.DeleteAgentId = Guid.Empty;
            assetGroup.ExportAgentId = Guid.Empty;
            assetGroup.AccountCloseAgentId = Guid.Empty;

            existingAssetGroup.TrackingDetails = trackingDetails;

            var result = await writer.UpdateAsync(assetGroup).ConfigureAwait(false);

            deleteAgentReader.Verify(m => m.ReadByIdAsync(It.IsAny<Guid>(), It.IsAny<ExpandOptions>()), Times.Never);
        }


        [Theory(DisplayName = "When UpdateAsync is called with a different qualifier, then fail."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithChangedQualifier_Then_Fail(
            AssetGroup assetGroup,
            [Frozen] AssetGroup existingAssetGroup,
            AssetGroupWriter writer)
        {
            existingAssetGroup.Qualifier = AssetQualifier.CreateForApacheCassandra("host");

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.UpdateAsync(assetGroup)).ConfigureAwait(false);
            
            Assert.Equal("qualifier", exn.Target);
            Assert.Equal(ConflictType.InvalidValue_Immutable, exn.ConflictType);
        }

        [Theory(DisplayName = "When UpdateAsync is called with a different qualifier and user is a service admin, then pass."), ValidData(WriteAction.Update, treatAsAdmin: true)]
        public async Task When_UpdateAsyncWithChangedQualifierAsServiceAdmin_Then_Pass(
            AssetGroup assetGroup,
            [Frozen] AssetGroup existingAssetGroup,
            TrackingDetails trackingDetails,
            IFixture fixture)
        {
            // Do this so that the mapper logic is triggered.
            fixture.FreezeMapper();
            var writer = fixture.Create<AssetGroupWriter>();

            existingAssetGroup.TrackingDetails = trackingDetails;
            existingAssetGroup.Qualifier = AssetQualifier.CreateForApacheCassandra("host");
            assetGroup.Qualifier = AssetQualifier.CreateForApacheCassandra("HOST");

            var result = await writer.UpdateAsync(assetGroup).ConfigureAwait(false);
            Assert.Equal("AssetType=ApacheCassandra;Host=HOST", result.Qualifier.Value);
        }

        [Theory(DisplayName = "When UpdateAsync without changing the qualifier, then skip qualifier unique checking."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncNotChangingTheQualifier_Then_SkipQualifierUniqueChecking(
            [Frozen] Mock<IAssetGroupReader> entityReader,
            AssetGroup assetGroup,
            [Frozen] AssetGroup coreAssetGroup,
            AssetGroupWriter writer,
            AssetQualifier qualifier,
            TrackingDetails trackingDetails)
        {
            coreAssetGroup.TrackingDetails = trackingDetails;

            assetGroup.Qualifier = qualifier;
            coreAssetGroup.Qualifier = qualifier;

            await writer.UpdateAsync(assetGroup).ConfigureAwait(false);

            entityReader.Verify(m => m.ReadByFiltersAsync(It.IsAny<IFilterCriteria<AssetGroup>>(), It.IsAny<ExpandOptions>()), Times.Never);
        }

        [Theory(DisplayName = "When UpdateAsync is called with different owner id, then ensure it exists."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithMissingDifferentOwnerId_Then_Fail(
            [Frozen] Mock<IAssetGroupReader> readerMock,
            [Frozen] Mock<IDataOwnerReader> ownerReader,
            AssetGroup assetGroup,
            [Frozen] AssetGroup coreAssetGroup,
            AssetGroupWriter writer,
            Guid newOwnerId)
        {
            assetGroup.OwnerId = newOwnerId;
            readerMock.Setup(m => m.ReadByIdAsync(assetGroup.Id, ExpandOptions.None)).ReturnsAsync(coreAssetGroup);
            ownerReader.Setup(m => m.ReadByIdAsync(assetGroup.OwnerId, ExpandOptions.None)).ReturnsAsync(null as DataOwner);

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.UpdateAsync(assetGroup)).ConfigureAwait(false);

            Assert.Equal(ConflictType.DoesNotExist, exn.ConflictType);
            Assert.Equal("ownerId", exn.Target);
            Assert.Equal(assetGroup.OwnerId.ToString(), exn.Value);
        }

        [Theory(DisplayName = "When UpdateAsync is called with different owner id and it has a pending transfer request, then fail."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithDifferentOwnerIdAndPendingTransferRequest_Then_Fail(
            [Frozen] Mock<IAssetGroupReader> readerMock,
            [Frozen] Mock<IDataOwnerReader> ownerReader,
            AssetGroup assetGroup,
            [Frozen] AssetGroup coreAssetGroup,
            AssetGroupWriter writer,
            DataOwner dataOwner,
            Guid newOwnerId)
        {
            assetGroup.OwnerId = newOwnerId;
            dataOwner.Id = newOwnerId;
            assetGroup.HasPendingTransferRequest = true;
            coreAssetGroup.HasPendingTransferRequest = true;
            readerMock.Setup(m => m.ReadByIdAsync(assetGroup.Id, ExpandOptions.None)).ReturnsAsync(coreAssetGroup);
            ownerReader.Setup(m => m.ReadByIdAsync(dataOwner.Id, ExpandOptions.None)).ReturnsAsync(dataOwner);

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.UpdateAsync(assetGroup)).ConfigureAwait(false);

            Assert.Equal(ConflictType.InvalidValue_Immutable, exn.ConflictType);
            Assert.Equal("HasPendingTransferRequest", exn.Target);
            Assert.Equal(assetGroup.OwnerId.ToString(), exn.Value);
        }

        [Theory(DisplayName = "When UpdateAsync is called and owner id and all agent ids are removed, then fail."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithRemovedOwnerIdAndAgentIds_Then_Fail(
            AssetGroup assetGroup,
            AssetGroupWriter writer)
        {
            assetGroup.OwnerId = Guid.Empty;
            assetGroup.DeleteAgentId = Guid.Empty;
            assetGroup.ExportAgentId = Guid.Empty;
            assetGroup.AccountCloseAgentId = Guid.Empty;

            var exn = await Assert.ThrowsAsync<MissingPropertyException>(() => writer.UpdateAsync(assetGroup)).ConfigureAwait(false);
            Assert.Equal("ownerId,deleteAgentId", exn.ParamName);
        }

        [Theory(DisplayName = "When GetDataOwnersAsync is called with empty owner id and delete agent id, then return null."), ValidData]
        public async Task When_GetDataOwnersAsyncCalledWithEmptyOwnerIdAndDeleteAgentId_Then_ReturnNull(
            AssetGroup assetGroup,
            AssetGroupWriter writer)
        {
            assetGroup.OwnerId = Guid.Empty;
            assetGroup.DeleteAgentId = Guid.Empty;

            var dataOwners = await writer.GetDataOwnersAsync(WriteAction.Create, assetGroup).ConfigureAwait(false);

            Assert.Null(dataOwners);
        }

        [Theory(DisplayName = "When GetDataOwnersAsync is called with empty owner id and null delete agent id, then return null."), ValidData]
        public async Task When_GetDataOwnersAsyncCalledWithEmptyOwnerIdAndNullDeleteAgentId_Then_ReturnNull(
            AssetGroup assetGroup,
            AssetGroupWriter writer)
        {
            assetGroup.OwnerId = Guid.Empty;
            assetGroup.DeleteAgentId = null;

            var dataOwners = await writer.GetDataOwnersAsync(WriteAction.Create, assetGroup).ConfigureAwait(false);

            Assert.Null(dataOwners);
        }

        [Theory(DisplayName = "When CreateAsync is called with linked agents, then update the linked agents' capabilities.")]
        [InlineValidData(WriteAction.Create, "Delete")]
        [InlineValidData(WriteAction.Create, "Export")]
        [InlineValidData(WriteAction.Create, "AccountClose")]
        [InlineValidData(WriteAction.Create, "Delete,Export")]
        [InlineValidData(WriteAction.Create, "AccountClose,Delete,Export")]
        public async Task When_CreateAsyncIsCalledWithLinkedAgents_Then_UpdateLinkedAgentCapabilities(
            string capabilities,
            DeleteAgent existingAgent,
            AssetGroup assetGroup,
            [Frozen] Mock<IDeleteAgentReader> agentReader,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            AssetGroupWriter writer)
        {
            existingAgent.Id = Guid.NewGuid();
            existingAgent.Capabilities = null; // Start out empty.
            existingAgent.TrackingDetails = new TrackingDetails();

            if (capabilities.Contains("Delete"))
            {
                assetGroup.DeleteAgentId = existingAgent.Id;
            }
            else
            {
                assetGroup.DeleteAgentId = null;
            }

            if (capabilities.Contains("Export"))
            {
                assetGroup.ExportAgentId = existingAgent.Id;
            }
            else
            {
                assetGroup.ExportAgentId = null;
            }

            if (capabilities.Contains("AccountClose"))
            {
                assetGroup.AccountCloseAgentId = existingAgent.Id;
            }
            else
            {
                assetGroup.AccountCloseAgentId = null;
            }

            agentReader
                .Setup(m => m.ReadByIdAsync(existingAgent.Id, ExpandOptions.WriteProperties))
                .ReturnsAsync(existingAgent);

            await writer.CreateAsync(assetGroup).ConfigureAwait(false);

            storageWriter.Verify(m => m.UpsertAssetGroupWithSideEffectsAsync(It.IsAny<AssetGroup>(), WriteAction.Create, new[] { existingAgent }), Times.Once);
            Assert.Equal(capabilities, string.Join(",", existingAgent.Capabilities.Select(x => x.Value).OrderBy(x => x)));
        }

        [Theory(DisplayName = "When UpdateAsync is called with linked agents, then update the linked agents' capabilities.")]
        [InlineValidData(WriteAction.Update, "Delete")]
        [InlineValidData(WriteAction.Update, "Export")]
        [InlineValidData(WriteAction.Update, "AccountClose")]
        [InlineValidData(WriteAction.Update, "Delete,Export")]
        [InlineValidData(WriteAction.Update, "AccountClose,Delete,Export")]
        public async Task When_UpdateAsyncIsCalledWithLinkedAgents_Then_UpdateLinkedAgentCapabilities(
            string capabilities,
            DeleteAgent existingAgent,
            AssetGroup assetGroup,
            [Frozen] AssetGroup existingAssetGroup,
            [Frozen] Mock<IDeleteAgentReader> agentReader,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            TrackingDetails trackingDetails,
            AssetGroupWriter writer)
        {
            var expectedAgentIds = new List<Guid>();
            existingAgent.Id = Guid.NewGuid();
            existingAgent.Capabilities = null; // Start out empty.
            existingAgent.TrackingDetails = trackingDetails;
            existingAssetGroup.TrackingDetails = trackingDetails;

            if (capabilities.Contains("Delete"))
            {
                assetGroup.DeleteAgentId = existingAgent.Id;
                expectedAgentIds.Add(existingAgent.Id);
                expectedAgentIds.Add(existingAssetGroup.DeleteAgentId.Value);
            }

            if (capabilities.Contains("Export"))
            {
                assetGroup.ExportAgentId = existingAgent.Id;
                expectedAgentIds.Add(existingAgent.Id);
                expectedAgentIds.Add(existingAssetGroup.ExportAgentId.Value);
            }

            if (capabilities.Contains("AccountClose"))
            {
                assetGroup.AccountCloseAgentId = existingAgent.Id;
                expectedAgentIds.Add(existingAgent.Id);
                expectedAgentIds.Add(existingAssetGroup.AccountCloseAgentId.Value);
            }

            agentReader
                .Setup(m => m.ReadByIdsAsync(Is.Value<IEnumerable<Guid>>(x => expectedAgentIds.SequenceEqual(x)), ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { existingAgent });

            await writer.UpdateAsync(assetGroup).ConfigureAwait(false);

            storageWriter.Verify(m => m.UpsertAssetGroupWithSideEffectsAsync(It.IsAny<AssetGroup>(), WriteAction.Update, new[] { existingAgent }), Times.Once);
            Assert.Equal(capabilities, string.Join(",", existingAgent.Capabilities.Select(x => x.Value).OrderBy(x => x)));
        }

        [Theory(DisplayName = "When UpdateAsync is called with removed agent links, then update the linked agents' capabilities.")]
        [InlineValidData(WriteAction.Update, "Delete")]
        [InlineValidData(WriteAction.Update, "Export")]
        [InlineValidData(WriteAction.Update, "AccountClose")]
        [InlineValidData(WriteAction.Update, "Delete,Export")]
        [InlineValidData(WriteAction.Update, "AccountClose,Delete,Export")]
        public async Task When_UpdateAsyncIsCalledWithRemovedAgents_Then_UpdateLinkedAgentCapabilities(
            string capabilities,
            DeleteAgent existingAgent,
            AssetGroup assetGroup,
            AssetGroup existingAssetGroup,
            [Frozen] FilterResult<AssetGroup> queryResult,
            [Frozen] Mock<IDeleteAgentReader> agentReader,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            TrackingDetails trackingDetails,
            AssetGroupWriter writer,
            IFixture fixture)
        {
            assetGroup.Id = Guid.NewGuid();
            existingAgent.Id = Guid.NewGuid();
            existingAgent.Capabilities = null; // Start out empty.
            existingAgent.TrackingDetails = trackingDetails;
            existingAssetGroup.TrackingDetails = trackingDetails;

            // Ensure that one of the asset groups has the agent id set for the expected capability.
            queryResult.Values = fixture.Create<IEnumerable<AssetGroup>>();
            queryResult.Values.Last().Id = assetGroup.Id;

            // Change an id so that it triggers the flow.
            if (capabilities.Contains("Delete"))
            {
                existingAssetGroup.DeleteAgentId = existingAgent.Id;
                queryResult.Values.First().DeleteAgentId = existingAgent.Id;
            }

            if (capabilities.Contains("Export"))
            {
                existingAssetGroup.ExportAgentId = existingAgent.Id;
                queryResult.Values.First().ExportAgentId = existingAgent.Id;
            }

            if (capabilities.Contains("AccountClose"))
            {
                existingAssetGroup.AccountCloseAgentId = existingAgent.Id;
                queryResult.Values.First().AccountCloseAgentId = existingAgent.Id;
            }

            // Ensure that an agent is returned so that it triggers the asset group query.
            agentReader
                .Setup(m => m.ReadByIdsAsync(It.IsAny<IEnumerable<Guid>>(), ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { existingAgent });

            fixture.Inject(existingAssetGroup); // Freeze the asset group.
                     
            await writer.UpdateAsync(assetGroup).ConfigureAwait(false);

            storageWriter.Verify(m => m.UpsertAssetGroupWithSideEffectsAsync(It.IsAny<AssetGroup>(), WriteAction.Update, new[] { existingAgent }), Times.Once);
            Assert.Equal(capabilities, string.Join(",", existingAgent.Capabilities.Select(x => x.Value).OrderBy(x => x)));
        }

        [Theory(DisplayName = "When non-admin user updates with new variants and exiting entity not having variants, then fail."), ValidData(WriteAction.Update)]
        public async Task When_NonAdminUserUpdateWithNewVariantsAndExistingEnityNotHavingVariants_Then_Fail(
            AssetGroup assetGroup,
            IEnumerable<AssetGroupVariant> variants,
            AssetGroupWriter writer)
        {
            assetGroup.Variants = variants;

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.UpdateAsync(assetGroup)).ConfigureAwait(false);

            Assert.Equal(ConflictType.InvalidValue_Immutable, exn.ConflictType);
            Assert.Equal("variants", exn.Target);
        }

        [Theory(DisplayName = "When non-admin user updates with new variants, then fail."), ValidData(WriteAction.Update)]
        public async Task When_NonAdminUserAddNewVariants_Then_Fail(
            AssetGroup assetGroup,
            [Frozen] AssetGroup existingAssetGroup,
            IEnumerable<AssetGroupVariant> variants,
            AssetGroupVariant variant,
            AssetGroupWriter writer)
        {
            assetGroup.Variants = variants.Concat(new[] { variant });
            existingAssetGroup.Variants = variants;

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.UpdateAsync(assetGroup)).ConfigureAwait(false);

            Assert.Equal(ConflictType.InvalidValue_Immutable, exn.ConflictType);
            Assert.Equal($"variants[{variant.VariantId}]", exn.Target);
        }

        [Theory(DisplayName = "When non-admin user updates with removing variants, then fail."), ValidData(WriteAction.Update)]
        public async Task When_NonAdminUserRemoveExistingVariants_Then_Fail(
            AssetGroup assetGroup,
            [Frozen] AssetGroup existingAssetGroup,
            IEnumerable<AssetGroupVariant> variants,
            AssetGroupWriter writer)
        {
            var variantToRemove = variants.First().VariantId;

            assetGroup.Variants = variants.Where(x => x.VariantId != variantToRemove);
            existingAssetGroup.Variants = variants;

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.UpdateAsync(assetGroup)).ConfigureAwait(false);

            Assert.Equal(ConflictType.InvalidValue_Immutable, exn.ConflictType);
            Assert.Equal($"variants[{variantToRemove}]", exn.Target);
        }

        [Theory(DisplayName = "When non-admin user updates with variant having tfs tracking uris and existing variant not having tracking uris, then fail."), ValidData(WriteAction.Update)]
        public async Task When_NonAdminUserUpdateWithVariantHavingTrackingUrisAndExistingVariantNot_Then_Fail(
            AssetGroup assetGroup,
            [Frozen] AssetGroup existingAssetGroup,
            AssetGroupVariant existingVariant,
            AssetGroupVariant incomingVariant,
            AssetGroupWriter writer)
        {
            incomingVariant.VariantId = existingVariant.VariantId;
            incomingVariant.TfsTrackingUris = existingVariant.TfsTrackingUris;
            existingVariant.TfsTrackingUris = null;

            assetGroup.Variants = new[] { incomingVariant };
            existingAssetGroup.Variants = new[] { existingVariant };

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.UpdateAsync(assetGroup)).ConfigureAwait(false);

            Assert.Equal(ConflictType.InvalidValue_Immutable, exn.ConflictType);
            Assert.Equal($"variants[{incomingVariant.VariantId}].tfsTrackingUris", exn.Target);
        }

        [Theory(DisplayName = "When non-admin user updates with variant having different tfs tracking uris, then fail."), ValidData(WriteAction.Update)]
        public async Task When_NonAdminUserUpdateWithVariantHavingDifferentTrackingUris_Then_Fail(
            AssetGroup assetGroup,
            [Frozen] AssetGroup existingAssetGroup,
            AssetGroupVariant existingVariant,
            AssetGroupVariant incomingVariant,
            AssetGroupWriter writer)
        {
            incomingVariant.VariantId = existingVariant.VariantId;

            assetGroup.Variants = new[] { incomingVariant };
            existingAssetGroup.Variants = new[] { existingVariant };

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.UpdateAsync(assetGroup)).ConfigureAwait(false);

            Assert.Equal(ConflictType.InvalidValue_Immutable, exn.ConflictType);
            Assert.Equal($"variants[{incomingVariant.VariantId}].tfsTrackingUris", exn.Target);
        }

        [Theory(DisplayName = "When non-admin user updates with variant property changed, then fail."), ValidData(WriteAction.Update)]
        public async Task When_NonAdminUserUpdateWithVariantPropertyChanged_Then_Fail(
            AssetGroup assetGroup,
            [Frozen] AssetGroup existingAssetGroup,
            AssetGroupVariant existingVariant,
            AssetGroupVariant incomingVariant,
            IFixture fixture)
        {
            fixture.Inject<IValidator>(new Validator());
            var writer = fixture.Create<AssetGroupWriter>();

            incomingVariant.VariantId = existingVariant.VariantId;
            incomingVariant.VariantName = existingVariant.VariantName;
            incomingVariant.VariantState = existingVariant.VariantState;
            incomingVariant.VariantExpiryDate = existingVariant.VariantExpiryDate;
            incomingVariant.TfsTrackingUris = existingVariant.TfsTrackingUris;
            incomingVariant.EgrcId = existingVariant.EgrcId;
            incomingVariant.EgrcName = existingVariant.EgrcName;
            incomingVariant.DisableSignalFiltering = !existingVariant.DisableSignalFiltering;

            assetGroup.Variants = new[] { incomingVariant };
            existingAssetGroup.Variants = new[] { existingVariant };

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.UpdateAsync(assetGroup)).ConfigureAwait(false);

            Assert.Equal(ConflictType.InvalidValue_Immutable, exn.ConflictType);
            Assert.Equal($"variants[{incomingVariant.VariantId}].disableSignalFiltering", exn.Target);
        }

        [Theory(DisplayName = "When non-admin user updates without chaning the variants, then succeed."), ValidData(WriteAction.Update)]
        public async Task When_NonAdminUserUpdatesWithoutChangingTheVariants_Then_Succeed(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            AssetGroup assetGroup,
            [Frozen] AssetGroup existingAssetGroup,
            AssetGroupWriter writer,
            IEnumerable<AssetGroupVariant> variants,
            TrackingDetails trackingDetails)
        {
            assetGroup.Variants = variants;
            existingAssetGroup.Variants = variants;
            existingAssetGroup.TrackingDetails = trackingDetails;

            await writer.UpdateAsync(assetGroup).ConfigureAwait(false);

            storageWriter.Verify(m => m.UpdateAssetGroupAsync(existingAssetGroup), Times.Once);
        }

        [Theory(DisplayName = "When non-admin user updates, then check variants immutable."), ValidData(WriteAction.Update)]
        public async Task When_NonAdminUserUpdates_Then_CheckVariantsImmutable(
            [Frozen] Mock<IValidator> validator,
            AssetGroup assetGroup,
            [Frozen] AssetGroup existingAssetGroup,
            AssetGroupWriter writer,
            IEnumerable<AssetGroupVariant> variants,
            TrackingDetails trackingDetails)
        {
            assetGroup.Variants = variants;
            existingAssetGroup.Variants = variants;
            existingAssetGroup.TrackingDetails = trackingDetails;

            await writer.UpdateAsync(assetGroup).ConfigureAwait(false);

            foreach (var variant in variants)
            {
                validator.Verify(m => m.Immutable(variant, variant, It.IsAny<CreateException>(), nameof(AssetGroupVariant.TfsTrackingUris)), Times.Exactly(2));
            }
        }

        [Theory(DisplayName = "When UpdateAsync is called with both delete agent id and delete sharing request id set, then fail."), ValidData(WriteAction.Update)]
        public async Task When_UpdateWithDeleteAgentAndRequestSet_Then_Fail(
            AssetGroup assetGroup,
            AssetGroupWriter writer)
        {
            assetGroup.DeleteAgentId = Guid.NewGuid();
            assetGroup.DeleteSharingRequestId = Guid.NewGuid();

            var exn = await Assert.ThrowsAsync<MutuallyExclusiveException>(() => writer.UpdateAsync(assetGroup)).ConfigureAwait(false);

            Assert.Equal("deleteSharingRequestId", exn.Source);
            Assert.Equal("deleteAgentId", exn.ParamName);
        }

        [Theory(DisplayName = "When UpdateAsync is called with a different delete sharing request id, then fail.")]
        [InlineValidData(WriteAction.Update, "{0F77CCCF-0DCF-41C7-8A12-2A5599E38AFC}", "")]
        [InlineValidData(WriteAction.Update, "", "{0F77CCCF-0DCF-41C7-8A12-2A5599E38AFC}")]
        [InlineValidData(WriteAction.Update, "{FF1133CE-F070-4A39-8609-FF806319FAAA}", "{0F77CCCF-0DCF-41C7-8A12-2A5599E38AFC}")]
        public async Task When_UpdateWithChangedDeleteSharingRequestId_Then_Fail(
            string newValue,
            string existingValue,
            AssetGroup assetGroup,
            [Frozen] AssetGroup existingAssetGroup,
            AssetGroupWriter writer)
        {
            assetGroup.DeleteAgentId = null;
            assetGroup.DeleteSharingRequestId = newValue == string.Empty ? (Guid?)null : Guid.Parse(newValue);
            existingAssetGroup.DeleteSharingRequestId = existingValue == string.Empty ? (Guid?)null : Guid.Parse(existingValue);

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.UpdateAsync(assetGroup)).ConfigureAwait(false);

            Assert.Equal(ConflictType.InvalidValue_Immutable, exn.ConflictType);
            Assert.Equal("deleteSharingRequestId", exn.Target);
        }

        [Theory(DisplayName = "When UpdateAsync is called with both export agent id and export sharing request id set, then fail."), ValidData(WriteAction.Update)]
        public async Task When_UpdateWithExportAgentAndRequestSet_Then_Fail(
            AssetGroup assetGroup,
            AssetGroupWriter writer)
        {
            assetGroup.ExportAgentId = Guid.NewGuid();
            assetGroup.ExportSharingRequestId = Guid.NewGuid();

            var exn = await Assert.ThrowsAsync<MutuallyExclusiveException>(() => writer.UpdateAsync(assetGroup)).ConfigureAwait(false);

            Assert.Equal("exportSharingRequestId", exn.Source);
            Assert.Equal("exportAgentId", exn.ParamName);
        }

        [Theory(DisplayName = "When UpdateAsync is called with a different export sharing request id, then fail.")]
        [InlineValidData(WriteAction.Update, "{0F77CCCF-0DCF-41C7-8A12-2A5599E38AFC}", "")]
        [InlineValidData(WriteAction.Update, "", "{0F77CCCF-0DCF-41C7-8A12-2A5599E38AFC}")]
        [InlineValidData(WriteAction.Update, "{FF1133CE-F070-4A39-8609-FF806319FAAA}", "{0F77CCCF-0DCF-41C7-8A12-2A5599E38AFC}")]
        public async Task When_UpdateWithChangedExportSharingRequestId_Then_Fail(
            string newValue,
            string existingValue,
            AssetGroup assetGroup,
            [Frozen] AssetGroup existingAssetGroup,
            AssetGroupWriter writer)
        {
            assetGroup.ExportAgentId = null;
            assetGroup.ExportSharingRequestId = newValue == string.Empty ? (Guid?)null : Guid.Parse(newValue);
            existingAssetGroup.ExportSharingRequestId = existingValue == string.Empty ? (Guid?)null : Guid.Parse(existingValue);

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.UpdateAsync(assetGroup)).ConfigureAwait(false);

            Assert.Equal(ConflictType.InvalidValue_Immutable, exn.ConflictType);
            Assert.Equal("exportSharingRequestId", exn.Target);
        }

        [Theory(DisplayName = "When RemoveVariants is called with correct asset group and caller with write permission, then succeed."), ValidData(WriteAction.Update, needVariants: true)]
        public async Task When_RemoveVariantsWithCorrectAssetGroupIdAndWritePermission_Then_Succeed(
            AssetGroup existingAssetGroup,
            [Frozen] Mock<IAssetGroupReader> entityReader,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            AssetGroupWriter writer)
        {
            entityReader.Setup(m => m.ReadByIdAsync(existingAssetGroup.Id, ExpandOptions.WriteProperties)).ReturnsAsync(existingAssetGroup);
            var variantIds = existingAssetGroup.Variants.Select(v => v.VariantId);

            await writer.RemoveVariantsAsync(existingAssetGroup.Id, variantIds, existingAssetGroup.ETag).ConfigureAwait(false);

            storageWriter.Verify(m => m.UpdateAssetGroupAsync(existingAssetGroup), Times.Once);
        }
        
        [Theory(DisplayName = "When RemoveVariants is called from a caller without write permission, then fail."), ValidData(WriteAction.Update, needVariants: true)]
        public async Task When_RemoveVariantsWithoutWritePermission_Then_Fail(
            [Frozen] AssetGroup existingAssetGroup,
            [Frozen] DataOwner dataOwner,
            IEnumerable<Guid> variantIds,
            AssetGroupWriter writer)
        {
            dataOwner.WriteSecurityGroups = new List<Guid>();
            existingAssetGroup.Owner = dataOwner;

            await Assert.ThrowsAsync<SecurityGroupMissingWritePermissionException>(
                () => writer.RemoveVariantsAsync(existingAssetGroup.Id, variantIds, existingAssetGroup.ETag)).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When RemoveVariants is called it removes the correct variant ids from the asset group."), ValidData(WriteAction.Update, needVariants: true)]
        public async Task When_RemoveVariants_Then_VariantsAreRemoved(
            [Frozen] AssetGroup existingAssetGroup,
            AssetGroupWriter writer)
        {
            var variantIds = existingAssetGroup.Variants.Select(v => v.VariantId);

            var assetGroup = await writer.RemoveVariantsAsync(existingAssetGroup.Id, variantIds, existingAssetGroup.ETag).ConfigureAwait(false);

            Assert.Empty(existingAssetGroup.Variants);
        }

        [Theory(DisplayName = "When RemoveVariants is called tracking details are updated."), ValidData(WriteAction.Update, needVariants: true)]
        public async Task When_RemoveVariants_Then_TrackingDetailsUpdated(
            [Frozen] AssetGroup existingAssetGroup,
            AssetGroupWriter writer)
        {
            var trackingDetailsInitialVersion = existingAssetGroup.TrackingDetails.Version;
            var variantIds = existingAssetGroup.Variants.Select(v => v.VariantId);

            var assetGroup = await writer.RemoveVariantsAsync(existingAssetGroup.Id, variantIds, existingAssetGroup.ETag).ConfigureAwait(false);

            Assert.Equal(trackingDetailsInitialVersion + 1, existingAssetGroup.TrackingDetails.Version);
        }

        [Theory(DisplayName = "When RemoveVariants is called and does not find an asset group, then fail."), ValidData(WriteAction.Update, needVariants: true)]
        public async Task When_RemoveVariantsWithNotFoundAssetGroup_Then_Fail(
            AssetGroup assetGroup,
            List<Guid> variantIds,
            [Frozen] Mock<IAssetGroupReader> entityReader,
            AssetGroupWriter writer)
        {
            entityReader.Setup(m => m.ReadByIdAsync(assetGroup.Id, ExpandOptions.WriteProperties)).ReturnsAsync(null as AssetGroup);

            var exn = await Assert.ThrowsAsync<EntityNotFoundException>(
                () => writer.RemoveVariantsAsync(assetGroup.Id, variantIds, assetGroup.ETag)).ConfigureAwait(false);

            Assert.Equal(assetGroup.Id, exn.Id);
            Assert.Equal("AssetGroup", exn.EntityType);
        }

        [Theory(DisplayName = "When RemoveVariants is called with an empty variant list, then fail."), ValidData(WriteAction.Update, needVariants: true)]
        public async Task When_RemoveVariantsWithEmptyVariantsList_Then_Fail(
            [Frozen] AssetGroup existingAssetGroup,
            AssetGroupWriter writer)
        {
            var variantIds = new List<Guid>();

            var exn = await Assert.ThrowsAsync<MissingPropertyException>(
                () => writer.RemoveVariantsAsync(existingAssetGroup.Id, variantIds, existingAssetGroup.ETag)).ConfigureAwait(false);

            Assert.Equal("variantIds", exn.ParamName);
        }

        [Theory(DisplayName = "When RemoveVariants is called with variant ids not in the asset group or the asset group has no variants, then fail."), ValidData(WriteAction.Update, needVariants: true)]
        public async Task When_RemoveVariantsWithVariantsNotInAssetGroup_Then_Fail(
            [Frozen]AssetGroup existingAssetGroup,
            AssetGroupWriter writer)
        {
            var variantIds = new List<Guid>() { new Guid() };
            var exn = await Assert.ThrowsAsync<ConflictException>(
                () => writer.RemoveVariantsAsync(existingAssetGroup.Id, variantIds, existingAssetGroup.ETag)).ConfigureAwait(false);
            Assert.Equal(ConflictType.DoesNotExist, exn.ConflictType);
            Assert.Equal(string.Join(";", variantIds), exn.Value);

            existingAssetGroup.Variants = new List<AssetGroupVariant>();
            exn = await Assert.ThrowsAsync<ConflictException>(
                () => writer.RemoveVariantsAsync(existingAssetGroup.Id, variantIds, existingAssetGroup.ETag)).ConfigureAwait(false);
            Assert.Equal(ConflictType.NullValue, exn.ConflictType);
        }

        [Theory(DisplayName = "When RemoveVariants is called with a null list of variant ids, then fail."), ValidData(WriteAction.Update, needVariants: true)]
        public async Task When_RemoveVariantsWithNullVarianIds_Then_Fail(
            [Frozen]AssetGroup existingAssetGroup,
            AssetGroupWriter writer)
        {
            var exn = await Assert.ThrowsAsync<MissingPropertyException>(
                () => writer.RemoveVariantsAsync(existingAssetGroup.Id, null, existingAssetGroup.ETag)).ConfigureAwait(false);

            Assert.Equal("variantIds", exn.ParamName);
        }

        [Theory(DisplayName = "When RemoveVariants is called with an old etag, then fail."), ValidData(WriteAction.Update, needVariants: true)]
        public async Task When_RemoveVariantsWithOldETag_Then_Fail(
            [Frozen]AssetGroup existingAssetGroup,
            IEnumerable<Guid> variantIds,
            AssetGroupWriter writer)
        {
            var exn = await Assert.ThrowsAsync<ETagMismatchException>(
                () => writer.RemoveVariantsAsync(existingAssetGroup.Id, variantIds, "Invalid ETag")).ConfigureAwait(false);

            Assert.Equal("Invalid ETag", exn.Value);
        }

        public class ValidDataAttribute : AutoMoqDataAttribute
        {
            public ValidDataAttribute(
                WriteAction action = WriteAction.Create,
                bool treatAsAdmin = false, 
                bool needVariants = false) : base(true)
            {
                var writeSecurityGroups = this.Fixture.Create<IEnumerable<Guid>>();

                this.Fixture.Customize<DataManagement.Models.V2.Entity>(obj =>
                    obj
                    .Without(x => x.Id)
                    .Without(x => x.ETag)
                    .Without(x => x.TrackingDetails));

                var ownerId = this.Fixture.Create<Guid>();

                this.Fixture.Customize<DataOwner>(obj =>
                    obj
                    .With(x => x.WriteSecurityGroups, writeSecurityGroups)
                    .Without(x => x.ServiceTree));

                this.Fixture.Customize<Inventory>(obj =>
                    obj
                    .With(x => x.OwnerId, ownerId));

                var connectionDetail = new ConnectionDetail();
                connectionDetail.Protocol = Policies.Current.Protocols.Ids.CommandFeedV1;
                connectionDetail.ReleaseState = ReleaseState.Prod;

                this.Fixture.Inject(DataAgentWriterTest.CreateConnectionDetails(connectionDetail));

                if (action == WriteAction.Create)
                {
                    this.Fixture.Customize<AssetGroup>(obj =>
                    obj
                    .With(x => x.OwnerId, ownerId)
                    .Without(x => x.QualifierParts)
                    .Do(x => x.QualifierParts = this.Fixture.Create<AssetQualifier>().Properties)
                    .Without(x => x.DeleteSharingRequestId)
                    .Without(x => x.ExportSharingRequestId)
                    .Without(x => x.DataAssets)
                    .Without(x => x.DeleteAgent)
                    .Without(x => x.ExportAgent)
                    .Without(x => x.AccountCloseAgent)
                    .Without(x => x.Inventory)
                    .Without(x => x.Variants)
                    .Without(x => x.Owner)
                    .Without(x => x.ComplianceState)
                    .Without(x => x.HasPendingTransferRequest));
                }
                else if (needVariants)
                {
                    var id = this.Fixture.Create<Guid>();
                    var deleteAgentId = this.Fixture.Create<Guid>();
                    var exportAgentId = this.Fixture.Create<Guid>();
                    var accountCloseAgentId = this.Fixture.Create<Guid>();
                    var inventoryId = this.Fixture.Create<Guid>();
                    var trackingDetails = this.Fixture.Create<TrackingDetails>();

                    this.Fixture.Customize<AssetGroup>(obj =>
                    obj
                    .With(x => x.QualifierParts, this.Fixture.Create<AssetQualifier>().Properties)
                    .With(x => x.Id, id)
                    .With(x => x.ETag, "ETag")
                    .With(x => x.OwnerId, ownerId)
                    .With(x => x.DeleteAgentId, deleteAgentId)
                    .With(x => x.ExportAgentId, exportAgentId)
                    .With(x => x.AccountCloseAgentId, accountCloseAgentId)
                    .With(x => x.InventoryId, inventoryId)
                    .With(x => x.TrackingDetails, trackingDetails)
                    .Without(x => x.DeleteSharingRequestId)
                    .Without(x => x.ExportSharingRequestId)
                    .Without(x => x.DataAssets)
                    .Without(x => x.DeleteAgent)
                    .Without(x => x.ExportAgent)
                    .Without(x => x.AccountCloseAgent)
                    .Without(x => x.Inventory)
                    .Without(x => x.Owner)
                    .Without(x => x.ComplianceState)
                    .Without(x => x.HasPendingTransferRequest));
                }
                else
                {
                    var id = this.Fixture.Create<Guid>();
                    var deleteAgentId = this.Fixture.Create<Guid>();
                    var exportAgentId = this.Fixture.Create<Guid>();
                    var accountCloseAgentId = this.Fixture.Create<Guid>();
                    var inventoryId = this.Fixture.Create<Guid>();

                    this.Fixture.Customize<AssetGroup>(obj =>
                    obj
                    .With(x => x.QualifierParts, this.Fixture.Create<AssetQualifier>().Properties)
                    .With(x => x.Id, id)
                    .With(x => x.ETag, "ETag")
                    .With(x => x.OwnerId, ownerId)
                    .With(x => x.DeleteAgentId, deleteAgentId)
                    .With(x => x.ExportAgentId, exportAgentId)
                    .With(x => x.AccountCloseAgentId, accountCloseAgentId)
                    .With(x => x.InventoryId, inventoryId)
                    .Without(x => x.DeleteSharingRequestId)
                    .Without(x => x.ExportSharingRequestId)
                    .Without(x => x.DataAssets)
                    .Without(x => x.DeleteAgent)
                    .Without(x => x.ExportAgent)
                    .Without(x => x.AccountCloseAgent)
                    .Without(x => x.Inventory)
                    .Without(x => x.Variants)
                    .Without(x => x.Owner)
                    .Without(x => x.HasPendingTransferRequest));
                }

                this.Fixture.Customize<FilterResult<AssetGroup>>(obj =>
                    obj
                    .With(x => x.Values, Enumerable.Empty<AssetGroup>()));

                this.Fixture.RegisterAuthorizationClasses(writeSecurityGroups, treatAsAdmin: treatAsAdmin);
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