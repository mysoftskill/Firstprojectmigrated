namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Writer.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Client;
    using Microsoft.PrivacyServices.DataManagement.Client.ServiceTree;
    using Microsoft.PrivacyServices.DataManagement.Common;
    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.ActiveDirectory;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Reader;
    using Microsoft.PrivacyServices.DataManagement.Models.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;

    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;

    public class EntityWriterTest
    {
        [Theory(DisplayName = "When CreateAsync is called, then the storage layer is called and returned."), DataOwnerWriterTest.ValidData]
        public async Task When_CreateAsync_Then_CallAndReturnStorageValue(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            DataOwner dataOwner,
            [Frozen] DataOwner storageDataOwner,
            DataOwnerWriter writer)
        {
            var result = await writer.CreateAsync(dataOwner).ConfigureAwait(false);

            Assert.Equal(storageDataOwner, result);

            storageWriter.Verify(m => m.CreateDataOwnerAsync(dataOwner), Times.Once);
        }
        
        [Theory(DisplayName = "When CreateAsync is called, then set the id for the entity."), DataOwnerWriterTest.ValidData]
        public async Task When_CreateAsync_Then_SetTheId(
            DataOwner dataOwner,
            DataOwnerWriter writer)
        {
            await writer.CreateAsync(dataOwner).ConfigureAwait(false);

            Assert.NotEqual(Guid.Empty, dataOwner.Id);
        }

        [Theory(DisplayName = "When CreateAsync is called, then set the tracking details for the entity."), DataOwnerWriterTest.ValidData]
        public async Task When_CreateAsync_Then_SetTheTrackingDetails(
            [Frozen] AuthenticatedPrincipal authPrincipal,
            [Frozen] Mock<IDateFactory> dateFactory,
            DateTimeOffset currentTime,
            DataOwner dataOwner,
            DataOwnerWriter writer)
        {
            dateFactory.Setup(m => m.GetCurrentTime()).Returns(currentTime);

            await writer.CreateAsync(dataOwner).ConfigureAwait(false);

            Assert.NotNull(dataOwner.TrackingDetails);
            Assert.Equal(authPrincipal.UserId, dataOwner.TrackingDetails.CreatedBy);
            Assert.Equal(currentTime, dataOwner.TrackingDetails.CreatedOn);
            Assert.Equal(authPrincipal.UserId, dataOwner.TrackingDetails.UpdatedBy);
            Assert.Equal(currentTime, dataOwner.TrackingDetails.UpdatedOn);
            Assert.Equal(1, dataOwner.TrackingDetails.Version);
        }

        [Theory(DisplayName = "When CreateAsync is called with id already set, then fail."), DataOwnerWriterTest.ValidData]
        public async Task When_CreateAsyncCalledWithId_Then_Fail(
            DataOwner dataOwner,
            DataOwnerWriter writer)
        {
            dataOwner.Id = Guid.NewGuid();

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("id", exn.ParamName);
            Assert.Equal(dataOwner.Id.ToString(), exn.Value);
        }

        [Theory(DisplayName = "When CreateAsync is called with etag already set, then fail."), DataOwnerWriterTest.ValidData]
        public async Task When_CreateAsyncCalledWithETag_Then_Fail(
            DataOwner dataOwner,
            DataOwnerWriter writer)
        {
            dataOwner.ETag = "value";

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("eTag", exn.ParamName);
            Assert.Equal(dataOwner.ETag, exn.Value);
        }

        [Theory(DisplayName = "When CreateAsync is called with tracking details already set, then fail."), DataOwnerWriterTest.ValidData]
        public async Task When_CreateAsyncCalledWithTrackingDetails_Then_Fail(
            DataOwner dataOwner,
            TrackingDetails trackingDetails,
            DataOwnerWriter writer)
        {
            dataOwner.TrackingDetails = trackingDetails;

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("trackingDetails", exn.ParamName);
            Assert.Null(exn.Value);
        }

        [Theory(DisplayName = "When CreateAsync is called with auth principal that has no username, then fail."), DataOwnerWriterTest.ValidData]
        public async Task When_CreateAsyncCalledWithMissingUserName_Then_Fail(
            [Frozen] AuthenticatedPrincipal authPrincipal,
            DataOwner dataOwner,
            DataOwnerWriter writer)
        {
            authPrincipal.UserId = null;

            var exn = await Assert.ThrowsAsync<MissingWritePermissionException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("Unknown", exn.UserName);
        }

        [Theory(DisplayName = "When CreateAsync is called with auth principal that has no security groups, then fail."), DataOwnerWriterTest.ValidData]
        public async Task When_CreateAsyncCalledWithMissingSecurityGroup_Then_Fail(
            [Frozen] AuthenticatedPrincipal authPrincipal,
            [Frozen] Mock<ICachedActiveDirectory> cachedActiveDirectory,
            DataOwner dataOwner,
            DataOwnerWriter writer)
        {
            cachedActiveDirectory.Setup(m => m.GetSecurityGroupIdsAsync(authPrincipal)).ReturnsAsync(Enumerable.Empty<Guid>());

            var exn = await Assert.ThrowsAsync<SecurityGroupMissingWritePermissionException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal(authPrincipal.UserAlias, exn.UserName);
            Assert.Equal("ServiceEditor", exn.Role);
        }

        [Theory(DisplayName = "When CreateAsync is called with auth principal that has null security groups, then fail."), DataOwnerWriterTest.ValidData]
        public async Task When_CreateAsyncCalledWithNullSecurityGroup_Then_Fail(
            [Frozen] AuthenticatedPrincipal authPrincipal,
            [Frozen] Mock<ICachedActiveDirectory> cachedActiveDirectory,
            DataOwner dataOwner,
            DataOwnerWriter writer)
        {
            cachedActiveDirectory.Setup(m => m.GetSecurityGroupIdsAsync(authPrincipal)).ReturnsAsync(null as IEnumerable<Guid>);

            var exn = await Assert.ThrowsAsync<SecurityGroupMissingWritePermissionException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal(authPrincipal.UserAlias, exn.UserName);
            Assert.Equal("ServiceEditor", exn.Role);
        }

        [Theory(DisplayName = "When CreateAsync is called with auth principal that is in the admin group, then allow."), DataOwnerWriterTest.ValidData]
        public async Task When_CreateAsyncCalledWithAdminSecurityGroup_Then_Pass(
            [Frozen] ICoreConfiguration coreConfiguration,
            [Frozen] AuthenticatedPrincipal authPrincipal,
            [Frozen] Mock<ICachedActiveDirectory> cachedActiveDirectory,
            IFixture fixture,
            DataOwner dataOwner,
            DataOwnerWriter writer)
        {
            dataOwner.WriteSecurityGroups = fixture.CreateMany<Guid>();

            var groups = coreConfiguration.ServiceAdminSecurityGroups;
            cachedActiveDirectory.Setup(m => m.GetSecurityGroupIdsAsync(authPrincipal)).ReturnsAsync(groups.Select(s => Guid.Parse(s)));
                        
            await writer.CreateAsync(dataOwner).ConfigureAwait(false);

            cachedActiveDirectory.Verify(m => m.GetSecurityGroupIdsAsync(authPrincipal));
        }

        [Theory(DisplayName = "When CreateAsync is called with auth principal that is not in entity security groups, then fail."), DataOwnerWriterTest.ValidData]
        public async Task When_CreateAsyncCalledWithUnknownSecurityGroup_Then_Fail(
            [Frozen] AuthenticatedPrincipal authPrincipal,
            IFixture fixture,
            DataOwner dataOwner,
            DataOwnerWriter writer)
        {
            dataOwner.WriteSecurityGroups = fixture.CreateMany<Guid>();

            var exn = await Assert.ThrowsAsync<SecurityGroupMissingWritePermissionException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal(authPrincipal.UserAlias, exn.UserName);
            Assert.Equal(string.Join(";", dataOwner.WriteSecurityGroups.Select(x => x.ToString())), exn.SecurityGroups);
        }

        [Theory(DisplayName = "When UpdateAsync is called, then the storage layer is called with the correct history item."), DataOwnerWriterTest.ValidData(WriteAction.Update)]
        public async Task When_UpdateAsync_Then_StorageLayerCalledWithCorrectHistoryItem(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            DataOwner dataOwner,
            [Frozen] DataOwner storageDataOwner,
            DataOwnerWriter writer,
            TrackingDetails trackingDetails)
        {
            storageDataOwner.TrackingDetails = trackingDetails;

            var result = await writer.UpdateAsync(dataOwner).ConfigureAwait(false);

            Assert.Equal(storageDataOwner, result);

            storageDataOwner.TrackingDetails = trackingDetails;

            storageWriter.Verify(m => m.UpdateDataOwnerAsync(storageDataOwner), Times.Once);
        }

        [Theory(DisplayName = "When UpdateAsync is called, then the entity reader is called."), DataOwnerWriterTest.ValidData(WriteAction.Update)]
        public async Task When_UpdateAsync_Then_EntityReaderIsCalled(
            [Frozen] Mock<IDataOwnerReader> entityReader,
            DataOwner dataOwner,
            [Frozen] DataOwner storageDataOwner,
            DataOwnerWriter writer,
            TrackingDetails trackingDetails)
        {
            storageDataOwner.TrackingDetails = trackingDetails;

            var result = await writer.UpdateAsync(dataOwner).ConfigureAwait(false);

            Assert.Equal(storageDataOwner, result);

            entityReader.Verify(m => m.ReadByIdAsync(dataOwner.Id, ExpandOptions.TrackingDetails | ExpandOptions.ServiceTree), Times.Once);
        }

        [Theory(DisplayName = "When UpdateAsync is called, then update the tracking details for the entity."), DataOwnerWriterTest.ValidData(WriteAction.Update)]
        public async Task When_UpdateAsync_Then_UpdateTheTrackingDetails(
            [Frozen] AuthenticatedPrincipal authPrincipal,
            [Frozen] Mock<IDateFactory> dateFactory,
            [Frozen] Mock<IDataOwnerReader> readerMock,
            DateTimeOffset currentTime,
            DataOwner dataOwner,
            DataOwner storageDataOwner,
            DataOwnerWriter writer,
            TrackingDetails trackingDetails)
        {
            trackingDetails.Version = 1;
            storageDataOwner.TrackingDetails = trackingDetails;

            readerMock.Setup(m => m.ReadByIdAsync(dataOwner.Id, ExpandOptions.TrackingDetails | ExpandOptions.ServiceTree)).ReturnsAsync(storageDataOwner);
            
            dateFactory.Setup(m => m.GetCurrentTime()).Returns(currentTime);

            await writer.UpdateAsync(dataOwner).ConfigureAwait(false);
            
            Assert.Equal(authPrincipal.UserId, storageDataOwner.TrackingDetails.UpdatedBy);
            Assert.Equal(currentTime, storageDataOwner.TrackingDetails.UpdatedOn);
            Assert.Equal(2, storageDataOwner.TrackingDetails.Version);

            Assert.NotEqual(currentTime, storageDataOwner.TrackingDetails.CreatedOn);
            Assert.NotEqual(authPrincipal.UserId, storageDataOwner.TrackingDetails.CreatedBy);
        }

        [Theory(DisplayName = "When UpdateAsync is called with empty ETag, then fail."), DataOwnerWriterTest.ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithEmptyETag_Then_Fail(
            DataOwner dataOwner,
            DataOwnerWriter writer)
        {
            dataOwner.ETag = string.Empty;

            var exn = await Assert.ThrowsAsync<MissingPropertyException>(() => writer.UpdateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("eTag", exn.ParamName);
        }

        [Theory(DisplayName = "When UpdateAsync is called with empty Id, then fail."), DataOwnerWriterTest.ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithEmptyId_Then_Fail(
            DataOwner dataOwner,
            DataOwnerWriter writer)
        {
            dataOwner.Id = Guid.Empty;

            var exn = await Assert.ThrowsAsync<MissingPropertyException>(() => writer.UpdateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("id", exn.ParamName);
        }

        [Theory(DisplayName = "When UpdateAsync is called with tracking details already set, then fail."), DataOwnerWriterTest.ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncCalledWithTrackingDetails_Then_Fail(
            DataOwner dataOwner,
            TrackingDetails trackingDetails,
            DataOwnerWriter writer)
        {
            dataOwner.TrackingDetails = trackingDetails;

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.UpdateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("trackingDetails", exn.ParamName);
            Assert.Null(exn.Value);
        }

        [Theory(DisplayName = "When UpdateAsync is called with non existing entity, then fail."), DataOwnerWriterTest.ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithNonExistingEntity_Then_Fail(
            [Frozen] Mock<IDataOwnerReader> readerMock,
            DataOwner dataOwner,
            DataOwnerWriter writer)
        {
            readerMock.Setup(m => m.ReadByIdAsync(dataOwner.Id, ExpandOptions.TrackingDetails | ExpandOptions.ServiceTree)).ReturnsAsync(null as DataOwner);

            var exn = await Assert.ThrowsAsync<EntityNotFoundException>(() => writer.UpdateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal(dataOwner.Id, exn.Id);
            Assert.Equal("DataOwner", exn.EntityType);
        }

        [Theory(DisplayName = "When UpdateAsync is called with mismatched ETag, then fail."), DataOwnerWriterTest.ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithNonMismatchedEtag_Then_Fail(
            DataOwner dataOwner,
            DataOwnerWriter writer,
            Guid etag)
        {
            dataOwner.ETag = etag.ToString();

            var exn = await Assert.ThrowsAsync<ETagMismatchException>(() => writer.UpdateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal(etag.ToString(), exn.Value);
        }

        [Theory(DisplayName = "When CreateAsync is called with ServiceTree and the user is not in the service admin list, then fail."), DataOwnerWriterTest.ValidServiceTreeData]
        public async Task When_CreateAsyncCalledWithServiceTreeAndUserNotInTheServiceAdminList_Then_Fail(
            [Frozen] AuthenticatedPrincipal authPrincipal,
            DataOwner dataOwner,
            DataOwnerWriter writer)
        {
            authPrincipal.UserAlias = "user name";

            var exn = await Assert.ThrowsAsync<ServiceTreeMissingWritePermissionException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("user name", exn.UserName);
            Assert.Equal($"{dataOwner.ServiceTree.ServiceId}", exn.ServiceId);
        }

        [Theory(DisplayName = "When CreateAsync is called with ServiceTree, the user fails the security group check and the service admin list is null, then fail."), DataOwnerWriterTest.ValidServiceTreeData]
        public async Task When_CreateAsyncCalledWithServiceTreeAndUserNotTheServiceAdminListIsNull_Then_Fail(
            [Frozen] Mock<IServiceClient> serviceClient,
            DataOwner dataOwner,
            DataOwnerWriter writer,
            IEnumerable<Guid> writeSecurityGroups,
            HttpResult httpResult,
            Service service)
        {
            dataOwner.WriteSecurityGroups = writeSecurityGroups;

            service.AdminUserNames = null;
            serviceClient.Setup(m => m.ReadByIdAsync(It.IsAny<Guid>(), It.IsAny<RequestContext>())).ReturnsAsync(new HttpResult<Service>(httpResult, service));

            await Assert.ThrowsAsync<SecurityGroupMissingWritePermissionException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When UpdateAsync is called with existing entity does not have write security groups, then pass the authorization check."), DataOwnerWriterTest.ValidServiceTreeData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithExistingEntityNotHaveWriteSecurityGroups_Then_PassAuthorizationCheck(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            DataOwner dataOwner,
            [Frozen] DataOwner storageDataOwner,
            DataOwnerWriter writer,
            TrackingDetails trackingDetails)
        {            
            storageDataOwner.TrackingDetails = trackingDetails;
            storageDataOwner.WriteSecurityGroups = null;

            var result = await writer.UpdateAsync(dataOwner).ConfigureAwait(false);

            Assert.Equal(storageDataOwner, result);
            storageWriter.Verify(m => m.UpdateDataOwnerAsync(storageDataOwner), Times.Once);
        }
        
        [Theory(DisplayName = "When DeleteAsync is called, then the storage layer is called with the correct history item."), DataOwnerWriterTest.ValidData(WriteAction.Update)]
        public async Task When_DeleteAsync_Then_StorageLayerCalledWithCorrectHistoryItem(
            [Frozen] Mock<IDataOwnerReader> entityReader,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            Guid id,
            string etag,
            [Frozen] DataOwner storageDataOwner,
            DataOwnerWriter writer,
            TrackingDetails trackingDetails)
        {
            entityReader.Setup(m => m.IsLinkedToAnyOtherEntities(It.IsAny<Guid>())).ReturnsAsync(false);
            entityReader.Setup(m => m.HasPendingCommands(It.IsAny<Guid>())).ReturnsAsync(false);

            storageDataOwner.TrackingDetails = trackingDetails;
            storageDataOwner.ETag = etag;
            storageDataOwner.IsDeleted = true;

            await writer.DeleteAsync(id, etag).ConfigureAwait(false);
            
            storageWriter.Verify(m => m.UpdateDataOwnerAsync(storageDataOwner), Times.Once);
        }

        [Theory(DisplayName = "When DeleteAsync is called with a different eTag, then the exception is thrown."), DataOwnerWriterTest.ValidData(WriteAction.Update)]
        public async Task When_DeleteAsyncWithDifferntEtag_Then_Fail(
            Guid id,
            string etag,
            [Frozen] DataOwner storageDataOwner,
            DataOwnerWriter writer,
            TrackingDetails trackingDetails)
        {
            storageDataOwner.TrackingDetails = trackingDetails;

            await Assert.ThrowsAsync<ETagMismatchException>(() => writer.DeleteAsync(id, etag)).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When DeleteAsync is called with non existing entity, then fail."), DataOwnerWriterTest.ValidData(WriteAction.Update)]
        public async Task When_DeleteAsyncWithNonExistingEntity_Then_Fail(
            [Frozen] Mock<IDataOwnerReader> readerMock,
            Guid id,
            string etag,
            DataOwnerWriter writer)
        {
            readerMock.Setup(m => m.ReadByIdAsync(id, ExpandOptions.WriteProperties)).ReturnsAsync(null as DataOwner);

            var exn = await Assert.ThrowsAsync<EntityNotFoundException>(() => writer.DeleteAsync(id, etag)).ConfigureAwait(false);

            Assert.Equal(id, exn.Id);
            Assert.Equal("DataOwner", exn.EntityType);
        }

        internal static void PopulateIds(params Entity[] entities)
        {
            foreach (var entity in entities)
            {
                entity.Id = Guid.NewGuid();
            }
        }
    }
}