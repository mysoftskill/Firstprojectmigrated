namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Writer.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Reader;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.UnitTest;
    using Microsoft.PrivacyServices.DataManagement.Models.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;

    public class SharingRequestWriterTest
    {        
        [Theory(DisplayName = "When CreateAsync is called, then fail."), ValidData(WriteAction.Create)]
        public async Task When_CreateAsync_Then_Fail(
            SharingRequest sharingRequest,
            SharingRequestWriter writer)
        {
            sharingRequest.Id = Guid.Empty;
            sharingRequest.ETag = null;
            sharingRequest.TrackingDetails = null;

            await Assert.ThrowsAsync<NotImplementedException>(() => writer.CreateAsync(sharingRequest)).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When UpdateAsync is called, then fail."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsync_Then_Fail(
            SharingRequest sharingRequest,
            SharingRequestWriter writer)
        {
            sharingRequest.TrackingDetails = null;

            await Assert.ThrowsAsync<NotImplementedException>(() => writer.UpdateAsync(sharingRequest)).ConfigureAwait(false);
        }

        #region ApproveAsync
        [Theory(DisplayName = "When ApproveAsync is called and sharing request not found, then fail."), ValidData]
        public async Task When_ApproveAsyncWithMissingRequest_Then_Fail(
            SharingRequest sharingRequest,
            [Frozen] Mock<ISharingRequestReader> reader,
            SharingRequestWriter writer)
        {
            reader.Setup(m => m.ReadByIdAsync(sharingRequest.Id, ExpandOptions.WriteProperties)).ReturnsAsync(null as SharingRequest);

            var exn = await Assert.ThrowsAsync<EntityNotFoundException>(() => writer.ApproveAsync(sharingRequest.Id, sharingRequest.ETag)).ConfigureAwait(false);

            Assert.Equal(sharingRequest.Id, exn.Id);
            Assert.Equal("SharingRequest", exn.EntityType);
        }

        [Theory(DisplayName = "When ApproveAsync is called and sharing request etags do not match, then fail."), ValidData]
        public async Task When_ApproveAsyncWithETagMismatch_Then_Fail(
            SharingRequest sharingRequest,
            [Frozen] SharingRequest existingSharingRequest,
            SharingRequestWriter writer)
        {
            existingSharingRequest.ETag = "other";

            var exn = await Assert.ThrowsAsync<ETagMismatchException>(() => writer.ApproveAsync(sharingRequest.Id, sharingRequest.ETag)).ConfigureAwait(false);

            Assert.Equal(sharingRequest.ETag, exn.Value);
        }

        [Theory(DisplayName = "When ApproveAsync is called, then update tracking details for the sharing request."), ValidData]
        public async Task When_ApproveAsync_Then_UpdateRequestTrackingDetails(
            [Frozen] SharingRequest sharingRequest,            
            SharingRequestWriter writer)
        {
            sharingRequest.TrackingDetails.Version = 1;

            await writer.ApproveAsync(sharingRequest.Id, sharingRequest.ETag).ConfigureAwait(false);

            Assert.Equal(2, sharingRequest.TrackingDetails.Version);
        }

        [Theory(DisplayName = "When ApproveAsync is called, then soft delete the request."), ValidData]
        public async Task When_ApproveAsync_Then_SoftDeleteRequest(
            [Frozen] SharingRequest sharingRequest,
            SharingRequestWriter writer)
        {
            sharingRequest.IsDeleted = false;

            await writer.ApproveAsync(sharingRequest.Id, sharingRequest.ETag).ConfigureAwait(false);

            Assert.True(sharingRequest.IsDeleted);
        }

        [Theory(DisplayName = "When ApproveAsync is called with DELETE relationships, then update asset group agent links."), ValidData]
        public async Task When_ApproveAsyncDeleteRelationship_Then_UpdateAssetGroupAgentLinks(
            [Frozen] SharingRequest sharingRequest,
            SharingRelationship relationship,
            AssetGroup assetGroup,
            [Frozen] Mock<IAssetGroupReader> assetGroupReader,
            SharingRequestWriter writer)
        {
            sharingRequest.Relationships.Clear();
            sharingRequest.Relationships[assetGroup.Id] = relationship;
            relationship.AssetGroupId = assetGroup.Id;

            assetGroup.DeleteSharingRequestId = sharingRequest.Id;
            assetGroup.DeleteAgentId = Guid.NewGuid();

            Action<IEnumerable<Guid>> verify = x => x.SequenceEqual(new[] { assetGroup.Id });
            assetGroupReader.Setup(m => m.ReadByIdsAsync(Is.Value(verify), ExpandOptions.WriteProperties)).ReturnsAsync(new[] { assetGroup });

            await writer.ApproveAsync(sharingRequest.Id, sharingRequest.ETag).ConfigureAwait(false);

            Assert.Null(assetGroup.DeleteSharingRequestId);
            Assert.Equal(sharingRequest.DeleteAgentId, assetGroup.DeleteAgentId);
        }

        [Theory(DisplayName = "When ApproveAsync is called with EXPORT relationships, then update asset group agent links."), ValidData]
        public async Task When_ApproveAsyncExportRelationship_Then_UpdateAssetGroupAgentLinks(
            [Frozen] SharingRequest sharingRequest,
            SharingRelationship relationship,
            AssetGroup assetGroup,
            [Frozen] Mock<IAssetGroupReader> assetGroupReader,
            SharingRequestWriter writer)
        {
            sharingRequest.Relationships.Clear();
            sharingRequest.Relationships[assetGroup.Id] = relationship;
            relationship.AssetGroupId = assetGroup.Id;

            assetGroup.ExportSharingRequestId = sharingRequest.Id;
            assetGroup.ExportAgentId = Guid.NewGuid();

            Action<IEnumerable<Guid>> verify = x => x.SequenceEqual(new[] { assetGroup.Id });
            assetGroupReader.Setup(m => m.ReadByIdsAsync(Is.Value(verify), ExpandOptions.WriteProperties)).ReturnsAsync(new[] { assetGroup });

            await writer.ApproveAsync(sharingRequest.Id, sharingRequest.ETag).ConfigureAwait(false);

            Assert.Null(assetGroup.ExportSharingRequestId);
            Assert.Equal(sharingRequest.DeleteAgentId, assetGroup.ExportAgentId);
        }

        [Theory(DisplayName = "When ApproveAsync is called, then update asset group tracking details."), ValidData]
        public async Task When_ApproveAsync_Then_UpdateAssetGroupTrackingDetails(
            [Frozen] SharingRequest sharingRequest,            
            [Frozen] IEnumerable<AssetGroup> assetGroups,
            SharingRequestWriter writer,
            IFixture fixture)
        {
            sharingRequest.Relationships.Clear();

            foreach (var ag in assetGroups)
            {
                ag.TrackingDetails.Version = 1;
                sharingRequest.Relationships[ag.Id] = fixture.Create<SharingRelationship>();
                sharingRequest.Relationships[ag.Id].AssetGroupId = ag.Id;
            }
            
            await writer.ApproveAsync(sharingRequest.Id, sharingRequest.ETag).ConfigureAwait(false);
            
            foreach (var ag in assetGroups)
            {
                Assert.Equal(2, ag.TrackingDetails.Version);
            }
        }

        [Theory(DisplayName = "When ApproveAsync is called, then update capabilities on corresponding data agent.")]
        [InlineValidData(WriteAction.Update, "", "Delete", "Delete")]
        [InlineValidData(WriteAction.Update, "", "Export", "Export")]
        [InlineValidData(WriteAction.Update, "Delete", "Delete", "Delete")]
        [InlineValidData(WriteAction.Update, "Export", "Export", "Export")]
        [InlineValidData(WriteAction.Update, "Export", "Delete", "Delete,Export")]
        [InlineValidData(WriteAction.Update, "Delete", "Export", "Delete,Export")]
        [InlineValidData(WriteAction.Update, "Delete,Export", "Delete", "Delete,Export")]
        [InlineValidData(WriteAction.Update, "Delete,Export", "Export", "Delete,Export")]
        [InlineValidData(WriteAction.Update, "", "Delete,Export", "Delete,Export")]
        public async Task When_ApproveAsync_Then_UpdateAgentCapabilities(
            string initialAgentCapabilities,
            string assetCapabilityLinks,
            string finalAgentCapabilities,
            [Frozen] SharingRequest sharingRequest,
            SharingRelationship relationship,
            AssetGroup assetGroup,
            DeleteAgent deleteAgent,
            [Frozen] Mock<IAssetGroupReader> assetGroupReader,
            [Frozen] Mock<IDeleteAgentReader> deleteAgentReader,
            SharingRequestWriter writer)
        {
            sharingRequest.Relationships.Clear();
            sharingRequest.Relationships[assetGroup.Id] = relationship;
            relationship.AssetGroupId = assetGroup.Id;
                        
            assetGroup.DeleteSharingRequestId = assetCapabilityLinks.Contains("Delete") ? sharingRequest.Id : (Guid?)null;
            assetGroup.ExportSharingRequestId = assetCapabilityLinks.Contains("Export") ? sharingRequest.Id : (Guid?)null;

            deleteAgent.Id = sharingRequest.DeleteAgentId;
            deleteAgent.Capabilities = initialAgentCapabilities.Split(',').Where(x => !string.IsNullOrEmpty(x)).Select(Policies.Current.Capabilities.CreateId).ToList();
            deleteAgent.TrackingDetails.Version = 1;            

            assetGroupReader.Setup(m => m.ReadByIdsAsync(It.IsAny<IEnumerable<Guid>>(), ExpandOptions.WriteProperties)).ReturnsAsync(new[] { assetGroup });
            deleteAgentReader.Setup(m => m.ReadByIdAsync(deleteAgent.Id, ExpandOptions.WriteProperties)).ReturnsAsync(deleteAgent);

            await writer.ApproveAsync(sharingRequest.Id, sharingRequest.ETag).ConfigureAwait(false);

            Assert.Equal(finalAgentCapabilities, string.Join(",", deleteAgent.Capabilities.OrderBy(x => x)));
            Assert.Equal(2, deleteAgent.TrackingDetails.Version);
        }
        
        [Theory(DisplayName = "When ApproveAsync is called, then save all entities in batch."), ValidData]
        public async Task When_ApproveAsync_Then_StoreUpdatedEntities(            
            [Frozen] SharingRequest sharingRequest,
            [Frozen] IEnumerable<AssetGroup> assetGroups,
            [Frozen] DeleteAgent deleteAgent,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            SharingRequestWriter writer)
        {
            await writer.ApproveAsync(sharingRequest.Id, sharingRequest.ETag).ConfigureAwait(false);

            // The frozen values are the ones returned by the storage read calls.
            // All of those should be updated and then passed back to storage for saving.
            Action<IEnumerable<Entity>> verify = x =>
            {
                x.Where(y => y is AssetGroup).SortedSequenceAssert(assetGroups, y => y.Id, Assert.Equal);
                Assert.True(x.Contains(sharingRequest), "Missing sharing request");
                Assert.True(x.Contains(deleteAgent), "Missing delete agent");
            };

            storageWriter.Verify(m => m.UpdateEntitiesAsync(Is.Value(verify)), Times.Once);
        }
        #endregion

        #region DeleteAsync
        [Theory(DisplayName = "When DeleteAsync is called and sharing request not found, then fail."), ValidData]
        public async Task When_DeleteAsyncWithMissingRequest_Then_Fail(
            SharingRequest sharingRequest,
            [Frozen] Mock<ISharingRequestReader> reader,
            SharingRequestWriter writer)
        {
            reader.Setup(m => m.IsLinkedToAnyOtherEntities(sharingRequest.Id)).ReturnsAsync(false);
            reader.Setup(m => m.ReadByIdAsync(sharingRequest.Id, ExpandOptions.WriteProperties)).ReturnsAsync(null as SharingRequest);

            var exn = await Assert.ThrowsAsync<EntityNotFoundException>(() => writer.DeleteAsync(sharingRequest.Id, sharingRequest.ETag)).ConfigureAwait(false);

            Assert.Equal(sharingRequest.Id, exn.Id);
            Assert.Equal("SharingRequest", exn.EntityType);
        }

        [Theory(DisplayName = "When DeleteAsync is called and sharing request etags do not match, then fail."), ValidData]
        public async Task When_DeleteAsyncWithETagMismatch_Then_Fail(
            SharingRequest sharingRequest,
            [Frozen] SharingRequest existingSharingRequest,
            [Frozen] Mock<ISharingRequestReader> sharingRequestReader,
            SharingRequestWriter writer)
        {
            sharingRequestReader.Setup(m => m.IsLinkedToAnyOtherEntities(sharingRequest.Id)).ReturnsAsync(false);

            existingSharingRequest.ETag = "other";

            var exn = await Assert.ThrowsAsync<ETagMismatchException>(() => writer.DeleteAsync(sharingRequest.Id, sharingRequest.ETag)).ConfigureAwait(false);

            Assert.Equal(sharingRequest.ETag, exn.Value);
        }

        [Theory(DisplayName = "When DeleteAsync is called, then update tracking details for the sharing request."), ValidData]
        public async Task When_DeleteAsync_Then_UpdateRequestTrackingDetails(
            [Frozen] AuthenticatedPrincipal principal,
            [Frozen] SharingRequest sharingRequest,
            [Frozen] Mock<ISharingRequestReader> sharingRequestReader,
            SharingRequestWriter writer)
        {
            sharingRequestReader.Setup(m => m.IsLinkedToAnyOtherEntities(sharingRequest.Id)).ReturnsAsync(false);
            sharingRequestReader.Setup(m => m.HasPendingCommands(sharingRequest.Id)).ReturnsAsync(false);

            sharingRequest.TrackingDetails.Version = 1;

            await writer.DeleteAsync(sharingRequest.Id, sharingRequest.ETag).ConfigureAwait(false);

            Assert.Equal(principal.UserId, sharingRequest.TrackingDetails.UpdatedBy);
        }

        [Theory(DisplayName = "When DeleteAsync is called, then soft delete the request."), ValidData]
        public async Task When_DeleteAsync_Then_SoftDeleteRequest(
            [Frozen] SharingRequest sharingRequest,
            [Frozen] Mock<ISharingRequestReader> sharingRequestReader,
            SharingRequestWriter writer)
        {
            sharingRequestReader.Setup(m => m.IsLinkedToAnyOtherEntities(sharingRequest.Id)).ReturnsAsync(false);
            sharingRequestReader.Setup(m => m.HasPendingCommands(sharingRequest.Id)).ReturnsAsync(false);

            sharingRequest.IsDeleted = false;

            await writer.DeleteAsync(sharingRequest.Id, sharingRequest.ETag).ConfigureAwait(false);

            Assert.True(sharingRequest.IsDeleted);
        }

        [Theory(DisplayName = "When DeleteAsync is called with DELETE relationships, then update asset group agent links."), ValidData]
        public async Task When_DeleteAsyncDeleteRelationship_Then_UpdateAssetGroupAgentLinks(
            [Frozen] SharingRequest sharingRequest,
            SharingRelationship relationship,
            AssetGroup assetGroup,
            [Frozen] Mock<ISharingRequestReader> sharingRequestReader,
            [Frozen] Mock<IAssetGroupReader> assetGroupReader,
            SharingRequestWriter writer)
        {
            sharingRequestReader.Setup(m => m.IsLinkedToAnyOtherEntities(sharingRequest.Id)).ReturnsAsync(false);

            sharingRequest.Relationships.Clear();
            sharingRequest.Relationships[assetGroup.Id] = relationship;
            relationship.AssetGroupId = assetGroup.Id;

            assetGroup.DeleteSharingRequestId = sharingRequest.Id;

            Action<IEnumerable<Guid>> verify = x => x.SequenceEqual(new[] { assetGroup.Id });
            assetGroupReader.Setup(m => m.ReadByIdsAsync(Is.Value(verify), ExpandOptions.WriteProperties)).ReturnsAsync(new[] { assetGroup });

            await writer.DeleteAsync(sharingRequest.Id, sharingRequest.ETag).ConfigureAwait(false);

            Assert.Null(assetGroup.DeleteSharingRequestId);
            Assert.NotNull(assetGroup.DeleteAgentId);
            Assert.NotNull(assetGroup.ExportAgentId);
            Assert.NotNull(assetGroup.ExportSharingRequestId);
        }

        [Theory(DisplayName = "When DeleteAsync is called with EXPORT relationships, then update asset group agent links."), ValidData]
        public async Task When_DeleteAsyncExportRelationship_Then_UpdateAssetGroupAgentLinks(
            [Frozen] SharingRequest sharingRequest,
            SharingRelationship relationship,
            AssetGroup assetGroup,
            [Frozen] Mock<ISharingRequestReader> sharingRequestReader,
            [Frozen] Mock<IAssetGroupReader> assetGroupReader,
            SharingRequestWriter writer)
        {
            sharingRequestReader.Setup(m => m.IsLinkedToAnyOtherEntities(sharingRequest.Id)).ReturnsAsync(false);

            sharingRequest.Relationships.Clear();
            sharingRequest.Relationships[assetGroup.Id] = relationship;
            relationship.AssetGroupId = assetGroup.Id;

            assetGroup.ExportSharingRequestId = sharingRequest.Id;
            assetGroup.ExportAgentId = Guid.NewGuid();

            Action<IEnumerable<Guid>> verify = x => x.SequenceEqual(new[] { assetGroup.Id });
            assetGroupReader.Setup(m => m.ReadByIdsAsync(Is.Value(verify), ExpandOptions.WriteProperties)).ReturnsAsync(new[] { assetGroup });

            await writer.DeleteAsync(sharingRequest.Id, sharingRequest.ETag).ConfigureAwait(false);

            Assert.Null(assetGroup.ExportSharingRequestId);
            Assert.NotNull(assetGroup.DeleteAgentId);
            Assert.NotNull(assetGroup.ExportAgentId);
            Assert.NotNull(assetGroup.DeleteSharingRequestId);
        }

        [Theory(DisplayName = "When DeleteAsync is called, then update asset group tracking details."), ValidData]
        public async Task When_DeleteAsync_Then_UpdateAssetGroupTrackingDetails(
            [Frozen] SharingRequest sharingRequest,
            [Frozen] IEnumerable<AssetGroup> assetGroups,
            [Frozen] Mock<ISharingRequestReader> sharingRequestReader,
            SharingRequestWriter writer,
            IFixture fixture)
        {
            sharingRequestReader.Setup(m => m.IsLinkedToAnyOtherEntities(sharingRequest.Id)).ReturnsAsync(false);
            sharingRequestReader.Setup(m => m.HasPendingCommands(sharingRequest.Id)).ReturnsAsync(false);

            sharingRequest.Relationships.Clear();

            foreach (var ag in assetGroups)
            {
                ag.TrackingDetails.Version = 1;
                sharingRequest.Relationships[ag.Id] = fixture.Create<SharingRelationship>();
                sharingRequest.Relationships[ag.Id].AssetGroupId = ag.Id;
            }

            await writer.DeleteAsync(sharingRequest.Id, sharingRequest.ETag).ConfigureAwait(false);

            foreach (var ag in assetGroups)
            {
                Assert.Equal(2, ag.TrackingDetails.Version);
            }
        }
        
        [Theory(DisplayName = "When DeleteAsync is called, then save all entities in batch."), ValidData]
        public async Task When_DeleteAsync_Then_StoreUpdatedEntities(
            [Frozen] SharingRequest sharingRequest,
            [Frozen] IEnumerable<AssetGroup> assetGroups,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            [Frozen] Mock<ISharingRequestReader> sharingRequestReader,
            SharingRequestWriter writer)
        {
            sharingRequestReader.Setup(m => m.IsLinkedToAnyOtherEntities(sharingRequest.Id)).ReturnsAsync(false);
            sharingRequestReader.Setup(m => m.HasPendingCommands(sharingRequest.Id)).ReturnsAsync(false);

            await writer.DeleteAsync(sharingRequest.Id, sharingRequest.ETag).ConfigureAwait(false);

            // The frozen values are the ones returned by the storage read calls.
            // All of those should be updated and then passed back to storage for saving.
            Action<IEnumerable<Entity>> verify = x =>
            {
                x.Where(y => y is AssetGroup).SortedSequenceAssert(assetGroups, y => y.Id, Assert.Equal);
                Assert.True(x.Contains(sharingRequest), "Missing sharing request");
                Assert.False(x.Any(y => y is DeleteAgent), "No delete agents should be updated.");
            };

            storageWriter.Verify(m => m.UpdateEntitiesAsync(Is.Value(verify)), Times.Once);
        }
        #endregion

        public class ValidDataAttribute : AutoMoqDataAttribute
        {
            public ValidDataAttribute(WriteAction action = WriteAction.Update) : base(true)
            {
                var writeSecurityGroups = this.Fixture.Create<IEnumerable<Guid>>();
                
                var ownerId = this.Fixture.Create<Guid>();
                
                this.Fixture.Customize<DataOwner>(obj =>
                    obj
                    .With(x => x.WriteSecurityGroups, writeSecurityGroups)
                    .Without(x => x.ServiceTree));
                
                if (action == WriteAction.Create)
                {
                }
                else
                {
                    var id = this.Fixture.Create<Guid>();

                    this.Fixture.Customize<SharingRequest>(obj =>
                        obj
                        .With(x => x.Id, id)
                        .With(x => x.ETag, "ETag"));
                }

                this.Fixture.RegisterAuthorizationClasses(writeSecurityGroups);
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