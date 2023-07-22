// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.UnitTests.Actions
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Actions;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Store;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.UnitTests.TestUtility;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Utility;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class ForeachActionSetTests
    {
        private class ForeachActionSetTestException : Exception { }

        private readonly Mock<IModelManipulator> mockModel = new Mock<IModelManipulator>();
        private readonly Mock<IExecuteContext> mockExecCtx = new Mock<IExecuteContext>();
        private readonly Mock<IActionFactory> mockFact = new Mock<IActionFactory>();
        private readonly Mock<IParseContext> mockParseCtx = new Mock<IParseContext>();
        private readonly Mock<IActionStore> mockStore = new Mock<IActionStore>();

        private readonly CancellationTokenSource cancelSource = new CancellationTokenSource();

        private const string DefTag = "tag";

        private Mock<IAction> mockAction;

        private ForeachActionSet testObj;

        private IExecuteContext execCtx;
        private IActionFactory fact;
        private IParseContext parseCtx;
        private IActionStore store;

        private Mock<IAction> SetupMockAction(
            string tag,
            bool execContinue,
            bool addToStore,
            Exception actionFailException = null)
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

            action.Setup(o => o.ExpandDefinition(It.IsAny<IParseContext>(), It.IsAny<IActionStore>())).Returns(true);

            action
                .Setup(o => o.Validate(It.IsAny<IParseContext>(), It.IsAny<IDictionary<string, ModelValue>>()))
                .Returns(true);

            if (actionFailException == null)
            {
                action
                    .Setup(
                        o => o.ExecuteAsync(
                            It.IsAny<IExecuteContext>(),
                            It.IsAny<ActionRefCore>(),
                            It.IsAny<object>()))
                    .ReturnsAsync(new ExecuteResult(execContinue));
            }
            else
            {
                action
                    .Setup(
                        o => o.ExecuteAsync(
                            It.IsAny<IExecuteContext>(),
                            It.IsAny<ActionRefCore>(),
                            It.IsAny<object>()))
                    .Returns(Task.FromException<ExecuteResult>(actionFailException));
            }

            if (addToStore)
            {
                this.mockStore.Setup(o => o.GetAction(tag)).Returns(action.Object);
            }

            return action;
        }

        public class DataRow
        {
            public string Key { get; set; }
        }

        private (ForeachActionSet.Args, ActionRefCore, ForeachActionSetDef, object, ICollection<DataRow>) SetupTestObj(
            ForeachActionSet.Args args = null,
            ForeachActionSetDef def = null,
            int collectionCount = 1,
            bool useLocalModel = false,
            bool useLoopModel = false,
            bool useLoopResult = false,
            bool returnFalseOnEmpty = false,
            LoopResultCondition condition = LoopResultCondition.AlwaysTrue,
            string tagName = ForeachActionSetTests.DefTag)
        {
            IDictionary<string, ModelValue> argXform = new Dictionary<string, ModelValue>
            {
                { "DataRowPropertyName", new ModelValue { Const = 1 } },
                { "Collection", new ModelValue { Const = 1 } },
            };

            object modelIn = new object();
            ICollection<DataRow> collection = collectionCount >= 0 ? new List<DataRow>() : null;

            def = def ?? new ForeachActionSetDef
            {
                Actions = new List<ActionRef> { new ActionRef { Tag = tagName, ExecutionOrder = 1 } },
                LocalModelTransform = useLocalModel ? new Dictionary<string, ModelValue>() : null,
                LocalModelMode = useLocalModel ? ModelMode.Local : ModelMode.Input,
                LoopResultTransform = useLoopResult ? 
                    new Dictionary<string, ModelValueUpdate> { { "A", new ModelValueUpdate { Select = "Test" } } } : 
                    null,
                LoopModelTransform = useLoopModel ? new Dictionary<string, ModelValue>() : null,
                LoopModelMode = useLoopModel ? ModelMode.Local : ModelMode.Input,
                ReturnNotContinueOnEmpty = returnFalseOnEmpty,
                LoopResultCondition = condition,
            };

            for (int i = 0; i < collectionCount; ++i)
            {
                collection?.Add(new DataRow { Key = "Blargons" + i.ToStringInvariant() });
            }

            args = args ?? new ForeachActionSet.Args
            {
                CollectionItemKeyPropertyName = nameof(DataRow.Key),
                DataRowPropertyName = "DataRow",
                Collection = collection,
            };

            this.mockModel
                .Setup(o => o.TransformTo<ForeachActionSet.Args>(It.IsAny<object>()))
                .Returns(args);

            this.testObj = new ForeachActionSet(this.mockModel.Object);

            Assert.IsTrue(this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, "localTag", def));
            Assert.IsTrue(this.testObj.ExpandDefinition(this.parseCtx, this.store));
            Assert.IsTrue(this.testObj.Validate(this.parseCtx, argXform));

            this.mockModel.Setup(o => o.MergeModels(this.execCtx, modelIn, null, argXform)).Returns(args);

            return (args, new ActionRefCore { ArgTransform = argXform }, def, modelIn, collection);
        }

        [TestInitialize]
        public void Init()
        {
            this.mockAction = this.SetupMockAction(ForeachActionSetTests.DefTag, true, true);

            this.mockFact.Setup(o => o.Create(It.IsAny<string>())).Returns(this.mockAction.Object);

            this.mockStore.Setup(o => o.GetAction(It.IsAny<string>())).Returns(this.mockAction.Object);

            this.mockModel.Setup(o => o.ToEnumerable(It.IsAny<object>())).Returns((object o) => (IEnumerable)o);

            this.mockExecCtx.SetupGet(o => o.CancellationToken).Returns(this.cancelSource.Token);
            this.mockExecCtx.SetupGet(o => o.IsSimulation).Returns(false);

            this.parseCtx = this.mockParseCtx.Object;
            this.execCtx = this.mockExecCtx.Object;
            this.store = this.mockStore.Object;
            this.fact = this.mockFact.Object;
        }

        [TestMethod]
        public void ValidateReturnsFalseIfInputModeUsedWithLocalTransform()
        {
            ForeachActionSetDef def;
            bool result;

            def = new ForeachActionSetDef
            {
                Actions = new List<ActionRef> { new ActionRef { Tag = "tag1", ExecutionOrder = 0 } },
                LoopModelTransform = new Dictionary<string, ModelValue> { { "key", new ModelValue { Const = new object() } } },
                LoopModelMode = ModelMode.Input,
            };

            this.testObj = new ForeachActionSet(this.mockModel.Object);

            // test
            result = this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, "tag", def);

            // verify
            Assert.IsFalse(result);
            Assert.IsFalse(this.testObj.IsValid);
            this.mockParseCtx.Verify(
                o => o.LogError(It.Is<string>(p => p.Contains("but a local loop model transform was found"))),
                Times.Once);
        }

        [TestMethod]
        public void ValidateReturnsTrueIfNoErrorsFound()
        {
            ActionSetDef def;
            bool result;

            def = new ForeachActionSetDef
            {
                Actions = new List<ActionRef> { new ActionRef { Tag = "tag1", ExecutionOrder = 0 } },
            };

            this.testObj = new ForeachActionSet(this.mockModel.Object);

            // test
            result = this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, "tag", def);

            // verify
            Assert.IsTrue(result);
            Assert.IsTrue(this.testObj.IsValid);
        }

        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public async Task ExecuteThrowsIfCancelTokenSignaled()
        {
            ActionRefCore refCore;
            object modelIn;

            (_, refCore, _, modelIn, _) = this.SetupTestObj();

            this.cancelSource.Cancel();

            // test
            await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);
        }
        
        [TestMethod]
        public async Task ExecuteUsesInputLocalAndInputLoopWhenSpecified()
        {
            ForeachActionSetDef def;
            ActionRefCore refCore;
            object modelIn;

            (_, refCore, def, modelIn, _) = this.SetupTestObj(useLocalModel: false, useLoopModel: false);
            
            // test
            await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            this.mockModel
                .Verify(
                    o => o.MergeModels(
                        It.IsAny<IExecuteContext>(),
                        It.IsAny<object>(),
                        It.IsAny<object>(),
                        It.Is<IDictionary<string, ModelValue>>(p => p != refCore.ArgTransform)),
                    Times.Never);

            this.mockAction.Verify(o => o.ExecuteAsync(this.execCtx, def.Actions[0], modelIn), Times.Once);
        }

        [TestMethod]
        public async Task ExecuteUsesLocalLocalAndInputLoopWhenSpecified()
        {
            ForeachActionSetDef def;
            ActionRefCore refCore;
            object modelLocal = new object();
            object modelIn;

            (_, refCore, def, modelIn, _) = this.SetupTestObj(useLocalModel: true, useLoopModel: false);

            this.mockModel
                .Setup(
                    o => o.MergeModels(
                        this.execCtx, 
                        modelIn, 
                        null, 
                        It.Is<IDictionary<string, ModelValue>>(p => p == def.LocalModelTransform)))
                .Returns(modelLocal);
            
            // test
            await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            this.mockModel
                .Verify(
                    o => o.MergeModels(
                        It.IsAny<IExecuteContext>(),
                        It.IsAny<object>(),
                        It.IsAny<object>(),
                        It.Is<IDictionary<string, ModelValue>>(p => p != refCore.ArgTransform)),
                    Times.Once);

            this.mockModel
                .Verify(
                    o => o.MergeModels(
                        this.execCtx,
                        modelIn,
                        null,
                        It.Is<IDictionary<string, ModelValue>>(p => p == def.LocalModelTransform)),
                    Times.Once);

            this.mockAction.Verify(o => o.ExecuteAsync(this.execCtx, def.Actions[0], modelLocal), Times.Once);
        }


        [TestMethod]
        public async Task ExecuteUsesInputLocalAndLocalLoopWhenSpecified()
        {
            ForeachActionSetDef def;
            ActionRefCore refCore;
            object modelLoop = new object();
            object modelIn;

            (_, refCore, def, modelIn, _) = this.SetupTestObj(useLocalModel: false, useLoopModel: true);

            this.mockModel
                .Setup(
                    o => o.MergeModels(
                        this.execCtx,
                        modelIn,
                        null,
                        It.Is<IDictionary<string, ModelValue>>(p => p == def.LoopModelTransform)))
                .Returns(modelLoop);

            // test
            await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            this.mockModel
                .Verify(
                    o => o.MergeModels(
                        It.IsAny<IExecuteContext>(),
                        It.IsAny<object>(),
                        It.IsAny<object>(),
                        It.Is<IDictionary<string, ModelValue>>(p => p != refCore.ArgTransform)),
                    Times.Once);

            this.mockModel
                .Verify(
                    o => o.MergeModels(
                        this.execCtx,
                        modelIn,
                        null,
                        It.Is<IDictionary<string, ModelValue>>(p => p == def.LoopModelTransform)),
                    Times.Once);

            this.mockAction.Verify(o => o.ExecuteAsync(this.execCtx, def.Actions[0], modelLoop), Times.Once);
        }

        [TestMethod]
        public async Task ExecuteUsesLoopResultTransformToWriteToLocalModelIfSpecified()
        {
            ForeachActionSetDef def;
            ActionRefCore refCore;
            object modelLocal = new object();
            object modelLoop = new object();
            object modelIn;

            (_, refCore, def, modelIn, _) = this.SetupTestObj(useLocalModel: true, useLoopModel: true, useLoopResult: true);

            this.mockModel
                .Setup(
                    o => o.MergeModels(
                        this.execCtx,
                        modelIn,
                        null,
                        It.Is<IDictionary<string, ModelValue>>(p => p == def.LocalModelTransform)))
                .Returns(modelLocal);

            this.mockModel
                .Setup(
                    o => o.MergeModels(
                        this.execCtx,
                        modelLocal,
                        null,
                        It.Is<IDictionary<string, ModelValue>>(p => p == def.LoopModelTransform)))
                .Returns(modelLoop);

            // test
            await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            this.mockModel
                .Verify(
                    o => o.MergeModels(
                        It.IsAny<IExecuteContext>(),
                        It.IsAny<object>(),
                        It.IsAny<object>(),
                        It.Is<IDictionary<string, ModelValue>>(p => p != refCore.ArgTransform)),
                    Times.Exactly(2));

            this.mockModel
                .Verify(
                    o => o.MergeModels(
                        this.execCtx,
                        modelIn,
                        null,
                        It.Is<IDictionary<string, ModelValue>>(p => p == def.LocalModelTransform)),
                    Times.Once);

            this.mockModel
                .Verify(
                    o => o.MergeModels(
                        this.execCtx,
                        modelLocal,
                        null,
                        It.Is<IDictionary<string, ModelValue>>(p => p == def.LoopModelTransform)),
                    Times.Once);

            this.mockModel
                .Verify(
                    o => o.MergeModels(
                        this.execCtx,
                        modelLoop,
                        modelLocal,
                        It.Is<IDictionary<string, ModelValueUpdate>>(p => p == def.LoopResultTransform)),
                    Times.Once);

            this.mockAction.Verify(o => o.ExecuteAsync(this.execCtx, def.Actions[0], modelLoop), Times.Once);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task ExecuteExecutesTheLoopActionsOnceForEveryLoopElement(bool throwActionExceptions)
        {
            const string TagNameLocal = "FailableActionTag";

            const int Count = 3;

            Mock<IAction> mockActionLocal;


            ForeachActionSet.Args args;
            ICollection<DataRow> coll;
            ForeachActionSetDef def;
            ActionRefCore refCore;
            object modelIn;
            string outKeyVal;

            mockActionLocal = 
                this.SetupMockAction(TagNameLocal, true, true, throwActionExceptions ? new DataActionException(false) : null);

            (args, refCore, def, modelIn, coll) = this.SetupTestObj(collectionCount: Count, tagName: TagNameLocal);

            this.mockModel
                .Setup(
                    o => o.TryExtractValue(
                        It.IsAny<IContext>(),
                        It.IsAny<object>(),
                        args.CollectionItemKeyPropertyName,
                        null,
                        out outKeyVal))
                .OutCallback((IContext c, object p, string k, string d, out string r) => r = ((DataRow)p).Key)
                .Returns(true);

            // test
            await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            foreach (DataRow obj in coll)
            {
                string outKeyVal2;

                this.mockModel
                    .Verify(
                        o => o.TryExtractValue(
                            this.execCtx,
                            obj,
                            args.CollectionItemKeyPropertyName,
                            null,
                            out outKeyVal2),
                        Times.Once);

                this.mockModel
                    .Verify(
                        o => o.AddSubmodel(
                            this.execCtx,
                            modelIn,
                            args.DataRowPropertyName,
                            It.Is<object>(p => p == obj),
                            MergeMode.ReplaceExisting),
                        Times.Once);

                this.mockExecCtx.Verify(o => o.Log(It.Is<string>(p => p.Contains(obj.Key))), Times.Once);
                this.mockExecCtx.Verify(o => o.OnActionUpdate(It.Is<string>(p => p.Contains(obj.Key))), Times.Once);
            }

            mockActionLocal.Verify(o => o.ExecuteAsync(this.execCtx, def.Actions[0], modelIn), Times.Exactly(Count));

            this.mockModel.Verify(o => o.RemoveSubmodel(modelIn, args.DataRowPropertyName), Times.Exactly(Count));
        }

        [TestMethod]
        public async Task ExecuteAbortsLoopIfFatalDataActionExceptionOccurs()
        {
            const string TagNameLocal = "FailableActionTag";

            const int Count = 3;

            Mock<IAction> mockActionLocal;
            
            ForeachActionSet.Args args;
            ICollection<DataRow> coll;
            ForeachActionSetDef def;
            ActionRefCore refCore;
            object modelIn;
            string outKeyVal;

            mockActionLocal =
                this.SetupMockAction(TagNameLocal, true, true, new DataActionException(true));

            (args, refCore, def, modelIn, coll) = this.SetupTestObj(collectionCount: Count, tagName: TagNameLocal);

            this.mockModel
                .Setup(
                    o => o.TryExtractValue(
                        It.IsAny<IContext>(),
                        It.IsAny<object>(),
                        args.CollectionItemKeyPropertyName,
                        null,
                        out outKeyVal))
                .OutCallback((IContext c, object p, string k, string d, out string r) => r = ((DataRow)p).Key)
                .Returns(true);

            try
            {
                await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);
                Assert.Fail("Expected DataActionException exception but no exception thrown");
            }
            catch (DataActionException e)
            {
                Assert.IsTrue(e.IsFatal);

                mockActionLocal.Verify(
                    o => o.ExecuteAsync(It.IsAny<IExecuteContext>(), It.IsAny<ActionRefCore>(), It.IsAny<object>()),
                    Times.Once);
            }
            catch (Exception e)
            {
                Assert.Fail($"Expected DataActionException exception but got {e.GetType().Name} exception");
            }
        }

        [TestMethod]
        public async Task ExecuteAbortsLoopIfAnyNonDataActionExceptionOccurs()
        {
            const string TagNameLocal = "FailableActionTag";

            const int Count = 3;

            Mock<IAction> mockActionLocal;

            ForeachActionSet.Args args;
            ICollection<DataRow> coll;
            ForeachActionSetDef def;
            ActionRefCore refCore;
            object modelIn;
            string outKeyVal;

            mockActionLocal =
                this.SetupMockAction(TagNameLocal, true, true, new ForeachActionSetTestException());

            (args, refCore, def, modelIn, coll) = this.SetupTestObj(collectionCount: Count, tagName: TagNameLocal);

            this.mockModel
                .Setup(
                    o => o.TryExtractValue(
                        It.IsAny<IContext>(),
                        It.IsAny<object>(),
                        args.CollectionItemKeyPropertyName,
                        null,
                        out outKeyVal))
                .OutCallback((IContext c, object p, string k, string d, out string r) => r = ((DataRow)p).Key)
                .Returns(true);

            try
            {
                await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);
                Assert.Fail("Expected ForeachActionSetTestException exception but no exception thrown");
            }
            catch (ForeachActionSetTestException)
            {
                mockActionLocal.Verify(
                    o => o.ExecuteAsync(It.IsAny<IExecuteContext>(), It.IsAny<ActionRefCore>(), It.IsAny<object>()),
                    Times.Once);
            }
            catch (Exception e)
            {
                Assert.Fail($"Expected ForeachActionSetTestException exception but got {e.GetType().Name} exception");
            }
        }

        [TestMethod]
        [DataRow(0, true, LoopResultCondition.AlwaysTrue, null, null, false)]
        [DataRow(0, true, LoopResultCondition.FalseIfAll, null, null, false)]
        [DataRow(0, true, LoopResultCondition.FalseIfAny, null, null, false)]
        [DataRow(0, false, LoopResultCondition.AlwaysTrue, null, null, true)]
        [DataRow(0, false, LoopResultCondition.FalseIfAll, null, null, true)]
        [DataRow(0, false, LoopResultCondition.FalseIfAny, null, null, true)]
        [DataRow(1, true, LoopResultCondition.AlwaysTrue, false, null, true)]
        [DataRow(1, true, LoopResultCondition.AlwaysTrue, true, null, true)]
        [DataRow(1, true, LoopResultCondition.FalseIfAll, false, null, false)]
        [DataRow(1, true, LoopResultCondition.FalseIfAll, true, null, true)]
        [DataRow(1, true, LoopResultCondition.FalseIfAny, false, null, false)]
        [DataRow(1, true, LoopResultCondition.FalseIfAny, true, null, true)]
        [DataRow(2, true, LoopResultCondition.AlwaysTrue, false, false, true)]
        [DataRow(2, true, LoopResultCondition.AlwaysTrue, false, true, true)]
        [DataRow(2, true, LoopResultCondition.AlwaysTrue, true, false, true)]
        [DataRow(2, true, LoopResultCondition.AlwaysTrue, true, true, true)]
        [DataRow(2, true, LoopResultCondition.FalseIfAll, false, false, false)]
        [DataRow(2, true, LoopResultCondition.FalseIfAll, false, true, true)]
        [DataRow(2, true, LoopResultCondition.FalseIfAll, true, false, true)]
        [DataRow(2, true, LoopResultCondition.FalseIfAll, true, true, true)]
        [DataRow(2, true, LoopResultCondition.FalseIfAny, false, false, false)]
        [DataRow(2, true, LoopResultCondition.FalseIfAny, false, true, false)]
        [DataRow(2, true, LoopResultCondition.FalseIfAny, true, false, false)]
        [DataRow(2, true, LoopResultCondition.FalseIfAny, true, true, true)]
        public async Task ExecuteReturnsCorrectResultBasedOnInnerSetResultAndDefSettings(
            int count,
            bool returnFalseOnEmpty,
            LoopResultCondition condition,
            bool? actionResult1,
            bool? actionResult2,
            bool expected)
        {
            ForeachActionSet.Args args;
            ForeachActionSetDef def;
            ActionRefCore refCore;
            object modelIn;
            string outKeyVal;

            ExecuteResult result;

            (args, refCore, def, modelIn, _) = this.SetupTestObj(
                collectionCount: count,
                condition: condition,
                returnFalseOnEmpty: returnFalseOnEmpty);

            this.mockAction
                .SetupSequence(o => o.ExecuteAsync(It.IsAny<IExecuteContext>(), It.IsAny<ActionRefCore>(), It.IsAny<object>()))
                .ReturnsAsync(new ExecuteResult(actionResult1 ?? true))
                .ReturnsAsync(new ExecuteResult(actionResult2 ?? true));

            this.mockModel
                .Setup(
                    o => o.TryExtractValue(
                        It.IsAny<IContext>(),
                        It.IsAny<object>(),
                        args.CollectionItemKeyPropertyName,
                        null,
                        out outKeyVal))
                .OutCallback((IContext c, object p, string k, string d, out string r) => r = ((DataRow)p).Key)
                .Returns(true);

            // test
            result = await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            Assert.AreEqual(expected, result.Continue);

            this.mockAction.Verify(o => o.ExecuteAsync(this.execCtx, def.Actions[0], modelIn), Times.Exactly(count));
            this.mockAction.Verify(
                o => o.ExecuteAsync(It.IsAny<IExecuteContext>(), It.IsAny<ActionRefCore>(), It.IsAny<object>()), 
                Times.Exactly(count));
        }
    }
}
