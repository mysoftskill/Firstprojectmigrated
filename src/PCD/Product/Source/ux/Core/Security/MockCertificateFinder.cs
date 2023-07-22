using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Microsoft.PrivacyServices.UX.Core.Security
{
    public sealed class MockCerficateFinder : ICertificateFinder
    {
        /// <summary>
        /// Finds certificate by subject name.
        /// </summary>
        /// <param name="subjectName">The serial number of the certificate.</param>
        /// <returns>The certificate.</returns>
        public X509Certificate2 FindBySubjectName(string subjectName)
        {
            return CreateTestCert();
        }

        public static X509Certificate2 CreateTestCert()
        {
            byte[] testCert;

            // It's used for unit test only.
            using (Stream certStream = File.OpenRead("Core/Security/test.cer"))
            {
                testCert = new byte[certStream.Length];
                certStream.Read(testCert, 0, (int)certStream.Length);
            }

            return new X509Certificate2(testCert, "password");
        }
    }
}
