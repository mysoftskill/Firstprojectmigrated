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

    public class VariantDefinitionWriterTest
    {
        [Theory(DisplayName = "When CreateAsync is called, then the storage layer is called and returned."), ValidData]
        public async Task When_CreateAsync_Then_CallAndReturnStorageValue(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            VariantDefinition variantDefinition,
            [Frozen] VariantDefinition storageVariantDefinition,
            VariantDefinitionWriter writer)
        {
            var result = await writer.CreateAsync(variantDefinition).ConfigureAwait(false);

            Assert.Equal(storageVariantDefinition, result);

            storageWriter.Verify(m => m.CreateVariantDefinitionAsync(variantDefinition), Times.Once);
        }

        [Theory(DisplayName = "When CreateAsync is called without an ownerId, then pass."), ValidData]
        public async Task When_CreateAsyncWithoutOwnerId_Then_Pass(
            VariantDefinition variantDefinition,
            VariantDefinitionWriter writer)
        {
            variantDefinition.OwnerId = null;

            await writer.CreateAsync(variantDefinition).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When CreateAsync is called with an owner, then fail."), ValidData]
        public async Task When_CreateAsyncWithOwner_Then_Fail(
            DataOwner dataOwner,
            VariantDefinition variantDefinition,
            VariantDefinitionWriter writer)
        {
            variantDefinition.Owner = dataOwner;

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(variantDefinition)).ConfigureAwait(false);

            Assert.Equal("owner", exn.ParamName);
            Assert.Null(exn.Value);
        }

        [Theory(DisplayName = "When CreateAsync is called with an owner id that does not exist, then fail."), ValidData]
        public async Task When_CreateAsyncWithUnknownOwner_Then_Fail(
            [Frozen] Mock<IDataOwnerReader> dataOwnerReader,
            VariantDefinition variantDefinition,
            VariantDefinitionWriter writer)
        {
            dataOwnerReader.Setup(m => m.ReadByIdAsync(variantDefinition.OwnerId.Value, ExpandOptions.None)).ReturnsAsync(null as DataOwner);

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.CreateAsync(variantDefinition)).ConfigureAwait(false);

            Assert.Equal("ownerId", exn.Target);
            Assert.Equal(ConflictType.DoesNotExist, exn.ConflictType);
        }

        [Theory(DisplayName = "When CreateAsync is called and user is not in the variant editor groups, then fail."), ValidData(treatAsVariantEditor: false)]
        public async Task When_CreateAsyncWithMissingSecurityGroup_Then_Fail(
            VariantDefinition variantDefinition,
            VariantDefinitionWriter writer)
        {
            await Assert.ThrowsAsync<MissingWritePermissionException>(() => writer.CreateAsync(variantDefinition)).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When CreateAsync is called and variant state is set to not Active, then fail."), ValidData(treatAsVariantEditor: true)]
        public async Task When_CreateAsyncWithState_Then_Fail(
            VariantDefinition variantDefinition,
            VariantDefinitionWriter writer)
        {
            // Set the state in the variant definition to 'Closed'.
            variantDefinition.State = VariantDefinitionState.Closed;

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.CreateAsync(variantDefinition)).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When UpdateAsync is called, then the storage layer is called and returned."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsync_Then_CallAndReturnStorageValue(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            VariantDefinition variantDefinition,
            [Frozen] VariantDefinition existingVariantDefinition,
            VariantDefinitionWriter writer,
            TrackingDetails trackingDetails)
        {
            existingVariantDefinition.TrackingDetails = trackingDetails;

            var result = await writer.UpdateAsync(variantDefinition).ConfigureAwait(false);

            Assert.Equal(existingVariantDefinition, result);

            storageWriter.Verify(m => m.UpdateVariantDefinitionAsync(existingVariantDefinition), Times.Once);
        }

        [Theory(DisplayName = "When UpdateAsync is called with an owner id that does not exist, then fail."), ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncWithUnknownOwner_Then_Fail(
            [Frozen] Mock<IDataOwnerReader> dataOwnerReader,
            VariantDefinition variantDefinition,
            VariantDefinitionWriter writer,
            Guid newDataOwnerId)
        {
            variantDefinition.OwnerId = newDataOwnerId;
            dataOwnerReader.Setup(m => m.ReadByIdAsync(variantDefinition.OwnerId.Value, ExpandOptions.None)).ReturnsAsync(null as DataOwner);

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.UpdateAsync(variantDefinition)).ConfigureAwait(false);

            Assert.Equal("ownerId", exn.Target);
            Assert.Equal(ConflictType.DoesNotExist, exn.ConflictType);
        }

        [Theory(DisplayName = "When UpdateAsync is called and user is not in the editor security group, then fail."), ValidData(WriteAction.Update, treatAsVariantEditor: false)]
        public async Task When_UpdateAsyncWithMissingExistingSg_Then_Fail(
            VariantDefinition variantDefinition,
            VariantDefinitionWriter writer)
        {
            await Assert.ThrowsAsync<MissingWritePermissionException>(() => writer.UpdateAsync(variantDefinition)).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When UpdateAsync is called with State Active and Reason not None, then fail."), ValidData(WriteAction.Update, true, VariantDefinitionState.Active, VariantDefinitionReason.Expired)]
        public async Task When_UpdateAsyncWithStateActiveAndReasonNotNone_Then_Fail(
            VariantDefinition variantDefinition,
            VariantDefinitionWriter writer)
        {
            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.UpdateAsync(variantDefinition)).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When UpdateAsync is called with State Closed and Reason is None, then fail."), ValidData(WriteAction.Update, true, VariantDefinitionState.Closed, VariantDefinitionReason.None)]
        public async Task When_UpdateAsyncWithStateVlosedAndReasonNone_Then_Fail(
            VariantDefinition variantDefinition,
            VariantDefinitionWriter writer)
        {
            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.UpdateAsync(variantDefinition)).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When DeleteAsync is called and state is Closed, then soft delete the definition."), ValidData(WriteAction.SoftDelete, true, VariantDefinitionState.Closed, VariantDefinitionReason.Intentional)]
        public async Task When_DeleteAsync_Then_SoftDeleteDefinition(
            [Frozen] VariantDefinition variantDefinition,
            [Frozen] Mock<IVariantDefinitionReader> variantDefinitionReader,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            TrackingDetails trackingDetails,
            VariantDefinitionWriter writer)
        {
            variantDefinitionReader.Setup(m => m.IsLinkedToAnyOtherEntities(variantDefinition.Id)).ReturnsAsync(false);
            variantDefinitionReader.Setup(m => m.HasPendingCommands(variantDefinition.Id)).ReturnsAsync(false);

            variantDefinition.TrackingDetails = trackingDetails;

            await writer.DeleteAsync(variantDefinition.Id, variantDefinition.ETag).ConfigureAwait(false);

            storageWriter.Verify(m => m.UpdateVariantDefinitionAsync(variantDefinition), Times.Once);
            Assert.True(variantDefinition.IsDeleted);
        }

        [Theory(DisplayName = "When DeleteAsync is called without permisson, it fails."), ValidData(WriteAction.SoftDelete, treatAsVariantEditor: false)]
        public async Task When_DeleteAsync_WithoutPermission_Then_Fail(
            [Frozen] VariantDefinition variantDefinition,
            [Frozen] Mock<IVariantDefinitionReader> variantDefinitionReader,
            VariantDefinitionWriter writer)
        {
            variantDefinitionReader.Setup(m => m.IsLinkedToAnyOtherEntities(variantDefinition.Id)).ReturnsAsync(false);
            variantDefinitionReader.Setup(m => m.HasPendingCommands(variantDefinition.Id)).ReturnsAsync(false);

            await Assert.ThrowsAsync<MissingWritePermissionException>(() => writer.DeleteAsync(variantDefinition.Id, variantDefinition.ETag)).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When DeleteAsync is called with a variant linked to other entities, it fails."), ValidData(WriteAction.SoftDelete)]
        public async Task When_DeleteAsync_DeleteLinkedVariant_Then_Fail(
            [Frozen] VariantDefinition variantDefinition,
            [Frozen] Mock<IVariantDefinitionReader> variantDefinitionReader,
            VariantDefinitionWriter writer)
        {
            variantDefinitionReader.Setup(m => m.IsLinkedToAnyOtherEntities(variantDefinition.Id)).ReturnsAsync(true);
            variantDefinitionReader.Setup(m => m.HasPendingCommands(variantDefinition.Id)).ReturnsAsync(false);

            await Assert.ThrowsAsync<ConflictException>(() => writer.DeleteAsync(variantDefinition.Id, variantDefinition.ETag)).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When DeleteAsync is called with a variant in active state, it fails."), ValidData(WriteAction.SoftDelete)]
        public async Task When_DeleteAsync_DeleteActiveVariant_Then_Fail(
            [Frozen] VariantDefinition variantDefinition,
            [Frozen] Mock<IVariantDefinitionReader> variantDefinitionReader,
            TrackingDetails trackingDetails,
            VariantDefinitionWriter writer)
        {
            variantDefinitionReader.Setup(m => m.IsLinkedToAnyOtherEntities(variantDefinition.Id)).ReturnsAsync(false);
            variantDefinition.TrackingDetails = trackingDetails;
            variantDefinition.State = VariantDefinitionState.Active;

            await Assert.ThrowsAsync<ConflictException>(() => writer.DeleteAsync(variantDefinition.Id, variantDefinition.ETag)).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When DeleteAsync is called with a variant linked to other entities, it succeeds if force option is specifed."), ValidData(WriteAction.SoftDelete, true, VariantDefinitionState.Closed, VariantDefinitionReason.Intentional)]
        public async Task When_DeleteAsync_ForceDeleteLinkedVariant_Then_Succeed(
            [Frozen] VariantDefinition variantDefinition,
            [Frozen] Mock<IVariantDefinitionReader> variantDefinitionReader,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            TrackingDetails trackingDetails,
            VariantDefinitionWriter writer)
        {
            variantDefinitionReader.Setup(m => m.IsLinkedToAnyOtherEntities(variantDefinition.Id)).ReturnsAsync(false);
            variantDefinitionReader.Setup(m => m.HasPendingCommands(variantDefinition.Id)).ReturnsAsync(false);

            variantDefinition.TrackingDetails = trackingDetails;

            await writer.DeleteAsync(variantDefinition.Id, variantDefinition.ETag, false, true).ConfigureAwait(false);

            storageWriter.Verify(m => m.UpdateVariantDefinitionAsync(variantDefinition), Times.Once);

            Assert.True(variantDefinition.IsDeleted);
        }

        [Theory(DisplayName = "When DelinkAssetGroups, all existing AssetGroup can be unlinked."), ValidData(WriteAction.SoftDelete, true, VariantDefinitionState.Closed, VariantDefinitionReason.Intentional)]
        public async Task When_DeleteAsync_ForceDeleteLinkedVariant(
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            [Frozen] VariantDefinition variantDefinition,
            [Frozen] Mock<IVariantDefinitionReader> variantDefinitionReader,
            VariantDefinitionWriter writer)
        {
            var assetGroups = new List<AssetGroup>()
            {
                new AssetGroup
                {
                    Variants = new List<AssetGroupVariant>()
                    {
                        new AssetGroupVariant()
                        { 
                            VariantId = variantDefinition.Id,
                        },
                        new AssetGroupVariant()
                        {
                            VariantId = Guid.NewGuid(),
                        },
                    }
                },
                new AssetGroup
                {
                    Variants = new List<AssetGroupVariant>()
                    {
                        new AssetGroupVariant()
                        {
                            VariantId = Guid.NewGuid(),
                        },
                        new AssetGroupVariant()
                        {
                            VariantId = variantDefinition.Id,
                        },
                    }
                },
            };

            variantDefinitionReader.Setup(m => m.IsLinkedToAnyOtherEntities(variantDefinition.Id)).ReturnsAsync(true);
            variantDefinitionReader.Setup(m => m.GetLinkedAssetGroups(variantDefinition.Id)).ReturnsAsync(assetGroups);

            // Capture passed in parameter for UpdateEntitiesAsync
            List<Entity> passedInAssetGroups = null;
            storageWriter
                .Setup(m => m.UpdateEntitiesAsync(It.IsAny<IEnumerable<Entity>>()))
                .Callback<IEnumerable<Entity>>(entities => passedInAssetGroups = entities.ToList())
                .Returns<IEnumerable<Entity>>(entities => Task.FromResult(entities));

            await writer.WriteAsync(WriteAction.SoftDelete, variantDefinition).ConfigureAwait(false);

            // Verify if UpdateEntitiesAsync was called once
            storageWriter.Verify(m => m.UpdateEntitiesAsync(assetGroups), Times.Once);

            // Verify if UpdateEntitiesAsync was called with correct input data
            Assert.NotNull(passedInAssetGroups);
            Assert.Equal(2, passedInAssetGroups.Count());
            Assert.Single((passedInAssetGroups[0] as AssetGroup).Variants);
            Assert.NotEqual(variantDefinition.Id, (passedInAssetGroups[0] as AssetGroup).Variants.First().VariantId);
            Assert.Single((passedInAssetGroups[1] as AssetGroup).Variants);
            Assert.NotEqual(variantDefinition.Id, (passedInAssetGroups[1] as AssetGroup).Variants.First().VariantId);
        }

        public class ValidDataAttribute : AutoMoqDataAttribute
        {
            public ValidDataAttribute(WriteAction action = WriteAction.Create, 
                bool treatAsVariantEditor = true, 
                VariantDefinitionState state = VariantDefinitionState.Active,
                VariantDefinitionReason reason = VariantDefinitionReason.None) : base(true)
            {
                var writeSecurityGroups = this.Fixture.Create<IEnumerable<Guid>>();

                this.Fixture.Customize<Entity>(obj =>
                    obj
                    .Without(x => x.Id)
                    .Without(x => x.ETag)
                    .Without(x => x.TrackingDetails));

                this.Fixture.Customize<DataOwner>(obj =>
                    obj
                    .With(x => x.WriteSecurityGroups, writeSecurityGroups)
                    .Without(x => x.ServiceTree));

                if (action == WriteAction.Create)
                {
                    this.Fixture.Customize<VariantDefinition>(obj =>
                    obj
                    .Without(x => x.State)
                    .Without(x => x.Reason)
                    .Without(x => x.Owner));
                }
                else
                {
                    var id = this.Fixture.Create<Guid>();
                    var ownerId = this.Fixture.Create<Guid>();

                    this.Fixture.Customize<VariantDefinition>(obj =>
                    obj
                    .With(x => x.Id, id)
                    .With(x => x.ETag, "ETag")
                    .With(x => x.OwnerId, ownerId)
                    .With(x => x.State, state)
                    .With(x => x.Reason, reason)
                    .With(x => x.IsDeleted, false)
                    .Without(x => x.TrackingDetails)
                    .Without(x => x.Owner));
                }

                this.Fixture.Customize<FilterResult<VariantDefinition>>(obj =>
                    obj
                    .With(x => x.Values, Enumerable.Empty<VariantDefinition>()));

                var storageWriterMock = this.Fixture.Create<Mock<IPrivacyDataStorageWriter>>();
                storageWriterMock.Setup(m => m.UpdateEntitiesAsync(It.IsAny<IEnumerable<Entity>>())).Returns<IEnumerable<Entity>>(v => Task.FromResult(v));
                this.Fixture.Inject(storageWriterMock);

                this.Fixture.RegisterAuthorizationClasses(writeSecurityGroups, treatAsVariantEditor: treatAsVariantEditor);
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
