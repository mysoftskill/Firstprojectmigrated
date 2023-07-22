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
    public class ActionRefTests
    {
        private readonly Mock<IParseContext> mockCtx = new Mock<IParseContext>();

        [TestMethod]
        [DataRow(false, false, true, null)]
        [DataRow(true, true, true, null)]
        [DataRow(true, false, false, "the action tag")]
        public void ValidateReturnsCorrectResultForInnerDefValidation(
            bool hasInnerDef,
            bool hasValidInnerDef,
            bool expectedResult,
            string expectedErr)
        {
            ActionRef testObj = new ActionRef
            {
                Tag = "tag",
                Description = "desc",
                ArgTransform = null,
                ResultTransform = null,
            };

            bool result;

            if (hasInnerDef)
            {
                testObj.Inline = new ActionDef
                {
                    Tag = hasValidInnerDef ? "tag" : null,
                    Type = "type"
                };
            }

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
    }
}
