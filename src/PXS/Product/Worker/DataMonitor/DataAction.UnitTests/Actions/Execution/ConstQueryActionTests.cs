// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.UnitTests.Actions
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Actions;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Store;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class ConstQueryActionTests
    {
        private readonly Mock<IModelManipulator> mockModel = new Mock<IModelManipulator>();
        private readonly Mock<IExecuteContext> mockExecCtx = new Mock<IExecuteContext>();
        private readonly Mock<IActionFactory> mockFact = new Mock<IActionFactory>();
        private readonly Mock<IParseContext> mockParseCtx = new Mock<IParseContext>();
        private readonly Mock<IActionStore> mockStore = new Mock<IActionStore>();

        private readonly CancellationTokenSource cancelSource = new CancellationTokenSource();

        private ConstQueryAction testObj;

        private IExecuteContext execCtx;
        private IActionFactory fact;
        private IParseContext parseCtx;
        private IActionStore store;

        [TestInitialize]
        public void Init()
        {
            this.mockExecCtx.SetupGet(o => o.CancellationToken).Returns(this.cancelSource.Token);
            this.mockExecCtx.SetupGet(o => o.IsSimulation).Returns(false);

            this.parseCtx = this.mockParseCtx.Object;
            this.execCtx = this.mockExecCtx.Object;
            this.store = this.mockStore.Object;
            this.fact = this.mockFact.Object;

            this.testObj = new ConstQueryAction(this.mockModel.Object);
        }

        [TestMethod]
        public async Task ExecuteReturnsInputObjectAndMergesItToModel()
        {
            object defTranslate = new object();
            object modelIn = new object();
            object def = new object();

            ActionRefCore refCore = new ActionRefCore
            {
                ResultTransform = new Dictionary<string, ModelValueUpdate>
                {
                    {  "Target", new ModelValueUpdate { Select = "$" } },
                }
            };

            ExecuteResult result;

            this.mockModel.Setup(o => o.TransformFrom(def)).Returns(defTranslate);

            Assert.IsTrue(this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, "localTag", def));
            Assert.IsTrue(this.testObj.ExpandDefinition(this.parseCtx, this.store));
            Assert.IsTrue(this.testObj.Validate(this.parseCtx, null));

            // test
            result = await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            Assert.IsTrue(result.Continue);

            this.mockModel.Verify(o => o.TransformFrom(def), Times.Once);

            this.mockModel.Verify(o => o.MergeModels(this.execCtx, defTranslate, modelIn, refCore.ResultTransform), Times.Once);
        }
    }
}
