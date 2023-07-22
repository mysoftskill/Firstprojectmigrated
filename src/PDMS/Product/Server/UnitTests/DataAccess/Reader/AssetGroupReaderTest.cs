namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Reader.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Models.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;
    using Microsoft.PrivacyServices.Identity;
    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;

    public class AssetGroupReaderTest
    {
        private const int MaxPageSize = 5;

        [Theory(DisplayName = "When ReadById returns null, then return null."), ValidData]
        public async Task When_ReadByIdIsNull_Then_ReturnNull(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            Guid id,
            AssetGroupReader reader)
        {
            storageReader.Setup(m => m.GetAssetGroupAsync(id, false)).ReturnsAsync(null as AssetGroup);

            var result = await reader.ReadByIdAsync(id, ExpandOptions.None).ConfigureAwait(false);

            Assert.Null(result);
        }

        [Theory(DisplayName = "When ReadById is called without expansions, then data access GetAssetGroupAsync is invoked without tracking details."), ValidData]
        public async Task When_ReadById_Then_GetAssetGroupAsyncInvoked(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            [Frozen] AssetGroup assetGroup,
            Guid id,
            AssetGroupReader reader)
        {
            var result = await reader.ReadByIdAsync(id, ExpandOptions.None).ConfigureAwait(false);

            Assert.Equal(assetGroup, result);

            storageReader.Verify(m => m.GetAssetGroupAsync(id, false), Times.Once);
        }

        [Theory(DisplayName = "When ReadById is called, but TransferRequestReader does not reaturn any result, then fails."), ValidData]
        public async Task When_ReadById_But_TransferRequestReaderDoesNotReturnAnything_Then_Fails(
            [Frozen] Mock<ITransferRequestReader> transferRequestReader,
            [Frozen] AssetGroup assetGroup,
            FilterResult<TransferRequest> transferRequestFilterResult,
            TransferRequest transferRequest,
            AssetGroupReader reader)
        {
            assetGroup.HasPendingTransferRequest = true;
            transferRequest.RequestState = TransferRequestStates.Pending;
            transferRequestFilterResult.Total = 1;
            transferRequestFilterResult.Values = new TransferRequest[] { transferRequest };

            Action<TransferRequestFilterCriteria> filter = f =>
            {
                Assert.Equal(assetGroup.OwnerId, f.SourceOwnerId);
                Assert.Equal(assetGroup.Id, f.AssetGroupId);
            };

            transferRequestReader.Setup(m => m.ReadByFiltersAsync(Is.Value(filter), ExpandOptions.None)).ReturnsAsync(transferRequestFilterResult);

            var exn = await Assert.ThrowsAsync<ConflictException>(() => reader.ReadByIdAsync(assetGroup.Id, ExpandOptions.None)).ConfigureAwait(false);

            Assert.Equal("hasPendingTransferRequest", exn.Target);
        }

        [Theory(DisplayName = "When ReadById is called, but the pending transfer request target owner can not be found, then fails."), ValidData]
        public async Task When_ReadById_But_ThePendingTransferRequestTargetOwnerCannotBeFound_Then_Fails(
            [Frozen] Mock<IDataOwnerReader> dataOwnerReader,
            [Frozen] FilterResult<TransferRequest> transferRequestFilterResult,
            [Frozen] AssetGroup assetGroup,
            TransferRequest transferRequest,
            AssetGroupReader reader,
            IFixture fixture)
        {
            assetGroup.HasPendingTransferRequest = true;
            transferRequestFilterResult.Total = 1;
            transferRequestFilterResult.Values = new TransferRequest[] { transferRequest };

            transferRequest.AssetGroups = new[] { assetGroup.Id };
            transferRequest.RequestState = TransferRequestStates.Pending;

            var expectedDataOwnerIds = new[] { transferRequest.TargetOwnerId };

            dataOwnerReader.Setup(m => m.ReadByIdsAsync(Is.Value<IEnumerable<Guid>>(x => expectedDataOwnerIds.SequenceEqual(x)), ExpandOptions.None)).ReturnsAsync(new[] { fixture.Create<DataOwner>() });

            var exn = await Assert.ThrowsAsync<ConflictException>(() => reader.ReadByIdAsync(assetGroup.Id, ExpandOptions.None)).ConfigureAwait(false);

            Assert.Equal("targetOwnerId", exn.Target);
        }

        [Theory(DisplayName = "When ReadById is called with tracking details expansion, then data access GetAssetGroupAsync is invoked with tracking details."), ValidData]
        public async Task When_ReadByIdWithTrackingDetails_Then_GetAssetGroupAsyncInvokedWithTrackingDetails(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            Guid id,
            AssetGroupReader reader)
        {
            await reader.ReadByIdAsync(id, ExpandOptions.TrackingDetails).ConfigureAwait(false);

            storageReader.Verify(m => m.GetAssetGroupAsync(id, true), Times.Once);
        }

        [Theory(DisplayName = "When ReadByFilters is called without expansions, then data access GetAssetGroupsAsync is invoked without tracking details."), ValidData]
        public async Task When_ReadByFilters_Then_GetAssetGroupsAsyncInvoked(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            [Frozen] FilterResult<AssetGroup> assetGroups,
            AssetGroupFilterCriteria filterCriteria,
            AssetGroupReader reader)
        {
            var result = await reader.ReadByFiltersAsync(filterCriteria, ExpandOptions.None).ConfigureAwait(false);

            Assert.Equal(assetGroups, result);

            storageReader.Verify(m => m.GetAssetGroupsAsync(filterCriteria, false), Times.Once);
        }

        [Theory(DisplayName = "When ReadByFilters is called with tracking details expansion, then data access GetAssetGroupsAsync is invoked with tracking details."), ValidData]
        public async Task When_ReadByFiltersWithTrackingDetails_Then_GetAssetGroupsAsyncInvokedWithTrackingDetails(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            AssetGroupFilterCriteria filterCriteria,
            AssetGroupReader reader)
        {
            await reader.ReadByFiltersAsync(filterCriteria, ExpandOptions.TrackingDetails).ConfigureAwait(false);

            storageReader.Verify(m => m.GetAssetGroupsAsync(filterCriteria, true), Times.Once);
        }

        [Theory(DisplayName = "When ReadByFilters is called, then TransferRequestReader is called for checking pending transfer requests."), ValidData]
        public async Task When_ReadByFilters_Then_TransferRequestReaderIsCalled(
            [Frozen] Mock<ITransferRequestReader> transferRequestReader,
            [Frozen] FilterResult<AssetGroup> assetGroups,
            [Frozen] Mock<IDataOwnerReader> dataOwnerReader,
            DataOwner dataOwner,
            FilterResult<TransferRequest> transferRequestFilterResult,
            AssetGroup assetGroup,
            TransferRequest transferRequest,
            AssetGroupFilterCriteria filterCriteria,
            AssetGroupReader reader)
        {
            filterCriteria.OwnerId = assetGroup.OwnerId;
            assetGroup.HasPendingTransferRequest = true;
            assetGroups.Values = new[] { assetGroup };
            transferRequestFilterResult.Total = 1;
            transferRequestFilterResult.Values = new[] { transferRequest };
            transferRequest.AssetGroups = new[] { assetGroup.Id };
            transferRequest.RequestState = TransferRequestStates.Pending;
            dataOwner.Id = transferRequest.TargetOwnerId;

            var expectedOwnerIds = new[] { transferRequest.TargetOwnerId };
            dataOwnerReader.Setup(m => m.ReadByIdsAsync(Is.Value<IEnumerable<Guid>>(x => expectedOwnerIds.SequenceEqual(x)), ExpandOptions.None)).ReturnsAsync(new[] { dataOwner });

            Action<TransferRequestFilterCriteria> filter = f =>
            {
                Assert.Equal(assetGroup.OwnerId, f.SourceOwnerId);
            };

            transferRequestReader.Setup(m => m.ReadByFiltersAsync(Is.Value(filter), ExpandOptions.None)).ReturnsAsync(transferRequestFilterResult);

            var result = await reader.ReadByFiltersAsync(filterCriteria, ExpandOptions.None).ConfigureAwait(false);

            Assert.Equal(transferRequest.TargetOwnerId, assetGroup.PendingTransferRequestTargetOwnerId);
            Assert.Equal(dataOwner.Name, assetGroup.PendingTransferRequestTargetOwnerName);
        }

        [Theory(DisplayName = "When ReadByFilters is called, but TransferRequestReader returns more than one request for an asset, then fails."), ValidData]
        public async Task When_ReadByFilters_But_TransferRequestReaderReturnsMoreThanOneRequestForAnAsset_Then_Fails(
            [Frozen] Mock<ITransferRequestReader> transferRequestReader,
            [Frozen] FilterResult<AssetGroup> assetGroups,
            FilterResult<TransferRequest> transferRequestFilterResult,
            AssetGroup assetGroup,
            TransferRequest transferRequest1,
            TransferRequest transferRequest2,
            AssetGroupFilterCriteria filterCriteria,
            AssetGroupReader reader)
        {
            filterCriteria.OwnerId = assetGroup.OwnerId;
            assetGroup.HasPendingTransferRequest = true;
            assetGroups.Values = new[] { assetGroup };
            transferRequestFilterResult.Total = 2;
            transferRequestFilterResult.Values = new[] { transferRequest1, transferRequest2 };
            transferRequest1.AssetGroups = new[] { assetGroup.Id };
            transferRequest1.RequestState = TransferRequestStates.Pending;
            transferRequest2.AssetGroups = new[] { assetGroup.Id };
            transferRequest2.RequestState = TransferRequestStates.Pending;

            Action<TransferRequestFilterCriteria> filter = f =>
            {
                Assert.Equal(assetGroup.OwnerId, f.SourceOwnerId);
            };

            transferRequestReader.Setup(m => m.ReadByFiltersAsync(Is.Value(filter), ExpandOptions.None)).ReturnsAsync(transferRequestFilterResult);

            var exn = await Assert.ThrowsAsync<ConflictException>(() => reader.ReadByFiltersAsync(filterCriteria, ExpandOptions.None)).ConfigureAwait(false);

            Assert.Equal("transferRequests", exn.Target);
        }

        [Theory(DisplayName = "When FindByAssetQualifier is called with not fully specified qualifier, then fail the full specification check.")]
        [InlineAutoMoqData("AssetType=AzureBlob;AccountName=AzureBlobAccount", "ContainerName", DisableRecursionCheck = true)]
        [InlineAutoMoqData("AssetType=AzureBlob;AccountName=AzureBlobAccount;ContainerName=AzureBlobContainerName", "BlobPattern", DisableRecursionCheck = true)]
        public async Task When_FindByAssetQualifierWithNonFullySpecifiedQualifier_Then_FailTheFullSpecificationCheck(
            string qualifierValue,
            string missingProperty,
            AssetGroupReader reader)
        {
            var qualifier = AssetQualifier.Parse(qualifierValue);

            var exn = await Assert.ThrowsAsync<MissingPropertyException>(() => reader.FindByAssetQualifierAsync(qualifier)).ConfigureAwait(false);

            Assert.Equal($"qualifier[{missingProperty}]", exn.ParamName);
        }

        [Theory(DisplayName = "When FindByAssetQualifier is called with fully specified qualifier, then pass the full specification check."), ValidData]
        public async Task When_FindByAssetQualifierWithFullySpecifiedQualifier_Then_PassTheFullSpecificationCheck(AssetGroupReader reader)
        {
            var qualifier = AssetQualifier.CreateForAzureBlob("a", "b", "c");

            var result = await reader.FindByAssetQualifierAsync(qualifier).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When FindByAssetQualifier is called, then the correct filter criteria is used to search the storage."), ValidData]
        public async Task When_FindByAssetQualifier_Then_CorrectFilterCriteriaIsUsed(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            AssetGroupReader reader)
        {
            var qualifier = AssetQualifier.CreateForAzureBlob("a", "b", "c");

            Action<AssetGroupFilterCriteria> filter = f =>
            {
                Assert.Equal(2, f.Qualifier.Count);
                Assert.True(f.Qualifier.ContainsKey("AssetType") && f.Qualifier["AssetType"].Value == "AzureBlob" && f.Qualifier["AssetType"].ComparisonType == StringComparisonType.EqualsCaseSensitive);
                Assert.True(f.Qualifier.ContainsKey("AccountName") && f.Qualifier["AccountName"].Value == "a" && f.Qualifier["AccountName"].ComparisonType == StringComparisonType.Equals);
            };

            var result = await reader.FindByAssetQualifierAsync(qualifier).ConfigureAwait(false);

            storageReader.Verify(m => m.GetAssetGroupsAsync(Is.Value(filter), false), Times.Once);
        }

        [Theory(DisplayName = "When none of the existing asset groups share required properties with the provided asset qualifer, then FindByAssetQualifier returns null."), ValidData]
        public async Task When_NoneAssetGroupsShareRequiredPropertiesWithProvidedAssetQualifier_Then_FindByAssetQualifierReturnsNull(
            [Frozen] FilterResult<AssetGroup> assetGroupFilterResult,
            AssetGroupReader reader,
            AssetQualifier qualifier)
        {
            assetGroupFilterResult.Values = Enumerable.Empty<AssetGroup>();

            var result = await reader.FindByAssetQualifierAsync(qualifier).ConfigureAwait(false);

            Assert.Null(result);
        }

	 [Theory(DisplayName = "When FindByAssetQualifier is called, then the most specific asset group is returned."), ValidData]
         public async Task When_FindByAssetQualifierCalled_Then_MostSpecificAssetGroupIsReturned([Frozen] FilterResult<AssetGroup> assetGroupFilterResult, AssetGroupReader reader, Fixture fixture)
         {
            var assetGroups = fixture.CreateMany<AssetGroup>(3);

            assetGroups.ElementAt(0).Qualifier = AssetQualifier.CreateForAzureDocumentDB("a1");
            assetGroups.ElementAt(1).Qualifier = AssetQualifier.CreateForAzureDocumentDB("a1", "b1");
            assetGroups.ElementAt(2).Qualifier = AssetQualifier.CreateForAzureDocumentDB("a1", "b1", "c1");

            assetGroupFilterResult.Values = assetGroups;

            var qualifier1 = AssetQualifier.CreateForAzureDocumentDB("a1", "b1", "c1");
            var qualifier2 = AssetQualifier.CreateForAzureDocumentDB("a1", "b1", "c2");
            var qualifier3 = AssetQualifier.CreateForAzureDocumentDB("a1", "b2", "c2");

            var result1 = await reader.FindByAssetQualifierAsync(qualifier1).ConfigureAwait(false);
            var result2 = await reader.FindByAssetQualifierAsync(qualifier2).ConfigureAwait(false);
            var result3 = await reader.FindByAssetQualifierAsync(qualifier3).ConfigureAwait(false);

            Assert.Equal(assetGroups.ElementAt(2), result1);
            Assert.Equal(assetGroups.ElementAt(1), result2);
            Assert.Equal(assetGroups.ElementAt(0), result3);
        }

        [Theory(DisplayName = "When FindByAssetQualifier is called and there are more than PageSize matches, then the most specific asset group is returned."), ValidData]
        public async Task When_FindByAssetQualifierCalledAndMaxPageMatches_Then_MostSpecificAssetGroupIsReturned([Frozen] FilterResult<AssetGroup> assetGroupFilterResult, AssetGroupReader reader, Fixture fixture)
        {
            var pageCount = MaxPageSize;
            var numGroups = pageCount + 3;
            var assetGroups = fixture.CreateMany<AssetGroup>(numGroups);

            // Create PageCount asset qualifiers
            for (int index = 0; index < pageCount; index++)
            {
                assetGroups.ElementAt(index).Qualifier = AssetQualifier.CreateForAzureDocumentDB($"a{index}");
            }

            // Add 3 more qualifiers with more specific asset qualifiers
            int result1Index = pageCount;
            assetGroups.ElementAt(result1Index).Qualifier = AssetQualifier.CreateForAzureDocumentDB($"a{pageCount}");
            int result2Index = pageCount+1;
            assetGroups.ElementAt(result2Index).Qualifier = AssetQualifier.CreateForAzureDocumentDB($"a{pageCount}", $"b{pageCount}");
            int result3Index = pageCount+2;
            assetGroups.ElementAt(result3Index).Qualifier = AssetQualifier.CreateForAzureDocumentDB($"a{pageCount}", $"b{pageCount}", $"c{pageCount}");


            assetGroupFilterResult.Values = assetGroups;

            var qualifier1 = AssetQualifier.CreateForAzureDocumentDB($"a{result1Index}", $"b{result2Index}", $"c{result2Index}");
            var qualifier2 = AssetQualifier.CreateForAzureDocumentDB($"a{result1Index}", $"b{result1Index}", $"c{result2Index}");
            var qualifier3 = AssetQualifier.CreateForAzureDocumentDB($"a{result1Index}", $"b{result1Index}", $"c{result1Index}");

            var result1 = await reader.FindByAssetQualifierAsync(qualifier1).ConfigureAwait(false);
            var result2 = await reader.FindByAssetQualifierAsync(qualifier2).ConfigureAwait(false);
            var result3 = await reader.FindByAssetQualifierAsync(qualifier3).ConfigureAwait(false);

            Assert.Equal(assetGroups.ElementAt(result1Index).Qualifier, result1.Qualifier);
            Assert.Equal(assetGroups.ElementAt(result2Index).Qualifier, result2.Qualifier);
            Assert.Equal(assetGroups.ElementAt(result3Index).Qualifier, result3.Qualifier);
        }

        [Theory(DisplayName = "IsLinkedToAnyOtherEntities returns correct value based on the existence of sharing request or variant reuqest on this asset group.")]
        [InlineValidData(false, false)]
        [InlineValidData(false, true)]
        [InlineValidData(true, false)]
        [InlineValidData(true, true)]
        public async void When_NotContainedInSharingRequest_Then_IsLinkedReturnsFalse(
            bool hasSharingRequest,
            bool hasVariantRequest,
            [Frozen] Mock<ISharingRequestReader> sharingRequestReader,
            [Frozen] Mock<IVariantRequestReader> variantRequestReader,
            AssetGroupReader reader, 
            Guid id)
        {
            var sharingRequestFilterResult = new FilterResult<SharingRequest> { Total = hasSharingRequest ? 1 : 0 };
            var variantRequestFilterResult = new FilterResult<VariantRequest> { Total = hasVariantRequest ? 1 : 0 };

            Action<SharingRequestFilterCriteria> sharingRequestVerify = v =>
            {
                Assert.Equal(id, v.AssetGroupId);
            };

            Action<VariantRequestFilterCriteria> variantRequestVerify = v =>
            {
                Assert.Equal(id, v.AssetGroupId);
            };

            sharingRequestReader.Setup(m => m.ReadByFiltersAsync(Is.Value(sharingRequestVerify), ExpandOptions.None)).ReturnsAsync(sharingRequestFilterResult);
            variantRequestReader.Setup(m => m.ReadByFiltersAsync(Is.Value(variantRequestVerify), ExpandOptions.None)).ReturnsAsync(variantRequestFilterResult);

            var result = await reader.IsLinkedToAnyOtherEntities(id).ConfigureAwait(false);

            Assert.Equal(hasSharingRequest || hasVariantRequest, result);
        }

        [Theory(DisplayName = "HasPendingCommands returns false."), ValidData]
        public async void EnsurePendingCommandsReturnsFalse(
            AssetGroupReader reader)
        {
            var result = await reader.HasPendingCommands(It.IsAny<Guid>()).ConfigureAwait(false);

            Assert.False(result);
        }

        [Theory(DisplayName = "Verify AssetGroupReader.ReadByIds.")]
        [InlineValidData(ExpandOptions.None, false, DisableRecursionCheck = true)]
        [InlineValidData(ExpandOptions.TrackingDetails, true, DisableRecursionCheck = true)]
        public async void VerifyAssetGroupReaderReadByIds(
            ExpandOptions options,
            bool includeTrackingDetails,
            IEnumerable<Guid> ids,
            [Frozen] Mock<IPrivacyDataStorageReader> storage,
            AssetGroupReader reader)
        {
            await reader.ReadByIdsAsync(ids, options).ConfigureAwait(false);
            storage.Verify(m => m.GetAssetGroupsAsync(ids, includeTrackingDetails), Times.Once);
        }

        public class ValidDataAttribute : AutoMoqDataAttribute
        {
            public ValidDataAttribute() : base(true)
            {
                this.Fixture.Customize<FilterResult<AssetGroup>>(obj =>
                    obj.With(x => x.Total, 2 * MaxPageSize)); // max of 2 pages returned

                this.Fixture.Customize<FilterResult<DataAsset>>(obj =>
                    obj.With(x => x.Total, 0)); // Ensure we never hit page limits.

                this.Fixture.Customize<FilterResult<TransferRequest>>(obj =>
                    obj.With(x => x.Total, 0)); // Ensure we never hit page limits.

                this.Fixture.Customize<FilterResult<VariantRequest>>(obj =>
                     obj.With(x => x.Total, 0)); // Ensure we never hit page limits.

                this.Fixture.Customize<AssetGroup>(obj =>
                    obj
                    .With(x => x.Qualifier, AssetQualifier.CreateForAzureBlob("a", "b", "c"))
                    .With(x => x.HasPendingTransferRequest, false));

                // Make the paging values predictable to avoid random test failures.
                this.Fixture.Customize<Mock<ICoreConfiguration>>(obj =>
                    obj.Do(x => x.SetupGet(m => m.MaxPageSize).Returns(MaxPageSize)));
            }
        }

        public class InlineValidDataAttribute : InlineAutoMoqDataAttribute
        {
            public InlineValidDataAttribute(params object[] values) : base(new ValidDataAttribute(), values)
            {
            }
        }
    }
}
