namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Writer.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authorization;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Reader;
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

    public class AssetGroupRelationshipManagerTest
    {
        private static readonly CapabilityId DeleteId = Policies.Current.Capabilities.Ids.Delete;
        private static readonly CapabilityId ExportId = Policies.Current.Capabilities.Ids.Export;
        private static readonly SetAgentRelationshipParameters.ActionType SetAction = SetAgentRelationshipParameters.ActionType.Set;
        private static readonly SetAgentRelationshipParameters.ActionType ClearAction = SetAgentRelationshipParameters.ActionType.Clear;
        private static readonly SetAgentRelationshipResponse.StatusType UpdatedStatus = SetAgentRelationshipResponse.StatusType.Updated;
        private static readonly SetAgentRelationshipResponse.StatusType RequestedStatus = SetAgentRelationshipResponse.StatusType.Requested;
        private static readonly SetAgentRelationshipResponse.StatusType RemovedStatus = SetAgentRelationshipResponse.StatusType.Removed;

        #region Consistency Error Validations
        [Theory(DisplayName = "When an unknown asset group id is provided, then fail."), AutoMoqData(true)]
        public async Task When_AnUnknownAssetGroupIdIsProvided_Then_Fail(
            SetAgentRelationshipParameters.Action action,
            SetAgentRelationshipParameters.Relationship relationship1,
            SetAgentRelationshipParameters.Relationship relationship2,
            SetAgentRelationshipParameters parameters,
            AssetGroup storedAssetGroup,
            [Frozen] Mock<IEntityReader<AssetGroup>> assetGroupReader,
            AssetGroupRelationshipManager manager)
        {
            // Setup valid data.
            action.Verb = SetAction;
            relationship1.Actions = new[] { action };
            relationship2.Actions = new[] { action };
            parameters.Relationships = new[] { relationship1, relationship2 };
            storedAssetGroup.Id = relationship1.AssetGroupId;

            // Setup storage results.
            assetGroupReader
                .Setup(m => m.ReadByIdsAsync(It.IsAny<IEnumerable<Guid>>(), ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { storedAssetGroup });

            // Execute.
            var exn = await Assert.ThrowsAsync<ConflictException>(() => manager.ApplyChanges(parameters)).ConfigureAwait(false);

            // Verify.
            Assert.Equal(ConflictType.DoesNotExist, exn.ConflictType);
            Assert.Equal($"relationships[{relationship2.AssetGroupId}].assetGroup", exn.Target);
            Assert.Equal(relationship2.AssetGroupId.ToString(), exn.Value);
        }

        [Theory(DisplayName = "When the provided etags do not match with the values in storage, then fail."), AutoMoqData(true)]
        public async Task When_ProvidedETagsDoNotMatch_Then_Fail(
            SetAgentRelationshipParameters.Action action,
            SetAgentRelationshipParameters.Relationship relationship,
            SetAgentRelationshipParameters parameters,
            AssetGroup storedAssetGroup,
            [Frozen] Mock<IEntityReader<AssetGroup>> assetGroupReader,
            AssetGroupRelationshipManager manager)
        {
            // Setup valid data.
            action.Verb = SetAction;
            relationship.Actions = new[] { action };
            parameters.Relationships = new[] { relationship };
            storedAssetGroup.Id = relationship.AssetGroupId;

            // Make the data invalid.
            storedAssetGroup.ETag = "unknown";

            // Setup storage results.
            assetGroupReader
                .Setup(m => m.ReadByIdsAsync(It.IsAny<IEnumerable<Guid>>(), ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { storedAssetGroup });

            // Execute.
            var exn = await Assert.ThrowsAsync<ConflictException>(() => manager.ApplyChanges(parameters)).ConfigureAwait(false);

            // Verify.
            Assert.Equal(ConflictType.InvalidValue, exn.ConflictType);
            Assert.Equal($"relationships[{relationship.AssetGroupId}].eTag", exn.Target);
            Assert.Equal(relationship.ETag, exn.Value);
        }

        [Theory(DisplayName = "When a provided asset group id identifies an asset group with no owner, then fail."), AutoMoqData(true)]
        public async Task When_AssetGroupHasNoOwner_Then_Fail(
            SetAgentRelationshipParameters.Action action,
            SetAgentRelationshipParameters.Relationship relationship,
            SetAgentRelationshipParameters parameters,
            AssetGroup storedAssetGroup,
            [Frozen] Mock<IEntityReader<AssetGroup>> assetGroupReader,
            AssetGroupRelationshipManager manager)
        {
            // Setup valid data.
            action.Verb = SetAction;
            relationship.Actions = new[] { action };
            parameters.Relationships = new[] { relationship };
            storedAssetGroup.Id = relationship.AssetGroupId;
            storedAssetGroup.ETag = relationship.ETag;

            // Make the data invalid.
            storedAssetGroup.OwnerId = Guid.Empty;

            // Setup storage results.
            assetGroupReader
                .Setup(m => m.ReadByIdsAsync(It.IsAny<IEnumerable<Guid>>(), ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { storedAssetGroup });

            // Execute.
            var exn = await Assert.ThrowsAsync<ConflictException>(() => manager.ApplyChanges(parameters)).ConfigureAwait(false);

            // Verify.
            Assert.Equal(ConflictType.DoesNotExist, exn.ConflictType);
            Assert.Equal($"relationships[{relationship.AssetGroupId}].assetGroup.ownerId", exn.Target);
        }

        [Theory(DisplayName = "When the associated asset groups have different owners, then fail."), AutoMoqData(true)]
        public async Task When_AssetGroupsHaveDifferentOwners_Then_Fail(
            SetAgentRelationshipParameters.Action action,
            SetAgentRelationshipParameters.Relationship relationship1,
            SetAgentRelationshipParameters.Relationship relationship2,
            SetAgentRelationshipParameters parameters,
            AssetGroup storedAssetGroup1,
            AssetGroup storedAssetGroup2,
            [Frozen] Mock<IEntityReader<AssetGroup>> assetGroupReader,
            AssetGroupRelationshipManager manager)
        {
            // Setup valid data.
            action.Verb = SetAction;
            relationship1.Actions = new[] { action };
            relationship2.Actions = new[] { action };
            parameters.Relationships = new[] { relationship1, relationship2 };

            storedAssetGroup1.Id = relationship1.AssetGroupId;
            storedAssetGroup1.ETag = relationship1.ETag;
            storedAssetGroup2.Id = relationship2.AssetGroupId;
            storedAssetGroup2.ETag = relationship2.ETag;

            // Make the data invalid.
            storedAssetGroup1.OwnerId = Guid.NewGuid();
            storedAssetGroup2.OwnerId = Guid.NewGuid();

            // Setup storage results.
            assetGroupReader
                .Setup(m => m.ReadByIdsAsync(It.IsAny<IEnumerable<Guid>>(), ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { storedAssetGroup1, storedAssetGroup2 });

            // Execute.
            var exn = await Assert.ThrowsAsync<ConflictException>(() => manager.ApplyChanges(parameters)).ConfigureAwait(false);

            // Verify.
            Assert.Equal(ConflictType.InvalidValue, exn.ConflictType);
            Assert.Equal($"relationships[{relationship2.AssetGroupId}].assetGroup.ownerId", exn.Target);
            Assert.Equal(storedAssetGroup2.OwnerId.ToString(), exn.Value);
        }

        [Theory(DisplayName = "When an unknown agent id is provided, then fail."), AutoMoqData(true)]
        public async Task When_UnknownAgentId_Then_Fail(
            SetAgentRelationshipParameters.Action action,
            SetAgentRelationshipParameters.Relationship relationship,
            SetAgentRelationshipParameters parameters,
            AssetGroup storedAssetGroup,
            [Frozen] Mock<IEntityReader<AssetGroup>> assetGroupReader,
            [Frozen] Mock<IDeleteAgentReader> deleteAgentReader,
            AssetGroupRelationshipManager manager)
        {
            // Setup valid data.
            action.Verb = SetAction;
            action.DeleteAgentId = Guid.NewGuid();
            relationship.Actions = new[] { action };
            parameters.Relationships = new[] { relationship };
            storedAssetGroup.Id = relationship.AssetGroupId;
            storedAssetGroup.ETag = relationship.ETag;

            // Setup storage results.
            assetGroupReader.Setup(m => m.ReadByIdsAsync(It.IsAny<IEnumerable<Guid>>(), ExpandOptions.WriteProperties)).ReturnsAsync(new[] { storedAssetGroup });
            deleteAgentReader.Setup(m => m.ReadByIdAsync(action.DeleteAgentId.Value, ExpandOptions.WriteProperties)).ReturnsAsync(null as DeleteAgent);

            // Execute.
            var exn = await Assert.ThrowsAsync<ConflictException>(() => manager.ApplyChanges(parameters)).ConfigureAwait(false);

            // Verify.
            Assert.Equal(ConflictType.DoesNotExist, exn.ConflictType);
            Assert.Equal($"relationships[{relationship.AssetGroupId}].actions[{action.CapabilityId}].deleteAgentId", exn.Target);
            Assert.Equal(action.DeleteAgentId.ToString(), exn.Value);
        }

        [Theory(DisplayName = "When provided agent does not share the same owner and does not have sharing enabled, then fail."), AutoMoqData(true)]
        public async Task When_AgentOwnerIsDifferentAndNotSharingEnabled_Then_Fail(
            SetAgentRelationshipParameters.Action action,
            SetAgentRelationshipParameters.Relationship relationship,
            SetAgentRelationshipParameters parameters,
            AssetGroup storedAssetGroup,
            DeleteAgent storedDeleteAgent,
            [Frozen] Mock<IEntityReader<AssetGroup>> assetGroupReader,
            [Frozen] Mock<IDeleteAgentReader> deleteAgentReader,
            AssetGroupRelationshipManager manager)
        {
            // Setup valid data.
            action.Verb = SetAction;
            action.DeleteAgentId = Guid.NewGuid();
            relationship.Actions = new[] { action };
            parameters.Relationships = new[] { relationship };
            storedAssetGroup.Id = relationship.AssetGroupId;
            storedAssetGroup.ETag = relationship.ETag;
            storedDeleteAgent.Id = action.DeleteAgentId.Value;

            // Make the data invalid.
            storedAssetGroup.OwnerId = Guid.NewGuid();
            storedDeleteAgent.OwnerId = Guid.NewGuid();
            storedDeleteAgent.SharingEnabled = false;

            // Setup storage results.
            Action<IEnumerable<Guid>> verify = x =>
            {
                parameters.Relationships.Select(y => y.AssetGroupId).SortedSequenceAssert(x, _ => _, Assert.Equal);
            };

            assetGroupReader.Setup(m => m.ReadByIdsAsync(Is.Value(verify), ExpandOptions.WriteProperties)).ReturnsAsync(new[] { storedAssetGroup });
            deleteAgentReader.Setup(m => m.ReadByIdAsync(action.DeleteAgentId.Value, ExpandOptions.WriteProperties)).ReturnsAsync(storedDeleteAgent);

            // Execute.
            var exn = await Assert.ThrowsAsync<ConflictException>(() => manager.ApplyChanges(parameters)).ConfigureAwait(false);

            // Verify.
            Assert.Equal(ConflictType.InvalidValue, exn.ConflictType);
            Assert.Equal($"relationships[{relationship.AssetGroupId}].actions[{action.CapabilityId}].deleteAgentId", exn.Target);
            Assert.Equal(action.DeleteAgentId.ToString(), exn.Value);
        }
        #endregion

        #region Property Error Validations
        [Theory(DisplayName = "When there are duplicate asset group ids provided, then fail."), AutoMoqData]
        public async Task When_DuplicateAssetGroupIds_Then_Fail(
            SetAgentRelationshipParameters.Action action,
            SetAgentRelationshipParameters.Relationship relationship1,
            SetAgentRelationshipParameters.Relationship relationship2,
            SetAgentRelationshipParameters parameters,
            AssetGroupRelationshipManager manager)
        {
            action.Verb = SetAction;
            action.DeleteAgentId = Guid.NewGuid();
            relationship1.Actions = new[] { action };
            relationship2.Actions = new[] { action };

            relationship2.AssetGroupId = relationship1.AssetGroupId;

            parameters.Relationships = new[] { relationship1, relationship2 };

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => manager.ApplyChanges(parameters)).ConfigureAwait(false);

            Assert.Equal($"relationships[{relationship2.AssetGroupId}].assetGroupId", exn.ParamName);
            Assert.Equal(relationship2.AssetGroupId.ToString(), exn.Value);
        }

        [Theory(DisplayName = "When there are duplicate capability ids for an asset group, then fail."), AutoMoqData]
        public async Task When_DuplicateCapabilitiesForAnAssetGroup_Then_Fail(
            SetAgentRelationshipParameters.Action action,
            SetAgentRelationshipParameters.Relationship relationship,
            SetAgentRelationshipParameters parameters,
            AssetGroupRelationshipManager manager)
        {
            action.Verb = SetAction;
            action.DeleteAgentId = Guid.NewGuid();
            action.CapabilityId = DeleteId;

            relationship.Actions = new[] { action, action };

            parameters.Relationships = new[] { relationship };

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => manager.ApplyChanges(parameters)).ConfigureAwait(false);

            Assert.Equal($"relationships[{relationship.AssetGroupId}].actions[{action.CapabilityId}].capabilityId", exn.ParamName);
            Assert.Equal(action.CapabilityId.ToString(), exn.Value);
        }

        [Theory(DisplayName = "When there are different actions for the same asset group, then fail."), AutoMoqData]
        public async Task When_DifferentActionsForSameAssetGroup_Then_Fail(
            SetAgentRelationshipParameters.Action action1,
            SetAgentRelationshipParameters.Action action2,
            SetAgentRelationshipParameters.Relationship relationship,
            SetAgentRelationshipParameters parameters,
            AssetGroupRelationshipManager manager)
        {
            action1.Verb = SetAction;
            action1.DeleteAgentId = Guid.NewGuid();
            action1.CapabilityId = DeleteId;

            action2.Verb = ClearAction;
            action2.DeleteAgentId = null;
            action1.CapabilityId = ExportId;

            relationship.Actions = new[] { action1, action2 };

            parameters.Relationships = new[] { relationship };

            var exn = await Assert.ThrowsAsync<MutuallyExclusiveException>(() => manager.ApplyChanges(parameters)).ConfigureAwait(false);

            Assert.Equal($"relationships[{relationship.AssetGroupId}].actions[{action2.CapabilityId}].verb", exn.ParamName);
            Assert.Equal(action2.Verb.ToString(), exn.Value);
            Assert.Equal(action1.Verb.ToString(), exn.Source);
        }

        [Theory(DisplayName = "When there are different actions across asset groups, then fail."), AutoMoqData]
        public async Task When_DifferentActionsAcrossAssetGroups_Then_Fail(
            SetAgentRelationshipParameters.Action action1,
            SetAgentRelationshipParameters.Action action2,
            SetAgentRelationshipParameters.Relationship relationship1,
            SetAgentRelationshipParameters.Relationship relationship2,
            SetAgentRelationshipParameters parameters,
            AssetGroupRelationshipManager manager)
        {
            action1.Verb = SetAction;
            action1.DeleteAgentId = Guid.NewGuid();
            relationship1.Actions = new[] { action1 };

            action2.Verb = ClearAction;
            action2.DeleteAgentId = null;
            relationship2.Actions = new[] { action2 };

            parameters.Relationships = new[] { relationship1, relationship2 };

            var exn = await Assert.ThrowsAsync<MutuallyExclusiveException>(() => manager.ApplyChanges(parameters)).ConfigureAwait(false);

            Assert.Equal($"relationships[{relationship2.AssetGroupId}].actions[{action2.CapabilityId}].verb", exn.ParamName);
            Assert.Equal(action2.Verb.ToString(), exn.Value);
            Assert.Equal(action1.Verb.ToString(), exn.Source);
        }

        [Theory(DisplayName = "When a SET action does not have an agent id, then fail."), AutoMoqData]
        public async Task When_SetActionMissingAgentId_Then_Fail(
            SetAgentRelationshipParameters.Action action,
            SetAgentRelationshipParameters.Relationship relationship,
            SetAgentRelationshipParameters parameters,
            AssetGroupRelationshipManager manager)
        {
            action.Verb = SetAction;
            action.DeleteAgentId = null;

            relationship.Actions = new[] { action };

            parameters.Relationships = new[] { relationship };

            var exn = await Assert.ThrowsAsync<MissingPropertyException>(() => manager.ApplyChanges(parameters)).ConfigureAwait(false);

            Assert.Equal($"relationships[{relationship.AssetGroupId}].actions[{action.CapabilityId}].deleteAgentId", exn.ParamName);
        }

        [Theory(DisplayName = "When a CLEAR action has an agent id, then fail."), AutoMoqData]
        public async Task When_ClearActionHasAgentId_Then_Fail(
            SetAgentRelationshipParameters.Action action,
            SetAgentRelationshipParameters.Relationship relationship,
            SetAgentRelationshipParameters parameters,
            AssetGroupRelationshipManager manager)
        {
            action.Verb = ClearAction;
            action.DeleteAgentId = Guid.NewGuid();

            relationship.Actions = new[] { action };

            parameters.Relationships = new[] { relationship };

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => manager.ApplyChanges(parameters)).ConfigureAwait(false);

            Assert.Equal($"relationships[{relationship.AssetGroupId}].actions[{action.CapabilityId}].deleteAgentId", exn.ParamName);
            Assert.Equal(action.DeleteAgentId.ToString(), exn.Value);
        }

        [Theory(DisplayName = "When multiple agent ids provided for the same asset group, then fail."), AutoMoqData]
        public async Task When_MultipleAgentIdsForAssetGroup_Then_Fail(
            SetAgentRelationshipParameters.Action action1,
            SetAgentRelationshipParameters.Action action2,
            SetAgentRelationshipParameters.Relationship relationship,
            SetAgentRelationshipParameters parameters,
            AssetGroupRelationshipManager manager)
        {
            action1.Verb = SetAction;
            action1.DeleteAgentId = Guid.NewGuid();
            action1.CapabilityId = DeleteId;

            action2.Verb = SetAction;
            action2.DeleteAgentId = Guid.NewGuid();
            action2.CapabilityId = ExportId;

            relationship.Actions = new[] { action1, action2 };

            parameters.Relationships = new[] { relationship };

            var exn = await Assert.ThrowsAsync<MutuallyExclusiveException>(() => manager.ApplyChanges(parameters)).ConfigureAwait(false);

            Assert.Equal($"relationships[{relationship.AssetGroupId}].actions[{action2.CapabilityId}].deleteAgentId", exn.ParamName);
            Assert.Equal(action2.DeleteAgentId.ToString(), exn.Value);
            Assert.Equal(action1.DeleteAgentId.ToString(), exn.Source);
        }

        [Theory(DisplayName = "When multiple agent ids provided across asset groups, then fail."), AutoMoqData]
        public async Task When_MultipleAgentIdsAcrossAssetGroups_Then_Fail(
            SetAgentRelationshipParameters.Action action1,
            SetAgentRelationshipParameters.Action action2,
            SetAgentRelationshipParameters.Relationship relationship1,
            SetAgentRelationshipParameters.Relationship relationship2,
            SetAgentRelationshipParameters parameters,
            AssetGroupRelationshipManager manager)
        {
            action1.Verb = SetAction;
            action1.DeleteAgentId = Guid.NewGuid();
            relationship1.Actions = new[] { action1 };

            action2.Verb = SetAction;
            action2.DeleteAgentId = Guid.NewGuid();
            relationship2.Actions = new[] { action2 };

            parameters.Relationships = new[] { relationship1, relationship2 };

            var exn = await Assert.ThrowsAsync<MutuallyExclusiveException>(() => manager.ApplyChanges(parameters)).ConfigureAwait(false);

            Assert.Equal($"relationships[{relationship2.AssetGroupId}].actions[{action2.CapabilityId}].deleteAgentId", exn.ParamName);
            Assert.Equal(action2.DeleteAgentId.ToString(), exn.Value);
            Assert.Equal(action1.DeleteAgentId.ToString(), exn.Source);
        }

        [Theory(DisplayName = "When asset group is provided without any actions, then fail."), AutoMoqData]
        public async Task When_NoActionsProvided_Then_Fail(
            SetAgentRelationshipParameters.Relationship relationship,
            SetAgentRelationshipParameters parameters,
            AssetGroupRelationshipManager manager)
        {
            relationship.Actions = null;
            parameters.Relationships = new[] { relationship };

            var exn = await Assert.ThrowsAsync<MissingPropertyException>(() => manager.ApplyChanges(parameters)).ConfigureAwait(false);

            Assert.Equal($"relationships[{relationship.AssetGroupId}].actions", exn.ParamName);
        }

        [Theory(DisplayName = "When no relationships are provided, then fail."), AutoMoqData]
        public async Task When_NoRelationshipsProvided_Then_Fail(
            SetAgentRelationshipParameters parameters,
            AssetGroupRelationshipManager manager)
        {
            parameters.Relationships = null;

            var exn = await Assert.ThrowsAsync<MissingPropertyException>(() => manager.ApplyChanges(parameters)).ConfigureAwait(false);

            Assert.Equal("relationships", exn.ParamName);
        }
        #endregion

        #region Authorization tests
        [Theory(DisplayName = "When apply changes and all asset groups share same owner, then authorize against asset group owner."), AutoMoqData(true)]
        public async Task When_ApplyChangesCalledWithValidData_Then_AuthorizeAgainstTheAssetGroupOwner(
           SetAgentRelationshipParameters.Action action,
           SetAgentRelationshipParameters.Relationship relationship,
           SetAgentRelationshipParameters parameters,
           AssetGroup storedAssetGroup,
           DataOwner storedDataOwner,
           [Frozen] DeleteAgent storedDeleteAgent,
           [Frozen] Mock<IEntityReader<AssetGroup>> assetGroupReader,
           [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
           [Frozen] Mock<IDataOwnerReader> dataOwnerReader,
           [Frozen] Mock<IAuthorizationProvider> authorizationProvider,
           AssetGroupRelationshipManager manager)
        {
            // Setup valid data.
            action.Verb = SetAction;
            action.CapabilityId = DeleteId;
            relationship.Actions = new[] { action };
            parameters.Relationships = new[] { relationship };

            storedAssetGroup.Id = relationship.AssetGroupId;
            storedAssetGroup.ETag = relationship.ETag;
            storedAssetGroup.DeleteSharingRequestId = null;
            storedAssetGroup.ExportSharingRequestId = null;

            storedDeleteAgent.Id = action.DeleteAgentId.Value;
            storedDeleteAgent.OwnerId = storedAssetGroup.OwnerId;

            // Setup storage results.
            assetGroupReader
                .Setup(m => m.ReadByIdsAsync(It.IsAny<IEnumerable<Guid>>(), ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { storedAssetGroup });

            dataOwnerReader
                .Setup(m => m.ReadByIdAsync(storedAssetGroup.OwnerId, ExpandOptions.WriteProperties))
                .ReturnsAsync(storedDataOwner);

            storageWriter
                .Setup(m => m.UpdateEntitiesAsync(It.IsAny<IEnumerable<Entity>>()))
                .Returns<IEnumerable<Entity>>(x => Task.FromResult(x));

            // Execute.
            await manager.ApplyChanges(parameters).ConfigureAwait(false);

            // Verify authorization occurs.
            Action<Func<Task<IEnumerable<DataOwner>>>> verify = f =>
            {
                var owners = f().ConfigureAwait(false).GetAwaiter().GetResult();
                owners.SequenceAssert(new[] { storedDataOwner }, Assert.Equal);
            };

            authorizationProvider.Verify(m => m.AuthorizeAsync(AuthorizationRole.ServiceEditor, Is.Value(verify)), Times.Once);
        }

        [Theory(DisplayName = "When apply changes for CLEAR and all links share same agent id, then try authorize against agent owner."), AutoMoqData(true)]
        public async Task When_ApplyChangesCalledWithAllLinkedAgentIdsSame_Then_AuthorizeAgainstThAgentOwner(
           SetAgentRelationshipParameters.Action action,
           SetAgentRelationshipParameters.Relationship relationship,
           SetAgentRelationshipParameters parameters,
           AssetGroup storedAssetGroup,
           DataOwner storedDataOwner,
           [Frozen] DeleteAgent storedDeleteAgent,
           [Frozen] Mock<IEntityReader<AssetGroup>> assetGroupReader,
           [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
           [Frozen] Mock<IDataOwnerReader> dataOwnerReader,
           [Frozen] Mock<IAuthorizationProvider> authorizationProvider,
           AssetGroupRelationshipManager manager)
        {
            // Setup valid data.
            action.Verb = ClearAction;
            action.CapabilityId = DeleteId;
            action.DeleteAgentId = null;
            relationship.Actions = new[] { action };
            parameters.Relationships = new[] { relationship };

            storedAssetGroup.Id = relationship.AssetGroupId;
            storedAssetGroup.ETag = relationship.ETag;
            storedAssetGroup.DeleteSharingRequestId = null;
            storedAssetGroup.ExportSharingRequestId = null;

            storedDeleteAgent.Id = storedAssetGroup.DeleteAgentId.Value;

            // Setup storage results.
            assetGroupReader
                .Setup(m => m.ReadByIdsAsync(It.IsAny<IEnumerable<Guid>>(), ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { storedAssetGroup });

            dataOwnerReader
                .Setup(m => m.ReadByIdAsync(storedDeleteAgent.OwnerId, ExpandOptions.None))
                .ReturnsAsync(storedDataOwner);

            storageWriter
                .Setup(m => m.UpdateEntitiesAsync(It.IsAny<IEnumerable<Entity>>()))
                .Returns<IEnumerable<Entity>>(x => Task.FromResult(x));

            // Execute.
            await manager.ApplyChanges(parameters).ConfigureAwait(false);

            // Verify authorization occurs.
            Action<Func<Task<IEnumerable<DataOwner>>>> verify = f =>
            {
                var owners = f().ConfigureAwait(false).GetAwaiter().GetResult();
                owners.SequenceAssert(new[] { storedDataOwner }, Assert.Equal);
            };

            authorizationProvider.Verify(m => m.TryAuthorizeAsync(AuthorizationRole.ServiceEditor, Is.Value(verify)), Times.Once);
        }

        [Theory(DisplayName = "When delete agent id is null, then do not consider it as set."), AutoMoqData(true)]
        public async Task When_DeleteAgentNull_Then_DoNotGetItsOwner(
           SetAgentRelationshipParameters.Action action,
           SetAgentRelationshipParameters.Relationship relationship,
           SetAgentRelationshipParameters parameters,
           AssetGroup storedAssetGroup,
           [Frozen] DeleteAgent storedDeleteAgent,
           [Frozen] Mock<IEntityReader<AssetGroup>> assetGroupReader,
           [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
           [Frozen] Mock<IDataOwnerReader> dataOwnerReader,
           AssetGroupRelationshipManager manager)
        {
            // Setup valid data.
            action.Verb = ClearAction;
            action.CapabilityId = DeleteId;
            action.DeleteAgentId = null;
            relationship.Actions = new[] { action };
            parameters.Relationships = new[] { relationship };

            storedAssetGroup.Id = relationship.AssetGroupId;
            storedAssetGroup.ETag = relationship.ETag;
            storedAssetGroup.DeleteSharingRequestId = null;
            storedAssetGroup.ExportSharingRequestId = null;
            storedAssetGroup.DeleteAgentId = Guid.Empty;

            storedDeleteAgent.Id = storedAssetGroup.DeleteAgentId.Value;

            // Setup storage results.
            assetGroupReader
                .Setup(m => m.ReadByIdsAsync(It.IsAny<IEnumerable<Guid>>(), ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { storedAssetGroup });
            
            storageWriter
                .Setup(m => m.UpdateEntitiesAsync(It.IsAny<IEnumerable<Entity>>()))
                .Returns<IEnumerable<Entity>>(x => Task.FromResult(x));

            // Execute.
            await manager.ApplyChanges(parameters).ConfigureAwait(false);

            dataOwnerReader
                .Verify(m => m.ReadByIdAsync(storedDeleteAgent.OwnerId, ExpandOptions.None), Times.Never);
        }
        #endregion

        #region Valid SET scenarios that result in direct asset group updates
        [Theory(DisplayName = "When SET DELETE for asset groups that share owner with the agent, then assign delete agent id and update agent capabilities."), AutoMoqData(true)]
        public async Task When_SETDELETE_ForAssetGroupsSharingOwnerWithAgent_Then_Pass(
            SetAgentRelationshipParameters.Action action,
            SetAgentRelationshipParameters.Relationship relationship,
            SetAgentRelationshipParameters parameters,
            AssetGroup storedAssetGroup,
            SharingRequest storedSharingRequest,
            SharingRelationship storedSharingRelationship,
            [Frozen] DeleteAgent storedDeleteAgent,
            [Frozen] Mock<IEntityReader<AssetGroup>> assetGroupReader,
            [Frozen] Mock<ISharingRequestReader> sharingRequestReader,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            AssetGroupRelationshipManager manager)
        {
            // Setup valid data.
            action.Verb = SetAction;
            action.CapabilityId = DeleteId;
            relationship.Actions = new[] { action };
            parameters.Relationships = new[] { relationship };

            storedAssetGroup.Id = relationship.AssetGroupId;
            storedAssetGroup.ETag = relationship.ETag;
            storedAssetGroup.DeleteAgentId = Guid.NewGuid(); // Make sure it changes.
            storedAssetGroup.ExportAgentId = Guid.NewGuid(); // Should not be cleared.
            storedAssetGroup.DeleteSharingRequestId = Guid.NewGuid(); // Should be cleared.
            storedAssetGroup.ExportSharingRequestId = null; // Should be cleared.

            storedDeleteAgent.Id = action.DeleteAgentId.Value;
            storedDeleteAgent.OwnerId = storedAssetGroup.OwnerId;
            storedDeleteAgent.Capabilities = new[] { ExportId }; // Make sure it is additive.

            storedSharingRequest.Relationships.Clear();
            storedSharingRequest.Relationships[storedAssetGroup.Id] = storedSharingRelationship;
            storedSharingRelationship.Capabilities = new[] { DeleteId }; // Should remove the entire request.

            // Verify data written to storage.
            Action<IEnumerable<Entity>> verify = x =>
            {
                Assert.Equal(3, x.Count()); // The asset group, the agent, and the sharing request.
                x.Select(this.VerifyTrackingDetailsUpdates);

                var savedDeleteAgent = x.Single(y => y.Id == storedDeleteAgent.Id) as DeleteAgent; // Guarantees the agent is saved.
                savedDeleteAgent.Capabilities.OrderBy(_ => _).SequenceAssert(new[] { DeleteId, ExportId }, Assert.Equal);

                var savedAssetGroup = x.Single(y => y.Id == storedAssetGroup.Id) as AssetGroup; // Guarantees the asset is saved.
                Assert.Equal(storedDeleteAgent.Id, savedAssetGroup.DeleteAgentId);
                Assert.NotNull(savedAssetGroup.ExportAgentId);
                Assert.NotEqual(storedDeleteAgent.Id, savedAssetGroup.ExportAgentId);
                Assert.Null(savedAssetGroup.DeleteSharingRequestId);
                Assert.Null(savedAssetGroup.ExportSharingRequestId);

                Assert.True(storedSharingRequest.IsDeleted);
            };

            // Setup storage results.
            assetGroupReader
                .Setup(m => m.ReadByIdsAsync(It.IsAny<IEnumerable<Guid>>(), ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { storedAssetGroup });

            sharingRequestReader
                .Setup(m => m.ReadByIdsAsync(new[] { storedAssetGroup.DeleteSharingRequestId.Value }, ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { storedSharingRequest });

            storageWriter
                .Setup(m => m.UpdateEntitiesAsync(Is.Value(verify)))
                .Returns<IEnumerable<Entity>>(x => Task.FromResult(x));

            // Execute.
            var response = await manager.ApplyChanges(parameters).ConfigureAwait(false);

            // Verify response mapping.
            Assert.Single(response.Results);

            var responseAssetGroup = response.Results.Single(x => x.AssetGroupId == storedAssetGroup.Id);
            Assert.Equal(storedAssetGroup.ETag, responseAssetGroup.ETag);

            Assert.Single(responseAssetGroup.Capabilities);

            var capabilityResult = responseAssetGroup.Capabilities.Single(x => x.CapabilityId == DeleteId);
            Assert.Equal(UpdatedStatus, capabilityResult.Status);
            Assert.Null(capabilityResult.SharingRequestId);
        }

        [Theory(DisplayName = "When SET EXPORT for asset groups that share owner with the agent, then assign delete agent id and update agent capabilities."), AutoMoqData(true)]
        public async Task When_SETEXPORT_ForAssetGroupsSharingOwnerWithAgent_Then_Pass(
            SetAgentRelationshipParameters.Action action,
            SetAgentRelationshipParameters.Relationship relationship,
            SetAgentRelationshipParameters parameters,
            AssetGroup storedAssetGroup,
            SharingRequest storedSharingRequest,
            SharingRelationship storedSharingRelationship,
            [Frozen] DeleteAgent storedDeleteAgent,
            [Frozen] Mock<IEntityReader<AssetGroup>> assetGroupReader,
            [Frozen] Mock<ISharingRequestReader> sharingRequestReader,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            AssetGroupRelationshipManager manager)
        {
            // Setup valid data.
            action.Verb = SetAction;
            action.CapabilityId = ExportId;
            relationship.Actions = new[] { action };
            parameters.Relationships = new[] { relationship };

            storedAssetGroup.Id = relationship.AssetGroupId;
            storedAssetGroup.ETag = relationship.ETag;
            storedAssetGroup.DeleteAgentId = null; // Should not be set.
            storedAssetGroup.DeleteSharingRequestId = Guid.NewGuid();
            storedAssetGroup.ExportAgentId = null; // Make sure it changes.
            storedAssetGroup.ExportSharingRequestId = storedAssetGroup.DeleteSharingRequestId;

            storedDeleteAgent.Id = action.DeleteAgentId.Value;
            storedDeleteAgent.OwnerId = storedAssetGroup.OwnerId;
            storedDeleteAgent.Capabilities = null; // Make sure it handles null.

            storedSharingRequest.Relationships.Clear();
            storedSharingRequest.Relationships[storedAssetGroup.Id] = storedSharingRelationship;
            storedSharingRelationship.Capabilities = new[] { DeleteId, ExportId }; // Should remove just the Export capability.

            // Verify data written to storage.
            Action<IEnumerable<Entity>> verify = x =>
            {
                Assert.Equal(3, x.Count()); // The asset group, the agent, and the sharing request.

                var savedDeleteAgent = x.Single(y => y.Id == storedDeleteAgent.Id) as DeleteAgent; // Guarantees the agent is saved.
                savedDeleteAgent.Capabilities.OrderBy(_ => _).SequenceAssert(new[] { ExportId }, Assert.Equal);

                var savedAssetGroup = x.Single(y => y.Id == storedAssetGroup.Id) as AssetGroup; // Guarantees the asset is saved.
                Assert.Equal(storedDeleteAgent.Id, savedAssetGroup.ExportAgentId);
                Assert.Null(savedAssetGroup.DeleteAgentId);

                Assert.Equal(new[] { DeleteId }, storedSharingRelationship.Capabilities);
            };

            // Setup storage results.
            assetGroupReader
                .Setup(m => m.ReadByIdsAsync(It.IsAny<IEnumerable<Guid>>(), ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { storedAssetGroup });

            sharingRequestReader
                .Setup(m => m.ReadByIdsAsync(new[] { storedAssetGroup.DeleteSharingRequestId.Value }, ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { storedSharingRequest });

            storageWriter
                .Setup(m => m.UpdateEntitiesAsync(Is.Value(verify)))
                .Returns<IEnumerable<Entity>>(x => Task.FromResult(x));

            // Execute.
            var response = await manager.ApplyChanges(parameters).ConfigureAwait(false);

            // Verify response mapping.
            Assert.Single(response.Results);

            var responseAssetGroup = response.Results.Single(x => x.AssetGroupId == storedAssetGroup.Id);
            Assert.Equal(storedAssetGroup.ETag, responseAssetGroup.ETag);

            Assert.Single(responseAssetGroup.Capabilities);

            var capabilityResult = responseAssetGroup.Capabilities.Single(x => x.CapabilityId == ExportId);
            Assert.Equal(UpdatedStatus, capabilityResult.Status);
            Assert.Null(capabilityResult.SharingRequestId);
        }

        [Theory(DisplayName = "When SET EXPORT for one asset group and SET DELETE for another that share owner with the agent, then update agent capabilities correctly."), ValidData]
        public async Task When_SETEXPORT_And_SETDELETE_ForAssetGroupsSharingOwnerWithAgent_Then_Pass(
            SetAgentRelationshipParameters.Action action1,
            SetAgentRelationshipParameters.Action action2,
            SetAgentRelationshipParameters.Relationship relationship1,
            SetAgentRelationshipParameters.Relationship relationship2,
            SetAgentRelationshipParameters parameters,
            AssetGroup storedAssetGroup1,
            AssetGroup storedAssetGroup2,
            [Frozen] DeleteAgent storedDeleteAgent,
            [Frozen] Mock<IEntityReader<AssetGroup>> assetGroupReader,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            AssetGroupRelationshipManager manager)
        {
            // Setup valid data.
            action1.Verb = SetAction;
            action1.CapabilityId = DeleteId;
            action2.Verb = SetAction;
            action2.CapabilityId = ExportId;
            action2.DeleteAgentId = action1.DeleteAgentId;

            relationship1.Actions = new[] { action1 };
            relationship2.Actions = new[] { action2 };
            parameters.Relationships = new[] { relationship1, relationship2 };

            storedAssetGroup1.Id = relationship1.AssetGroupId;
            storedAssetGroup1.ETag = relationship1.ETag;
            storedAssetGroup2.Id = relationship2.AssetGroupId;
            storedAssetGroup2.ETag = relationship2.ETag;
            storedAssetGroup2.OwnerId = storedAssetGroup1.OwnerId;

            storedDeleteAgent.Id = action1.DeleteAgentId.Value;
            storedDeleteAgent.OwnerId = storedAssetGroup1.OwnerId;
            storedDeleteAgent.Capabilities = null; // Make sure it handles null.

            // Verify data written to storage.
            Action<IEnumerable<Entity>> verify = x =>
            {
                Assert.Equal(3, x.Count());

                var savedDeleteAgent = x.Single(y => y.Id == storedDeleteAgent.Id) as DeleteAgent; // Guarantees the agent is saved.
                savedDeleteAgent.Capabilities.OrderBy(_ => _).SequenceAssert(new[] { DeleteId, ExportId }, Assert.Equal);

                Assert.NotNull(x.Single(y => y.Id == storedAssetGroup1.Id));
                Assert.NotNull(x.Single(y => y.Id == storedAssetGroup2.Id));
            };

            // Setup storage results.
            assetGroupReader
                .Setup(m => m.ReadByIdsAsync(It.IsAny<IEnumerable<Guid>>(), ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { storedAssetGroup1, storedAssetGroup2 });

            storageWriter
                .Setup(m => m.UpdateEntitiesAsync(Is.Value(verify)))
                .Returns<IEnumerable<Entity>>(x => Task.FromResult(x));

            // Execute.
            var response = await manager.ApplyChanges(parameters).ConfigureAwait(false);

            // Verify response mapping.
            Assert.Equal(2, response.Results.Count());
        }        
        #endregion

        #region Valid SET scenarios that result in requests
        [Theory(DisplayName = "When SET DELETE for asset groups that require requests and request does not already exist, then create the request in storage."), ValidData]
        public async Task When_SETDELETE_ForAssetGroupsWithNoExistingRequests_Then_Pass(
            SetAgentRelationshipParameters.Action action,
            SetAgentRelationshipParameters.Relationship relationship,
            SetAgentRelationshipParameters parameters,
            AssetGroup storedAssetGroup,
            [Frozen] DataOwner storedDataOwner,
            [Frozen] DeleteAgent storedDeleteAgent,
            [Frozen] Mock<IEntityReader<AssetGroup>> assetGroupReader,
            [Frozen] Mock<ISharingRequestReader> sharingRequestReader,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            AssetGroupRelationshipManager manager)
        {
            // Setup valid data.
            action.Verb = SetAction;
            action.CapabilityId = DeleteId;
            relationship.Actions = new[] { action };
            parameters.Relationships = new[] { relationship };

            storedAssetGroup.Id = relationship.AssetGroupId;
            storedAssetGroup.ETag = relationship.ETag;
            storedAssetGroup.DeleteAgentId = Guid.NewGuid(); // Should be cleared.
            storedAssetGroup.ExportAgentId = null; // Should be cleared.
            storedAssetGroup.DeleteSharingRequestId = Guid.NewGuid(); // Should be updated.
            storedAssetGroup.ExportSharingRequestId = Guid.NewGuid(); // Should not change.

            storedDeleteAgent.Id = action.DeleteAgentId.Value;
            storedDeleteAgent.SharingEnabled = true;

            SharingRequest savedSharingRequest = null;

            // Verify data written to storage.
            Action<IEnumerable<Entity>> verify = x =>
            {
                Assert.Equal(3, x.Count());
                var savedDataOwner = x.Single(y => y is DataOwner); // Data owner must be saved as to ensure consistency.
                Assert.Equal(storedDataOwner.Id, savedDataOwner.Id);

                savedSharingRequest = x.Single(y => y is SharingRequest) as SharingRequest;
                Assert.NotEqual(Guid.Empty, savedSharingRequest.Id);
                Assert.False(savedSharingRequest.IsDeleted);
                Assert.Equal(storedAssetGroup.OwnerId, savedSharingRequest.OwnerId);
                Assert.Equal(storedDataOwner.Name, savedSharingRequest.OwnerName);

                Assert.Single(savedSharingRequest.Relationships);

                var relation = savedSharingRequest.Relationships[storedAssetGroup.Id];
                Assert.Equal(storedAssetGroup.Id, relation.AssetGroupId);
                Assert.Equal(storedAssetGroup.Qualifier, relation.AssetQualifier);
                relation.Capabilities.SequenceAssert(new[] { DeleteId }, Assert.Equal);

                Assert.Equal(savedSharingRequest.Id, storedAssetGroup.DeleteSharingRequestId);
                Assert.NotEqual(savedSharingRequest.Id, storedAssetGroup.ExportSharingRequestId);

                var savedAssetGroup = x.Single(y => y is AssetGroup) as AssetGroup;
                Assert.Null(savedAssetGroup.DeleteAgentId);
                Assert.Null(savedAssetGroup.ExportAgentId);
            };

            // Setup storage results.
            assetGroupReader
                .Setup(m => m.ReadByIdsAsync(It.IsAny<IEnumerable<Guid>>(), ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { storedAssetGroup });

            sharingRequestReader
                .Setup(m => m.ReadByFiltersAsync(It.IsAny<SharingRequestFilterCriteria>(), ExpandOptions.WriteProperties))
                .ReturnsAsync(new FilterResult<SharingRequest>()); // Return no results.

            storageWriter
                .Setup(m => m.UpdateEntitiesAsync(Is.Value(verify)))
                .Returns<IEnumerable<Entity>>(x => Task.FromResult(x));

            // Execute.
            var response = await manager.ApplyChanges(parameters).ConfigureAwait(false);

            // Verify response mapping.
            Assert.Single(response.Results);

            var responseAssetGroup = response.Results.Single(x => x.AssetGroupId == storedAssetGroup.Id);
            Assert.Equal(storedAssetGroup.ETag, responseAssetGroup.ETag);

            Assert.Single(responseAssetGroup.Capabilities);

            var capabilityResult = responseAssetGroup.Capabilities.Single(x => x.CapabilityId == DeleteId);
            Assert.Equal(RequestedStatus, capabilityResult.Status);
            Assert.Equal(savedSharingRequest.Id, capabilityResult.SharingRequestId);
        }

        [Theory(DisplayName = "When SET EXPORT for asset groups that require requests and request already exists, then update the request in storage."), ValidData]
        public async Task When_SETEXPORT_ForAssetGroupsWithExistingRequests_Then_Pass(
            SetAgentRelationshipParameters.Action action,
            SetAgentRelationshipParameters.Relationship relationship,
            SetAgentRelationshipParameters parameters,
            AssetGroup storedAssetGroup,
            SharingRequest storedSharingRequest,
            SharingRelationship storedSharingRelationship,
            [Frozen] DataOwner storedDataOwner,
            [Frozen] DeleteAgent storedDeleteAgent,
            [Frozen] Mock<IEntityReader<AssetGroup>> assetGroupReader,
            [Frozen] Mock<ISharingRequestReader> sharingRequestReader,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            AssetGroupRelationshipManager manager)
        {
            // Setup valid data.
            action.Verb = SetAction;
            action.CapabilityId = ExportId;
            relationship.Actions = new[] { action };
            parameters.Relationships = new[] { relationship };

            storedAssetGroup.Id = relationship.AssetGroupId;
            storedAssetGroup.ETag = relationship.ETag;
            storedAssetGroup.DeleteAgentId = null; // Should not be set.
            storedAssetGroup.ExportAgentId = null; // Should not be set.
            storedAssetGroup.DeleteSharingRequestId = null; // Should be corrected.
            storedAssetGroup.ExportSharingRequestId = null; // Should be updated.

            storedDeleteAgent.Id = action.DeleteAgentId.Value;
            storedDeleteAgent.SharingEnabled = true;

            storedSharingRequest.Relationships[storedAssetGroup.Id] = storedSharingRelationship;
            storedSharingRelationship.Capabilities = new[] { DeleteId }; // Make sure it is not cleared.

            // Verify data written to storage.
            Action<IEnumerable<Entity>> verify = x =>
            {
                Assert.Equal(2, x.Count());

                var savedSharingRequest = x.Single(y => y is SharingRequest) as SharingRequest;
                Assert.Equal(storedSharingRequest.Id, savedSharingRequest.Id);
                Assert.Equal(storedDataOwner.Name, savedSharingRequest.OwnerName);

                Assert.True(savedSharingRequest.Relationships.Count() > 1); // Should have existing values as well.

                var relation = savedSharingRequest.Relationships[storedAssetGroup.Id];
                relation.Capabilities.SequenceAssert(new[] { DeleteId, ExportId }, Assert.Equal); // Should be unioned with existing values.

                Assert.Equal(savedSharingRequest.Id, storedAssetGroup.ExportSharingRequestId);
                Assert.Equal(savedSharingRequest.Id, storedAssetGroup.DeleteSharingRequestId); // Should correct this if not correct before.
                Assert.Null(storedAssetGroup.DeleteAgentId);
                Assert.Null(storedAssetGroup.ExportAgentId);
            };

            // Setup storage results.
            assetGroupReader
                .Setup(m => m.ReadByIdsAsync(It.IsAny<IEnumerable<Guid>>(), ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { storedAssetGroup });

            sharingRequestReader
                .Setup(m => m.ReadByFiltersAsync(It.IsAny<SharingRequestFilterCriteria>(), ExpandOptions.WriteProperties))
                .ReturnsAsync(new FilterResult<SharingRequest> { Total = 1, Values = new[] { storedSharingRequest } });

            storageWriter
                .Setup(m => m.UpdateEntitiesAsync(Is.Value(verify)))
                .Returns<IEnumerable<Entity>>(x => Task.FromResult(x));

            // Execute.
            var response = await manager.ApplyChanges(parameters).ConfigureAwait(false);

            // Verify response mapping.
            Assert.Single(response.Results);

            var responseAssetGroup = response.Results.Single(x => x.AssetGroupId == storedAssetGroup.Id);
            Assert.Equal(storedAssetGroup.ETag, responseAssetGroup.ETag);

            Assert.Single(responseAssetGroup.Capabilities);

            var capabilityResult = responseAssetGroup.Capabilities.Single(x => x.CapabilityId == ExportId);
            Assert.Equal(RequestedStatus, capabilityResult.Status);
            Assert.Equal(storedSharingRequest.Id, capabilityResult.SharingRequestId);
        }
        #endregion

        #region Valid CLEAR scenarios
        [Theory(DisplayName = "When CLEAR DELETE for asset groups that have single pending requests, then delete the request in storage."), ValidData]
        public async Task When_CLEARDELETE_ForAssetGroupsWithSingleExistingRequests_Then_Pass(
            SetAgentRelationshipParameters.Action action,
            SetAgentRelationshipParameters.Relationship relationship,
            SetAgentRelationshipParameters parameters,
            AssetGroup storedAssetGroup,
            SharingRequest storedSharingRequest,
            SharingRelationship storedSharingRelationship,
            DeleteAgent storedDeleteAgent,
            [Frozen] Mock<IEntityReader<AssetGroup>> assetGroupReader,
            [Frozen] Mock<ISharingRequestReader> sharingRequestReader,
            [Frozen] Mock<IDeleteAgentReader> deleteAgentReader,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            AssetGroupRelationshipManager manager)
        {
            // Setup valid data.
            action.Verb = ClearAction;
            action.CapabilityId = DeleteId;
            action.DeleteAgentId = null;
            relationship.Actions = new[] { action };
            parameters.Relationships = new[] { relationship };

            storedAssetGroup.Id = relationship.AssetGroupId;
            storedAssetGroup.ETag = relationship.ETag;
            storedAssetGroup.DeleteAgentId = Guid.NewGuid(); // Should be removed.
            storedAssetGroup.DeleteSharingRequestId = Guid.NewGuid(); // Should be removed.

            storedSharingRequest.Relationships.Clear(); // Should remove the entire request.
            storedSharingRequest.Relationships[storedAssetGroup.Id] = storedSharingRelationship; // Should be removed.
            storedSharingRelationship.Capabilities = new[] { DeleteId }; // Should remove the entire relationship.

            // Verify data written to storage.
            Action<IEnumerable<Entity>> verify = x =>
            {
                Assert.Equal(3, x.Count());
                x.Select(this.VerifyTrackingDetailsUpdates);

                var savedSharingRequest = x.Single(y => y is SharingRequest) as SharingRequest;
                Assert.Equal(storedSharingRequest.Id, savedSharingRequest.Id);
                Assert.True(savedSharingRequest.IsDeleted);
                Assert.Null(storedAssetGroup.DeleteAgentId); // Should be cleared.
                Assert.Null(storedAssetGroup.DeleteSharingRequestId); // Should be cleared.
            };

            // Setup storage results.
            assetGroupReader
                .Setup(m => m.ReadByIdsAsync(It.IsAny<IEnumerable<Guid>>(), ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { storedAssetGroup });

            sharingRequestReader
                .Setup(m => m.ReadByIdsAsync(new[] { storedAssetGroup.DeleteSharingRequestId.Value }, ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { storedSharingRequest });

            deleteAgentReader
                .Setup(m => m.ReadByIdsAsync(new[] { storedAssetGroup.DeleteAgentId.Value }, ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { storedDeleteAgent });

            storageWriter
                .Setup(m => m.UpdateEntitiesAsync(Is.Value(verify)))
                .Returns<IEnumerable<Entity>>(x => Task.FromResult(x));

            // Execute.
            var response = await manager.ApplyChanges(parameters).ConfigureAwait(false);

            // Verify response mapping.
            Assert.Single(response.Results);

            var responseAssetGroup = response.Results.Single(x => x.AssetGroupId == storedAssetGroup.Id);
            Assert.Equal(storedAssetGroup.ETag, responseAssetGroup.ETag);

            Assert.Single(responseAssetGroup.Capabilities);

            var capabilityResult = responseAssetGroup.Capabilities.Single(x => x.CapabilityId == DeleteId);
            Assert.Equal(RemovedStatus, capabilityResult.Status);
            Assert.Null(capabilityResult.SharingRequestId);
        }

        [Theory(DisplayName = "When CLEAR EXPORT for asset groups that having many pending requests for the same asset group, then update the request in storage."), ValidData]
        public async Task When_CLEAREXPORT_ForAssetGroupsWithMultipleExistingRequestsSameAssetGroup_Then_Pass(
            SetAgentRelationshipParameters.Action action,
            SetAgentRelationshipParameters.Relationship relationship,
            SetAgentRelationshipParameters parameters,
            AssetGroup storedAssetGroup,
            SharingRequest storedSharingRequest,
            SharingRelationship storedSharingRelationship,
            DeleteAgent storedDeleteAgent,
            [Frozen] Mock<IEntityReader<AssetGroup>> assetGroupReader,
            [Frozen] Mock<ISharingRequestReader> sharingRequestReader,
            [Frozen] Mock<IDeleteAgentReader> deleteAgentReader,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            AssetGroupRelationshipManager manager)
        {
            // Setup valid data.
            action.Verb = ClearAction;
            action.CapabilityId = ExportId;
            action.DeleteAgentId = null;
            relationship.Actions = new[] { action };
            parameters.Relationships = new[] { relationship };

            storedAssetGroup.Id = relationship.AssetGroupId;
            storedAssetGroup.ETag = relationship.ETag;
            storedAssetGroup.ExportAgentId = Guid.NewGuid(); // Should be removed.
            storedAssetGroup.ExportSharingRequestId = Guid.NewGuid(); // Should be removed.

            storedSharingRequest.Relationships[storedAssetGroup.Id] = storedSharingRelationship;
            storedSharingRelationship.Capabilities = new[] { DeleteId, ExportId }; // Should remove just the ExportId.

            // Verify data written to storage.
            Action<IEnumerable<Entity>> verify = x =>
            {
                Assert.Equal(3, x.Count());

                var savedSharingRequest = x.Single(y => y is SharingRequest) as SharingRequest;
                Assert.Equal(storedSharingRequest.Id, savedSharingRequest.Id);

                var savedRelationship = savedSharingRequest.Relationships[storedAssetGroup.Id];
                savedRelationship.Capabilities.SequenceAssert(new[] { DeleteId }, Assert.Equal);
                
                Assert.Null(storedAssetGroup.ExportAgentId); // Should be cleared.
                Assert.Null(storedAssetGroup.ExportSharingRequestId); // Should be cleared.
            };

            // Setup storage results.
            assetGroupReader
                .Setup(m => m.ReadByIdsAsync(It.IsAny<IEnumerable<Guid>>(), ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { storedAssetGroup });

            sharingRequestReader
                .Setup(m => m.ReadByIdsAsync(new[] { storedAssetGroup.ExportSharingRequestId.Value }, ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { storedSharingRequest });

            deleteAgentReader
                .Setup(m => m.ReadByIdsAsync(new[] { storedAssetGroup.ExportAgentId.Value }, ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { storedDeleteAgent });

            storageWriter
                .Setup(m => m.UpdateEntitiesAsync(Is.Value(verify)))
                .Returns<IEnumerable<Entity>>(x => Task.FromResult(x));

            // Execute.
            var response = await manager.ApplyChanges(parameters).ConfigureAwait(false);

            // Verify response mapping.
            Assert.Single(response.Results);

            var responseAssetGroup = response.Results.Single(x => x.AssetGroupId == storedAssetGroup.Id);
            Assert.Equal(storedAssetGroup.ETag, responseAssetGroup.ETag);

            Assert.Single(responseAssetGroup.Capabilities);

            var capabilityResult = responseAssetGroup.Capabilities.Single(x => x.CapabilityId == ExportId);
            Assert.Equal(RemovedStatus, capabilityResult.Status);
            Assert.Null(capabilityResult.SharingRequestId);
        }

        [Theory(DisplayName = "When CLEAR EXPORT for asset groups that having many pending requests across asset groups, then update the request in storage."), ValidData]
        public async Task When_CLEAREXPORT_ForAssetGroupsWithMultipleExistingRequests_Then_Pass(
            SetAgentRelationshipParameters.Action action,
            SetAgentRelationshipParameters.Relationship relationship,
            SetAgentRelationshipParameters parameters,
            AssetGroup storedAssetGroup,
            SharingRequest storedSharingRequest,
            SharingRelationship storedSharingRelationship,
            DeleteAgent storedDeleteAgent,
            [Frozen] Mock<IEntityReader<AssetGroup>> assetGroupReader,
            [Frozen] Mock<ISharingRequestReader> sharingRequestReader,
            [Frozen] Mock<IDeleteAgentReader> deleteAgentReader,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            AssetGroupRelationshipManager manager)
        {
            // Setup valid data.
            action.Verb = ClearAction;
            action.CapabilityId = ExportId;
            action.DeleteAgentId = null;
            relationship.Actions = new[] { action };
            parameters.Relationships = new[] { relationship };

            storedAssetGroup.Id = relationship.AssetGroupId;
            storedAssetGroup.ETag = relationship.ETag;
            storedAssetGroup.ExportAgentId = Guid.NewGuid(); // Should be removed.
            storedAssetGroup.ExportSharingRequestId = Guid.NewGuid(); // Should be removed.

            storedSharingRequest.Relationships[storedAssetGroup.Id] = storedSharingRelationship; // Should be removed.
            storedSharingRelationship.Capabilities = new[] { ExportId }; // Should remove the entire relationship.

            // Verify data written to storage.
            Action<IEnumerable<Entity>> verify = x =>
            {
                Assert.Equal(3, x.Count());

                var savedSharingRequest = x.Single(y => y is SharingRequest) as SharingRequest;
                Assert.Equal(storedSharingRequest.Id, savedSharingRequest.Id);
                Assert.False(savedSharingRequest.Relationships.ContainsKey(storedAssetGroup.Id)); // Should be removed.      
                Assert.Null(storedAssetGroup.ExportAgentId); // Should be cleared.
                Assert.Null(storedAssetGroup.ExportSharingRequestId); // Should be cleared.
            };

            // Setup storage results.
            assetGroupReader
                .Setup(m => m.ReadByIdsAsync(It.IsAny<IEnumerable<Guid>>(), ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { storedAssetGroup });

            sharingRequestReader
                .Setup(m => m.ReadByIdsAsync(new[] { storedAssetGroup.ExportSharingRequestId.Value }, ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { storedSharingRequest });

            deleteAgentReader
                .Setup(m => m.ReadByIdsAsync(new[] { storedAssetGroup.ExportAgentId.Value }, ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { storedDeleteAgent });

            storageWriter
                .Setup(m => m.UpdateEntitiesAsync(Is.Value(verify)))
                .Returns<IEnumerable<Entity>>(x => Task.FromResult(x));

            // Execute.
            var response = await manager.ApplyChanges(parameters).ConfigureAwait(false);

            // Verify response mapping.
            Assert.Single(response.Results);

            var responseAssetGroup = response.Results.Single(x => x.AssetGroupId == storedAssetGroup.Id);
            Assert.Equal(storedAssetGroup.ETag, responseAssetGroup.ETag);

            Assert.Single(responseAssetGroup.Capabilities);

            var capabilityResult = responseAssetGroup.Capabilities.Single(x => x.CapabilityId == ExportId);
            Assert.Equal(RemovedStatus, capabilityResult.Status);
            Assert.Null(capabilityResult.SharingRequestId);
        }

        [Theory(DisplayName = "When CLEAR DELETE for asset groups and no other asset group linked for delete to the agent, then remove delete from agent capabilities."), ValidData]
        public async Task When_CLEARDELETE_ForAssetGroupsNoOtherLinked_Then_RemoveDeleteAgentCapabilities(
            SetAgentRelationshipParameters.Action action,
            SetAgentRelationshipParameters.Relationship relationship,
            SetAgentRelationshipParameters parameters,
            AssetGroup storedAssetGroup,
            DeleteAgent storedDeleteAgent,
            [Frozen] Mock<IEntityReader<AssetGroup>> assetGroupReader,
            [Frozen] Mock<IDeleteAgentReader> deleteAgentReader,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            AssetGroupRelationshipManager manager)
        {
            // Setup valid data.
            action.Verb = ClearAction;
            action.CapabilityId = DeleteId;
            action.DeleteAgentId = null;
            relationship.Actions = new[] { action };
            parameters.Relationships = new[] { relationship };

            storedAssetGroup.Id = relationship.AssetGroupId;
            storedAssetGroup.ETag = relationship.ETag;
            storedAssetGroup.DeleteAgentId = storedDeleteAgent.Id;

            storedDeleteAgent.Capabilities = new[] { DeleteId }; // Delete should be removed.

            // Verify data written to storage.
            Action<IEnumerable<Entity>> verify = x =>
            {
                var agent = x.First(y => y is DeleteAgent) as DeleteAgent;
                agent.Capabilities.SequenceAssert(Enumerable.Empty<CapabilityId>(), Assert.Equal);
            };

            // Setup storage results.
            assetGroupReader
                .Setup(m => m.ReadByIdsAsync(It.IsAny<IEnumerable<Guid>>(), ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { storedAssetGroup });

            assetGroupReader
                .Setup(m => m.ReadByFiltersAsync(It.IsAny<IFilterCriteria<AssetGroup>>(), ExpandOptions.None))
                .ReturnsAsync(new FilterResult<AssetGroup> { Values = Enumerable.Empty<AssetGroup>() });

            deleteAgentReader
                .Setup(m => m.ReadByIdsAsync(new[] { storedAssetGroup.DeleteAgentId.Value }, ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { storedDeleteAgent });

            storageWriter
                .Setup(m => m.UpdateEntitiesAsync(Is.Value(verify)))
                .Returns<IEnumerable<Entity>>(x => Task.FromResult(x));

            // Execute.
            await manager.ApplyChanges(parameters).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When CLEAR DELETE for asset groups and some other asset group linked for delete to the agent, then keep delete in agent capabilities."), ValidData]
        public async Task When_CLEARDELETE_ForAssetGroupsAndOthersLinked_Then_KeepDeleteAgentCapabilities(
            SetAgentRelationshipParameters.Action action,
            SetAgentRelationshipParameters.Relationship relationship,
            SetAgentRelationshipParameters parameters,
            AssetGroup storedAssetGroup,
            AssetGroup remainingLinkedAssetGroup,
            DeleteAgent storedDeleteAgent,
            [Frozen] Mock<IEntityReader<AssetGroup>> assetGroupReader,
            [Frozen] Mock<IDeleteAgentReader> deleteAgentReader,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            AssetGroupRelationshipManager manager)
        {
            // Setup valid data.
            action.Verb = ClearAction;
            action.CapabilityId = DeleteId;
            action.DeleteAgentId = null;
            relationship.Actions = new[] { action };
            parameters.Relationships = new[] { relationship };

            storedAssetGroup.Id = relationship.AssetGroupId;
            storedAssetGroup.ETag = relationship.ETag;
            storedAssetGroup.DeleteAgentId = storedDeleteAgent.Id;

            remainingLinkedAssetGroup.DeleteAgentId = storedDeleteAgent.Id; // Ensure another asset is linked for delete.
            remainingLinkedAssetGroup.ExportAgentId = storedDeleteAgent.Id; // Ensure another asset is linked for export.
            storedDeleteAgent.Capabilities = new[] { DeleteId, ExportId }; // Both should be preserved.

            // Verify data written to storage.
            Action<IEnumerable<Entity>> verify = x =>
            {
                var agent = x.FirstOrDefault(y => y is DeleteAgent) as DeleteAgent;
                Assert.Null(agent); // Since no capabilities changed, we should not update the agent.
            };

            // Setup storage results.
            assetGroupReader
                .Setup(m => m.ReadByIdsAsync(It.IsAny<IEnumerable<Guid>>(), ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { storedAssetGroup });

            assetGroupReader
                .Setup(m => m.ReadByFiltersAsync(It.IsAny<IFilterCriteria<AssetGroup>>(), ExpandOptions.None))
                .ReturnsAsync(new FilterResult<AssetGroup> { Values = new[] { remainingLinkedAssetGroup } });

            deleteAgentReader
                .Setup(m => m.ReadByIdsAsync(new[] { storedAssetGroup.DeleteAgentId.Value }, ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { storedDeleteAgent });

            storageWriter
                .Setup(m => m.UpdateEntitiesAsync(Is.Value(verify)))
                .Returns<IEnumerable<Entity>>(x => Task.FromResult(x));

            // Execute.
            await manager.ApplyChanges(parameters).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When CLEAR EXPORT for asset groups and no other asset group linked for export to the agent, then remove export from agent capabilities."), ValidData]
        public async Task When_CLEAREXPORT_ForAssetGroupsNoOtherLinked_Then_RemoveExportAgentCapabilities(
            SetAgentRelationshipParameters.Action action,
            SetAgentRelationshipParameters.Relationship relationship,
            SetAgentRelationshipParameters parameters,
            AssetGroup storedAssetGroup,
            AssetGroup remainingLinkedAssetGroup,
            DeleteAgent storedDeleteAgent,
            [Frozen] Mock<IEntityReader<AssetGroup>> assetGroupReader,
            [Frozen] Mock<IDeleteAgentReader> deleteAgentReader,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            AssetGroupRelationshipManager manager)
        {
            // Setup valid data.
            action.Verb = ClearAction;
            action.CapabilityId = ExportId;
            action.DeleteAgentId = null;
            relationship.Actions = new[] { action };
            parameters.Relationships = new[] { relationship };

            storedAssetGroup.Id = relationship.AssetGroupId;
            storedAssetGroup.ETag = relationship.ETag;
            storedAssetGroup.ExportAgentId = storedDeleteAgent.Id;

            remainingLinkedAssetGroup.DeleteAgentId = storedDeleteAgent.Id; // Ensure another asset is linked for delete.
            storedDeleteAgent.Capabilities = new[] { DeleteId, ExportId }; // Delete should be preserved; Export removed.

            // Verify data written to storage.
            Action<IEnumerable<Entity>> verify = x =>
            {
                var agent = x.First(y => y is DeleteAgent) as DeleteAgent;
                agent.Capabilities.SequenceAssert(new[] { DeleteId }, Assert.Equal);
            };

            // Setup storage results.
            assetGroupReader
                .Setup(m => m.ReadByIdsAsync(It.IsAny<IEnumerable<Guid>>(), ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { storedAssetGroup });

            assetGroupReader
                .Setup(m => m.ReadByFiltersAsync(It.IsAny<IFilterCriteria<AssetGroup>>(), ExpandOptions.None))
                .ReturnsAsync(new FilterResult<AssetGroup> { Values = new[] { remainingLinkedAssetGroup } });

            deleteAgentReader
                .Setup(m => m.ReadByIdsAsync(new[] { storedAssetGroup.ExportAgentId.Value }, ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { storedDeleteAgent });

            storageWriter
                .Setup(m => m.UpdateEntitiesAsync(Is.Value(verify)))
                .Returns<IEnumerable<Entity>>(x => Task.FromResult(x));

            // Execute.
            await manager.ApplyChanges(parameters).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When CLEAR EXPORT for asset groups and some other asset group linked for export to the agent, then keep export in agent capabilities."), ValidData]
        public async Task When_CLEAREXPORT_ForAssetGroupsAndOthersLinked_Then_KeepExportAgentCapabilities(
            SetAgentRelationshipParameters.Action action,
            SetAgentRelationshipParameters.Relationship relationship,
            SetAgentRelationshipParameters parameters,
            AssetGroup storedAssetGroup,
            AssetGroup remainingLinkedAssetGroup,
            DeleteAgent storedDeleteAgent,
            [Frozen] Mock<IEntityReader<AssetGroup>> assetGroupReader,
            [Frozen] Mock<IDeleteAgentReader> deleteAgentReader,
            [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
            AssetGroupRelationshipManager manager)
        {
            // Setup valid data.
            action.Verb = ClearAction;
            action.CapabilityId = ExportId;
            action.DeleteAgentId = null;
            relationship.Actions = new[] { action };
            parameters.Relationships = new[] { relationship };

            storedAssetGroup.Id = relationship.AssetGroupId;
            storedAssetGroup.ETag = relationship.ETag;
            storedAssetGroup.ExportAgentId = storedDeleteAgent.Id;

            remainingLinkedAssetGroup.ExportAgentId = storedDeleteAgent.Id; // Ensure another asset is linked for export.
            storedDeleteAgent.Capabilities = new[] { DeleteId, ExportId }; // Delete should be removed; Export preserved.

            // Verify data written to storage.
            Action<IEnumerable<Entity>> verify = x =>
            {
                x.Select(this.VerifyTrackingDetailsUpdates);

                var agent = x.First(y => y is DeleteAgent) as DeleteAgent;
                agent.Capabilities.SequenceAssert(new[] { ExportId }, Assert.Equal);
            };

            // Setup storage results.
            assetGroupReader
                .Setup(m => m.ReadByIdsAsync(It.IsAny<IEnumerable<Guid>>(), ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { storedAssetGroup });

            assetGroupReader
                .Setup(m => m.ReadByFiltersAsync(It.IsAny<IFilterCriteria<AssetGroup>>(), ExpandOptions.None))
                .ReturnsAsync(new FilterResult<AssetGroup> { Values = new[] { remainingLinkedAssetGroup } });

            deleteAgentReader
                .Setup(m => m.ReadByIdsAsync(new[] { storedAssetGroup.ExportAgentId.Value }, ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { storedDeleteAgent });

            storageWriter
                .Setup(m => m.UpdateEntitiesAsync(Is.Value(verify)))
                .Returns<IEnumerable<Entity>>(x => Task.FromResult(x));

            // Execute.
            await manager.ApplyChanges(parameters).ConfigureAwait(false);
        }
        #endregion

        #region CLEAR scenarios for shared data agent owner
        [Theory(DisplayName = "When asset groups have different owners for CLEAR and all unlinked agent ids are equal and user is authorized for the agent owner, then process the clear request."), AutoMoqData(true)]
        public async Task ValidSharedAgentUnlinkScenario(
           SetAgentRelationshipParameters.Action action1,
           SetAgentRelationshipParameters.Action action2,
           SetAgentRelationshipParameters.Relationship relationship1,
           SetAgentRelationshipParameters.Relationship relationship2,
           SetAgentRelationshipParameters parameters,
           AssetGroup storedAssetGroup1,
           AssetGroup storedAssetGroup2,
           [Frozen] Mock<IEntityReader<AssetGroup>> assetGroupReader,
           [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
           [Frozen] Mock<IAuthorizationProvider> authorizationProvider,
           AssetGroupRelationshipManager manager)
        {
            // Setup valid data. (all links to remove have same agent id)
            action1.Verb = ClearAction;
            action1.CapabilityId = DeleteId;
            action1.DeleteAgentId = null;
            relationship1.Actions = new[] { action1 };
            action2.Verb = ClearAction;
            action2.CapabilityId = DeleteId;
            action2.DeleteAgentId = null;
            relationship2.Actions = new[] { action2 };
            parameters.Relationships = new[] { relationship1, relationship2 };

            storedAssetGroup1.Id = relationship1.AssetGroupId;
            storedAssetGroup1.ETag = relationship1.ETag;
            storedAssetGroup1.DeleteSharingRequestId = null;
            storedAssetGroup1.ExportSharingRequestId = null;

            storedAssetGroup2.Id = relationship2.AssetGroupId;
            storedAssetGroup2.ETag = relationship2.ETag;
            storedAssetGroup2.DeleteSharingRequestId = null;
            storedAssetGroup2.ExportSharingRequestId = null;
            storedAssetGroup2.DeleteAgentId = storedAssetGroup1.DeleteAgentId;

            // Setup storage results.
            assetGroupReader
                .Setup(m => m.ReadByIdsAsync(It.IsAny<IEnumerable<Guid>>(), ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { storedAssetGroup1, storedAssetGroup2 });

            authorizationProvider
                .Setup(m => m.TryAuthorizeAsync(It.IsAny<AuthorizationRole>(), It.IsAny<Func<Task<IEnumerable<DataOwner>>>>()))
                .ReturnsAsync(true);

            storageWriter
                .Setup(m => m.UpdateEntitiesAsync(It.IsAny<IEnumerable<Entity>>()))
                .Returns<IEnumerable<Entity>>(x => Task.FromResult(x));

            // Execute.
            var results = await manager.ApplyChanges(parameters).ConfigureAwait(false);

            // Verify.
            Assert.Equal(RemovedStatus, results.Results.Single(x => x.AssetGroupId == storedAssetGroup1.Id).Capabilities.Single().Status);
            Assert.Equal(RemovedStatus, results.Results.Single(x => x.AssetGroupId == storedAssetGroup2.Id).Capabilities.Single().Status);
        }

        [Theory(DisplayName = "When asset group loses owner id and agent id, then delete the asset group."), AutoMoqData(true)]
        public async Task DeleteAssetGroupIfOrphaned(
           SetAgentRelationshipParameters.Action action1,
           SetAgentRelationshipParameters.Relationship relationship1,
           SetAgentRelationshipParameters parameters,
           AssetGroup storedAssetGroup1,
           [Frozen] Mock<IEntityReader<AssetGroup>> assetGroupReader,
           [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
           [Frozen] Mock<IAuthorizationProvider> authorizationProvider,
           AssetGroupRelationshipManager manager)
        {
            // Setup valid data. (all links to remove have same agent id)
            action1.Verb = ClearAction;
            action1.CapabilityId = DeleteId;
            action1.DeleteAgentId = null;
            relationship1.Actions = new[] { action1 };
            parameters.Relationships = new[] { relationship1 };

            storedAssetGroup1.IsDeleted = false;
            storedAssetGroup1.OwnerId = Guid.Empty; // This one should be deleted.
            storedAssetGroup1.Id = relationship1.AssetGroupId;
            storedAssetGroup1.ETag = relationship1.ETag;
            storedAssetGroup1.DeleteSharingRequestId = null;
            storedAssetGroup1.ExportSharingRequestId = null;

            // Setup storage results.
            assetGroupReader
                .Setup(m => m.ReadByIdsAsync(It.IsAny<IEnumerable<Guid>>(), ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { storedAssetGroup1 });

            authorizationProvider
                .Setup(m => m.TryAuthorizeAsync(It.IsAny<AuthorizationRole>(), It.IsAny<Func<Task<IEnumerable<DataOwner>>>>()))
                .ReturnsAsync(true);

            storageWriter
                .Setup(m => m.UpdateEntitiesAsync(It.IsAny<IEnumerable<Entity>>()))
                .Returns<IEnumerable<Entity>>(x => Task.FromResult(x));

            // Execute.
            var results = await manager.ApplyChanges(parameters).ConfigureAwait(false);

            // Verify.
            Assert.Equal(RemovedStatus, results.Results.Single(x => x.AssetGroupId == storedAssetGroup1.Id).Capabilities.Single().Status);            
            Assert.True(storedAssetGroup1.IsDeleted);
        }

        [Theory(DisplayName = "When asset groups have different owners for CLEAR and all unlinked agent ids are equal but user is not authorized for the agent owner, then fail the request."), AutoMoqData(true)]
        public async Task IndalidSharedAgentUnlinkScenarioDueToNotAuthorized(
           SetAgentRelationshipParameters.Action action1,
           SetAgentRelationshipParameters.Action action2,
           SetAgentRelationshipParameters.Relationship relationship1,
           SetAgentRelationshipParameters.Relationship relationship2,
           SetAgentRelationshipParameters parameters,
           AssetGroup storedAssetGroup1,
           AssetGroup storedAssetGroup2,
           [Frozen] Mock<IEntityReader<AssetGroup>> assetGroupReader,
           [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
           [Frozen] Mock<IAuthorizationProvider> authorizationProvider,
           AssetGroupRelationshipManager manager)
        {
            // Setup valid data. (all links to remove have same agent id)
            action1.Verb = ClearAction;
            action1.CapabilityId = DeleteId;
            action1.DeleteAgentId = null;
            relationship1.Actions = new[] { action1 };
            action2.Verb = ClearAction;
            action2.CapabilityId = DeleteId;
            action2.DeleteAgentId = null;
            relationship2.Actions = new[] { action2 };
            parameters.Relationships = new[] { relationship1, relationship2 };

            storedAssetGroup1.Id = relationship1.AssetGroupId;
            storedAssetGroup1.ETag = relationship1.ETag;
            storedAssetGroup1.DeleteSharingRequestId = null;
            storedAssetGroup1.ExportSharingRequestId = null;

            storedAssetGroup2.Id = relationship2.AssetGroupId;
            storedAssetGroup2.ETag = relationship2.ETag;
            storedAssetGroup2.DeleteSharingRequestId = null;
            storedAssetGroup2.ExportSharingRequestId = null;
            storedAssetGroup2.DeleteAgentId = storedAssetGroup1.DeleteAgentId;

            // Setup storage results.
            assetGroupReader
                .Setup(m => m.ReadByIdsAsync(It.IsAny<IEnumerable<Guid>>(), ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { storedAssetGroup1, storedAssetGroup2 });

            authorizationProvider
                .Setup(m => m.TryAuthorizeAsync(It.IsAny<AuthorizationRole>(), It.IsAny<Func<Task<IEnumerable<DataOwner>>>>()))
                .ReturnsAsync(false);

            storageWriter
                .Setup(m => m.UpdateEntitiesAsync(It.IsAny<IEnumerable<Entity>>()))
                .Returns<IEnumerable<Entity>>(x => Task.FromResult(x));

            // Execute.
            await Assert.ThrowsAnyAsync<CoreException>(() => manager.ApplyChanges(parameters)).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When asset groups have different owners for CLEAR and unlinked agent ids are different, then fail the request."), AutoMoqData(true)]
        public async Task InvalidSharedAgentUnlinkScenarioDueToDifferentAgentIds(
           SetAgentRelationshipParameters.Action action1,
           SetAgentRelationshipParameters.Action action2,
           SetAgentRelationshipParameters.Relationship relationship1,
           SetAgentRelationshipParameters.Relationship relationship2,
           SetAgentRelationshipParameters parameters,
           AssetGroup storedAssetGroup1,
           AssetGroup storedAssetGroup2,
           [Frozen] Mock<IEntityReader<AssetGroup>> assetGroupReader,
           [Frozen] Mock<IPrivacyDataStorageWriter> storageWriter,
           [Frozen] Mock<IAuthorizationProvider> authorizationProvider,
           AssetGroupRelationshipManager manager)
        {
            // Setup valid data. (all links to remove have same agent id)
            action1.Verb = ClearAction;
            action1.CapabilityId = DeleteId;
            action1.DeleteAgentId = null;
            relationship1.Actions = new[] { action1 };
            action2.Verb = ClearAction;
            action2.CapabilityId = DeleteId;
            action2.DeleteAgentId = null;
            relationship2.Actions = new[] { action2 };
            parameters.Relationships = new[] { relationship1, relationship2 };

            storedAssetGroup1.Id = relationship1.AssetGroupId;
            storedAssetGroup1.ETag = relationship1.ETag;
            storedAssetGroup1.DeleteSharingRequestId = null;
            storedAssetGroup1.ExportSharingRequestId = null;

            storedAssetGroup2.Id = relationship2.AssetGroupId;
            storedAssetGroup2.ETag = relationship2.ETag;
            storedAssetGroup2.DeleteSharingRequestId = null;
            storedAssetGroup2.ExportSharingRequestId = null;

            // Setup storage results.
            assetGroupReader
                .Setup(m => m.ReadByIdsAsync(It.IsAny<IEnumerable<Guid>>(), ExpandOptions.WriteProperties))
                .ReturnsAsync(new[] { storedAssetGroup1, storedAssetGroup2 });

            authorizationProvider
                .Setup(m => m.TryAuthorizeAsync(It.IsAny<AuthorizationRole>(), It.IsAny<Func<Task<IEnumerable<DataOwner>>>>()))
                .ReturnsAsync(false);

            storageWriter
                .Setup(m => m.UpdateEntitiesAsync(It.IsAny<IEnumerable<Entity>>()))
                .Returns<IEnumerable<Entity>>(x => Task.FromResult(x));

            // Execute.
            await Assert.ThrowsAnyAsync<CoreException>(() => manager.ApplyChanges(parameters)).ConfigureAwait(false);
        }
        #endregion

        private Entity VerifyTrackingDetailsUpdates(Entity e)
        {
            Assert.Equal(2, e.TrackingDetails.Version);
            return e;
        }

        #region AutoFixture customizations
        public class ValidDataAttribute : AutoMoqDataAttribute
        {
            public ValidDataAttribute() : base(true)
            {
                this.Fixture.Customize<TrackingDetails>(obj =>
                    obj.With(x => x.Version, 1)); // Prove this is updated.

                this.Fixture.Customize<AssetGroup>(obj =>
                obj
                .Without(x => x.DeleteAgentId)
                .Without(x => x.DeleteSharingRequestId)
                .Without(x => x.ExportAgentId)
                .Without(x => x.ExportSharingRequestId)
                .Without(x => x.QualifierParts)
                .Do(x => x.QualifierParts = this.Fixture.Create<AssetQualifier>().Properties));
            }
        }

        public class InlineValidDataAttribute : InlineAutoMoqDataAttribute
        {
            public InlineValidDataAttribute(params object[] values) : base(new ValidDataAttribute(), values)
            {
            }
        }
        #endregion
    }
}