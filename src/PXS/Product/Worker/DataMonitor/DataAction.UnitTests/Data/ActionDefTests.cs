// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.UnitTests.Data
{
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class ActionDefTests
    {
        private readonly Mock<IParseContext> mockCtx = new Mock<IParseContext>();

        private void RunTest(
            string type,
            string tag,
            bool expectedResult,
            string expectedErr)
        {
            ActionDef testObj = new ActionDef { Tag = tag, Type = type };

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
        [DataRow("type", true, null)]
        public void ValidateReturnsCorrectResultForTagValidation(
            string tag,
            bool expectedResult,
            string expectedErr)
        {
            this.RunTest("type", tag, expectedResult, expectedErr);
        }

        [TestMethod]
        [DataRow(null, false, "the action type")]
        [DataRow("", false, "the action type")]
        [DataRow(" ", false, "the action type")]
        [DataRow("tag", true, null)]
        public void ValidateReturnsCorrectResultForTypeValidation(
            string type,
            bool expectedResult,
            string expectedErr)
        {
            this.RunTest(type, "tag", expectedResult, expectedErr);
        }
    }
}
