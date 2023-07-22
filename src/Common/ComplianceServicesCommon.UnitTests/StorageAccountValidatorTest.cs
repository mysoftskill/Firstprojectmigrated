// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Azure.ComplianceServices.Common.UnitTests
{
    using Microsoft.Azure.ComplianceServices.Common.AzureServiceUrlValidator;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class StorageAccountValidatorTest
    {
        [TestMethod]
        [DataRow("testaccount1")]
        [DataRow("foo")]
        [DataRow("101")]
        [DataRow("testaccount1234567890123")]
        public void ValidStorageAccountNames(string accountName)
        {
            var result = StorageAccountValidator.IsValidStorageAccountName(accountName);
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        [DataRow("hi")]
        [DataRow("testaccount1234567890123toolong")]
        [DataRow("TestAccount123")]
        [DataRow("testaccount-123")]
        public void InvalidStorageAccountNames(string accountName)
        {
            var result = StorageAccountValidator.IsValidStorageAccountName(accountName);
            Assert.IsFalse(result.IsValid);
        }
    }
}
