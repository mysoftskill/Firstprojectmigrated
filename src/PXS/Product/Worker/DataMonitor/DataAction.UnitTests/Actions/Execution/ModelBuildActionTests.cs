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
    public class ModelBuildActionTests
    {
        private readonly Mock<IModelManipulator> mockModel = new Mock<IModelManipulator>();
        private readonly Mock<IExecuteContext> mockExecCtx = new Mock<IExecuteContext>();
        private readonly Mock<IActionFactory> mockFact = new Mock<IActionFactory>();
        private readonly Mock<IParseContext> mockParseCtx = new Mock<IParseContext>();
        private readonly Mock<IActionStore> mockStore = new Mock<IActionStore>();

        private readonly CancellationTokenSource cancelSource = new CancellationTokenSource();

        private ModelBuildAction testObj;

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

            this.testObj = new ModelBuildAction(this.mockModel.Object);
        }

        [TestMethod]
        public void ParseAndProcessReturnsFalseIfNonNullDefinitionProvided()
        {
            object def = new object();

            bool result;

            // test
            result = this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, "localTag", def);
            
            // verify
            Assert.IsFalse(result);
            this.mockParseCtx.Verify(
                o => o.LogError(It.Is<string>(p => p.Contains("require no action definition, but a definition of type"))),
                Times.Once);
        }

        [TestMethod]
        public async Task ExecuteReturnsInputObjectAndMergesItToModel()
        {
            object modelIn = new object();

            ActionRefCore refCore = new ActionRefCore
            {
                ResultTransform = new Dictionary<string, ModelValueUpdate>
                {
                    {  "Target", new ModelValueUpdate { Select = "Source" } },
                }
            };

            ExecuteResult result;

            Assert.IsTrue(this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, "localTag", null));
            Assert.IsTrue(this.testObj.ExpandDefinition(this.parseCtx, this.store));
            Assert.IsTrue(this.testObj.Validate(this.parseCtx, null));

            // test
            result = await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            Assert.IsTrue(result.Continue);

            this.mockModel.Verify(o => o.MergeModels(this.execCtx, modelIn, modelIn, refCore.ResultTransform), Times.Once);
        }
    }
}
