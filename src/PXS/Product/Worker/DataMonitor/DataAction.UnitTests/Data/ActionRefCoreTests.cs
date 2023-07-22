// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.UnitTests.Data
{
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class ActionRefCoreTests
    {
        private readonly Mock<IParseContext> mockCtx = new Mock<IParseContext>();

        private void RunTest(
            string desc,
            string tag,
            IDictionary<string, ModelValue> argMap,
            IDictionary<string, ModelValueUpdate> resultMap,
            bool expectedResult,
            string expectedErr)
        {
            ActionRefCore testObj = new ActionRefCore
            {
                Tag = tag,
                Description = desc,
                ArgTransform = argMap,
                ResultTransform = resultMap
            };

            bool result;

            // test
            result = testObj.ValidateAndNormalize(this.mockCtx.Object);

            // validate
            Assert.AreEqual(expectedResult, result);

            if (expectedErr != null)
            {
                this.mockCtx.Verify(
                    o => o.LogError(It.Is<string>(p => p.Contains(expectedErr))),
                    Times.AtLeastOnce);
            }
        }

        [TestMethod]
        [DataRow(null, false, "the action tag")]
        [DataRow("", false, "the action tag")]
        [DataRow(" ", false, "the action tag")]
        [DataRow("tag", true, null)]
        public void ValidateReturnsCorrectResultForTagValidation(
            string tag,
            bool expectedResult,
            string expectedErr)
        {
            this.RunTest("desc", tag, null, null, expectedResult, expectedErr);
        }

        [TestMethod]
        [DataRow("", false, "the action description")]
        [DataRow(" ", false, "the action description")]
        [DataRow(null, true, null)]
        [DataRow("desc", true, null)]
        public void ValidateReturnsCorrectResultForDescriptionValidation(
            string desc,
            bool expectedResult,
            string expectedErr)
        {
            this.RunTest(desc, "tag", null, null, expectedResult, expectedErr);
        }

        [TestMethod]
        [DataRow(false, false, true, null)]
        [DataRow(true, true, true, null)]
        [DataRow(true, false, false, "null model values are not permitted")]
        public void ValidateReturnsCorrectResultForArgTransform(
            bool hasArgMap,
            bool hasValidModel,
            bool expectedResult,
            string expectedErr)
        {
            IDictionary<string, ModelValue> map = null;

            if (hasArgMap)
            {
                map = new Dictionary<string, ModelValue>
                {
                    { "key", hasValidModel ? new ModelValue { Const = new object() } : null }
                };
            }

            this.RunTest("desc", "tag", map, null, expectedResult, expectedErr);
        }

        [TestMethod]
        [DataRow(false, false, true, null)]
        [DataRow(true, true, true, null)]
        [DataRow(true, false, false, "null model values are not permitted")]
        public void ValidateReturnsCorrectResultForResultTransform(
            bool hasResultMap,
            bool hasValidModel,
            bool expectedResult,
            string expectedErr)
        {
            IDictionary<string, ModelValueUpdate> map = null;

            if (hasResultMap)
            {
                map = new Dictionary<string, ModelValueUpdate>
                {
                    { "key", hasValidModel ? new ModelValueUpdate { Const = new object() } : null }
                };
            }

            this.RunTest("desc", "tag", null, map, expectedResult, expectedErr);
        }
    }
}
