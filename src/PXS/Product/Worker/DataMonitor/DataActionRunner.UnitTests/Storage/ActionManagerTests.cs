// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.Runner.UnitTests.Storage
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.Practices.Unity;
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.TemplateBuilder;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Store;
    using Microsoft.PrivacyServices.DataMonitor.Runner.Data;
    using Microsoft.PrivacyServices.DataMonitor.Runner.Storage;
    using Microsoft.PrivacyServices.DataMonitor.Runner.Utility;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class ActionManagerTests
    {
        private readonly Mock<IActionLibraryAccessor> mockRet = new Mock<IActionLibraryAccessor>();
        private readonly Mock<ILocalUnityRegistrar> mockUnityReg = new Mock<ILocalUnityRegistrar>();
        private readonly Mock<IQueue<JobWorkItem>> mockQ = new Mock<IQueue<JobWorkItem>>();
        private readonly Mock<IUnityContainer> mockChild = new Mock<IUnityContainer>();
        private readonly Mock<IUnityContainer> mockRoot = new Mock<IUnityContainer>(MockBehavior.Strict);
        private readonly Mock<IActionRefStore> mockActionRefs = new Mock<IActionRefStore>();
        private readonly Mock<IExecuteContext> mockExecCtx = new Mock<IExecuteContext>();
        private readonly Mock<ITemplateStore> mockTemplates = new Mock<ITemplateStore>();
        private readonly Mock<IParseContext> mockParseCtx = new Mock<IParseContext>();
        private readonly Mock<IActionStore> mockActions = new Mock<IActionStore>();

        public ICollection<ActionRefRunnable> resultRefSet = new List<ActionRefRunnable>
        {
            new ActionRefRunnable { Tag = "TAG", Id = "ID" }
        };

        private IParseContext pctx;
        private ActionManager testObj;

        [TestInitialize]
        public void Init()
        {
            this.mockUnityReg
                .Setup(o => o.SetupLocalContainer(It.IsAny<IUnityContainer>(), It.IsAny<IActionLibraryAccessor>())) 
                .Returns(this.mockChild.Object);

            this.mockChild
                .Setup(o => o.Resolve(typeof(IActionRefStore), It.IsAny<string>(), It.IsAny<ResolverOverride[]>()))
                .Returns(this.mockActionRefs.Object);
            this.mockChild
                .Setup(o => o.Resolve(typeof(ITemplateStore), It.IsAny<string>(), It.IsAny<ResolverOverride[]>()))
                .Returns(this.mockTemplates.Object);
            this.mockChild
                .Setup(o => o.Resolve(typeof(IActionStore), It.IsAny<string>(), It.IsAny<ResolverOverride[]>()))
                .Returns(this.mockActions.Object);

            this.mockActionRefs.Setup(o => o.RefreshAsync(It.IsAny<IParseContext>())).ReturnsAsync(true);
            this.mockTemplates.Setup(o => o.RefreshAsync(It.IsAny<IParseContext>(), It.IsAny<bool>())).ReturnsAsync(true);
            this.mockActions.Setup(o => o.RefreshAsync(It.IsAny<IParseContext>(), It.IsAny<bool>())).ReturnsAsync(true);

            this.mockActionRefs
                .Setup(
                    o => o.UpdateAsync(
                        It.IsAny<IParseContext>(), 
                        It.IsAny<ICollection<string>>(), 
                        It.IsAny<ICollection<ActionRefRunnable>>()))
                .ReturnsAsync(true);

            this.mockTemplates
                .Setup(
                    o => o.UpdateAsync(
                        It.IsAny<IParseContext>(),
                        It.IsAny<ICollection<string>>(),
                        It.IsAny<ICollection<TemplateDef>>()))
                .ReturnsAsync(true);

            this.mockActions
                .Setup(
                    o => o.UpdateAsync(
                        It.IsAny<IParseContext>(),
                        It.IsAny<ICollection<string>>(),
                        It.IsAny<ICollection<ActionDef>>()))
                .ReturnsAsync(true);

            this.mockActionRefs.Setup(o => o.EnumerateReferences()).Returns(() => this.resultRefSet);

            this.mockActions
                .Setup(o => o.ValidateReference(It.IsAny<IParseContext>(), It.IsAny<ActionRef>()))
                .Returns(true);

            this.pctx = this.mockParseCtx.Object;

            this.testObj = new ActionManager(this.mockRoot.Object, this.mockUnityReg.Object, this.mockRet.Object);
        }

        [TestMethod]
        public void ConstructorCallsUnityRegistrarWithProvidedParams()
        {
            // verify action done in init
            this.mockUnityReg.Verify(o => o.SetupLocalContainer(this.mockRoot.Object, this.mockRet.Object), Times.Once);
        }

        [TestMethod]
        public async Task InitializeRetrievesActionsTemplatesAndRefsFromStore()
        {
            bool result;

            result = await this.testObj.InitializeAndRetrieveActionsAsync(this.pctx, false);

            Assert.IsTrue(result);

            this.mockActionRefs.Verify(o => o.RefreshAsync(this.pctx), Times.Once);
            this.mockTemplates.Verify(o => o.RefreshAsync(this.pctx, false), Times.Once);
            this.mockActions.Verify(o => o.RefreshAsync(this.pctx, false), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task InitializeThrowsIfCalledTwice()
        {
            await this.testObj.InitializeAndRetrieveActionsAsync(this.pctx, false);
            await this.testObj.InitializeAndRetrieveActionsAsync(this.pctx, false);
        }

        [TestMethod]
        public async Task InitializeReturnsFalseIfTemplateFetchFails()
        {
            bool result;

            this.mockTemplates.Setup(o => o.RefreshAsync(It.IsAny<IParseContext>(), false)).ReturnsAsync(false);

            // test
            result = await this.testObj.InitializeAndRetrieveActionsAsync(this.pctx, false);

            // verify
            Assert.IsFalse(result);

            this.mockTemplates.Verify(o => o.RefreshAsync(this.pctx, false), Times.Once);
        }

        [TestMethod]
        public async Task InitializeReturnsFalseIfActionFetchFails()
        {
            bool result;

            this.mockActions.Setup(o => o.RefreshAsync(It.IsAny<IParseContext>(), false)).ReturnsAsync(false);

            // test
            result = await this.testObj.InitializeAndRetrieveActionsAsync(this.pctx, false);

            // verify
            Assert.IsFalse(result);

            this.mockActions.Verify(o => o.RefreshAsync(this.pctx, false), Times.Once);
        }

        [TestMethod]
        public async Task InitializeReturnsFalseIfActionRefFetchFails()
        {
            bool result;

            this.mockActionRefs.Setup(o => o.RefreshAsync(It.IsAny<IParseContext>())).ReturnsAsync(false);

            // test
            result = await this.testObj.InitializeAndRetrieveActionsAsync(this.pctx, false);

            // verify
            Assert.IsFalse(result);

            this.mockActionRefs.Verify(o => o.RefreshAsync(this.pctx), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void EnumerateThrowsIfNotInitialized()
        {
            this.testObj.EnumerateContents();
        }

        [TestMethod]
        public async Task EnumerateReturnsValuesProvidedByInnerStores()
        {
            List<ActionRefRunnable> arefs = new List<ActionRefRunnable>();
            List<TemplateDef> tdefs = new List<TemplateDef>();
            List<ActionDef> adefs = new List<ActionDef>();

            ActionStoreContents result;

            this.mockActionRefs.Setup(o => o.EnumerateReferences()).Returns(arefs);
            this.mockTemplates.Setup(o => o.EnumerateTemplates()).Returns(tdefs);
            this.mockActions.Setup(o => o.EnumerateActions()).Returns(adefs);

            await this.testObj.InitializeAndRetrieveActionsAsync(this.pctx, false);

            // test 
            result = this.testObj.EnumerateContents();

            // verify
            Assert.IsNotNull(result);
            Assert.AreSame(result.ActionReferences, arefs);
            Assert.AreSame(result.Templates, tdefs);
            Assert.AreSame(result.Actions, adefs);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task EnqueueActionsThrowsIfNotInitialized()
        {
            await this.testObj.EnqueueActionsToExecuteAsync(this.mockQ.Object, CancellationToken.None);
        }

        [TestMethod]
        public async Task EnqueueActionsCleansUpIfRefListIsEmpty()
        {
            this.mockActionRefs.Setup(o => o.EnumerateReferences()).Returns(new List<ActionRefRunnable>());

            await this.testObj.InitializeAndRetrieveActionsAsync(this.pctx, false);

            // test
            await this.testObj.EnqueueActionsToExecuteAsync(this.mockQ.Object, CancellationToken.None);

            // verify
            this.mockChild.Verify(o => o.Dispose(), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task EnqueueActionsThrowsIfCleanedUp()
        {
            this.mockActionRefs.Setup(o => o.EnumerateReferences()).Returns(new List<ActionRefRunnable>());

            await this.testObj.InitializeAndRetrieveActionsAsync(this.pctx, false);

            // this should result in an empty queue which will cause the object to self cleanup
            await this.testObj.EnqueueActionsToExecuteAsync(this.mockQ.Object, CancellationToken.None);

            // test
            await this.testObj.EnqueueActionsToExecuteAsync(this.mockQ.Object, CancellationToken.None);
        }

        [TestMethod]
        public async Task EnqueueActionsEnqueuesOnceForEachRef()
        {
            const string TemplatePropName = "tpropname";
            const string TemplateName = "tname";

            Func<JobWorkItem, string, bool, bool> verifier =
                (item, tag, templateHasValues) =>
                {
                    bool result = object.ReferenceEquals(this.testObj, item.Executor) && tag.Equals(item.ActionRef.Tag);

                    if (templateHasValues)
                    {
                        const string GroupName = nameof(ActionRefRunnable.Templates);

                        result = 
                            result &&
                            item.ExtensionProperties != null &&
                            item.ExtensionProperties.Count == 1 &&
                            item.ExtensionProperties.ContainsKey(GroupName) &&
                            item.ExtensionProperties[GroupName].ContainsKey(TemplatePropName) &&
                            TemplateName.Equals(item.ExtensionProperties[GroupName][TemplatePropName]);
                    }
                    else
                    {
                        result = item.ExtensionProperties == null || item.ExtensionProperties.Count == 0;
                    }

                    return result;
                };

            this.resultRefSet = new[] 
            {
                new ActionRefRunnable { Tag = "t2" },

                new ActionRefRunnable
                {
                    Tag = "t1",
                    Templates = new Dictionary<string, string> { { TemplatePropName, TemplateName } }
                },
            };

            await this.testObj.InitializeAndRetrieveActionsAsync(this.pctx, false);

            // test
            await this.testObj.EnqueueActionsToExecuteAsync(this.mockQ.Object, CancellationToken.None);

            // verify
            this.mockChild.Verify(o => o.Dispose(), Times.Never);
            this.mockQ.Verify(
                o => o.EnqueueAsync(It.Is<JobWorkItem>(p => verifier(p, "t1", true)), CancellationToken.None), 
                Times.Once);
            this.mockQ.Verify(
                o => o.EnqueueAsync(It.Is<JobWorkItem>(p => verifier(p, "t2", false)), CancellationToken.None), 
                Times.Once);
            this.mockQ.Verify(o => o.EnqueueAsync(It.IsAny<JobWorkItem>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task ExecuteThrowsIfNotInitialized()
        {
            await this.testObj.EnqueueActionsToExecuteAsync(this.mockQ.Object, CancellationToken.None);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task ExecuteActionsThrowsIfCleanedUp()
        {
            ActionRef aref = new ActionRef();

            this.mockActionRefs.Setup(o => o.EnumerateReferences()).Returns(new List<ActionRefRunnable>());

            await this.testObj.InitializeAndRetrieveActionsAsync(this.pctx, false);

            // this should result in an empty queue which will cause the object to self cleanup
            await this.testObj.EnqueueActionsToExecuteAsync(this.mockQ.Object, CancellationToken.None);

            // test
            await this.testObj.ExecuteActionAsync(this.mockExecCtx.Object, aref);
        }
        
        [TestMethod]
        public async Task ExecuteCallsExecuteOnStoreWithActionReturnsResultAndCleansUp()
        {
            IExecuteContext ctx = this.mockExecCtx.Object;
            object expected = new object();
            object result;

            this.resultRefSet = new[] { new ActionRefRunnable { Tag = "t1" } };

            await this.testObj.InitializeAndRetrieveActionsAsync(this.pctx, false);

            // this should result in an empty queue which will cause the object to self cleanup
            await this.testObj.EnqueueActionsToExecuteAsync(this.mockQ.Object, CancellationToken.None);

            this.mockActions
                .Setup(
                    o => o.ExecuteActionAsync(
                        It.IsAny<IExecuteContext>(), 
                        It.IsAny<ActionRef>()))
                .ReturnsAsync(expected);

            // test
            result = await this.testObj.ExecuteActionAsync(ctx, this.resultRefSet.First());

            // verify
            Assert.AreSame(expected, result);
            this.mockActions.Verify(o => o.ExecuteActionAsync(ctx, this.resultRefSet.First()), Times.Once);
            this.mockChild.Verify(o => o.Dispose(), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task UpdateThrowsIfNotInitialized()
        {
            await this.testObj.UpdateAsync(this.pctx, new ActionStoreUpdate());
        }

        [TestMethod]
        public async Task UpdateCallsIntoUpdateMethordsProvidedByInnerStores()
        {
            ActionStoreUpdate update = new ActionStoreUpdate
            {
                ActionReferenceDeletes = new List<string>(),
                ActionReferenceUpdates = new List<ActionRefRunnable>(),
                TemplateDeletes = new List<string>(),
                TemplateUpdates = new List<TemplateDef>(),
                ActionDeletes = new List<string>(),
                ActionUpdates = new List<ActionDef>(),
            };

            bool result;

            await this.testObj.InitializeAndRetrieveActionsAsync(this.pctx, false);

            // test 
            result = await this.testObj.UpdateAsync(this.pctx, update);

            // verify
            Assert.IsTrue(result);

            this.mockActionRefs
                .Verify(o => o.UpdateAsync(this.pctx, update.ActionReferenceDeletes, update.ActionReferenceUpdates), Times.Once);

            this.mockTemplates.Verify(o => o.UpdateAsync(this.pctx, update.TemplateDeletes, update.TemplateUpdates), Times.Once);
            this.mockActions.Verify(o => o.UpdateAsync(this.pctx, update.ActionDeletes, update.ActionUpdates), Times.Once);
        }

        [TestMethod]
        public async Task UpdateReturnFalseIfActionStoreReturnsFalse()
        {
            bool result;

            await this.testObj.InitializeAndRetrieveActionsAsync(this.pctx, false);

            this.mockActions
                .Setup(
                    o => o.UpdateAsync(
                        It.IsAny<IParseContext>(),
                        It.IsAny<ICollection<string>>(),
                        It.IsAny<ICollection<ActionDef>>()))
                .ReturnsAsync(false);

            // test 
            result = await this.testObj.UpdateAsync(this.pctx, new ActionStoreUpdate());

            // verify
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task UpdateReturnFalseIfActionRefStoreReturnsFalse()
        {
            bool result;

            await this.testObj.InitializeAndRetrieveActionsAsync(this.pctx, false);

            this.mockActionRefs
                .Setup(
                    o => o.UpdateAsync(
                        It.IsAny<IParseContext>(),
                        It.IsAny<ICollection<string>>(),
                        It.IsAny<ICollection<ActionRefRunnable>>()))
                .ReturnsAsync(false);

            // test 
            result = await this.testObj.UpdateAsync(this.pctx, new ActionStoreUpdate());

            // verify
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task UpdateReturnFalseIfTemplateStoreReturnsFalse()
        {
            bool result;

            await this.testObj.InitializeAndRetrieveActionsAsync(this.pctx, false);

            this.mockTemplates
                .Setup(
                    o => o.UpdateAsync(
                        It.IsAny<IParseContext>(),
                        It.IsAny<ICollection<string>>(),
                        It.IsAny<ICollection<TemplateDef>>()))
                .ReturnsAsync(false);

            // test 
            result = await this.testObj.UpdateAsync(this.pctx, new ActionStoreUpdate());

            // verify
            Assert.IsFalse(result);
        }
    }
}
