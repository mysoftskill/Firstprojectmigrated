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
    public class ActionSetDefTests
    {
        private readonly Mock<IContext> mockCtx = new Mock<IContext>();

        [TestMethod]
        public void ValidateReturnsFalseIfDuplicateExecutionOrderValuesAreSpecified()
        {
            ActionSetDef def;
            bool result;

            def = new ActionSetDef
            {
                Actions = new List<ActionRef>
                {
                    new ActionRef { Tag = "tag1", ExecutionOrder = 0 },
                    new ActionRef { Tag = "tag2", ExecutionOrder = 0 },
                }
            };

            // test
            result = def.ValidateAndNormalize(this.mockCtx.Object);

            // verify
            Assert.IsFalse(result);
            this.mockCtx.Verify(
                o => o.LogError(It.Is<string>(p => p.Contains("use the same execution order index"))),
                Times.Once);
        }

        [TestMethod]
        public void ValidateReturnsFalseIfInputModeUsedWithLocalTransform()
        {
            ActionSetDef def;
            bool result;

            def = new ActionSetDef
            {
                Actions = new List<ActionRef> { new ActionRef { Tag = "tag1" } },
                LocalModelTransform = new Dictionary<string, ModelValue> { { "key", new ModelValue { Const = new object() } } },
                LocalModelMode = ModelMode.Input,
            };

            // test
            result = def.ValidateAndNormalize(this.mockCtx.Object);

            // verify
            Assert.IsFalse(result);
            this.mockCtx.Verify(
                o => o.LogError(It.Is<string>(p => p.Contains("but a local model transform was found"))),
                Times.Once);
        }

        [TestMethod]
        public void ValidateReturnsFalseIfAnInternalActionRefFailsToValidate()
        {
            ActionSetDef def;
            bool result;

            def = new ActionSetDef
            {
                // tag must be specified for the action ref to be valid, so this will force it being invalid
                Actions = new List<ActionRef> { new ActionRef { Tag = null } },
            };

            // test
            result = def.ValidateAndNormalize(this.mockCtx.Object);

            // verify
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidateReturnsFalseIfInvalidModeSpecified()
        {
            ActionSetDef def;
            bool result;

            def = new ActionSetDef
            {
                Actions = new List<ActionRef> { new ActionRef { Tag = "tag1", ExecutionOrder = 0 } },
                LocalModelTransform = new Dictionary<string, ModelValue> { { "key", new ModelValue { Const = new object() } } },
                LocalModelMode = (ModelMode)int.MaxValue,
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
        public void ValidateReturnsFalseIfInvalidModelTransformSpecified()
        {
            ActionSetDef def;
            bool result;

            def = new ActionSetDef
            {
                Actions = new List<ActionRef> { new ActionRef { Tag = "tag1", ExecutionOrder = 0 } },
                LocalModelMode = ModelMode.Local,

                // specifying no properties for the ModelValue will cause it to be invalid as at least one property must be non-null
                LocalModelTransform = new Dictionary<string, ModelValue> { { "key", new ModelValue() } },
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

            def = new ActionSetDef
            {
                Actions = new List<ActionRef> { new ActionRef { Tag = "tag1" } },
                LocalModelTransform = new Dictionary<string, ModelValue> { { "key", new ModelValue { Const = new object() } } },
                LocalModelMode = ModelMode.Local,
            };

            // test
            result = def.ValidateAndNormalize(this.mockCtx.Object);

            // verify
            Assert.IsTrue(result);
        }
    }
}
