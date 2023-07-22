// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Azure.ComplianceServices.Common.UnitTests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CertHelperTest
    {
        [TestMethod]
        public void GetCertWithMostRecentIssueDate()
        {
            var certs = new List<X509Certificate2> { UnitTestData.UnitTestCertificate, UnitTestData.UnitTestCertificate2 };

            X509Certificate2 mostRecent = CertHelper.GetCertWithMostRecentIssueDate(certs, requirePrivateKey: false);

            Assert.IsNotNull(mostRecent);
            Assert.IsTrue(certs[0].NotBefore < certs[1].NotBefore);
            Assert.AreEqual(certs[1].NotBefore, mostRecent.NotBefore);
            Assert.AreEqual(certs[1].Thumbprint, mostRecent.Thumbprint);
        }

        [TestMethod]
        public void GetCertWithMostRecentIssueDateRequirePrivateKey()
        {
            var certs = new List<X509Certificate2> { UnitTestData.UnitTestCertificate, UnitTestData.UnitTestCertificate2 };
            Assert.IsTrue(certs[0].HasPrivateKey);
            Assert.IsFalse(certs[1].HasPrivateKey);

            X509Certificate2 mostRecent = CertHelper.GetCertWithMostRecentIssueDate(certs,  requirePrivateKey: true);

            Assert.IsNotNull(mostRecent);
            Assert.AreEqual(certs[0].NotBefore, mostRecent.NotBefore);
            Assert.AreEqual(certs[0].Thumbprint, mostRecent.Thumbprint);
        }

        [TestMethod]
        public void GetCertWithExactSubjectName()
        {
            var certs = new List<X509Certificate2> { UnitTestData.UnitTestCertificate2, UnitTestData.UnitTestCertificate3 };

            string expectedSubject = "unittest";
            var result = CertHelper.GetCertsWithExactSubjectName(certs, expectedSubject);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual($"CN={expectedSubject}", result.First().Subject);

            expectedSubject = "aad.unittest";
            result = CertHelper.GetCertsWithExactSubjectName(certs, expectedSubject);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual($"CN={expectedSubject}", result.First().Subject);
        }
    }
}
