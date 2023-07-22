// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.UnitTests.Store
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.Common.Exceptions;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Actions;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Store;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class ActionStoreTests
    {
        private readonly Mock<IModelManipulator> mockModel = new Mock<IModelManipulator>();
        private readonly Mock<IActionAccessor> mockAccessor = new Mock<IActionAccessor>(MockBehavior.Strict);
        private readonly Mock<IExecuteContext> mockExecCtx = new Mock<IExecuteContext>();
        private readonly Mock<IContextFactory> mockCtxFact = new Mock<IContextFactory>();
        private readonly Mock<IActionFactory> mockActFact = new Mock<IActionFactory>(MockBehavior.Strict);
        private readonly Mock<IParseContext> mockParseCtx = new Mock<IParseContext>();
        private readonly Mock<IContext> mockGenericCtx = new Mock<IContext>();
        private readonly Mock<IAction> mockActionUpdate = new Mock<IAction>(MockBehavior.Strict);
        private readonly Mock<IAction> mockAction = new Mock<IAction>(MockBehavior.Strict);

        private const string UpdateType = "UpdateTYPE";
        private const string UpdateTag = "UpdateTAG";
        private const string Type = "TYPE";
        private const string Tag = "TAG";

        private static readonly ActionDef Def = new ActionDef
        {
            Type = ActionStoreTests.Type,
            Tag = ActionStoreTests.Tag,
        };

        private bool expandResult = true;
        private bool parseResult = true;

        private ActionStore testObj;

        [TestInitialize]
        public void Init()
        {
            this.mockActFact
                .Setup(o => o.Create(ActionStoreTests.UpdateType)).Returns(this.mockActionUpdate.Object);
            this.mockActFact
                .Setup(o => o.Create(ActionStoreTests.Type)).Returns(this.mockAction.Object);

            this.mockAccessor.Setup(o => o.RetrieveActionsAsync()).ReturnsAsync(new[] { ActionStoreTests.Def });

            this.mockAccessor
                .Setup(
                    o => o.WriteActionChangesAsync(
                        It.IsAny<ICollection<string>>(),
                        It.IsAny<ICollection<ActionDef>>(),
                        It.IsAny<ICollection<ActionDef>>()))
                .Returns(Task.CompletedTask);

            this.mockCtxFact.Setup(o => o.Create<IExecuteContext>(It.IsAny<string>())).Returns(It.IsAny<IExecuteContext>());
            this.mockCtxFact.Setup(o => o.Create<IParseContext>(It.IsAny<string>())).Returns(It.IsAny<IParseContext>());
            this.mockCtxFact.Setup(o => o.Create<IContext>(It.IsAny<string>())).Returns(It.IsAny<IContext>());

            this.mockAction.SetupGet(o => o.Tag).Returns(ActionStoreTests.Tag);

            this.mockAction
                .Setup(o => o.ExpandDefinition(It.IsAny<IParseContext>(), It.IsAny<IActionFetcher>()))
                .Returns(() => this.expandResult);

            this.mockAction
                .Setup(
                    o => o.ParseAndProcessDefinition(
                        It.IsAny<IParseContext>(),
                        It.IsAny<IActionFactory>(),
                        It.IsAny<string>(),
                        It.IsAny<object>()))
                .Returns(() => this.parseResult);

            this.mockActionUpdate.SetupGet(o => o.Tag).Returns(ActionStoreTests.UpdateTag);

            this.mockActionUpdate
                .Setup(o => o.ExpandDefinition(It.IsAny<IParseContext>(), It.IsAny<IActionFetcher>()))
                .Returns(() => this.expandResult);

            this.mockActionUpdate
                .Setup(
                    o => o.ParseAndProcessDefinition(
                        It.IsAny<IParseContext>(),
                        It.IsAny<IActionFactory>(),
                        It.IsAny<string>(),
                        It.IsAny<object>()))
                .Returns(() => this.parseResult);

            this.testObj = new ActionStore(
                this.mockAccessor.Object,
                this.mockActFact.Object,
                this.mockCtxFact.Object,
                this.mockModel.Object);
        }

        [TestMethod]
        public void GetReturnsNullWhenStoreNotInitialized()
        {
            IAction result;

            // test
            result = this.testObj.GetAction("notExist");

            // verify
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetReturnsNullWhenStoreInitializedButNoItemExists()
        {
            IAction result;

            await this.testObj.RefreshAsync(this.mockParseCtx.Object, false);

            // test
            result = this.testObj.GetAction("notExist");

            // verify
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetReturnsNullNonNullWhenStoreInitialized()
        {
            IAction result;

            await this.testObj.RefreshAsync(this.mockParseCtx.Object, false);
            
            // test
            result = this.testObj.GetAction(ActionStoreTests.Tag);

            // verify
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task EnumerateReturnsEmptyListWhenRefreshDidNotPreserveDefs()
        {
            ICollection<ActionDef> result;

            await this.testObj.RefreshAsync(this.mockParseCtx.Object, false);

            // test
            result = this.testObj.EnumerateActions();

            // verify
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public async Task EnumerateReturnsPassedInActionsWhenRefreshDidPreserveDefs()
        {
            ICollection<ActionDef> result;

            await this.testObj.RefreshAsync(this.mockParseCtx.Object, true);

            // test
            result = this.testObj.EnumerateActions();

            // verify
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreSame(ActionStoreTests.Def, result.First());
        }

        [TestMethod]
        public void EnumerateReturnsEmptyListIfStoreNotInitialized()
        {
            ICollection<ActionDef> result;

            // test
            result = this.testObj.EnumerateActions();

            // verify
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void ValidateReturnsFalseWhenStoreNotInitialized()
        {
            bool result = this.testObj.ValidateReference(
                this.mockParseCtx.Object,
                new ActionRef { Tag = "notExist", Id = "notExist" });

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task ValidateReturnsFalseWhenActionDoesNotExist()
        {
            bool result;

            await this.testObj.RefreshAsync(this.mockParseCtx.Object, false);

            // test
            result = this.testObj.ValidateReference(
                this.mockParseCtx.Object, 
                new ActionRef { Tag = "notExist", Id = "notExist" });

            // verify
            Assert.IsFalse(result);
        }
        
        [TestMethod]
        public async Task ValidateReturnsFalseWhenActionDoesExistButActionValidationReturnsFalse()
        {
            bool result;

            await this.testObj.RefreshAsync(this.mockParseCtx.Object, false);

            this.mockAction
                .Setup(o => o.Validate(It.IsAny<IParseContext>(), It.IsAny<IDictionary<string, ModelValue>>()))
                .Returns(false);

            // test
            result = this.testObj.ValidateReference(
                this.mockParseCtx.Object,
                new ActionRef { Tag = ActionStoreTests.Tag, Id = ActionStoreTests.Tag });

            // verify
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task ValidateReturnsTrueWhenActionDoesExistAndActionValidationReturnsTrue()
        {
            bool result;

            await this.testObj.RefreshAsync(this.mockParseCtx.Object, false);

            this.mockAction
                .Setup(o => o.Validate(It.IsAny<IParseContext>(), It.IsAny<IDictionary<string, ModelValue>>()))
                .Returns(true);

            // test
            result = this.testObj.ValidateReference(
                this.mockParseCtx.Object,
                new ActionRef { Tag = ActionStoreTests.Tag, Id = ActionStoreTests.Tag });

            // verify
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task RefreshPullsActionDataFromTheRetrieverAndParsesIt()
        {
            // test
            await this.testObj.RefreshAsync(this.mockParseCtx.Object, false);

            // verify
            this.mockAccessor.Verify(o => o.RetrieveActionsAsync(), Times.Once);
            this.mockActFact.Verify(o => o.Create(ActionStoreTests.Type), Times.Once);

            Assert.AreEqual(1, this.testObj.Count);
            Assert.AreSame(this.mockAction.Object, this.testObj.GetAction(ActionStoreTests.Def.Tag));
        }

        [TestMethod]
        public async Task RefreshReturnsFalseIfMultipleActionsHaveSameTagInStore()
        {
            bool result;

            this.mockAccessor
                .Setup(o => o.RetrieveActionsAsync())
                .ReturnsAsync(new[] { ActionStoreTests.Def, ActionStoreTests.Def });

            // test
            result = await this.testObj.RefreshAsync(this.mockParseCtx.Object, false);

            // verify
            Assert.IsFalse(result);
            this.mockParseCtx.Verify(
                o => o.LogError(It.Is<string>(p => p.Contains("Action store contains duplicates"))),
                Times.AtLeastOnce);
        }

        [TestMethod]
        public async Task RefreshReturnsFalseIfActionTypeIsUnknown()
        {
            bool result;

            this.parseResult = false;

            this.mockActFact.Setup(o => o.Create(ActionStoreTests.Type)).Returns((IAction)null);

            // test
            result = await this.testObj.RefreshAsync(this.mockParseCtx.Object, false);

            // verify
            Assert.IsFalse(result);
        }


        [TestMethod]
        public async Task RefreshReturnsFalseIfActionParseReturnsFalse()
        {
            bool result;

            this.parseResult = false;

            // test
            result = await this.testObj.RefreshAsync(this.mockParseCtx.Object, false);

            // verify
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task RefreshReturnsFalseIfActionExpandReturnsFalse()
        {
            bool result;

            this.expandResult = false;

            // test
            result = await this.testObj.RefreshAsync(this.mockParseCtx.Object, false);

            // verify
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task UpdateParsesSuppliedNewActions()
        {
            await this.testObj.RefreshAsync(this.mockParseCtx.Object, false);
            
            // test
            await this.testObj.UpdateAsync(
                this.mockParseCtx.Object,
                null,
                new[] { new ActionDef { Tag = ActionStoreTests.UpdateTag, Type = ActionStoreTests.UpdateType } });

            // verify
            this.mockActFact.Verify(o => o.Create(ActionStoreTests.UpdateType), Times.Once);
        }

        [TestMethod]
        public async Task UpdateAddsNewActionToStoreAndCommitsToStore()
        {
            await this.testObj.RefreshAsync(this.mockParseCtx.Object, false);

            // test
            await this.testObj.UpdateAsync(
                this.mockParseCtx.Object,
                null,
                new[] { new ActionDef { Tag = ActionStoreTests.UpdateTag, Type = ActionStoreTests.UpdateType } });

            // verify
            Assert.AreEqual(2, this.testObj.Count);
            Assert.AreSame(this.mockActionUpdate.Object, this.testObj.GetAction(ActionStoreTests.UpdateTag));

            this.mockAccessor
                .Verify(
                    o => o.WriteActionChangesAsync(
                        It.Is<ICollection<string>>(p => p == null || p.Count == 0),
                        It.Is<ICollection<ActionDef>>(p => p == null || p.Count == 0),
                        It.Is<ICollection<ActionDef>>(
                            p => p.Count == 1 && ActionStoreTests.UpdateTag.EqualsIgnoreCase(p.First().Tag))),
                    Times.Once);
        }

        [TestMethod]
        public async Task UpdateReturnsTrueAndOverwritesExistingIfTagSameAsExistingTag()
        {
            bool result;

            await this.testObj.RefreshAsync(this.mockParseCtx.Object, false);

            // test
            result = await this.testObj.UpdateAsync(
                this.mockParseCtx.Object,
                null,
                new[] { ActionStoreTests.Def });

            // verify
            Assert.IsTrue(result);

            this.mockParseCtx.Verify(
                o => o.LogVerbose(It.Is<string>(p => p.Contains("Replaced existing action"))),
                Times.AtLeastOnce);

            Assert.AreSame(this.mockAction.Object, this.testObj.GetAction(ActionStoreTests.Def.Tag));

            this.mockAccessor
                .Verify(
                    o => o.WriteActionChangesAsync(
                        It.Is<ICollection<string>>(p => p == null || p.Count == 0),
                        It.Is<ICollection<ActionDef>>(
                            p => p.Count == 1 && ActionStoreTests.Tag.EqualsIgnoreCase(p.First().Tag)),
                        It.Is<ICollection<ActionDef>>(p => p == null || p.Count == 0)),
                    Times.Once);
        }

        [TestMethod]
        public async Task UpdateRemovesActionFromStoreWhenActionExists()
        {
            bool result;

            await this.testObj.RefreshAsync(this.mockParseCtx.Object, false);

            // test
            result = await this.testObj.UpdateAsync(
                this.mockParseCtx.Object,
                new[] { ActionStoreTests.Tag },
                null);

            // verify
            Assert.IsTrue(result);

            Assert.IsNull(this.testObj.GetAction(ActionStoreTests.Tag));

            this.mockAccessor
                .Verify(
                    o => o.WriteActionChangesAsync(
                        It.Is<ICollection<string>>(p => p.Count == 1 && ActionStoreTests.Tag.EqualsIgnoreCase(p.First())),
                        It.Is<ICollection<ActionDef>>(p => p == null || p.Count == 0),
                        It.Is<ICollection<ActionDef>>(p => p == null || p.Count == 0)),
                    Times.Once);
        }

        [TestMethod]
        public async Task UpdateIgnoresRemoveActionInstructionWhenWhenActionDoesNotExists()
        {
            bool result;

            await this.testObj.RefreshAsync(this.mockParseCtx.Object, false);

            // test
            result = await this.testObj.UpdateAsync(
                this.mockParseCtx.Object,
                new[] { ActionStoreTests.UpdateTag },
                null);

            // verify
            Assert.IsTrue(result);

            this.mockAccessor
                .Verify(
                    o => o.WriteActionChangesAsync(
                        It.Is<ICollection<string>>(p => p == null || p.Count == 0),
                        It.Is<ICollection<ActionDef>>(p => p == null || p.Count == 0),
                        It.Is<ICollection<ActionDef>>(p => p == null || p.Count == 0)),
                    Times.Once);
        }

        [TestMethod]
        public async Task UpdateReturnsFalseIfActionTypeIsUnknown()
        {
            const string ActionType = "unknownType";

            bool result;

            await this.testObj.RefreshAsync(this.mockParseCtx.Object, false);

            this.parseResult = false;

            this.mockActFact.Setup(o => o.Create(ActionType)).Returns((IAction)null);

            // test
            result = await this.testObj.UpdateAsync(
                this.mockParseCtx.Object,
                null,
                new[] { new ActionDef { Tag = "tag", Type = ActionType } });

            // verify
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task UpdateReturnsFalseIfActionParseReturnsFalse()
        {
            bool result;

            await this.testObj.RefreshAsync(this.mockParseCtx.Object, false);

            this.parseResult = false;

            // test
            result = await this.testObj.UpdateAsync(
                this.mockParseCtx.Object,
                null,
                new[] { new ActionDef { Tag = "tag", Type = ActionStoreTests.UpdateType } });

            // verify
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task UpdateReturnsFalseIfActionExpandReturnsFalse()
        {
            bool result;

            await this.testObj.RefreshAsync(this.mockParseCtx.Object, false);

            this.expandResult = false;

            // test
            result = await this.testObj.UpdateAsync(
                this.mockParseCtx.Object,
                null,
                new[] { new ActionDef { Tag = "tag", Type = ActionStoreTests.UpdateType } });

            // verify
            Assert.IsFalse(result);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task ExecuteThrowsIfStoreNotInitialized()
        {
            await this.testObj.ExecuteActionAsync(null, ActionStoreTests.Tag);
        }

        [TestMethod]
        [ExpectedException(typeof(ActionExecuteException))]
        public async Task ExecuteThrowsIfRequestedTagDoesNotExist()
        {
            await this.testObj.RefreshAsync(this.mockParseCtx.Object, false);

            // test
            await this.testObj.ExecuteActionAsync(null, "notexist");
        }

        [TestMethod]
        public async Task ExecuteExecutesRequestedActionWithProvidedContext()
        {
            object model = new object();

            await this.testObj.RefreshAsync(this.mockParseCtx.Object, false);

            this.mockModel.Setup(o => o.CreateEmpty()).Returns(model);

            this.mockAction
                .Setup(
                    o => o.ExecuteAsync(
                        It.IsAny<IExecuteContext>(),
                        It.IsAny<ActionRefCore>(),
                        It.IsAny<object>()))
                .ReturnsAsync(new ExecuteResult(true));

            // test
            await this.testObj.ExecuteActionAsync(this.mockExecCtx.Object, ActionStoreTests.Tag);

            // verify
            this.mockCtxFact.Verify(o => o.Create<IExecuteContext>(It.IsAny<string>()), Times.Never);
            this.mockModel.Verify(o => o.CreateEmpty(), Times.Once);

            this.mockAction.Verify(
                o => o.ExecuteAsync(
                    this.mockExecCtx.Object, 
                    It.Is<ActionRefCore>(p => ActionStoreTests.Tag.Equals(p.Tag)), model),
                Times.Once);
        }


        [TestMethod]
        public async Task ExecuteExecutesRequestedActionWithCreatedContextWhenNoneProvided()
        {
            object model = new object();

            await this.testObj.RefreshAsync(this.mockParseCtx.Object, false);

            this.mockModel.Setup(o => o.CreateEmpty()).Returns(model);

            this.mockAction
                .Setup(
                    o => o.ExecuteAsync(
                        It.IsAny<IExecuteContext>(),
                        It.IsAny<ActionRefCore>(),
                        It.IsAny<object>()))
                .ReturnsAsync(new ExecuteResult(true));

            // test
            await this.testObj.ExecuteActionAsync(null, ActionStoreTests.Tag);

            // verify
            this.mockCtxFact.Verify(o => o.Create<IExecuteContext>(It.IsAny<string>()), Times.Once);
            this.mockModel.Verify(o => o.CreateEmpty(), Times.Once);

            this.mockAction.Verify(
                o => o.ExecuteAsync(
                    this.mockExecCtx.Object, 
                    It.Is<ActionRefCore>(p => ActionStoreTests.Tag.Equals(p.Tag)), 
                    model),
                Times.Never);
        }

        [TestMethod]
        public async Task ExecuteExecutesRequestedActionWithSpecifiedActionRefIfOneIsProvided()
        {
            ActionRef actionRef = new ActionRef { Tag = ActionStoreTests.Tag };

            object model = new object();

            await this.testObj.RefreshAsync(this.mockParseCtx.Object, false);

            this.mockModel.Setup(o => o.CreateEmpty()).Returns(model);

            this.mockAction
                .Setup(
                    o => o.ExecuteAsync(
                        It.IsAny<IExecuteContext>(), 
                        It.IsAny<ActionRefCore>(), 
                        It.IsAny<object>()))
                .ReturnsAsync(new ExecuteResult(true));

            // test
            await this.testObj.ExecuteActionAsync(this.mockExecCtx.Object, actionRef);

            // verify
            this.mockCtxFact.Verify(o => o.Create<IExecuteContext>(It.IsAny<string>()), Times.Never);
            this.mockModel.Verify(o => o.CreateEmpty(), Times.Once);

            this.mockAction.Verify(o => o.ExecuteAsync(this.mockExecCtx.Object, actionRef, model), Times.Once);
        }
    }
}
