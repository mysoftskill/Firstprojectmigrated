using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Osgs.Infra.Platform;
using Microsoft.PrivacyServices.UX.Core.Security;
using Moq;

namespace Microsoft.PrivacyServices.UX.Tests.Mocks.Configuration
{
    public static class MockCertificateFinder
    {
        /// <summary>
        /// Creates a new instance of <see cref="ICertificateFinder"/> mock.
        /// </summary>
        public static Mock<ICertificateFinder> Create(string thumbprint = null, string subjectName = null)
        {
            var certficateFinder = new Mock<ICertificateFinder>(MockBehavior.Strict);

            certficateFinder
                    .Setup(m => m.FindBySubjectName(subjectName == null ? It.IsAny<string>() : subjectName))
                    .Returns(CreateTestCert());

            return certficateFinder;
        }

        public static X509Certificate2 CreateTestCert()
        {
            byte[] testCert;

            // It's used for unit test only.
            using (Stream certStream = File.OpenRead("Mocks/Configuration/test.cer"))
            {
                testCert = new byte[certStream.Length];
                certStream.Read(testCert, 0, (int)certStream.Length);
            }

            return new X509Certificate2(testCert, "password");
        }
    }
}
