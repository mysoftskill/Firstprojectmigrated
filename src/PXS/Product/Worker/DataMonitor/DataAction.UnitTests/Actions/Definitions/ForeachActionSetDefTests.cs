// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.UnitTests.Actions
{
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Actions;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class ForeachActionSetDefTests
    {
        private readonly Mock<IContext> mockCtx = new Mock<IContext>();

        [TestMethod]
        public void ValidateReturnsFalseIfInvalidLoopModeSpecified()
        {
            ActionSetDef def;
            bool result;

            def = new ForeachActionSetDef
            {
                Actions = new List<ActionRef> { new ActionRef { Tag = "tag1", ExecutionOrder = 0 } },
                LoopModelTransform = new Dictionary<string, ModelValue> { { "key", new ModelValue { Const = new object() } } },
                LoopModelMode = (ModelMode)int.MaxValue,
            };

            // test
            result = def.ValidateAndNormalize(this.mockCtx.Object);

            // verify
            Assert.IsFalse(result);
            this.mockCtx.Verify(
                o => o.LogError(It.Is<string>(p => p.Contains("unknown model mode"))),
                Times.Once);
        }

        [TestMethod]
        public void ValidateReturnsFalseIfInvalidLoopModelTransformSpecified()
        {
            ActionSetDef def;
            bool result;

            def = new ForeachActionSetDef
            {
                Actions = new List<ActionRef> { new ActionRef { Tag = "tag1", ExecutionOrder = 0 } },
                LoopModelMode = ModelMode.Local,

                // specifying no properties for the ModelValue will cause it to be invalid as at least one property must be non-null
                LoopModelTransform = new Dictionary<string, ModelValue> { { "key", new ModelValue() } },
            };

            // test
            result = def.ValidateAndNormalize(this.mockCtx.Object);

            // verify
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidateReturnsTrueIfAllValid()
        {
            ActionSetDef def;
            bool result;

            def = new ForeachActionSetDef
            {
                Actions = new List<ActionRef> { new ActionRef { Tag = "tag1" } },
                LoopModelTransform = new Dictionary<string, ModelValue> { { "key", new ModelValue { Const = new object() } } },
                LoopModelMode = ModelMode.Local,
            };

            // test
            result = def.ValidateAndNormalize(this.mockCtx.Object);

            // verify
            Assert.IsTrue(result);
        }
    }
}
