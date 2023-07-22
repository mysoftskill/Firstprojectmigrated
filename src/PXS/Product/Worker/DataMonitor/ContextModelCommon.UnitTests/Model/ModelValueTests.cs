// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.UnitTests.Utility.Model
{
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class ModelValueTests
    {
        private readonly Mock<IParseContext> mockCtx = new Mock<IParseContext>();

        private void RunTest(
            string select,
            string selectMany,
            object constVal,
            bool expectedResult,
            string expectedErr)
        {
            ModelValue testObj = new ModelValue { SelectMany = selectMany, Select = select, Const = constVal };

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
        [DataRow(null, false, "at least one of")]
        [DataRow("", false, "at least one of")]
        [DataRow(" ", false, "at least one of")]
        [DataRow("type", true, null)]
        public void ValidateReturnsCorrectResultForSelectValidation(
            string select,
            bool expectedResult,
            string expectedErr)
        {
            this.RunTest(select, null, null, expectedResult, expectedErr);
        }

        [TestMethod]
        [DataRow(null, false, "at least one of")]
        [DataRow("", false, "at least one of")]
        [DataRow(" ", false, "at least one of")]
        [DataRow("type", true, null)]
        public void ValidateReturnsCorrectResultForSelectManyValidation(
            string selectMany,
            bool expectedResult,
            string expectedErr)
        {
            this.RunTest(null, selectMany, null, expectedResult, expectedErr);
        }

        [TestMethod]
        [DataRow(null, false, "at least one of")]
        [DataRow("value", true, null)]
        public void ValidateReturnsCorrectResultForConstValidation(
            object constVal,
            bool expectedResult,
            string expectedErr)
        {
            this.RunTest(null, null, constVal, expectedResult, expectedErr);
        }
    }
}
