// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.Runner.UnitTests.Storage
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.TemplateBuilder;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Store;
    using Microsoft.PrivacyServices.DataMonitor.Runner.Data;
    using Microsoft.PrivacyServices.DataMonitor.Runner.Storage;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class ActionRefStoreTests
    {
        private readonly Mock<IActionLibraryAccessor> mockAccessor = new Mock<IActionLibraryAccessor>(MockBehavior.Strict);
        private readonly Mock<ITemplateStore> mockTemplateStore = new Mock<ITemplateStore>();
        private readonly Mock<IParseContext> mockParseCtx = new Mock<IParseContext>();
        private readonly Mock<IActionStore> mockActionStore = new Mock<IActionStore>();

        private const string TemplatePropName = "tpropname";
        private const string TemplateName = "tname";

        private const string UpdateTag = "UpdateTAG";
        private const string UpdateId = "UpdateID";
        private const string Tag = "TAG";
        private const string Id = "ID";

        private static readonly ActionRefRunnable Ref = new ActionRefRunnable
        {
            Tag = ActionRefStoreTests.Tag,
            Id = ActionRefStoreTests.Id,
            Templates = new Dictionary<string, string> { { ActionRefStoreTests.TemplatePropName, ActionRefStoreTests.TemplateName } }
        };

        private static readonly ActionRefRunnable UpdateRef = new ActionRefRunnable
        {
            Tag = ActionRefStoreTests.UpdateTag,
            Id = ActionRefStoreTests.UpdateId,
            Templates = new Dictionary<string, string> { { ActionRefStoreTests.TemplatePropName, ActionRefStoreTests.TemplateName } }
        };

        private ActionRefStore testObj;

        [TestInitialize]
        public void Init()
        {
            this.mockAccessor.Setup(o => o.RetrieveActionReferencesAsync()).ReturnsAsync(new[] { ActionRefStoreTests.Ref });

            this.mockAccessor
                .Setup(
                    o => o.WriteActionReferenceChangesAsync(
                        It.IsAny<ICollection<string>>(),
                        It.IsAny<ICollection<ActionRefRunnable>>(),
                        It.IsAny<ICollection<ActionRefRunnable>>()))
                .Returns(Task.CompletedTask);

            this.mockActionStore.Setup(o => o.ValidateReference(It.IsAny<IParseContext>(), It.IsAny<ActionRef>())).Returns(true);

            this.mockTemplateStore.Setup(o => o.ValidateReference(It.IsAny<IParseContext>(), It.IsAny<TemplateRef>())).Returns(true);

            this.testObj = new ActionRefStore(
                this.mockAccessor.Object,
                this.mockTemplateStore.Object,
                this.mockActionStore.Object);
        }

        [TestMethod]
        public void GetReturnsNullWhenStoreNotInitialized()
        {
            ActionRefRunnable result;

            // test
            result = this.testObj.GetReference("notExist");

            // verify
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetReturnsNullWhenStoreInitializedButNoItemExists()
        {
            ActionRefRunnable result;

            await this.testObj.RefreshAsync(this.mockParseCtx.Object);

            // test
            result = this.testObj.GetReference("notExist");

            // verify
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetReturnsNullNonNullWhenStoreInitialized()
        {
            ActionRefRunnable result;

            await this.testObj.RefreshAsync(this.mockParseCtx.Object);
            
            // test
            result = this.testObj.GetReference(ActionRefStoreTests.Id);

            // verify
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task EnumerateReturnsPassedInActions()
        {
            ICollection<ActionRefRunnable> result;

            await this.testObj.RefreshAsync(this.mockParseCtx.Object);

            // test
            result = this.testObj.EnumerateReferences();

            // verify
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreSame(ActionRefStoreTests.Ref, result.First());
        }

        [TestMethod]
        public void EnumerateReturnsEmptyListIfStoreNotInitialized()
        {
            ICollection<ActionRefRunnable> result;

            // test
            result = this.testObj.EnumerateReferences();

            // verify
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public async Task RefreshPullsActionDataFromTheRetrieverAndParsesIt()
        {
            // test
            await this.testObj.RefreshAsync(this.mockParseCtx.Object);

            // verify
            this.mockAccessor.Verify(o => o.RetrieveActionReferencesAsync(), Times.Once);

            Assert.AreEqual(1, this.testObj.Count);
            Assert.AreSame(ActionRefStoreTests.Ref, this.testObj.GetReference(ActionRefStoreTests.Id));
        }

        [TestMethod]
        public async Task RefreshReturnsFalseIfMultipleActionsHaveSameTagInStore()
        {
            bool result;

            this.mockAccessor
                .Setup(o => o.RetrieveActionReferencesAsync())
                .ReturnsAsync(new[] { ActionRefStoreTests.Ref, ActionRefStoreTests.Ref });

            // test
            result = await this.testObj.RefreshAsync(this.mockParseCtx.Object);

            // verify
            Assert.IsFalse(result);
            this.mockParseCtx.Verify(
                o => o.LogError(It.Is<string>(p => p.Contains("Action reference store contains duplicates"))),
                Times.AtLeastOnce);
        }

        [TestMethod]
        public async Task RefreshReturnsFalseWhenActionStoreValidationReturnsFalse()
        {
            bool result;

            this.mockActionStore.Setup(o => o.ValidateReference(It.IsAny<IParseContext>(), It.IsAny<ActionRef>())).Returns(false);

            // test
            result = await this.testObj.RefreshAsync(this.mockParseCtx.Object);

            // verify
            Assert.IsFalse(result);

            this.mockActionStore.Verify(
                o => o.ValidateReference(this.mockParseCtx.Object, ActionRefStoreTests.Ref), Times.Once);
        }

        [TestMethod]
        public async Task RefreshReturnsFalseWhenTemplateStoreValidationReturnsFalse()
        {
            bool result;

            this.mockTemplateStore.Setup(o => o.ValidateReference(It.IsAny<IParseContext>(), It.IsAny<TemplateRef>())).Returns(false);

            // test
            result = await this.testObj.RefreshAsync(this.mockParseCtx.Object);

            // verify
            Assert.IsFalse(result);

            this.mockTemplateStore.Verify(
                o => o.ValidateReference(
                    this.mockParseCtx.Object, 
                    It.Is<TemplateRef>(p => ActionRefStoreTests.TemplateName.Equals(p.TemplateTag))), 
                Times.Once);
        }

        [TestMethod]
        public async Task UpdateAddsNewActionToStoreAndCommitsToStore()
        {
            await this.testObj.RefreshAsync(this.mockParseCtx.Object);

            // test
            await this.testObj.UpdateAsync(this.mockParseCtx.Object, null, new[] { ActionRefStoreTests.UpdateRef });

            // verify
            Assert.AreEqual(2, this.testObj.Count);

            ActionRefRunnable temp = this.testObj.GetReference(ActionRefStoreTests.UpdateId);

            Assert.AreSame(ActionRefStoreTests.UpdateRef, this.testObj.GetReference(ActionRefStoreTests.UpdateId));

            this.mockAccessor
                .Verify(
                    o => o.WriteActionReferenceChangesAsync(
                        It.Is<ICollection<string>>(p => p == null || p.Count == 0),
                        It.Is<ICollection<ActionRefRunnable>>(p => p == null || p.Count == 0),
                        It.Is<ICollection<ActionRefRunnable>>(
                            p => p.Count == 1 && ActionRefStoreTests.UpdateId.EqualsIgnoreCase(p.First().Id))),
                    Times.Once);
        }

        [TestMethod]
        public async Task UpdateReturnsTrueAndOverwritesExistingIfIdSameAsExistingId()
        {
            bool result;

            await this.testObj.RefreshAsync(this.mockParseCtx.Object);

            // test
            result = await this.testObj.UpdateAsync(
                this.mockParseCtx.Object,
                null,
                new[] { ActionRefStoreTests.Ref });

            // verify
            Assert.IsTrue(result);

            this.mockParseCtx.Verify(
                o => o.LogVerbose(It.Is<string>(p => p.Contains("Replaced existing action"))),
                Times.AtLeastOnce);

            Assert.AreSame(ActionRefStoreTests.Ref, this.testObj.GetReference(ActionRefStoreTests.Id));

            this.mockAccessor
                .Verify(
                    o => o.WriteActionReferenceChangesAsync(
                        It.Is<ICollection<string>>(p => p == null || p.Count == 0),
                        It.Is<ICollection<ActionRefRunnable>>(
                            p => p.Count == 1 && ActionRefStoreTests.Id.EqualsIgnoreCase(p.First().Id)),
                        It.Is<ICollection<ActionRefRunnable>>(p => p == null || p.Count == 0)),
                    Times.Once);
        }

        [TestMethod]
        public async Task UpdateReturnsFalseWhenActionStoreValidationReturnsFalse()
        {
            bool result;

            await this.testObj.RefreshAsync(this.mockParseCtx.Object);

            // this must be done after RefreshAsync()
            this.mockActionStore.Setup(o => o.ValidateReference(It.IsAny<IParseContext>(), It.IsAny<ActionRef>())).Returns(false);

            // test
            result = await this.testObj.UpdateAsync(
                this.mockParseCtx.Object,
                null,
                new[] { ActionRefStoreTests.UpdateRef });

            // verify
            Assert.IsFalse(result);

            this.mockActionStore.Verify(
                o => o.ValidateReference(this.mockParseCtx.Object, ActionRefStoreTests.UpdateRef), Times.Once);
        }

        [TestMethod]
        public async Task UpdateReturnsFalseWhenTemplateStoreValidationReturnsFalse()
        {
            bool result;

            await this.testObj.RefreshAsync(this.mockParseCtx.Object);

            this.mockTemplateStore.Invocations.Clear();

            // this must be done after RefreshAsync()
            this.mockTemplateStore.Setup(o => o.ValidateReference(It.IsAny<IParseContext>(), It.IsAny<TemplateRef>())).Returns(false);

            // test
            result = await this.testObj.UpdateAsync(
                this.mockParseCtx.Object,
                null,
                new[] { ActionRefStoreTests.UpdateRef });

            // verify
            Assert.IsFalse(result);

            this.mockTemplateStore.Verify(
                o => o.ValidateReference(
                    this.mockParseCtx.Object,
                    It.Is<TemplateRef>(p => ActionRefStoreTests.TemplateName.Equals(p.TemplateTag))),
                Times.Once);
        }

        [TestMethod]
        public async Task UpdateRemovesActionFromStoreWhenActionExists()
        {
            bool result;

            await this.testObj.RefreshAsync(this.mockParseCtx.Object);

            // test
            result = await this.testObj.UpdateAsync(
                this.mockParseCtx.Object,
                new[] { ActionRefStoreTests.Id },
                null);

            // verify
            Assert.IsTrue(result);

            Assert.IsNull(this.testObj.GetReference(ActionRefStoreTests.Id));

            this.mockAccessor
                .Verify(
                    o => o.WriteActionReferenceChangesAsync(
                        It.Is<ICollection<string>>(p => p.Count == 1 && ActionRefStoreTests.Id.EqualsIgnoreCase(p.First())),
                        It.Is<ICollection<ActionRefRunnable>>(p => p == null || p.Count == 0),
                        It.Is<ICollection<ActionRefRunnable>>(p => p == null || p.Count == 0)),
                    Times.Once);
        }

        [TestMethod]
        public async Task UpdateIgnoresRemoveActionInstructionWhenWhenActionDoesNotExists()
        {
            bool result;

            await this.testObj.RefreshAsync(this.mockParseCtx.Object);

            // test
            result = await this.testObj.UpdateAsync(
                this.mockParseCtx.Object,
                new[] { ActionRefStoreTests.UpdateId },
                null);

            // verify
            Assert.IsTrue(result);

            this.mockAccessor
                .Verify(
                    o => o.WriteActionReferenceChangesAsync(
                        It.Is<ICollection<string>>(p => p == null || p.Count == 0),
                        It.Is<ICollection<ActionRefRunnable>>(p => p == null || p.Count == 0),
                        It.Is<ICollection<ActionRefRunnable>>(p => p == null || p.Count == 0)),
                    Times.Once);
        }
    }
}
