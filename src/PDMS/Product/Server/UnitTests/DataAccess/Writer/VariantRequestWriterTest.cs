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
    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;
    using Microsoft.PrivacyServices.Identity;
    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;

    public class VariantRequestWriterTest
    {
        #region CreateAsync
        [Theory(DisplayName = "When CreateAsync is called and asset group has no pending variant requests, then the correct storage layer is called and returned."), ValidData]
        public async Task When_CreateAsync_Then_CallAndReturnStorageValue(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            [Frozen] Mock<IAssetGroupReader> assetGroupReader,
            [Frozen] Mock<IVariantRequestReader> variantRequestReader,
            AssetGroup assetGroup,
            AssetQualifier assetQualifier,
            TrackingDetails trackingDetails,
            VariantRequest variantRequest,
            [Frozen] VariantRequest storageVariantRequest,
            VariantRequestWriter writer)
        {
            assetGroup.Id = Guid.NewGuid();
            assetGroup.Qualifier = assetQualifier;
            assetGroup.TrackingDetails = trackingDetails;
            assetGroup.HasPendingVariantRequests = false;

            variantRequest.VariantRelationships = new Dictionary<Guid, VariantRelationship>();
            variantRequest.VariantRelationships.Add(assetGroup.Id, new VariantRelationship { AssetGroupId = assetGroup.Id });

            assetGroupReader.Setup(m => m.ReadByIdsAsync(variantRequest.VariantRelationships.Keys, ExpandOptions.WriteProperties)).ReturnsAsync(new[] { assetGroup });

            Action<VariantRequestFilterCriteria> filterVerify = x =>
            {
                Assert.Equal(assetGroup.Id, x.AssetGroupId);
            };
            variantRequestReader.Setup(m => m.ReadByFiltersAsync(Is.Value(filterVerify), ExpandOptions.None)).ReturnsAsync(new FilterResult<VariantRequest> { Total = 0 });

            var result = await writer.CreateAsync(variantRequest).ConfigureAwait(false);

            Assert.Equal(storageVariantRequest, result);

            storageWriter.Verify(m => m.UpsertVariantRequestWithSideEffectsAsync(variantRequest, It.IsAny<IEnumerable<AssetGroup>>()), Times.Once);
        }

        [Theory(DisplayName = "When CreateAsync is called and asset group has pending variant request, then the correct storage layer is called and returned."), ValidData]
        public async Task When_CreateAsync_And_AssetGroupHasPendingRequest_Then_CallAndReturnStorageValue(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            [Frozen] Mock<IAssetGroupReader> assetGroupReader,
            [Frozen] Mock<IVariantRequestReader> variantRequestReader,
            AssetGroup assetGroup,
            AssetQualifier assetQualifier,
            TrackingDetails trackingDetails,
            VariantRequest variantRequest,
            [Frozen] VariantRequest storageVariantRequest,
            VariantRequestWriter writer)
        {
            assetGroup.Id = Guid.NewGuid();
            assetGroup.Qualifier = assetQualifier;
            assetGroup.TrackingDetails = trackingDetails;
            assetGroup.HasPendingVariantRequests = true;

            variantRequest.VariantRelationships = new Dictionary<Guid, VariantRelationship>();
            variantRequest.VariantRelationships.Add(assetGroup.Id, new VariantRelationship { AssetGroupId = assetGroup.Id });

            assetGroupReader.Setup(m => m.ReadByIdsAsync(variantRequest.VariantRelationships.Keys, ExpandOptions.WriteProperties)).ReturnsAsync(new[] { assetGroup });

            Action<VariantRequestFilterCriteria> filterVerify = x =>
            {
                Assert.Equal(assetGroup.Id, x.AssetGroupId);
            };
            variantRequestReader.Setup(m => m.ReadByFiltersAsync(Is.Value(filterVerify), ExpandOptions.None)).ReturnsAsync(new FilterResult<VariantRequest> { Total = 0 });

            var result = await writer.CreateAsync(variantRequest).ConfigureAwait(false);

            Assert.Equal(storageVariantRequest, result);

            storageWriter.Verify(m => m.CreateVariantRequestAsync(variantRequest), Times.Once);
        }

        [Theory(DisplayName = "When CreateAsync is called as ServiceEditor but not VariantEditor, then the request meta data is populated correctly."), ValidData(WriteAction.Create, treatAsVariantEditor: false)]
        public async Task When_CreateAsyncAsServiceEditor_Then_PopulateRequestMetaDataCorrectly(
            [Frozen] Mock<IAssetGroupReader> assetGroupReader,
            [Frozen] Mock<IVariantRequestReader> variantRequestReader,
            [Frozen] DataOwner dataOwner,
            AssetGroup assetGroup,
            AssetQualifier assetQualifier,
            TrackingDetails trackingDetails,
            VariantRequest variantRequest,
            VariantRequestWriter writer)
        {
            assetGroup.Id = Guid.NewGuid();
            assetGroup.Qualifier = assetQualifier;
            assetGroup.TrackingDetails = trackingDetails;
            assetGroup.HasPendingVariantRequests = false;

            variantRequest.VariantRelationships = new Dictionary<Guid, VariantRelationship>();
            variantRequest.VariantRelationships.Add(assetGroup.Id, new VariantRelationship { AssetGroupId = assetGroup.Id });

            assetGroupReader.Setup(m => m.ReadByIdsAsync(variantRequest.VariantRelationships.Keys, ExpandOptions.WriteProperties)).ReturnsAsync(new[] { assetGroup });

            Action<VariantRequestFilterCriteria> filterVerify = x =>
            {
                Assert.Equal(assetGroup.Id, x.AssetGroupId);
            };
            variantRequestReader.Setup(m => m.ReadByFiltersAsync(Is.Value(filterVerify), ExpandOptions.None)).ReturnsAsync(new FilterResult<VariantRequest> { Total = 0 });

            var result = await writer.CreateAsync(variantRequest).ConfigureAwait(false);

            Assert.Equal(dataOwner.Name, variantRequest.OwnerName);
            Assert.Equal(assetQualifier.Value, variantRequest.VariantRelationships[assetGroup.Id].AssetQualifier.Value);
        }

        [Theory(DisplayName = "When CreateAsync is called as VariantEditor but not ServicEditor, then the request meta data is populated correctly."), ValidData(WriteAction.Create, treatAsVariantEditor: true)]
        public async Task When_CreateAsyncAsVariantEditor_Then_PopulateRequestMetaDataCorrectly(
            [Frozen] Mock<IAssetGroupReader> assetGroupReader,
            [Frozen] Mock<IVariantRequestReader> variantRequestReader,
            IEnumerable<Guid> securityGroups,
            [Frozen] DataOwner dataOwner,
            AssetGroup assetGroup,
            AssetQualifier assetQualifier,
            TrackingDetails trackingDetails,
            VariantRequest variantRequest,
            VariantRequestWriter writer)
        {
            assetGroup.Id = Guid.NewGuid();
            assetGroup.Qualifier = assetQualifier;
            assetGroup.TrackingDetails = trackingDetails;
            assetGroup.HasPendingVariantRequests = false;

            variantRequest.VariantRelationships = new Dictionary<Guid, VariantRelationship>();
            variantRequest.VariantRelationships.Add(assetGroup.Id, new VariantRelationship { AssetGroupId = assetGroup.Id });

            assetGroupReader.Setup(m => m.ReadByIdsAsync(variantRequest.VariantRelationships.Keys, ExpandOptions.WriteProperties)).ReturnsAsync(new[] { assetGroup });

            Action<VariantRequestFilterCriteria> filterVerify = x =>
            {
                Assert.Equal(assetGroup.Id, x.AssetGroupId);
            };
            variantRequestReader.Setup(m => m.ReadByFiltersAsync(Is.Value(filterVerify), ExpandOptions.None)).ReturnsAsync(new FilterResult<VariantRequest> { Total = 0 });

            dataOwner.WriteSecurityGroups = securityGroups;

            var result = await writer.CreateAsync(variantRequest).ConfigureAwait(false);

            Assert.Equal(dataOwner.Name, variantRequest.OwnerName);
            Assert.Equal(assetQualifier.Value, variantRequest.VariantRelationships[assetGroup.Id].AssetQualifier.Value);
        }

        [Theory(DisplayName = "When CreateAsync is called as VariantEditor and ServicEditor, then the request meta data is populated correctly."), ValidData(WriteAction.Create, treatAsVariantEditor: true)]
        public async Task When_CreateAsyncAsVariantEditorAndServiceEditor_Then_PopulateRequestMetaDataCorrectly(
            [Frozen] Mock<IAssetGroupReader> assetGroupReader,
            [Frozen] Mock<IVariantRequestReader> variantRequestReader,
            [Frozen] DataOwner dataOwner,
            AssetGroup assetGroup,
            AssetQualifier assetQualifier,
            TrackingDetails trackingDetails,
            VariantRequest variantRequest,
            VariantRequestWriter writer)
        {
            assetGroup.Id = Guid.NewGuid();
            assetGroup.Qualifier = assetQualifier;
            assetGroup.TrackingDetails = trackingDetails;
            assetGroup.HasPendingVariantRequests = false;

            variantRequest.VariantRelationships = new Dictionary<Guid, VariantRelationship>();
            variantRequest.VariantRelationships.Add(assetGroup.Id, new VariantRelationship { AssetGroupId = assetGroup.Id });

            assetGroupReader.Setup(m => m.ReadByIdsAsync(variantRequest.VariantRelationships.Keys, ExpandOptions.WriteProperties)).ReturnsAsync(new[] { assetGroup });

            Action<VariantRequestFilterCriteria> filterVerify = x =>
            {
                Assert.Equal(assetGroup.Id, x.AssetGroupId);
            };
            variantRequestReader.Setup(m => m.ReadByFiltersAsync(Is.Value(filterVerify), ExpandOptions.None)).ReturnsAsync(new FilterResult<VariantRequest> { Total = 0 });

            var result = await writer.CreateAsync(variantRequest).ConfigureAwait(false);

            Assert.Equal(dataOwner.Name, variantRequest.OwnerName);
            Assert.Equal(assetQualifier.Value, variantRequest.VariantRelationships[assetGroup.Id].AssetQualifier.Value);
        }

        [Theory(DisplayName = "When CreateAsync is called without VariantEditor or ServiceEditor role, then fail."), ValidData(WriteAction.Create, treatAsVariantEditor: false)]
        public async Task When_CreateAsyncIsCalledWithoutVariantEditorOrServiceEditor_Then_Fail(
            [Frozen] Mock<IAssetGroupReader> assetGroupReader,
            [Frozen] Mock<IVariantRequestReader> variantRequestReader,
            IEnumerable<Guid> securityGroups,
            [Frozen] DataOwner dataOwner,
            [Frozen] AuthenticatedPrincipal authenticatedPrincipal,
            AssetGroup assetGroup,
            AssetQualifier assetQualifier,
            TrackingDetails trackingDetails,
            VariantRequest variantRequest,
            VariantRequestWriter writer)
        {
            assetGroup.Id = Guid.NewGuid();
            assetGroup.Qualifier = assetQualifier;
            assetGroup.TrackingDetails = trackingDetails;
            assetGroup.HasPendingVariantRequests = false;

            variantRequest.VariantRelationships = new Dictionary<Guid, VariantRelationship>();
            variantRequest.VariantRelationships.Add(assetGroup.Id, new VariantRelationship { AssetGroupId = assetGroup.Id });

            assetGroupReader.Setup(m => m.ReadByIdsAsync(variantRequest.VariantRelationships.Keys, ExpandOptions.WriteProperties)).ReturnsAsync(new[] { assetGroup });

            Action<VariantRequestFilterCriteria> filterVerify = x =>
            {
                Assert.Equal(assetGroup.Id, x.AssetGroupId);
            };
            variantRequestReader.Setup(m => m.ReadByFiltersAsync(Is.Value(filterVerify), ExpandOptions.None)).ReturnsAsync(new FilterResult<VariantRequest> { Total = 0 });

            dataOwner.WriteSecurityGroups = securityGroups;

            var exn = await Assert.ThrowsAsync<SecurityGroupMissingWritePermissionException>(() => writer.CreateAsync(variantRequest)).ConfigureAwait(false);

            Assert.Equal(authenticatedPrincipal.UserAlias, exn.UserName);
            Assert.Equal(AuthorizationRole.ServiceEditor.ToString(), exn.Role);
        }

        [Theory(DisplayName = "When CreateAsync is called with owner id not set, then fail."), ValidData]
        public async Task When_CreateAsyncCalledWithoutOwnerId_Then_Fail(
            VariantRequest variantRequest,
            VariantRequestWriter writer)
        {
            variantRequest.OwnerId = Guid.Empty;

            var exn = await Assert.ThrowsAsync<MissingPropertyException>(() => writer.CreateAsync(variantRequest)).ConfigureAwait(false);

            Assert.Equal("ownerId", exn.ParamName);
        }

        [Theory(DisplayName = "When CreateAsync is called with requested variants not set, then fail."), ValidData]
        public async Task When_CreateAsyncCalledWithoutVariantRequests_Then_Fail(
            VariantRequest variantRequest,
            VariantRequestWriter writer)
        {
            variantRequest.RequestedVariants = null;

            var exn = await Assert.ThrowsAsync<MissingPropertyException>(() => writer.CreateAsync(variantRequest)).ConfigureAwait(false);

            Assert.Equal("requestedVariants", exn.ParamName);
        }

        [Theory(DisplayName = "When CreateAsync is called with requested variants empty, then fail."), ValidData]
        public async Task When_CreateAsyncCalledWithVariantRequestsEmpty_Then_Fail(
            VariantRequest variantRequest,
            VariantRequestWriter writer)
        {
            variantRequest.RequestedVariants = Enumerable.Empty<AssetGroupVariant>();

            var exn = await Assert.ThrowsAsync<MissingPropertyException>(() => writer.CreateAsync(variantRequest)).ConfigureAwait(false);

            Assert.Equal("requestedVariants", exn.ParamName);
        }

        [Theory(DisplayName = "When CreateAsync is called with variant relationships not set, then fail."), ValidData]
        public async Task When_CreateAsyncCalledWithoutVariantRelationships_Then_Fail(
            VariantRequest variantRequest,
            VariantRequestWriter writer)
        {
            variantRequest.VariantRelationships = null;

            var exn = await Assert.ThrowsAsync<MissingPropertyException>(() => writer.CreateAsync(variantRequest)).ConfigureAwait(false);

            Assert.Equal("variantRelationships", exn.ParamName);
        }

        [Theory(DisplayName = "When CreateAsync is called with variant relationships empty, then fail."), ValidData]
        public async Task When_CreateAsyncCalledWithVariantRelationshipsEmpty_Then_Fail(
            VariantRequest variantRequest,
            VariantRequestWriter writer)
        {
            variantRequest.VariantRelationships = new Dictionary<Guid, VariantRelationship>();

            var exn = await Assert.ThrowsAsync<MissingPropertyException>(() => writer.CreateAsync(variantRequest)).ConfigureAwait(false);

            Assert.Equal("variantRelationships", exn.ParamName);
        }

        [Theory(DisplayName = "When CreateAsync is called with variants containing duplicate values, then fail."), ValidData]
        public async Task When_CreateAsyncCalledWithVariantsContainingDuplicateValues_Then_Fail(
            VariantRequest variantRequest,
            AssetGroupVariant assetGroupVariant1,
            AssetGroupVariant assetGroupVariant2,
            VariantRequestWriter writer)
        {
            var variantId = Guid.NewGuid();
            assetGroupVariant1.VariantId = variantId;
            assetGroupVariant2.VariantId = variantId;

            variantRequest.RequestedVariants = new[] { assetGroupVariant1, assetGroupVariant2 };

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(variantRequest)).ConfigureAwait(false);

            Assert.Equal("requestedVariants", exn.ParamName);
        }

        [Theory(DisplayName = "When CreateAsync is called with empty asset group id variant relationships, then fail."), ValidData]
        public async Task When_CreateAsyncCalledWithEmptyAssetGroupIdVariantRelationships_Then_Fail(
            VariantRequest variantRequest,
            VariantRelationship variantRelationship,
            VariantRequestWriter writer)
        {
            variantRelationship.AssetGroupId = Guid.Empty;
            variantRequest.VariantRelationships.Add(variantRelationship.AssetGroupId, variantRelationship);

            var exn = await Assert.ThrowsAsync<MissingPropertyException>(() => writer.CreateAsync(variantRequest)).ConfigureAwait(false);

            Assert.Equal("variantRelationships.assetGroupId", exn.ParamName);
        }

        [Theory(DisplayName = "When CreateAsync is called with an owner id that does not exist, then fail."), ValidData]
        public async Task When_CreateAsyncWithUnknownOwner_Then_Fail(
            [Frozen] Mock<IDataOwnerReader> dataOwnerReader,
            VariantRequest variantRequest,
            VariantRequestWriter writer)
        {
            dataOwnerReader.Setup(m => m.ReadByIdAsync(variantRequest.OwnerId, ExpandOptions.None)).ReturnsAsync(null as DataOwner);

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.CreateAsync(variantRequest)).ConfigureAwait(false);

            Assert.Equal("ownerId", exn.Target);
            Assert.Equal(ConflictType.DoesNotExist, exn.ConflictType);
        }

        [Theory(DisplayName = "When CreateAsync is called with a variant id that does not exist, then fail."), ValidData]
        public async Task When_CreateAsyncWithUnknownVariant_Then_Fail(
            VariantRequest variantRequest,
            AssetGroupVariant assetGroupVariant,
            VariantRequestWriter writer)
        {
            variantRequest.RequestedVariants = variantRequest.RequestedVariants.Concat(new[] { assetGroupVariant });

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.CreateAsync(variantRequest)).ConfigureAwait(false);

            Assert.Equal("requestedVariants", exn.Target);
            Assert.Equal(ConflictType.DoesNotExist, exn.ConflictType);
        }

        [Theory(DisplayName = "When CreateAsync is called with an asset group id that does not exist, then fail."), ValidData]
        public async Task When_CreateAsyncWithUnknownAssetGroup_Then_Fail(
            VariantRequest variantRequest,
            Guid nonExistingAssetGroupId,
            VariantRequestWriter writer)
        {
            variantRequest.VariantRelationships.Add(nonExistingAssetGroupId, new VariantRelationship { AssetGroupId = nonExistingAssetGroupId });

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.CreateAsync(variantRequest)).ConfigureAwait(false);

            Assert.Equal($"variantRelationships[{nonExistingAssetGroupId.ToString()}].assetGroup", exn.Target);
            Assert.Equal(ConflictType.DoesNotExist, exn.ConflictType);
        }

        [Theory(DisplayName = "When CreateAsync is called with asset groups having empty owners, then fail."), ValidData]
        public async Task When_CreateAsyncCalledWithAssetGroupsHavingEmptyOwners_Then_Fail(
            VariantRequest variantRequest,
            [Frozen] IEnumerable<AssetGroup> assetGroups,
            VariantRequestWriter writer)
        {
            assetGroups.First().OwnerId = Guid.Empty;

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.CreateAsync(variantRequest)).ConfigureAwait(false);

            Assert.Equal($"variantRelationships[{variantRequest.VariantRelationships.Keys.First().ToString()}].assetGroup.ownerId", exn.Target);
            Assert.Equal(ConflictType.DoesNotExist, exn.ConflictType);
        }

        [Theory(DisplayName = "When CreateAsync is called with asset groups having different owners as the request, then fail."), ValidData]
        public async Task When_CreateAsyncCalledWithAssetGroupsHavingDifferentOwnerAsRequest_Then_Fail(
            VariantRequest variantRequest,
            Guid ownerId,
            VariantRequestWriter writer)
        {
            variantRequest.OwnerId = ownerId;

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.CreateAsync(variantRequest)).ConfigureAwait(false);

            Assert.Equal($"variantRelationships[{variantRequest.VariantRelationships.Keys.First().ToString()}].assetGroup.ownerId", exn.Target);
            Assert.Equal(ConflictType.InvalidValue, exn.ConflictType);
        }

        #endregion

        #region UpdateAsync
        [Theory(DisplayName = "When UpdateAsync is called, then the storage layer is called and returned."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsync_Then_CallAndReturnStorageValue(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            VariantRequest variantRequest,
            [Frozen] VariantRequest existingVariantRequest,
            VariantRequestWriter writer,
            TrackingDetails trackingDetails)
        {
            existingVariantRequest.TrackingDetails = trackingDetails;

            var result = await writer.UpdateAsync(variantRequest).ConfigureAwait(false);

            Assert.Equal(existingVariantRequest, result);

            storageWriter.Verify(m => m.UpdateVariantRequestAsync(existingVariantRequest), Times.Once);
        }

        [Theory(DisplayName = "When UpdateAsync is called, and not VariantEditor, then fail."), ValidData(WriteAction.Update, false)]
        public async Task When_UpdateAsync_Then_CallWhenNotVariantEditorThenFail(
            VariantRequest variantRequest,
            [Frozen] VariantRequest existingVariantRequest,
            VariantRequestWriter writer,
            TrackingDetails trackingDetails)
        {
            existingVariantRequest.TrackingDetails = trackingDetails;

            var exn = await Assert.ThrowsAsync<MissingWritePermissionException>(() => writer.UpdateAsync(variantRequest)).ConfigureAwait(false);

            Assert.Equal("User does not have write permissions.", exn.Message);
        }

        [Theory(DisplayName = "When UpdateAsync is called with a different owner id, then fail."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithChangedOwnerId_Then_Fail(
            VariantRequest variantRequest,
            [Frozen] VariantRequest existingVariantRequest,
            VariantRequestWriter writer)
        {
            existingVariantRequest.OwnerId = Guid.NewGuid();

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.UpdateAsync(variantRequest)).ConfigureAwait(false);

            Assert.Equal("ownerId", exn.Target);
            Assert.Equal(ConflictType.InvalidValue_Immutable, exn.ConflictType);
        }

        [Theory(DisplayName = "When UpdateAsync is called with a different owner name, then fail."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithChangedOwnerName_Then_Fail(
            VariantRequest variantRequest,
            [Frozen] VariantRequest existingVariantRequest,
            string ownerName,
            VariantRequestWriter writer)
        {
            existingVariantRequest.OwnerName = ownerName;

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.UpdateAsync(variantRequest)).ConfigureAwait(false);

            Assert.Equal("ownerName", exn.Target);
            Assert.Equal(ConflictType.InvalidValue_Immutable, exn.ConflictType);
        }

        [Theory(DisplayName = "When UpdateAsync without changing the variants, then skip variant existence checking."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncNotChangingTheVariants_Then_SkipVariantExistenceChecking(
            [Frozen] Mock<IVariantDefinitionReader> entityReader,
            VariantRequest variantRequest,
            [Frozen] VariantRequest coreVariantRequest,
            VariantRequestWriter writer,
            IEnumerable<AssetGroupVariant> requestedVariants,
            TrackingDetails trackingDetails)
        {
            coreVariantRequest.TrackingDetails = trackingDetails;

            variantRequest.RequestedVariants = requestedVariants;
            coreVariantRequest.RequestedVariants = requestedVariants;

            await writer.UpdateAsync(variantRequest).ConfigureAwait(false);

            entityReader.Verify(m => m.ReadByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<ExpandOptions>()), Times.Never);
        }

        [Theory(DisplayName = "When UpdateAsync is called with a variant that is changed, then fail."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithChangedVariant_Then_Fail(
            VariantRequest variantRequest,
            AssetGroupVariant assetGroupVariant,
            VariantRequestWriter writer)
        {
            variantRequest.RequestedVariants =  new List<AssetGroupVariant>() { assetGroupVariant };

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.UpdateAsync(variantRequest)).ConfigureAwait(false);

            Assert.Equal("requestedVariants", exn.Target);
            Assert.Equal(ConflictType.InvalidValue_Immutable, exn.ConflictType);
        }

        [Theory(DisplayName = "When UpdateAsync is called with an additional variant id, then fail."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithAdditionalVariant_Then_Fail(
            VariantRequest variantRequest,
            AssetGroupVariant assetGroupVariant,
            VariantRequestWriter writer)
        {
            variantRequest.RequestedVariants = variantRequest.RequestedVariants.Concat(new[] { assetGroupVariant });

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.UpdateAsync(variantRequest)).ConfigureAwait(false);

            Assert.Equal("requestedVariants", exn.Target);
            Assert.Equal(ConflictType.InvalidValue_Immutable, exn.ConflictType);
        }

        [Theory(DisplayName = "When UpdateAsync without changing the variant relations, then skip asset group existence checking."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncNotChangingTheVariantRelations_Then_SkipAssetGroupExistenceChecking(
            [Frozen] Mock<IAssetGroupReader> entityReader,
            VariantRequest variantRequest,
            [Frozen] VariantRequest coreVariantRequest,
            VariantRequestWriter writer,
            IDictionary<Guid, VariantRelationship> variantRelationships,
            TrackingDetails trackingDetails)
        {
            coreVariantRequest.TrackingDetails = trackingDetails;

            variantRequest.VariantRelationships = variantRelationships;
            coreVariantRequest.VariantRelationships = variantRelationships;

            await writer.UpdateAsync(variantRequest).ConfigureAwait(false);

            entityReader.Verify(m => m.ReadByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<ExpandOptions>()), Times.Never);
        }

        [Theory(DisplayName = "When UpdateAsync is called with a changed asset group id, then fail."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithChangedAssetGroup_Then_Fail(
            VariantRequest variantRequest,
            Guid nonExistingAssetGroupId,
            VariantRequestWriter writer)
        {
            VariantRelationship variantRelationship = new VariantRelationship() { AssetGroupId = nonExistingAssetGroupId };
            variantRequest.VariantRelationships = new Dictionary<Guid, VariantRelationship>()
            {
                { nonExistingAssetGroupId, variantRelationship }
            };

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.UpdateAsync(variantRequest)).ConfigureAwait(false);

            Assert.Equal("variantRelationships", exn.Target);
            Assert.Equal(ConflictType.InvalidValue_Immutable, exn.ConflictType);
        }

        [Theory(DisplayName = "When UpdateAsync is called with an additional asset group id, then fail."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithAnAdditionalAssetGroup_Then_Fail(
            VariantRequest variantRequest,
            Guid nonExistingAssetGroupId,
            VariantRequestWriter writer)
        {
            variantRequest.VariantRelationships = new Dictionary<Guid, VariantRelationship>();
            variantRequest.VariantRelationships.Add(nonExistingAssetGroupId, new VariantRelationship { AssetGroupId = nonExistingAssetGroupId });

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.UpdateAsync(variantRequest)).ConfigureAwait(false);

            Assert.Equal("variantRelationships", exn.Target);
            Assert.Equal(ConflictType.InvalidValue_Immutable, exn.ConflictType);
        }
        #endregion

        #region DeleteAsync
        [Theory(DisplayName = "When DeleteAsync is called and variant request not found, then fail."), ValidData(WriteAction.SoftDelete)]
        public async Task When_DeleteAsyncWithMissingRequest_Then_Fail(
            VariantRequest variantRequest,
            [Frozen] Mock<IVariantRequestReader> reader,
            VariantRequestWriter writer)
        {
            reader.Setup(m => m.IsLinkedToAnyOtherEntities(variantRequest.Id)).ReturnsAsync(false);
            reader.Setup(m => m.ReadByIdAsync(variantRequest.Id, ExpandOptions.WriteProperties)).ReturnsAsync(null as VariantRequest);

            var exn = await Assert.ThrowsAsync<EntityNotFoundException>(() => writer.DeleteAsync(variantRequest.Id, variantRequest.ETag)).ConfigureAwait(false);

            Assert.Equal(variantRequest.Id, exn.Id);
            Assert.Equal("VariantRequest", exn.EntityType);
        }

        [Theory(DisplayName = "When DeleteAsync is called and variant request etags do not match, then fail."), ValidData(WriteAction.SoftDelete)]
        public async Task When_DeleteAsyncWithETagMismatch_Then_Fail(
            VariantRequest variantRequest,
            [Frozen] VariantRequest existingVariantRequest,
            [Frozen] Mock<IVariantRequestReader> variantRequestReader,
            VariantRequestWriter writer)
        {
            variantRequestReader.Setup(m => m.IsLinkedToAnyOtherEntities(variantRequest.Id)).ReturnsAsync(false);
            existingVariantRequest.ETag = "other";

            var exn = await Assert.ThrowsAsync<ETagMismatchException>(() => writer.DeleteAsync(variantRequest.Id, variantRequest.ETag)).ConfigureAwait(false);

            Assert.Equal(variantRequest.ETag, exn.Value);
        }

        [Theory(DisplayName = "When DeleteAsync is called, then update tracking details for the variant request."), ValidData(WriteAction.SoftDelete)]
        public async Task When_DeleteAsync_Then_UpdateRequestTrackingDetails(
            [Frozen] AuthenticatedPrincipal principal,
            [Frozen] VariantRequest variantRequest,
            [Frozen] Mock<IVariantRequestReader> variantRequestReader,
            TrackingDetails trackingDetails,
            VariantRequestWriter writer)
        {
            variantRequest.TrackingDetails = trackingDetails;
            variantRequestReader.Setup(m => m.IsLinkedToAnyOtherEntities(variantRequest.Id)).ReturnsAsync(false);

            await writer.DeleteAsync(variantRequest.Id, variantRequest.ETag).ConfigureAwait(false);

            Assert.Equal(principal.UserId, variantRequest.TrackingDetails.UpdatedBy);
        }

        [Theory(DisplayName = "When DeleteAsync is called, then soft delete the request."), ValidData(WriteAction.SoftDelete)]
        public async Task When_DeleteAsync_Then_SoftDeleteRequest(
            [Frozen] VariantRequest variantRequest,
            [Frozen] Mock<IVariantRequestReader> variantRequestReader,
            TrackingDetails trackingDetails,
            VariantRequestWriter writer)
        {
            variantRequest.TrackingDetails = trackingDetails;
            variantRequestReader.Setup(m => m.IsLinkedToAnyOtherEntities(variantRequest.Id)).ReturnsAsync(false);
            variantRequest.IsDeleted = false;

            await writer.DeleteAsync(variantRequest.Id, variantRequest.ETag).ConfigureAwait(false);

            Assert.True(variantRequest.IsDeleted);
        }

        [Theory(DisplayName = "When DeleteAsync is called, then update asset group HasPendingVariantRequests flag and tracking details."), ValidData(WriteAction.SoftDelete)]
        public async Task When_DeleteAsync_Then_UpdateAssetGroupHasPendingVariantRequestsFlag(
            [Frozen] VariantRequest variantRequest,
            VariantRelationship relationship,
            TrackingDetails variantRequestTrackingDetails,
            AssetGroup assetGroup,
            TrackingDetails assetGroupTrackingDetails,
            [Frozen] Mock<IVariantRequestReader> variantRequestReader,
            [Frozen] Mock<IAssetGroupReader> assetGroupReader,
            VariantRequestWriter writer)
        {
            variantRequestReader.Setup(m => m.IsLinkedToAnyOtherEntities(variantRequest.Id)).ReturnsAsync(false);
            variantRequestReader.Setup(m => m.HasPendingCommands(variantRequest.Id)).ReturnsAsync(false);

            variantRequest.TrackingDetails = variantRequestTrackingDetails;
            variantRequest.VariantRelationships.Clear();
            variantRequest.VariantRelationships[assetGroup.Id] = relationship;
            relationship.AssetGroupId = assetGroup.Id;

            assetGroup.TrackingDetails = assetGroupTrackingDetails;
            
            Action<IEnumerable<Guid>> verify = x => x.SequenceEqual(new[] { assetGroup.Id });
            assetGroupReader.Setup(m => m.ReadByIdsAsync(Is.Value(verify), ExpandOptions.WriteProperties)).ReturnsAsync(new[] { assetGroup });

            Action<VariantRequestFilterCriteria> filterVerify = x =>
            {
                Assert.Equal(assetGroup.Id, x.AssetGroupId);
            };

            variantRequestReader.Setup(m => m.ReadByFiltersAsync(Is.Value(filterVerify), ExpandOptions.None)).ReturnsAsync(new FilterResult<VariantRequest> { Total = 1 });

            await writer.DeleteAsync(variantRequest.Id, variantRequest.ETag).ConfigureAwait(false);

            Assert.False(assetGroup.HasPendingVariantRequests);
            Assert.Equal(2, assetGroup.TrackingDetails.Version);
        }

        [Theory(DisplayName = "When DeleteAsync is called, then save all entities in batch."), ValidData(WriteAction.SoftDelete)]
        public async Task When_DeleteAsync_Then_StoreUpdatedEntities(
            [Frozen] VariantRequest variantRequest,
            TrackingDetails trackingDetails,
            [Frozen] IEnumerable<AssetGroup> assetGroups,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            [Frozen] Mock<IVariantRequestReader> variantRequestReader,
            VariantRequestWriter writer)
        {
            variantRequest.TrackingDetails = trackingDetails;
            variantRequestReader.Setup(m => m.IsLinkedToAnyOtherEntities(variantRequest.Id)).ReturnsAsync(false);
            variantRequestReader.Setup(m => m.ReadByFiltersAsync(It.IsAny<VariantRequestFilterCriteria>(), ExpandOptions.None)).ReturnsAsync(new FilterResult<VariantRequest> { Total = 1 });

            await writer.DeleteAsync(variantRequest.Id, variantRequest.ETag).ConfigureAwait(false);

            // The frozen values are the ones returned by the storage read calls.
            // All of those should be updated and then passed back to storage for saving.
            Action<IEnumerable<Entity>> verify = x =>
            {
                x.Where(y => y is AssetGroup).SortedSequenceAssert(assetGroups, y => y.Id, Assert.Equal);
                Assert.True(x.Contains(variantRequest), "Missing variant request");
            };

            storageWriter.Verify(m => m.UpdateEntitiesAsync(Is.Value(verify)), Times.Once);
        }

        [Theory(DisplayName = "When DeleteAsync is called with only VariantEditor privilege, then succeeds."), ValidData(WriteAction.SoftDelete)]
        public async Task When_DeleteAsyncWithOnlyVariantEditorPrivilege_Then_Succeeds(
            IEnumerable<Guid> securityGroups,
            [Frozen] DataOwner existingOwner,
            [Frozen] AuthenticatedPrincipal principal,
            [Frozen] VariantRequest variantRequest,
            [Frozen] Mock<IVariantRequestReader> variantRequestReader,
            TrackingDetails trackingDetails,
            VariantRequestWriter writer)
        {
            // A tempory test, we might need to revisit this test and update it if the logic is changed in the future.
            existingOwner.WriteSecurityGroups = securityGroups;
            variantRequest.TrackingDetails = trackingDetails;
            variantRequestReader.Setup(m => m.IsLinkedToAnyOtherEntities(variantRequest.Id)).ReturnsAsync(false);
            variantRequestReader.Setup(m => m.HasPendingCommands(variantRequest.Id)).ReturnsAsync(false);

            await writer.DeleteAsync(variantRequest.Id, variantRequest.ETag).ConfigureAwait(false);

            Assert.Equal(principal.UserId, variantRequest.TrackingDetails.UpdatedBy);
        }

        [Theory(DisplayName = "When DeleteAsync is called without VariantEditor role or ServiceEditor role, then fail."), ValidData(WriteAction.SoftDelete, treatAsVariantEditor: false)]
        public async Task When_DeleteAsyncWithoutVariantEditorRole_Then_Fail(
            IEnumerable<Guid> securityGroups,
            [Frozen] DataOwner existingOwner,
            [Frozen] AuthenticatedPrincipal authenticatedPrincipal,
            VariantRequest variantRequest,
            VariantRequestWriter writer)
        {
            // A tempory test as well.
            existingOwner.WriteSecurityGroups = securityGroups;

            var exn = await Assert.ThrowsAsync<SecurityGroupMissingWritePermissionException>(() => writer.DeleteAsync(variantRequest.Id, variantRequest.ETag)).ConfigureAwait(false);

            Assert.Equal(authenticatedPrincipal.UserAlias, exn.UserName);
            Assert.Equal(AuthorizationRole.ServiceEditor.ToString(), exn.Role);
        }
        #endregion

        #region ApproveAsync
        [Theory(DisplayName = "When ApproveAsync is called and variant request not found, then fail."), ValidData(WriteAction.Update)]
        public async Task When_ApproveAsyncWithMissingRequest_Then_Fail(
            VariantRequest variantRequest,
            [Frozen] Mock<IVariantRequestReader> reader,
            VariantRequestWriter writer)
        {
            reader.Setup(m => m.ReadByIdAsync(variantRequest.Id, ExpandOptions.WriteProperties)).ReturnsAsync(null as VariantRequest);

            var exn = await Assert.ThrowsAsync<EntityNotFoundException>(() => writer.ApproveAsync(variantRequest.Id, variantRequest.ETag)).ConfigureAwait(false);

            Assert.Equal(variantRequest.Id, exn.Id);
            Assert.Equal("VariantRequest", exn.EntityType);
        }

        [Theory(DisplayName = "When ApproveAsync is called and variant request etags do not match, then fail."), ValidData(WriteAction.Update)]
        public async Task When_ApproveAsyncWithETagMismatch_Then_Fail(
            VariantRequest variantRequest,
            [Frozen] VariantRequest existingVariantRequest,
            VariantRequestWriter writer)
        {
            existingVariantRequest.ETag = "other";

            var exn = await Assert.ThrowsAsync<ETagMismatchException>(() => writer.ApproveAsync(variantRequest.Id, variantRequest.ETag)).ConfigureAwait(false);

            Assert.Equal(variantRequest.ETag, exn.Value);
        }

        [Theory(DisplayName = "When ApproveAsync is called without VariantEditor role, then fail."), ValidData(WriteAction.Update, treatAsVariantEditor: false)]
        public async Task When_ApproveAsyncWithoutVariantEditorRole_Then_Fail(
            [Frozen] AuthenticatedPrincipal authenticatedPrincipal,
            VariantRequest variantRequest,
            VariantRequestWriter writer)
        {
            var exn = await Assert.ThrowsAsync<MissingWritePermissionException>(() => writer.ApproveAsync(variantRequest.Id, variantRequest.ETag)).ConfigureAwait(false);

            Assert.Equal(authenticatedPrincipal.UserId, exn.UserName);
            Assert.Equal(AuthorizationRole.VariantEditor.ToString(), exn.Role);
        }

        [Theory(DisplayName = "When ApproveAsync is called, then update tracking details for the variant request."), ValidData(WriteAction.Update)]
        public async Task When_ApproveAsync_Then_UpdateRequestTrackingDetails(
            [Frozen] AuthenticatedPrincipal principal,
            [Frozen] VariantRequest variantRequest,
            TrackingDetails trackingDetails,
            VariantRequestWriter writer)
        {
            variantRequest.TrackingDetails = trackingDetails;

            await writer.ApproveAsync(variantRequest.Id, variantRequest.ETag).ConfigureAwait(false);

            Assert.Equal(principal.UserId, variantRequest.TrackingDetails.UpdatedBy);
        }

        [Theory(DisplayName = "When ApproveAsync is called, then soft delete the request."), ValidData(WriteAction.Update)]
        public async Task When_ApproveAsync_Then_SoftApproveRequest(
            [Frozen] VariantRequest variantRequest,
            TrackingDetails trackingDetails,
            VariantRequestWriter writer)
        {
            variantRequest.TrackingDetails = trackingDetails;
            variantRequest.IsDeleted = false;

            await writer.ApproveAsync(variantRequest.Id, variantRequest.ETag).ConfigureAwait(false);

            Assert.True(variantRequest.IsDeleted);
        }

        [Theory(DisplayName = "When ApproveAsync is called with asset group having empty variants, then add requested variants to the asset groups."), ValidData(WriteAction.Update)]
        public async Task When_ApproveAsyncWithAssetGroupHavingEmptyVariants_Then_AssignRequestedVariantsToAssetGroups(
            [Frozen] VariantRequest variantRequest,
            [Frozen] IEnumerable<AssetGroup> assetGroups,
            TrackingDetails trackingDetails,
            VariantRequestWriter writer)
        {
            variantRequest.TrackingDetails = trackingDetails;

            await writer.ApproveAsync(variantRequest.Id, variantRequest.ETag).ConfigureAwait(false);

            foreach (var assetGroup in assetGroups)
            {
                var intersect = assetGroup.Variants.Select(x => x.VariantId).Intersect(variantRequest.RequestedVariants.Select(x => x.VariantId));
                Assert.True(intersect.Count() == variantRequest.RequestedVariants.Count());
            }
        }

        [Theory(DisplayName = "When ApproveAsync is called with asset group having non-empty variants, then add requested variants to the asset groups."), ValidData(WriteAction.Update)]
        public async Task When_ApproveAsyncWithAssetGroupHavingNonEmptyVariants_Then_AssignRequestedVariantsToAssetGroups(
            [Frozen] VariantRequest variantRequest,
            [Frozen] IEnumerable<AssetGroup> assetGroups,
            TrackingDetails trackingDetails,
            VariantRequestWriter writer,
            IFixture fixture)
        {
            variantRequest.TrackingDetails = trackingDetails;
            var requestedVariant = variantRequest.RequestedVariants.First();

            foreach (var assetGroup in assetGroups)
            {
                var variants = fixture.Create<IEnumerable<AssetGroupVariant>>().Concat(new[] { new AssetGroupVariant { VariantId = requestedVariant.VariantId, VariantState = VariantState.Requested } });
                assetGroup.Variants = variants;
            }

            await writer.ApproveAsync(variantRequest.Id, variantRequest.ETag).ConfigureAwait(false);

            foreach (var assetGroup in assetGroups)
            {
                var intersect = assetGroup.Variants.Select(x => x.VariantId).Intersect(variantRequest.RequestedVariants.Select(x => x.VariantId));
                Assert.True(intersect.Count() == variantRequest.RequestedVariants.Count());
                Assert.Equal(VariantState.Approved, assetGroup.Variants.Single(x => x.VariantId == requestedVariant.VariantId).VariantState);
            }
        }

        [Theory(DisplayName = "When ApproveAsync is called, then assign approved variants to asset groups."), ValidData(WriteAction.Update)]
        public async Task When_ApproveAsyncIsCalled_Then_AssignApprovedVariantsToAssetGroups(
            [Frozen] VariantRequest variantRequest,
            [Frozen] IEnumerable<AssetGroup> assetGroups,
            TrackingDetails trackingDetails,
            VariantRequestWriter writer)
        {
            variantRequest.TrackingDetails = trackingDetails;

            await writer.ApproveAsync(variantRequest.Id, variantRequest.ETag).ConfigureAwait(false);

            foreach (var assetGroup in assetGroups)
            {
                Assert.True(assetGroup.Variants.All(x => x.VariantState == VariantState.Approved));
            }
        }

        [Theory(DisplayName = "When ApproveAsync is called, then update variant tfs list in asset groups."), ValidData(WriteAction.Update)]
        public async Task When_ApproveAsyncIsCalled_Then_UpdateTfsList(
            [Frozen] VariantRequest variantRequest,
            [Frozen] IEnumerable<AssetGroup> assetGroups,
            Uri workItemUri,
            TrackingDetails trackingDetails,
            VariantRequestWriter writer)
        {
            variantRequest.WorkItemUri = workItemUri;
            variantRequest.TrackingDetails = trackingDetails;

            await writer.ApproveAsync(variantRequest.Id, variantRequest.ETag).ConfigureAwait(false);

            foreach (var assetGroup in assetGroups)
            {
                Assert.True(assetGroup.Variants.All(x => x.TfsTrackingUris.Contains(workItemUri)));
                Assert.True(assetGroup.Variants.All(x => x.TfsTrackingUris.Select(y => y.Equals(workItemUri)).Count() == 1)); // uri only included once
            }
        }

        [Theory(DisplayName = "When ApproveAsync is called with asset group having no other pending variant requests, then remove its HasPendingVariantRequests flag."), ValidData(WriteAction.Update)]
        public async Task When_ApproveAsyncWithAssetGroupHavingNoOtherPendingRequests_Then_RemoveItsHasPendingVariantRequestsFlag(
            [Frozen] VariantRequest variantRequest,
            [Frozen] IEnumerable<AssetGroup> assetGroups,
            [Frozen] Mock<IVariantRequestReader> variantRequestReader,
            TrackingDetails trackingDetails,
            VariantRequestWriter writer)
        {
            variantRequest.TrackingDetails = trackingDetails;

            foreach (var assetGroup in assetGroups)
            {
                Action<VariantRequestFilterCriteria> filterVerify = x =>
                {
                    Assert.Equal(assetGroup.Id, x.AssetGroupId);
                };

                variantRequestReader.Setup(m => m.ReadByFiltersAsync(Is.Value(filterVerify), ExpandOptions.None)).ReturnsAsync(new FilterResult<VariantRequest> { Total = 1 });
            }

            await writer.ApproveAsync(variantRequest.Id, variantRequest.ETag).ConfigureAwait(false);

            foreach (var assetGroup in assetGroups)
            {
                Assert.False(assetGroup.HasPendingVariantRequests);
            }
        }

        [Theory(DisplayName = "When ApproveAsync is called, then save all entities in batch."), ValidData(WriteAction.Update)]
        public async Task When_ApproveAsync_Then_StoreUpdatedEntities(
            [Frozen] VariantRequest variantRequest,
            TrackingDetails trackingDetails,
            [Frozen] IEnumerable<AssetGroup> assetGroups,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            [Frozen] Mock<IVariantRequestReader> variantRequestReader,
            VariantRequestWriter writer)
        {
            variantRequest.TrackingDetails = trackingDetails;
            variantRequestReader.Setup(m => m.ReadByFiltersAsync(It.IsAny<VariantRequestFilterCriteria>(), ExpandOptions.None)).ReturnsAsync(new FilterResult<VariantRequest> { Total = 1 });

            await writer.ApproveAsync(variantRequest.Id, variantRequest.ETag).ConfigureAwait(false);

            // The frozen values are the ones returned by the storage read calls.
            // All of those should be updated and then passed back to storage for saving.
            Action<IEnumerable<Entity>> verify = x =>
            {
                x.Where(y => y is AssetGroup).SortedSequenceAssert(assetGroups, y => y.Id, Assert.Equal);
                Assert.True(x.Contains(variantRequest), "Missing variant request");
            };

            storageWriter.Verify(m => m.UpdateEntitiesAsync(Is.Value(verify)), Times.Once);
        }
        #endregion

        public class ValidDataAttribute : AutoMoqDataAttribute
        {
            public ValidDataAttribute(WriteAction action = WriteAction.Create, bool treatAsVariantEditor = true) : base(true)
            {
                var writeSecurityGroups = this.Fixture.Create<IEnumerable<Guid>>();
                
                // By default, that user is a ServiceEditor
                this.Fixture.Customize<DataOwner>(obj =>
                    obj
                    .With(x => x.WriteSecurityGroups, writeSecurityGroups)
                    .Without(x => x.ServiceTree));

                this.Fixture.Customize<Entity>(obj =>
                    obj
                    .Without(x => x.ETag)
                    .Without(x => x.TrackingDetails));
                
                this.Fixture.Customize<TrackingDetails>(obj =>
                    obj.With(x => x.Version, 1)); // Prove this is updated.

                var ownerId = this.Fixture.Create<Guid>();
                var ownerName = this.Fixture.Create<string>();

                this.Fixture.Customize<AssetGroup>(obj =>
                    obj
                    .With(x => x.OwnerId, ownerId)
                    .With(x => x.HasPendingVariantRequests, false)
                    .With(x => x.Qualifier, this.Fixture.Create<AssetQualifier>()));

                // Config the AutoFixture, so that by default the variants and asset groups of the variant request exist.
                IEnumerable<VariantDefinition> variants = new[] { new VariantDefinition { Id = Guid.NewGuid() } };
                IEnumerable<AssetGroup> assetGroups = new[] { new AssetGroup { Id = Guid.NewGuid(), OwnerId = ownerId, TrackingDetails = this.Fixture.Create<TrackingDetails>(), HasPendingVariantRequests = false } };

                IEnumerable<AssetGroupVariant> requestedVariants = variants.Select(x => new AssetGroupVariant { VariantId = x.Id, VariantState = VariantState.Approved, DisableSignalFiltering = false });

                this.Fixture.Inject(variants);
                this.Fixture.Inject(assetGroups);

                var variantRelationships = new Dictionary<Guid, VariantRelationship>();

                foreach (var assetGroup in assetGroups)
                {
                    variantRelationships.Add(assetGroup.Id, new VariantRelationship { AssetGroupId = assetGroup.Id });
                }

                if (action == WriteAction.Create)
                {
                    this.Fixture.Customize<VariantRequest>(obj =>
                        obj
                        .Without(x => x.Id)
                        .With(x => x.OwnerId, ownerId)
                        .With(x => x.RequestedVariants, requestedVariants)
                        .With(x => x.VariantRelationships, variantRelationships));
                }
                else
                {
                    var id = this.Fixture.Create<Guid>();

                    this.Fixture.Customize<VariantRequest>(obj =>
                        obj
                        .With(x => x.Id, id)
                        .With(x => x.ETag, "ETag")
                        .With(x => x.OwnerId, ownerId)
                        .With(x => x.OwnerName, ownerName)
                        .With(x => x.RequestedVariants, requestedVariants)
                        .With(x => x.VariantRelationships, variantRelationships));
                }

                var storageWriterMock = this.Fixture.Create<Mock<IPrivacyDataStorageWriter>>();
                storageWriterMock.Setup(m => m.UpdateEntitiesAsync(It.IsAny<IEnumerable<Entity>>())).Returns<IEnumerable<Entity>>(v => Task.FromResult(v));
                this.Fixture.Inject(storageWriterMock);

                this.Fixture.RegisterAuthorizationClasses(writeSecurityGroups, treatAsVariantEditor: treatAsVariantEditor);
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