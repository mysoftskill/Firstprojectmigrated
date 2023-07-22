// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.UnitTests.DataModel
{
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class ModelUtilitesTests
    {
        private readonly Mock<IParseContext> mockCtx = new Mock<IParseContext>();

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void ValidateModelValueMapReturnsTrueForNullOrEmptyMap(bool isNull)
        {
            IEnumerable<KeyValuePair<string, ModelValue>> input = null;

            bool result;

            if (isNull == false)
            {
                input = new List<KeyValuePair<string, ModelValue>>();
            }

            // test
            result = ModelUtilites.ValidateModelValueMap(this.mockCtx.Object, input);
            
            // validate
            Assert.IsTrue(result);
        }

        private void RunTest(
            string name,
            ModelValue value,
            bool expectedResult,
            string expectedErr)
        {
            IEnumerable<KeyValuePair<string, ModelValue>> input = new List<KeyValuePair<string, ModelValue>>
            {
                new KeyValuePair<string, ModelValue>(name, value),
            };

            bool result;

            // test
            result = ModelUtilites.ValidateModelValueMap(this.mockCtx.Object, input);

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
        [DataRow(false, false, false, "null model values are not permitted")]
        [DataRow(true, false, false, "at least one of")]
        [DataRow(true, true, true, null)]
        public void ValidateReturnsCorrectResultForValueValidation(
            bool hasValue,
            bool validModelValue,
            bool expectedResult,
            string expectedErr)
        {
            ModelValue value = null;

            if (hasValue)
            {
                value = new ModelValue { Const = validModelValue ? new object() : null };
            }

            this.RunTest("key", value, expectedResult, expectedErr);
        }

        [TestMethod]
        [DataRow(" ", false, "name is null or empty")]
        [DataRow("", false, "name is null or empty")]
        [DataRow(null, false, "name is null or empty")]
        [DataRow("name", true, null)]
        public void ValidateReturnsCorrectResultForNameValidation(
            string name,
            bool expectedResult,
            string expectedErr)
        {
            this.RunTest(name, new ModelValue { Const = new object() }, expectedResult, expectedErr);
        }
    }
}
