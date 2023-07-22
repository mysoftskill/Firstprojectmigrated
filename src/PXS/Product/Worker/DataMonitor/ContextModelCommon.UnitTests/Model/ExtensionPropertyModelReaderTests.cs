// ---------------------------------------------------------------------------
// <copyright file="AggregatingModelManipulatorTests.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.UnitTests.Utility.Model
{
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class ExtensionPropertyModelReaderTests
    {
        private readonly Mock<IContext> mockCtx = new Mock<IContext>(MockBehavior.Strict);

        private ExtensionPropertyModelReader testObj;
        private IContext ctx;

        [TestInitialize]
        public void Init()
        {
            this.testObj = new ExtensionPropertyModelReader();
            this.ctx = this.mockCtx.Object;
        }

        [TestMethod]
        [DataRow("@.g.n", true, "g", "n", null, "result", "result")]
        [DataRow("@.g.n", true, "g", "n", null, "result", "result")]
        [DataRow("#.g.n", false, "g", "n", null, null, null)]
        [DataRow("#.g.n", false, "g", "n", "def", null, "def")]
        [DataRow("@.g", false, "g", null, null, null, null)]
        [DataRow("@.g", false, "g", null, "def", null, "def")]
        [DataRow("@.g.n.e", false, "g", "n", null, null, null)]
        [DataRow("@.g.n.e", false, "g", "n", "def", null, "def")]
        [DataRow("@2.g.n", false, "g", "n", null, null, null)]
        [DataRow("@2.g.n", false, "g", "n", "def", null, "def")]
        public void TryExtractReturnsTheCorrectValue(
            string selector,
            bool shouldCallCtx,
            string groupName,
            string name,
            string defValue,
            string getResultValue,
            string expected)
        {
            object value;
            bool result;

            if (shouldCallCtx)
            {
                this.mockCtx.Setup(o => o.GetExtensionPropertyValue(groupName, name)).Returns(getResultValue);
            }

            // test 
            result = this.testObj.TryExtractValue(this.ctx, null, selector, defValue, out value);

            // verify
            Assert.AreEqual(shouldCallCtx && getResultValue != null, result);
            Assert.AreEqual(expected, value);

            if (shouldCallCtx)
            {
                this.mockCtx.Verify(o => o.GetExtensionPropertyValue(groupName, name), Times.Once);
            }

        }
    }
}
