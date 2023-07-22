namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Writer.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.DataAccess.Reader;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.UnitTest;
    using Microsoft.PrivacyServices.DataManagement.Models.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;
    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;

    public class TransferRequestWriterTest
    {
        #region CreateAsync

        [Theory(DisplayName = "When CreateAsync is called with correct parameters, then the storage layer is called and returned."), ValidData]
        public async Task When_CreateAsync_Then_SuccessfulProcessing(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            [Frozen] Mock<IAssetGroupReader> assetGroupReader,
            [Frozen] Mock<IDataOwnerReader> dataOwnerReader,
            [Frozen] TransferRequest storageTransferRequest,
            TransferRequest transferRequest,
            AssetGroup assetGroup,
            DataOwner sourceOwner,
            DataOwner targetOwner,
            TransferRequestWriter writer)
        {
            // Build source owner object.
            Guid sourceOwnerId = Guid.NewGuid();
            sourceOwner.Id = sourceOwnerId;
            sourceOwner.ETag = "ETag";
            sourceOwner.TrackingDetails = new TrackingDetails();
            sourceOwner.HasInitiatedTransferRequests = false;
            sourceOwner.HasPendingTransferRequests = false;

            // Setup data owner reader so that it returns source owner object
            // when called by the transfer request writer for source owner id. 
            dataOwnerReader
                .Setup(m => m.ReadByIdAsync(sourceOwnerId, ExpandOptions.WriteProperties))
                .ReturnsAsync(sourceOwner);

            // Build Target owner object.
            Guid targetOwnerId = Guid.NewGuid();
            targetOwner.Id = targetOwnerId;
            targetOwner.ETag = "ETag";
            targetOwner.TrackingDetails = new TrackingDetails();
            targetOwner.HasInitiatedTransferRequests = false;
            targetOwner.HasPendingTransferRequests = false;

            // Setup data owner reader so that it returns target owner object
            // when called by the transfer request writer for target owner id. 
            dataOwnerReader
                .Setup(m => m.ReadByIdAsync(targetOwnerId, ExpandOptions.WriteProperties))
                .ReturnsAsync(targetOwner);

            // Build asset group object.
            Guid assetGroupId = Guid.NewGuid();
            assetGroup.Id = assetGroupId;
            assetGroup.ETag = "ETag";
            assetGroup.TrackingDetails = new TrackingDetails();
            assetGroup.OwnerId = sourceOwnerId;
            assetGroup.HasPendingTransferRequest = false;

            // Setup asset group reader so that it returns above asset group object.
            Action<IEnumerable<Guid>> verify = x => x.SequenceEqual(new[] { assetGroupId });
            assetGroupReader
                .Setup(m => m.ReadByIdsAsync(Is.Value(verify), ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { assetGroup });

            transferRequest.SourceOwnerId = sourceOwnerId;
            transferRequest.TargetOwnerId = targetOwnerId;
            transferRequest.AssetGroups = new[] { assetGroupId };

            // Invoke Create Transfer Request API.
            var result = await writer.CreateAsync(transferRequest).ConfigureAwait(false);

            // Verify that we get correct object in the return value.
            Assert.Equal(storageTransferRequest, result);

            // Verify that the request state is 'Pending'.
            Assert.Equal(TransferRequestStates.Pending, result.RequestState);

            // Verify transfer request creation.
            Action<TransferRequest> verifyRequest = x =>
            {
                Assert.Equal(sourceOwnerId, x.SourceOwnerId);
                Assert.Equal(targetOwnerId, x.TargetOwnerId);
                Assert.Equal(TransferRequestStates.Pending, x.RequestState);
            };

            // Verify data written to storage.
            Action<IEnumerable<Entity>> verifyWrite = x =>
            {
                // We expect 3 entities to get updated alongwith creation of transfer request - source owner, target owner, asset group
                Assert.Equal(3, x.Count());

                // Verify that source owner object update is called with HasInitiatedTransferRequests set to true.
                var entity1 = x.Single(y => y.Id == transferRequest.SourceOwnerId);
                var updatingSourceOwner = entity1 as DataOwner;
                Assert.True(updatingSourceOwner.HasInitiatedTransferRequests);
                Assert.False(updatingSourceOwner.HasPendingTransferRequests);

                // Verify that target owner object update is called with HasPendingTransferRequests set to true.
                var entity2 = x.Single(y => y.Id == transferRequest.TargetOwnerId);
                var updatingTargetOwner = entity2 as DataOwner;
                Assert.False(updatingTargetOwner.HasInitiatedTransferRequests);
                Assert.True(updatingTargetOwner.HasPendingTransferRequests);

                // Verify that asset group object update is called with HasPendingTransferRequest set to true.
                var entity3 = x.Single(y => y.Id == assetGroupId);
                var updatingAssetGroup = entity3 as AssetGroup;
                Assert.True(updatingAssetGroup.HasPendingTransferRequest);
            };

            // Verify that CreateTransferRequestAsync gets called with expected values.
            storageWriter.Verify(m => m.CreateTransferRequestAsync(Is.Value(verifyRequest), Is.Value(verifyWrite)), Times.Once);
        }

        [Theory(DisplayName = "When CreateAsync is called with correct parameters, then successful processing."), ValidData]
        public async Task When_CreateAsyncWithMultipleAssetGroups_Then_SuccessfulProcessing(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            [Frozen] Mock<IAssetGroupReader> assetGroupReader,
            [Frozen] Mock<IDataOwnerReader> dataOwnerReader,
            [Frozen] TransferRequest storageTransferRequest,
            TransferRequest transferRequest,
            AssetGroup assetGroup1,
            AssetGroup assetGroup2,
            DataOwner sourceOwner,
            DataOwner targetOwner,
            TransferRequestWriter writer)
        {
            // Build source owner object.
            Guid sourceOwnerId = Guid.NewGuid();
            sourceOwner.Id = sourceOwnerId;
            sourceOwner.ETag = "ETag";
            sourceOwner.TrackingDetails = new TrackingDetails();
            sourceOwner.HasInitiatedTransferRequests = false;
            sourceOwner.HasPendingTransferRequests = false;

            // Setup data owner reader so that it returns source owner object
            // when called by the transfer request writer for source owner id. 
            dataOwnerReader
                .Setup(m => m.ReadByIdAsync(sourceOwnerId, ExpandOptions.WriteProperties))
                .ReturnsAsync(sourceOwner);

            // Build Target owner object.
            Guid targetOwnerId = Guid.NewGuid();
            targetOwner.Id = targetOwnerId;
            targetOwner.ETag = "ETag";
            targetOwner.TrackingDetails = new TrackingDetails();
            targetOwner.HasInitiatedTransferRequests = false;
            targetOwner.HasPendingTransferRequests = false;

            // Setup data owner reader so that it returns target owner object
            // when called by the transfer request writer for target owner id. 
            dataOwnerReader
                .Setup(m => m.ReadByIdAsync(targetOwnerId, ExpandOptions.WriteProperties))
                .ReturnsAsync(targetOwner);

            // Build first asset group object.
            Guid assetGroupId1 = Guid.NewGuid();
            assetGroup1.Id = assetGroupId1;
            assetGroup1.ETag = "ETag";
            assetGroup1.TrackingDetails = new TrackingDetails();
            assetGroup1.OwnerId = sourceOwnerId;
            assetGroup1.HasPendingTransferRequest = false;

            // Build first asset group object.
            Guid assetGroupId2 = Guid.NewGuid();
            assetGroup2.Id = assetGroupId2;
            assetGroup2.ETag = "ETag";
            assetGroup2.TrackingDetails = new TrackingDetails();
            assetGroup2.OwnerId = sourceOwnerId;
            assetGroup2.HasPendingTransferRequest = false;

            // Setup asset group reader so that it returns above asset group object.
            Action<IEnumerable<Guid>> verify = x => x.SequenceEqual(new[] { assetGroupId1, assetGroupId2 });
            assetGroupReader
                .Setup(m => m.ReadByIdsAsync(Is.Value(verify), ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { assetGroup1, assetGroup2 });

            transferRequest.SourceOwnerId = sourceOwnerId;
            transferRequest.TargetOwnerId = targetOwnerId;
            transferRequest.AssetGroups = new[] { assetGroupId1, assetGroupId2 };

            // Invoke Create Transfer Request API.
            var result = await writer.CreateAsync(transferRequest).ConfigureAwait(false);

            // Verify that we get correct object in the return value.
            Assert.Equal(storageTransferRequest, result);

            // Verify that the request state is 'Pending'.
            Assert.Equal(TransferRequestStates.Pending, result.RequestState);

            // Verify transfer request creation.
            Action<TransferRequest> verifyRequest = x =>
            {
                Assert.Equal(sourceOwnerId, x.SourceOwnerId);
                Assert.Equal(targetOwnerId, x.TargetOwnerId);
                Assert.Equal(TransferRequestStates.Pending, x.RequestState);
            };

            // Verify data written to storage.
            Action<IEnumerable<Entity>> verifyWrite = x =>
            {
                // We expect 3 entities to get updated alongwith creation of transfer request - source owner, target owner, asset group
                Assert.Equal(4, x.Count());

                // Verify that source owner object update is called with HasInitiatedTransferRequests set to true.
                var entity1 = x.Single(y => y.Id == transferRequest.SourceOwnerId);
                var updatingSourceOwner = entity1 as DataOwner;
                Assert.True(updatingSourceOwner.HasInitiatedTransferRequests);
                Assert.False(updatingSourceOwner.HasPendingTransferRequests);

                // Verify that target owner object update is called with HasPendingTransferRequests set to true.
                var entity2 = x.Single(y => y.Id == transferRequest.TargetOwnerId);
                var updatingTargetOwner = entity2 as DataOwner;
                Assert.False(updatingTargetOwner.HasInitiatedTransferRequests);
                Assert.True(updatingTargetOwner.HasPendingTransferRequests);

                // Verify that asset group object update is called with HasPendingTransferRequest set to true.
                var entity3 = x.Single(y => y.Id == assetGroupId1);
                var updatingAssetGroup1 = entity3 as AssetGroup;
                Assert.True(updatingAssetGroup1.HasPendingTransferRequest);

                // Verify that asset group object update is called with HasPendingTransferRequest set to true.
                var entity4 = x.Single(y => y.Id == assetGroupId2);
                var updatingAssetGroup2 = entity4 as AssetGroup;
                Assert.True(updatingAssetGroup2.HasPendingTransferRequest);
            };

            // Verify that CreateTransferRequestAsync gets called with expected values.
            storageWriter.Verify(m => m.CreateTransferRequestAsync(Is.Value(verifyRequest), Is.Value(verifyWrite)), Times.Once);
        }

        [Theory(DisplayName = "When CreateAsync is called with missing source owner id, then fail."), ValidData]
        public async Task When_CreateAsyncCalledWithoutSourceOwner_Then_Fail(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            TransferRequest transferRequest,
            TransferRequestWriter writer)
        {
            // Set source owner id in the transfer request to empty. 
            transferRequest.SourceOwnerId = Guid.Empty;
            var exn = await Assert.ThrowsAsync<MissingPropertyException>(() => writer.CreateAsync(transferRequest)).ConfigureAwait(false);

            // Verify the exception parameters.
            Assert.Equal("sourceOwnerId", exn.ParamName);

            // Verify that CreateTransferRequestAsync never got called.
            storageWriter.Verify(m => m.CreateTransferRequestAsync(It.IsAny<TransferRequest>(), It.IsAny<IEnumerable<Entity>>()), Times.Never);
        }

        [Theory(DisplayName = "When CreateAsync is called with missing target owner id, then fail."), ValidData]
        public async Task When_CreateAsyncCalledWithoutTargetOwner_Then_Fail(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            TransferRequest transferRequest,
            TransferRequestWriter writer)
        {
            // Set target owner id in the transfer request to empty. 
            transferRequest.TargetOwnerId = Guid.Empty;
            var exn = await Assert.ThrowsAsync<MissingPropertyException>(() => writer.CreateAsync(transferRequest)).ConfigureAwait(false);

            // Verify the exception parameters.
            Assert.Equal("targetOwnerId", exn.ParamName);

            // Verify that CreateTransferRequestAsync never got called.
            storageWriter.Verify(m => m.CreateTransferRequestAsync(It.IsAny<TransferRequest>(), It.IsAny<IEnumerable<Entity>>()), Times.Never);
        }

        [Theory(DisplayName = "When CreateAsync is called with missing asset groups, then fail."), ValidData]
        public async Task When_CreateAsyncCalledWithoutAssetGroups_Then_Fail(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            TransferRequest transferRequest,
            TransferRequestWriter writer)
        {
            // Set asset groups in the transfer request to empty. 
            transferRequest.AssetGroups = Enumerable.Empty<Guid>().ToArray();
            var exn = await Assert.ThrowsAsync<MissingPropertyException>(() => writer.CreateAsync(transferRequest)).ConfigureAwait(false);

            // Verify the exception parameters.
            Assert.Equal("assetGroups", exn.ParamName);

            // Verify that CreateTransferRequestAsync never got called.
            storageWriter.Verify(m => m.CreateTransferRequestAsync(It.IsAny<TransferRequest>(), It.IsAny<IEnumerable<Entity>>()), Times.Never);
        }

        [Theory(DisplayName = "When CreateAsync is called with state set, then fail."), ValidData]
        public async Task When_CreateAsyncCalledWithStateSet_Then_Fail(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            TransferRequest transferRequest,
            TransferRequestWriter writer)
        {
            // Set the state in the transfer request to 'Approved'. 
            transferRequest.RequestState = TransferRequestStates.Approved;
            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(transferRequest)).ConfigureAwait(false);

            // Verify the exception parameters.
            Assert.Equal("state", exn.ParamName);

            // Verify that CreateTransferRequestAsync never got called.
            storageWriter.Verify(m => m.CreateTransferRequestAsync(It.IsAny<TransferRequest>(), It.IsAny<IEnumerable<Entity>>()), Times.Never);
        }

        [Theory(DisplayName = "When CreateAsync is called with non-existing source owner, then fail."), ValidData]
        public async Task When_CreateAsyncCalledWithNonExistingSourceOwner_Then_Fail(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            [Frozen] Mock<IAssetGroupReader> assetGroupReader,
            [Frozen] Mock<IDataOwnerReader> dataOwnerReader,
            TransferRequest transferRequest,
            AssetGroup assetGroup,
            DataOwner targetOwner,
            TransferRequestWriter writer)
        {
            // Build source owner object.
            Guid sourceOwnerId = Guid.NewGuid();
            Guid targetOwnerId = Guid.NewGuid();
            targetOwner.Id = sourceOwnerId;

            // Setup data owner reader so that it returns null when called for source owner id. 
            dataOwnerReader
                .Setup(m => m.ReadByIdAsync(sourceOwnerId, ExpandOptions.WriteProperties))
                .ReturnsAsync(null as DataOwner);

            // Setup data owner reader so that it returns target owner object
            // when called by the transfer request writer for target owner id. 
            dataOwnerReader
                .Setup(m => m.ReadByIdAsync(targetOwnerId, ExpandOptions.WriteProperties))
                .ReturnsAsync(targetOwner);

            // Build asset group object.
            Guid assetGroupId = Guid.NewGuid();
            assetGroup.Id = assetGroupId;
            assetGroup.OwnerId = sourceOwnerId;
            assetGroup.HasPendingTransferRequest = false;

            // Setup asset group reader so that it returns above asset group object.
            Action<IEnumerable<Guid>> verify = x => x.SequenceEqual(new[] { assetGroupId });
            assetGroupReader
                .Setup(m => m.ReadByIdsAsync(Is.Value(verify), ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { assetGroup });

            transferRequest.SourceOwnerId = sourceOwnerId;
            transferRequest.TargetOwnerId = targetOwnerId;
            transferRequest.AssetGroups = new[] { assetGroupId };

            // Invoke Create Transfer Request API.
            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.CreateAsync(transferRequest)).ConfigureAwait(false);

            // Verify.
            Assert.Equal(ConflictType.DoesNotExist, exn.ConflictType);
            Assert.Equal("ownerId", exn.Target);
            Assert.Equal(sourceOwnerId.ToString(), exn.Value);

            // Verify that CreateTransferRequestAsync never got called.
            storageWriter.Verify(m => m.CreateTransferRequestAsync(It.IsAny<TransferRequest>(), It.IsAny<IEnumerable<Entity>>()), Times.Never);
        }

        [Theory(DisplayName = "When CreateAsync is called with non-existing target owner, then fail."), ValidData]
        public async Task When_CreateAsyncCalledWithNonExistingTargetOwner_Then_Fail(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            [Frozen] Mock<IAssetGroupReader> assetGroupReader,
            [Frozen] Mock<IDataOwnerReader> dataOwnerReader,
            TransferRequest transferRequest,
            AssetGroup assetGroup,
            DataOwner sourceOwner,
            TransferRequestWriter writer)
        {
            // Build source owner object.
            Guid sourceOwnerId = Guid.NewGuid();
            sourceOwner.Id = sourceOwnerId;

            // Setup data owner reader so that it returns source owner object
            // when called by the transfer request writer for source owner id. 
            dataOwnerReader
                .Setup(m => m.ReadByIdAsync(sourceOwnerId, ExpandOptions.WriteProperties))
                .ReturnsAsync(sourceOwner);

            // Setup data owner reader so that it returns target owner object
            // when called by the transfer request writer for target owner id. 
            Guid targetOwnerId = Guid.NewGuid();
            dataOwnerReader
                .Setup(m => m.ReadByIdAsync(targetOwnerId, ExpandOptions.WriteProperties))
                .ReturnsAsync(null as DataOwner);

            // Build asset group object.
            Guid assetGroupId = Guid.NewGuid();
            assetGroup.Id = assetGroupId;
            assetGroup.OwnerId = sourceOwnerId;
            assetGroup.HasPendingTransferRequest = false;

            // Setup asset group reader so that it returns above asset group object.
            Action<IEnumerable<Guid>> verify = x => x.SequenceEqual(new[] { assetGroupId });
            assetGroupReader
                .Setup(m => m.ReadByIdsAsync(Is.Value(verify), ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { assetGroup });

            transferRequest.SourceOwnerId = sourceOwnerId;
            transferRequest.TargetOwnerId = targetOwnerId;
            transferRequest.AssetGroups = new[] { assetGroupId };

            // Invoke Create Transfer Request API.
            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.CreateAsync(transferRequest)).ConfigureAwait(false);

            // Verify.
            Assert.Equal(ConflictType.DoesNotExist, exn.ConflictType);
            Assert.Equal("ownerId", exn.Target);
            Assert.Equal(targetOwnerId.ToString(), exn.Value);

            // Verify that CreateTransferRequestAsync never got called.
            storageWriter.Verify(m => m.CreateTransferRequestAsync(It.IsAny<TransferRequest>(), It.IsAny<IEnumerable<Entity>>()), Times.Never);
        }

        [Theory(DisplayName = "When CreateAsync is called with non-existing asset group, then fail."), ValidData]
        public async Task When_CreateAsyncCalledWithNonExistingAssetGroup_Then_Fail(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            [Frozen] Mock<IAssetGroupReader> assetGroupReader,
            [Frozen] Mock<IDataOwnerReader> dataOwnerReader,
            TransferRequest transferRequest,
            AssetGroup assetGroup1,
            AssetGroup assetGroup2,
            DataOwner sourceOwner,
            DataOwner targetOwner,
            TransferRequestWriter writer)
        {
            // Build source owner object.
            Guid sourceOwnerId = Guid.NewGuid();
            sourceOwner.Id = sourceOwnerId;

            // Setup data owner reader so that it returns source owner object
            // when called by the transfer request writer for source owner id. 
            dataOwnerReader
                .Setup(m => m.ReadByIdAsync(sourceOwnerId, ExpandOptions.WriteProperties))
                .ReturnsAsync(sourceOwner);

            // Setup data owner reader so that it returns target owner object
            // when called by the transfer request writer for target owner id. 
            Guid targetOwnerId = Guid.NewGuid();
            targetOwner.Id = targetOwnerId;
            dataOwnerReader
                .Setup(m => m.ReadByIdAsync(targetOwnerId, ExpandOptions.WriteProperties))
                .ReturnsAsync(targetOwner);

            // Build asset group object.
            Guid assetGroupId1 = Guid.NewGuid();
            assetGroup1.Id = assetGroupId1;
            assetGroup1.OwnerId = sourceOwnerId;
            assetGroup1.HasPendingTransferRequest = false;

            Guid assetGroupId2 = Guid.NewGuid();
            assetGroup2.Id = assetGroupId2;
            assetGroup2.OwnerId = sourceOwnerId;
            assetGroup2.HasPendingTransferRequest = false;

            // Setup asset group reader so that it returns above asset group object.
            Action<IEnumerable<Guid>> verify = x => x.SequenceEqual(new[] { assetGroupId1 });
            assetGroupReader
                .Setup(m => m.ReadByIdsAsync(Is.Value(verify), ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { assetGroup2 });

            transferRequest.SourceOwnerId = sourceOwnerId;
            transferRequest.TargetOwnerId = targetOwnerId;
            transferRequest.AssetGroups = new[] { assetGroupId1 };

            // Invoke Create Transfer Request API.
            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.CreateAsync(transferRequest)).ConfigureAwait(false);

            // Verify.
            Assert.Equal(ConflictType.DoesNotExist, exn.ConflictType);
            Assert.Equal("assetGroup", exn.Target);
            Assert.Equal(assetGroupId1.ToString(), exn.Value);

            // Verify that CreateTransferRequestAsync never got called.
            storageWriter.Verify(m => m.CreateTransferRequestAsync(It.IsAny<TransferRequest>(), It.IsAny<IEnumerable<Entity>>()), Times.Never);
        }

        [Theory(DisplayName = "When CreateAsync is called with asset group with no owner, then fail."), ValidData]
        public async Task When_CreateAsyncCalledWitAssetGroupNoOwner_Then_Fail(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            [Frozen] Mock<IAssetGroupReader> assetGroupReader,
            [Frozen] Mock<IDataOwnerReader> dataOwnerReader,
            TransferRequest transferRequest,
            AssetGroup assetGroup1,
            AssetGroup assetGroup2,
            DataOwner sourceOwner,
            DataOwner targetOwner,
            TransferRequestWriter writer)
        {
            // Build source owner object.
            Guid sourceOwnerId = Guid.NewGuid();
            sourceOwner.Id = sourceOwnerId;

            // Setup data owner reader so that it returns source owner object
            // when called by the transfer request writer for source owner id. 
            dataOwnerReader
                .Setup(m => m.ReadByIdAsync(sourceOwnerId, ExpandOptions.WriteProperties))
                .ReturnsAsync(sourceOwner);

            // Setup data owner reader so that it returns target owner object
            // when called by the transfer request writer for target owner id. 
            Guid targetOwnerId = Guid.NewGuid();
            targetOwner.Id = targetOwnerId;
            dataOwnerReader
                .Setup(m => m.ReadByIdAsync(targetOwnerId, ExpandOptions.WriteProperties))
                .ReturnsAsync(targetOwner);

            // Build asset group object.
            Guid assetGroupId1 = Guid.NewGuid();
            assetGroup1.Id = assetGroupId1;
            assetGroup1.OwnerId = sourceOwnerId;
            assetGroup1.HasPendingTransferRequest = false;

            Guid assetGroupId2 = Guid.NewGuid();
            assetGroup2.Id = assetGroupId2;
            assetGroup2.OwnerId = Guid.Empty;
            assetGroup2.HasPendingTransferRequest = false;

            // Setup asset group reader so that it returns above asset group object.
            Action<IEnumerable<Guid>> verify = x => x.SequenceEqual(new[] { assetGroupId1 });
            assetGroupReader
                .Setup(m => m.ReadByIdsAsync(Is.Value(verify), ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { assetGroup1, assetGroup2 });

            transferRequest.SourceOwnerId = sourceOwnerId;
            transferRequest.TargetOwnerId = targetOwnerId;
            transferRequest.AssetGroups = new[] { assetGroupId1, assetGroupId2 };

            // Invoke Create Transfer Request API.
            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.CreateAsync(transferRequest)).ConfigureAwait(false);

            // Verify.
            Assert.Equal(ConflictType.DoesNotExist, exn.ConflictType);
            Assert.Equal("assetGroupOwner", exn.Target);
            Assert.Equal(assetGroupId2.ToString(), exn.Value);

            // Verify that CreateTransferRequestAsync never got called.
            storageWriter.Verify(m => m.CreateTransferRequestAsync(It.IsAny<TransferRequest>(), It.IsAny<IEnumerable<Entity>>()), Times.Never);
        }

        [Theory(DisplayName = "When CreateAsync is called with asset group with different owner, then fail."), ValidData]
        public async Task When_CreateAsyncCalledWitAssetGroupDifferentOwner_Then_Fail(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            [Frozen] Mock<IAssetGroupReader> assetGroupReader,
            [Frozen] Mock<IDataOwnerReader> dataOwnerReader,
            TransferRequest transferRequest,
            AssetGroup assetGroup1,
            AssetGroup assetGroup2,
            DataOwner sourceOwner,
            DataOwner targetOwner,
            TransferRequestWriter writer)
        {
            // Build source owner object.
            Guid sourceOwnerId = Guid.NewGuid();
            sourceOwner.Id = sourceOwnerId;

            // Setup data owner reader so that it returns source owner object
            // when called by the transfer request writer for source owner id. 
            dataOwnerReader
                .Setup(m => m.ReadByIdAsync(sourceOwnerId, ExpandOptions.WriteProperties))
                .ReturnsAsync(sourceOwner);

            // Setup data owner reader so that it returns target owner object
            // when called by the transfer request writer for target owner id. 
            Guid targetOwnerId = Guid.NewGuid();
            targetOwner.Id = targetOwnerId;
            dataOwnerReader
                .Setup(m => m.ReadByIdAsync(targetOwnerId, ExpandOptions.WriteProperties))
                .ReturnsAsync(targetOwner);

            // Build asset group object.
            Guid assetGroupId1 = Guid.NewGuid();
            assetGroup1.Id = assetGroupId1;
            assetGroup1.OwnerId = sourceOwnerId;
            assetGroup1.HasPendingTransferRequest = false;

            Guid assetGroupId2 = Guid.NewGuid();
            assetGroup2.Id = assetGroupId2;
            assetGroup2.OwnerId = Guid.NewGuid();
            assetGroup2.HasPendingTransferRequest = false;

            // Setup asset group reader so that it returns above asset group object.
            Action<IEnumerable<Guid>> verify = x => x.SequenceEqual(new[] { assetGroupId1 });
            assetGroupReader
                .Setup(m => m.ReadByIdsAsync(Is.Value(verify), ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { assetGroup1, assetGroup2 });

            transferRequest.SourceOwnerId = sourceOwnerId;
            transferRequest.TargetOwnerId = targetOwnerId;
            transferRequest.AssetGroups = new[] { assetGroupId1, assetGroupId2 };

            // Invoke Create Transfer Request API.
            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.CreateAsync(transferRequest)).ConfigureAwait(false);

            // Verify.
            Assert.Equal(ConflictType.InvalidValue, exn.ConflictType);
            Assert.Equal("assetGroupOwner", exn.Target);
            Assert.Equal(assetGroupId2.ToString(), exn.Value);

            // Verify that CreateTransferRequestAsync never got called.
            storageWriter.Verify(m => m.CreateTransferRequestAsync(It.IsAny<TransferRequest>(), It.IsAny<IEnumerable<Entity>>()), Times.Never);
        }

        [Theory(DisplayName = "When CreateAsync is called with asset group already being transferred, then fail."), ValidData]
        public async Task When_CreateAsyncCalledWithAssetGroupBeingTransferred_Then_Fail(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            [Frozen] Mock<IAssetGroupReader> assetGroupReader,
            [Frozen] Mock<IDataOwnerReader> dataOwnerReader,
            TransferRequest transferRequest,
            AssetGroup assetGroup,
            DataOwner sourceOwner,
            DataOwner targetOwner,
            TransferRequestWriter writer)
        {
            // Build source owner object.
            Guid sourceOwnerId = Guid.NewGuid();
            sourceOwner.Id = sourceOwnerId;

            Guid targetOwnerId = Guid.NewGuid();
            targetOwner.Id = targetOwnerId;

            // Setup data owner reader so that it returns null when called for source owner id. 
            dataOwnerReader
                .Setup(m => m.ReadByIdAsync(sourceOwnerId, ExpandOptions.WriteProperties))
                .ReturnsAsync(sourceOwner);

            // Setup data owner reader so that it returns target owner object
            // when called by the transfer request writer for target owner id. 
            dataOwnerReader
                .Setup(m => m.ReadByIdAsync(targetOwnerId, ExpandOptions.WriteProperties))
                .ReturnsAsync(targetOwner);

            // Build asset group object.
            Guid assetGroupId = Guid.NewGuid();
            assetGroup.Id = assetGroupId;
            assetGroup.OwnerId = sourceOwnerId;
            assetGroup.HasPendingTransferRequest = true;

            // Setup asset group reader so that it returns above asset group object.
            Action<IEnumerable<Guid>> verify = x => x.SequenceEqual(new[] { assetGroupId });
            assetGroupReader
                .Setup(m => m.ReadByIdsAsync(Is.Value(verify), ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { assetGroup });

            transferRequest.SourceOwnerId = sourceOwnerId;
            transferRequest.TargetOwnerId = targetOwnerId;
            transferRequest.AssetGroups = new[] { assetGroupId };

            // Invoke Create Transfer Request API.
            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.CreateAsync(transferRequest)).ConfigureAwait(false);

            // Verify.
            Assert.Equal(ConflictType.LinkedEntityExists, exn.ConflictType);
            Assert.Equal("assetGroup", exn.Target);
            Assert.Equal(assetGroupId.ToString(), exn.Value);

            // Verify that CreateTransferRequestAsync never got called.
            storageWriter.Verify(m => m.CreateTransferRequestAsync(It.IsAny<TransferRequest>(), It.IsAny<IEnumerable<Entity>>()), Times.Never);
        }

        #endregion

        #region ApproveAsync

        [Theory(DisplayName = "When ApproveAsync is called with correct parameters, then successful processing."), ValidData]
        public async Task When_ApproveAsync_Then_SuccessfulProcessing(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            [Frozen] Mock<IAssetGroupReader> assetGroupReader,
            [Frozen] Mock<IDataOwnerReader> dataOwnerReader,
            [Frozen] Mock<ITransferRequestReader> transferRequestReader,
            TransferRequest transferRequest,
            AssetGroup assetGroup,
            DataOwner sourceOwner,
            DataOwner targetOwner,
            TransferRequestWriter writer)
        {
            // Build source owner object.
            Guid sourceOwnerId = Guid.NewGuid();
            sourceOwner.Id = sourceOwnerId;
            sourceOwner.ETag = "ETag";
            sourceOwner.TrackingDetails = new TrackingDetails();
            sourceOwner.HasInitiatedTransferRequests = true;
            sourceOwner.HasPendingTransferRequests = false;

            // Setup data owner reader so that it returns source owner object when transfer request writer asks it. 
            dataOwnerReader
                .Setup(m => m.ReadByIdAsync(sourceOwnerId, ExpandOptions.WriteProperties))
                .ReturnsAsync(sourceOwner);

            // Build Target owner object.
            Guid targetOwnerId = Guid.NewGuid();
            targetOwner.Id = targetOwnerId;
            targetOwner.ETag = "ETag";
            targetOwner.TrackingDetails = new TrackingDetails();
            targetOwner.HasInitiatedTransferRequests = false;
            targetOwner.HasPendingTransferRequests = true;

            // Setup data owner reader so that it returns target owner object when transfer request writer asks it. 
            dataOwnerReader
                .Setup(m => m.ReadByIdAsync(targetOwnerId, ExpandOptions.WriteProperties))
                .ReturnsAsync(targetOwner);

            // Build asset group object.
            Guid assetGroupId = Guid.NewGuid();
            assetGroup.Id = assetGroupId;
            assetGroup.ETag = "ETag";
            assetGroup.OwnerId = sourceOwnerId;
            assetGroup.TrackingDetails = new TrackingDetails();
            assetGroup.HasPendingTransferRequest = true;

            // Setup asset group reader so that it returns above asset group object when transfer request writer asks it. 
            Action<IEnumerable<Guid>> verify = x => x.SequenceEqual(new[] { assetGroupId });
            assetGroupReader
                .Setup(m => m.ReadByIdsAsync(Is.Value(verify), ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { assetGroup });

            // Setup transfer request reader so that it returns no other transfer requests initiated or pending of either owners. 
            FilterResult<TransferRequest> result = new FilterResult<TransferRequest>()
            {
                Count = 1,
                Total = 1
            };

            transferRequestReader
                .Setup(m => m.ReadByFiltersAsync(It.IsAny<IFilterCriteria<TransferRequest>>(), ExpandOptions.None))
                .ReturnsAsync(result);

            // Build transfer request object.
            Guid transferRequestId = Guid.NewGuid();
            transferRequest.Id = transferRequestId;
            transferRequest.ETag = "ETag";
            transferRequest.TrackingDetails = new TrackingDetails();
            transferRequest.IsDeleted = false;
            transferRequest.SourceOwnerId = sourceOwnerId;
            transferRequest.TargetOwnerId = targetOwnerId;
            transferRequest.AssetGroups = new[] { assetGroupId };

            // Setup transfer request reader so that it returns the above request when queried by Id. 
            transferRequestReader
                .Setup(m => m.ReadByIdAsync(transferRequestId, ExpandOptions.WriteProperties))
                .ReturnsAsync(transferRequest);

            // Setup transfer request reader so that it returns that this transfer request is not linked to any other objects. 
            transferRequestReader
                .Setup(m => m.IsLinkedToAnyOtherEntities(transferRequestId))
                .ReturnsAsync(false);

            transferRequestReader
                .Setup(m => m.HasPendingCommands(transferRequestId))
                .ReturnsAsync(false);

            // Invoke Create Transfer Request API.
            await writer.ApproveAsync(transferRequestId, "ETag").ConfigureAwait(false);

            // Verify data written to storage.
            Action<IEnumerable<Entity>> verifyUpdate = x =>
            {
                Assert.Equal(4, x.Count());

                var entity1 = x.Single(y => y.Id == transferRequest.SourceOwnerId);
                var updatingSourceOwner = entity1 as DataOwner;
                Assert.False(updatingSourceOwner.HasInitiatedTransferRequests);
                Assert.False(updatingSourceOwner.HasPendingTransferRequests);

                var entity2 = x.Single(y => y.Id == transferRequest.TargetOwnerId);
                var updatingTargetOwner = entity2 as DataOwner;
                Assert.False(updatingTargetOwner.HasInitiatedTransferRequests);
                Assert.False(updatingTargetOwner.HasPendingTransferRequests);

                var entity3 = x.Single(y => y.Id == assetGroupId);
                var updatingAssetGroup = entity3 as AssetGroup;
                Assert.False(updatingAssetGroup.HasPendingTransferRequest);
                Assert.Equal(targetOwnerId, updatingAssetGroup.OwnerId);
            };

            // Verify that CreateTransferRequestAsync gets called with expected values.
            storageWriter.Verify(m => m.UpdateEntitiesAsync(Is.Value(verifyUpdate)), Times.Once);

            // Verify that the request state is deleted.
            Assert.True(transferRequest.IsDeleted);
        }

        [Theory(DisplayName = "When ApproveAsync is called with other pending requests, then successful processing."), ValidData]
        public async Task When_ApproveAsyncWithOtherPending_Then_SuccessfulProcessing(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            [Frozen] Mock<IAssetGroupReader> assetGroupReader,
            [Frozen] Mock<IDataOwnerReader> dataOwnerReader,
            [Frozen] Mock<ITransferRequestReader> transferRequestReader,
            TransferRequest transferRequest,
            AssetGroup assetGroup,
            DataOwner sourceOwner,
            DataOwner targetOwner,
            TransferRequestWriter writer)
        {
            // Build source owner object.
            Guid sourceOwnerId = Guid.NewGuid();
            sourceOwner.Id = sourceOwnerId;
            sourceOwner.ETag = "ETag";
            sourceOwner.TrackingDetails = new TrackingDetails();
            sourceOwner.HasInitiatedTransferRequests = true;
            sourceOwner.HasPendingTransferRequests = false;

            // Setup data owner reader so that it returns source owner object when transfer request writer asks it. 
            dataOwnerReader
                .Setup(m => m.ReadByIdAsync(sourceOwnerId, ExpandOptions.WriteProperties))
                .ReturnsAsync(sourceOwner);

            // Build Target owner object.
            Guid targetOwnerId = Guid.NewGuid();
            targetOwner.Id = targetOwnerId;
            targetOwner.ETag = "ETag";
            targetOwner.TrackingDetails = new TrackingDetails();
            targetOwner.HasInitiatedTransferRequests = false;
            targetOwner.HasPendingTransferRequests = true;

            // Setup data owner reader so that it returns target owner object when transfer request writer asks it. 
            dataOwnerReader
                .Setup(m => m.ReadByIdAsync(targetOwnerId, ExpandOptions.WriteProperties))
                .ReturnsAsync(targetOwner);

            // Build asset group object.
            Guid assetGroupId = Guid.NewGuid();
            assetGroup.Id = assetGroupId;
            assetGroup.ETag = "ETag";
            assetGroup.TrackingDetails = new TrackingDetails();
            assetGroup.OwnerId = sourceOwnerId;
            assetGroup.HasPendingTransferRequest = true;

            // Setup asset group reader so that it returns above asset group object when transfer request writer asks it. 
            Action<IEnumerable<Guid>> verify = x => x.SequenceEqual(new[] { assetGroupId });
            assetGroupReader
                .Setup(m => m.ReadByIdsAsync(Is.Value(verify), ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { assetGroup });

            // Setup transfer request reader so that it returns other transfer requests initiated or pending of either owners. 
            FilterResult<TransferRequest> result = new FilterResult<TransferRequest>()
            {
                Count = 2,
                Total = 2
            };

            transferRequestReader
                .Setup(m => m.ReadByFiltersAsync(It.IsAny<IFilterCriteria<TransferRequest>>(), ExpandOptions.None))
                .ReturnsAsync(result);

            // Build transfer request object.
            Guid transferRequestId = Guid.NewGuid();
            transferRequest.Id = transferRequestId;
            transferRequest.ETag = "ETag";
            transferRequest.TrackingDetails = new TrackingDetails();
            transferRequest.IsDeleted = false;
            transferRequest.SourceOwnerId = sourceOwnerId;
            transferRequest.TargetOwnerId = targetOwnerId;
            transferRequest.AssetGroups = new[] { assetGroupId };

            // Setup transfer request reader so that it returns the above request when queried by Id. 
            transferRequestReader
                .Setup(m => m.ReadByIdAsync(transferRequestId, ExpandOptions.WriteProperties))
                .ReturnsAsync(transferRequest);

            // Setup transfer request reader so that it returns that this transfer request is not linked to any other objects. 
            transferRequestReader
                .Setup(m => m.IsLinkedToAnyOtherEntities(transferRequestId))
                .ReturnsAsync(false);

            transferRequestReader
                .Setup(m => m.HasPendingCommands(transferRequestId))
                .ReturnsAsync(false);

            // Invoke Create Transfer Request API.
            await writer.ApproveAsync(transferRequestId, "ETag").ConfigureAwait(false);

            // Verify data written to storage.
            Action<IEnumerable<Entity>> verifyUpdate = x =>
            {
                Assert.Equal(4, x.Count());

                var entity1 = x.Single(y => y.Id == transferRequest.SourceOwnerId);
                var updatingSourceOwner = entity1 as DataOwner;
                Assert.True(updatingSourceOwner.HasInitiatedTransferRequests);

                var entity3 = x.Single(y => y.Id == assetGroupId);
                var updatingAssetGroup = entity3 as AssetGroup;
                Assert.False(updatingAssetGroup.HasPendingTransferRequest);
                Assert.Equal(targetOwnerId, updatingAssetGroup.OwnerId);
            };

            // Verify that CreateTransferRequestAsync gets called with expected values.
            storageWriter.Verify(m => m.UpdateEntitiesAsync(Is.Value(verifyUpdate)), Times.Once);

            // Verify that the request state is deleted.
            Assert.True(transferRequest.IsDeleted);
        }

        [Theory(DisplayName = "When ApproveAsync is called and transfer request etags do not match, then fail."), ValidData]
        public async Task When_ApproveAsyncWithETagMismatch_Then_Fail(
            TransferRequest transferRequest,
            [Frozen] TransferRequest existingTransferRequest,
            TransferRequestWriter writer)
        {
            existingTransferRequest.ETag = "other";

            var exn = await Assert.ThrowsAsync<ETagMismatchException>(() => writer.ApproveAsync(transferRequest.Id, transferRequest.ETag)).ConfigureAwait(false);

            Assert.Equal(transferRequest.ETag, exn.Value);
        }

        #endregion

        #region DeleteAsync

        [Theory(DisplayName = "When DeleteAsync is called with other pending requests, then successful processing."), ValidData]
        public async Task When_DeleteAsyncWithOtherPending_Then_SuccessfulProcessing(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            [Frozen] Mock<IAssetGroupReader> assetGroupReader,
            [Frozen] Mock<IDataOwnerReader> dataOwnerReader,
            [Frozen] Mock<ITransferRequestReader> transferRequestReader,
            TransferRequest transferRequest,
            AssetGroup assetGroup,
            DataOwner sourceOwner,
            DataOwner targetOwner,
            TransferRequestWriter writer)
        {
            // Build source owner object.
            Guid sourceOwnerId = Guid.NewGuid();
            sourceOwner.Id = sourceOwnerId;
            sourceOwner.ETag = "ETag";
            sourceOwner.TrackingDetails = new TrackingDetails();
            sourceOwner.HasInitiatedTransferRequests = true;
            sourceOwner.HasPendingTransferRequests = false;

            // Setup data owner reader so that it returns source owner object when transfer request writer asks it. 
            dataOwnerReader
                .Setup(m => m.ReadByIdAsync(sourceOwnerId, ExpandOptions.WriteProperties))
                .ReturnsAsync(sourceOwner);

            // Build Target owner object.
            Guid targetOwnerId = Guid.NewGuid();
            targetOwner.Id = targetOwnerId;
            targetOwner.ETag = "ETag";
            targetOwner.TrackingDetails = new TrackingDetails();
            targetOwner.HasInitiatedTransferRequests = false;
            targetOwner.HasPendingTransferRequests = true;

            // Setup data owner reader so that it returns target owner object when transfer request writer asks it. 
            dataOwnerReader
                .Setup(m => m.ReadByIdAsync(targetOwnerId, ExpandOptions.WriteProperties))
                .ReturnsAsync(targetOwner);

            // Build asset group object.
            Guid assetGroupId = Guid.NewGuid();
            assetGroup.Id = assetGroupId;
            assetGroup.ETag = "ETag";
            assetGroup.TrackingDetails = new TrackingDetails();
            assetGroup.OwnerId = sourceOwnerId;
            assetGroup.HasPendingTransferRequest = true;

            // Setup asset group reader so that it returns above asset group object when transfer request writer asks it. 
            Action<IEnumerable<Guid>> verify = x => x.SequenceEqual(new[] { assetGroupId });
            assetGroupReader
                .Setup(m => m.ReadByIdsAsync(Is.Value(verify), ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { assetGroup });

            // Setup transfer request reader so that it returns other transfer requests initiated or pending of either owners. 
            FilterResult<TransferRequest> result = new FilterResult<TransferRequest>()
            {
                Count = 2,
                Total = 2
            };

            transferRequestReader
                .Setup(m => m.ReadByFiltersAsync(It.IsAny<IFilterCriteria<TransferRequest>>(), ExpandOptions.WriteProperties))
                .ReturnsAsync(result);

            // Build transfer request object.
            Guid transferRequestId = Guid.NewGuid();
            transferRequest.Id = transferRequestId;
            transferRequest.ETag = "ETag";
            transferRequest.TrackingDetails = new TrackingDetails();
            transferRequest.IsDeleted = false;
            transferRequest.SourceOwnerId = sourceOwnerId;
            transferRequest.TargetOwnerId = targetOwnerId;
            transferRequest.AssetGroups = new[] { assetGroupId };

            // Setup transfer request reader so that it returns the above request when queried by Id. 
            transferRequestReader
                .Setup(m => m.ReadByIdAsync(transferRequestId, ExpandOptions.WriteProperties))
                .ReturnsAsync(transferRequest);

            // Setup transfer request reader so that it returns that this transfer request is not linked to any other objects. 
            transferRequestReader
                .Setup(m => m.IsLinkedToAnyOtherEntities(transferRequestId))
                .ReturnsAsync(false);

            transferRequestReader
                .Setup(m => m.HasPendingCommands(transferRequestId))
                .ReturnsAsync(false);

            // Invoke Create Transfer Request API.
            await writer.DeleteAsync(transferRequestId, "ETag").ConfigureAwait(false);

            // Verify data written to storage.
            Action<IEnumerable<Entity>> verifyUpdate = x =>
            {
                Assert.Equal(4, x.Count());

                var entity1 = x.Single(y => y.Id == transferRequest.SourceOwnerId);
                var updatingSourceOwner = entity1 as DataOwner;
                Assert.True(updatingSourceOwner.HasInitiatedTransferRequests);

                var entity3 = x.Single(y => y.Id == assetGroupId);
                var updatingAssetGroup = entity3 as AssetGroup;
                Assert.False(updatingAssetGroup.HasPendingTransferRequest);
                Assert.Equal(sourceOwnerId, updatingAssetGroup.OwnerId);
            };

            // Verify that CreateTransferRequestAsync gets called with expected values.
            storageWriter.Verify(m => m.UpdateEntitiesAsync(Is.Value(verifyUpdate)), Times.Once);

            // Verify that the request state is deleted.
            Assert.True(transferRequest.IsDeleted);
        }

        #endregion

        public class ValidDataAttribute : AutoMoqDataAttribute
        {
            public ValidDataAttribute(WriteAction action = WriteAction.Create, bool treatAsTransferEditor = true) : base(true)
            {
                var writeSecurityGroups = this.Fixture.Create<IEnumerable<Guid>>();
                var ownerId = this.Fixture.Create<Guid>();
                this.Fixture.Customize<DataOwner>(obj =>
                    obj
                    .With(x => x.WriteSecurityGroups, writeSecurityGroups)
                    .Without(x => x.ServiceTree));

                if (action == WriteAction.Create)
                {
                    this.Fixture.Customize<TransferRequest>(obj =>
                        obj
                        .Without(x => x.Id)
                        .Without(x => x.TrackingDetails)
                        .Without(x => x.ETag)
                        .Without(x => x.RequestState));
                }
                else
                {
                    var id = this.Fixture.Create<Guid>();
                    this.Fixture.Customize<TransferRequest>(obj =>
                        obj
                        .With(x => x.Id, id)
                        .With(x => x.ETag, "ETag"));
                }

                var storageWriterMock = this.Fixture.Create<Mock<IPrivacyDataStorageWriter>>();
                storageWriterMock.Setup(m => m.UpdateEntitiesAsync(It.IsAny<IEnumerable<Entity>>())).Returns<IEnumerable<Entity>>(v => Task.FromResult(v));
                this.Fixture.Inject(storageWriterMock);

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
