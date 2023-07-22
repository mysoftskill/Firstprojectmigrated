// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.UnitTests.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Actions;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Store;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class ActionSetTests
    {
        private readonly Mock<IModelManipulator> mockModel = new Mock<IModelManipulator>();
        private readonly Mock<IExecuteContext> mockExecCtx = new Mock<IExecuteContext>();
        private readonly Mock<IActionFactory> mockFact = new Mock<IActionFactory>();
        private readonly Mock<IParseContext> mockParseCtx = new Mock<IParseContext>();
        private readonly Mock<IActionStore> mockStore = new Mock<IActionStore>();

        private readonly CancellationTokenSource cancelSource = new CancellationTokenSource();

        private Mock<IAction> mockAction;

        private ActionSet testObj;

        private IExecuteContext execCtx;
        private IActionFactory fact;
        private IParseContext parseCtx;
        private IActionStore store;

        [TestInitialize]
        public void Init()
        {
            this.mockAction = this.SetupMockAction("DefaultTestActionTag", true, false);

            this.mockFact.Setup(o => o.Create(It.IsAny<string>())).Returns(this.mockAction.Object);

            this.mockStore.Setup(o => o.GetAction(It.IsAny<string>())).Returns(this.mockAction.Object);

            this.testObj = new ActionSet(this.mockModel.Object);

            this.mockExecCtx.SetupGet(o => o.CancellationToken).Returns(this.cancelSource.Token);
            this.mockExecCtx.SetupGet(o => o.IsSimulation).Returns(false);

            this.parseCtx = this.mockParseCtx.Object;
            this.execCtx = this.mockExecCtx.Object;
            this.store = this.mockStore.Object;
            this.fact = this.mockFact.Object;
        }

        private Mock<IAction> SetupMockAction(
            string tag,
            bool execContinue,
            bool addToStore)
        {
            Mock<IAction> action = new Mock<IAction>();

            action.SetupGet(o => o.Tag).Returns(tag);

            action
                .Setup(
                    o => o.ParseAndProcessDefinition(
                        It.IsAny<IParseContext>(),
                        It.IsAny<IActionFactory>(),
                        It.IsAny<string>(),
                        It.IsAny<object>()))
                .Returns(true);

            action
                .Setup(o => o.ExpandDefinition(It.IsAny<IParseContext>(), It.IsAny<IActionStore>()))
                .Returns(true);

            action
                .Setup(o => o.Validate(It.IsAny<IParseContext>(), It.IsAny<IDictionary<string, ModelValue>>()))
                .Returns(true);

            action
                .Setup(
                    o => o.ExecuteAsync(
                        It.IsAny<IExecuteContext>(), 
                        It.IsAny<ActionRefCore>(), 
                        It.IsAny<object>()))
                .ReturnsAsync(new ExecuteResult(execContinue));

            if (addToStore)
            {
                this.mockStore.Setup(o => o.GetAction(tag)).Returns(action.Object);
            }

            return action;
        }

        [TestMethod]
        public void ProcessAndStoreDefinitionReturnsTrueAndDoesNothingElseIfNoActionsProvided()
        {
            bool result;

            // test
            result = this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, "tag", new ActionSetDef());

            // verify
            Assert.IsTrue(result);
            Assert.IsTrue(this.testObj.IsValid);
        }

        [TestMethod]
        public void ProcessAndStoreDefinitionReturnsFalseIfActionRefContainsInlineActionOfTypeThatDoesNotExist()
        {
            ActionSetDef def;
            bool result;

            def = new ActionSetDef
            {
                Actions = new List<ActionRef>
                {
                    new ActionRef
                    {
                        ExecutionOrder = 0,
                        Inline = new ActionDef { Tag = "tag2", Type = "type2" }
                    },
                },
            };

            this.mockFact.Setup(o => o.Create(It.IsAny<string>())).Returns((IAction)null);

            // test
            result = this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, "tag", def);

            // verify
            Assert.IsFalse(result);
            Assert.IsFalse(this.testObj.IsValid);
        }

        [TestMethod]
        public void ProcessAndStoreDefinitionReturnsFalseIfActionRefContainsInlineActionThatFailsToParse()
        {
            ActionSetDef def;
            bool result;

            def = new ActionSetDef
            {
                Actions = new List<ActionRef>
                {
                    new ActionRef
                    {
                        ExecutionOrder = 0,
                        Inline = new ActionDef { Tag = "tag2", Type = "type2" }
                    },
                },
            };

            this.mockAction
                .Setup(
                    o => o.ParseAndProcessDefinition(
                        It.IsAny<IParseContext>(),
                        It.IsAny<IActionFactory>(),
                        It.IsAny<string>(),
                        It.IsAny<object>()))
                .Returns(false);

            // test
            result = this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, "tag", def);

            // verify
            Assert.IsFalse(result);
            Assert.IsFalse(this.testObj.IsValid);
        }

        [TestMethod]
        public void ProcessAndStoreDefinitionCreatesAndParsesActionIfInlineActionFound()
        {
            const string Type2 = "type2";
            const string Tag2 = "type2";

            object def2 = new object();

            ActionSetDef def;
            bool result;

            def = new ActionSetDef
            {
                Actions = new List<ActionRef>
                {
                    new ActionRef
                    {
                        ExecutionOrder = 0,
                        Inline = new ActionDef { Tag = Tag2, Type = Type2, Def = def2 }
                    },
                },
            };

            // test
            result = this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, "tag", def);

            // verify
            Assert.IsTrue(result);
            Assert.IsTrue(this.testObj.IsValid);

            this.mockFact.Verify(o => o.Create(Type2), Times.Once);
            this.mockAction.Verify(o => o.ParseAndProcessDefinition(this.parseCtx, this.fact, Tag2, def2), Times.Once);
        }

        [TestMethod]
        public void ProcessAndStoreDefinitionReturnsTrueIfNoErrorsFound()
        {
            ActionSetDef def;
            bool result;

            def = new ActionSetDef
            {
                Actions = new List<ActionRef> { new ActionRef { Tag = "tag1", ExecutionOrder = 0 } },
            };

            // test
            result = this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, "tag", def);

            // verify
            Assert.IsTrue(result);
            Assert.IsTrue(this.testObj.IsValid);
        }

        [TestMethod]
        public void ExpandDefinitionReturnsFalseIfCannotFetchNonInlineActionFromStore()
        {
            const string Tag1 = "tag1";

            ActionSetDef def;
            bool result;

            def = new ActionSetDef
            {
                Actions = new List<ActionRef> { new ActionRef { Tag = Tag1, ExecutionOrder = 0 } },
            };

            this.mockStore.Setup(o => o.GetAction(It.IsAny<string>())).Returns((IAction)null);

            this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, "tag", def);

            // test
            result = this.testObj.ExpandDefinition(this.parseCtx, this.store);

            // verify
            Assert.IsFalse(result);
            Assert.IsFalse(this.testObj.IsValid);
            this.mockParseCtx.Verify(
                o => o.LogError(It.Is<string>(p => p.Contains("Unable to find referenced action"))),
                Times.Once);
        }

        [TestMethod]
        public void ExpandDefinitionReturnsFalseIfInnerActionFailsToExpand()
        {
            const string Tag1 = "tag1";

            ActionSetDef def;
            bool result;

            def = new ActionSetDef
            {
                Actions = new List<ActionRef> { new ActionRef { Tag = Tag1, ExecutionOrder = 0 } },
            };

            this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, "tag", def);

            this.mockAction
                .Setup(o => o.ExpandDefinition(It.IsAny<IParseContext>(), It.IsAny<IActionStore>()))
                .Returns(false);

            // test
            result = this.testObj.ExpandDefinition(this.parseCtx, this.store);

            // verify
            Assert.IsFalse(result);
            Assert.IsFalse(this.testObj.IsValid);
        }

        [TestMethod]
        public void ExpandDefinitionCallsIntoStoreToGetNonInlineActionAndExpandsThem()
        {
            const string Tag1 = "tag1";

            ActionSetDef def;
            bool result;

            def = new ActionSetDef
            {
                Actions = new List<ActionRef> { new ActionRef { Tag = Tag1, ExecutionOrder = 0 } },
            };

            this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, "tag", def);

            // test
            result = this.testObj.ExpandDefinition(this.parseCtx, this.store);

            // verify
            Assert.IsTrue(result);
            Assert.IsTrue(this.testObj.IsValid);

            this.mockStore.Verify(o => o.GetAction(Tag1), Times.Once);
            this.mockAction.Verify(o => o.ExpandDefinition(this.parseCtx, this.store), Times.Once);
        }

        [TestMethod]
        public void ExpandDefinitionDoesNotCallIntoStoreToGetInlineActionButDoesExpandsThem()
        {
            ActionSetDef def;
            bool result;

            def = new ActionSetDef
            {
                Actions = new List<ActionRef>
                {
                    new ActionRef
                    {
                        ExecutionOrder = 0,
                        Inline = new ActionDef { Tag = "tag2", Type = "type2", Def = new object() }
                    },
                },
            };

            this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, "tag", def);

            // test
            result = this.testObj.ExpandDefinition(this.parseCtx, this.store);

            // verify
            Assert.IsTrue(result);
            Assert.IsTrue(this.testObj.IsValid);

            this.mockStore.Verify(o => o.GetAction(It.IsAny<string>()), Times.Never);
            this.mockAction.Verify(o => o.ExpandDefinition(this.parseCtx, this.store), Times.Once);
        }

        [TestMethod]
        public void ValidateParseReturnsFalseIfInnerActionValidationReturnsFalse()
        {
            ActionSetDef def;
            bool result;

            IDictionary<string, ModelValue> argXform = new Dictionary<string, ModelValue>();

            def = new ActionSetDef
            {
                Actions = new List<ActionRef>
                {
                    new ActionRef
                    {
                        ExecutionOrder = 0,
                        Inline = new ActionDef { Tag = "tag2", Type = "type2", Def = new object() },
                        ArgTransform = argXform,
                    },
                },
            };

            this.mockAction
                .Setup(o => o.Validate(It.IsAny<IParseContext>(), It.IsAny<IDictionary<string, ModelValue>>()))
                .Returns(false);

            this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, "tag", def);
            this.testObj.ExpandDefinition(this.parseCtx, this.store);

            // test
            result = this.testObj.Validate(this.parseCtx, null);

            // verify
            Assert.IsFalse(result);
            Assert.IsFalse(this.testObj.IsValid);
        }

        [TestMethod]
        public void ValidateParseCallsIntoInnerActionsToValidateThem()
        {
            ActionSetDef def;
            bool result;

            IDictionary<string, ModelValue> argXform = new Dictionary<string, ModelValue>();

            def = new ActionSetDef
            {
                Actions = new List<ActionRef>
                {
                    new ActionRef
                    {
                        ExecutionOrder = 0,
                        Inline = new ActionDef { Tag = "tag2", Type = "type2", Def = new object() },
                        ArgTransform = argXform,
                    },
                },
            };

            this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, "tag", def);
            this.testObj.ExpandDefinition(this.parseCtx, this.store);

            // test
            result = this.testObj.Validate(this.parseCtx, null);

            // verify
            Assert.IsTrue(result);
            Assert.IsTrue(this.testObj.IsValid);

            this.mockAction.Verify(o => o.Validate(this.parseCtx, argXform), Times.Once);
        }

        [TestMethod]
        public async Task ExecuteCreatesLocalModelIfUseLocalModelModeDefined()
        {
            IDictionary<string, ModelValue> xform = 
                new Dictionary<string, ModelValue> { { "key", new ModelValue { Const = new object() } } };

            object localModel = new object();
            object model = new object();

            ActionSetDef def = new ActionSetDef
            {
                Actions = new List<ActionRef> { new ActionRef { Tag = "tag1", ExecutionOrder = 0 } },
                LocalModelTransform = xform,
                LocalModelMode = ModelMode.Local,
            };

            ExecuteResult result;

            this.mockModel.Setup(o => o.MergeModels(this.execCtx, model, null, xform)).Returns(localModel);

            this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, "tag", def);
            this.testObj.ExpandDefinition(this.parseCtx, this.store);
            this.testObj.Validate(this.parseCtx, null);

            // test
            result = await this.testObj.ExecuteAsync(this.execCtx, new ActionRefCore(), model);

            // verify
            Assert.IsTrue(result.Continue);

            this.mockModel.Verify(o => o.MergeModels(this.execCtx, model, null, xform), Times.Once);
            this.mockAction.Verify(o => o.ExecuteAsync(this.execCtx, def.Actions[0], localModel), Times.Once);
        }

        [TestMethod]
        public async Task ExecuteUsesInputModelIfInputModeDefined()
        {
            object model = new object();

            ActionSetDef def = new ActionSetDef
            {
                Actions = new List<ActionRef> { new ActionRef { Tag = "tag1", ExecutionOrder = 0 } },
            };

            ExecuteResult result;

            this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, "tag", def);
            this.testObj.ExpandDefinition(this.parseCtx, this.store);
            this.testObj.Validate(this.parseCtx, null);

            // test
            result = await this.testObj.ExecuteAsync(this.execCtx, new ActionRefCore(), model);

            // verify
            Assert.IsTrue(result.Continue);

            this.mockModel
                .Verify(
                    o => o.MergeModels(
                        It.IsAny<IExecuteContext>(),
                        It.IsAny<object>(),
                        It.IsAny<object>(),
                        It.IsAny<IDictionary<string, ModelValue>>()),
                    Times.Never);

            this.mockAction.Verify(o => o.ExecuteAsync(this.execCtx, def.Actions[0], model), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public async Task ExecuteThrowsIfCancelTokenSignaled()
        {
            object model = new object();

            ActionSetDef def = new ActionSetDef
            {
                Actions = new List<ActionRef> { new ActionRef { Tag = "tag1", ExecutionOrder = 0 } },
            };

            this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, "tag", def);
            this.testObj.ExpandDefinition(this.parseCtx, this.store);
            this.testObj.Validate(this.parseCtx, null);

            this.cancelSource.Cancel();

            // test
            await this.testObj.ExecuteAsync(this.execCtx, new ActionRefCore(), model);
        }

        [TestMethod]
        public async Task ExecuteExecutesAllInnerActionsIfAllReturnContinueAsTrue()
        {
            const string Tag1 = "ExecTestTag1";
            const string Tag2 = "ExecTestTag2";

            Mock<IAction> action1 = this.SetupMockAction(Tag1, true, true);
            Mock<IAction> action2 = this.SetupMockAction(Tag2, true, true);

            object model = new object();

            ActionSetDef def = new ActionSetDef
            {
                Actions = new List<ActionRef>
                {
                    new ActionRef { Tag = Tag1, ExecutionOrder = 1 },
                    new ActionRef { Tag = Tag2, ExecutionOrder = 2 }
                },
            };

            ExecuteResult result;
            ActionRef refUsed1;
            ActionRef refUsed2;

            this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, "tag", def);
            this.testObj.ExpandDefinition(this.parseCtx, this.store);
            this.testObj.Validate(this.parseCtx, null);

            // test
            result = await this.testObj.ExecuteAsync(this.execCtx, new ActionRefCore(), model);

            // verify
            Assert.IsTrue(result.Continue);

            refUsed1 = def.Actions.Single(p => Tag1.EqualsIgnoreCase(p.Tag));
            action1.Verify(o => o.ExecuteAsync(this.execCtx, refUsed1, model), Times.Once);

            refUsed2 = def.Actions.Single(p => Tag2.EqualsIgnoreCase(p.Tag));
            action2.Verify(o => o.ExecuteAsync(this.execCtx, refUsed2, model), Times.Once);
        }
        
        [TestMethod]
        public async Task ExecuteExecutesSkipsSubsequentInnerActionsIfOneReturnsContinueFalse()
        {
            const string Tag1 = "ExecTestTag1";
            const string Tag2 = "ExecTestTag2";

            Mock<IAction> action1 = this.SetupMockAction(Tag1, false, true);
            Mock<IAction> action2 = this.SetupMockAction(Tag2, true, true);

            object model = new object();

            ActionSetDef def = new ActionSetDef
            {
                Actions = new List<ActionRef>
                {
                    // deliberately reversed order to ensure ExecutionOrder property determines order and not list
                    //  order
                    new ActionRef { Tag = Tag2, ExecutionOrder = 2 },
                    new ActionRef { Tag = Tag1, ExecutionOrder = 1 },
                },
            };

            ExecuteResult result;
            ActionRef refUsed;

            this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, "tag", def);
            this.testObj.ExpandDefinition(this.parseCtx, this.store);
            this.testObj.Validate(this.parseCtx, null);

            // test
            result = await this.testObj.ExecuteAsync(this.execCtx, new ActionRefCore(), model);

            // verify
            Assert.IsFalse(result.Continue);

            refUsed = def.Actions.Single(p => Tag1.EqualsIgnoreCase(p.Tag));
            action1.Verify(o => o.ExecuteAsync(this.execCtx, refUsed, model), Times.Once);

            action2.Verify(
                o => o.ExecuteAsync(It.IsAny<IExecuteContext>(), It.IsAny<ActionRefCore>(), It.IsAny<object>()), 
                Times.Never);
        }
    }
}
