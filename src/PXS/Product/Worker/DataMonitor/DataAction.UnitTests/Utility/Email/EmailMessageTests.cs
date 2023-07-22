// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.UnitTests.Utility.Email
{
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Utility.Email;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class EmailMessageTests
    {
        private readonly Mock<IContext> mockCtx = new Mock<IContext>();

        private void RunParamTest(
            string to,
            string cc,
            string from,
            string subject,
            bool expected,
            string expectedCtxText)
        {
            EmailMessage def = new EmailMessage
            {
                ToAddresses = to != null ? new[] { to } : null,
                CcAddresses = cc != null ? new[] { cc } : null,

                FromAddress = from,
                Subject = subject,
            };

            // test
            bool result = def.ValidateAndNormalize(this.mockCtx.Object);

            // verify
            Assert.AreEqual(expected, result);
            if (expectedCtxText != null)
            {
                this.mockCtx.Verify(o => o.LogError(It.Is<string>(p => p.Contains(expectedCtxText))), Times.Once);
            }
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        public void ValidateReturnsFalseIfToInvalid(string value)
        {
            this.RunParamTest(value, "cc", "from", "subject", false, "at least one 'to' address");
        }

        [TestMethod]
        [DataRow(true, null, null)]
        [DataRow(false, "", "all specified 'cc' addresses")]
        [DataRow(false, " ", "all specified 'cc' addresses")]
        public void ValidateReturnsFalseIfCcInvalid(
            bool expected,
            string value,
            string expectedCtxText)
        {
            this.RunParamTest("to", value, "from", "subject", expected, expectedCtxText);
        }

        [TestMethod]
        public void ValidateReturnsTrueIfAllValid()
        {
            this.RunParamTest("to", "cc", "from", "subject", true, null);
        }
    }
}
