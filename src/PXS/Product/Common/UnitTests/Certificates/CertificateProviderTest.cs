// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.UnitTests
{
    using System;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class CertificateProviderTest
    {
        private readonly Mock<ILogger> mockLogger = CreateGenevaLogger();

        [TestMethod]
        public void CertificateProviderGetCertificateNull()
        {
            CertificateProvider provider = new CertificateProvider(mockLogger.Object);
            X509Certificate2 cert = provider.GetClientCertificate((ICertificateConfiguration)null);
            Assert.IsNull(cert);
        }

        [TestMethod]
        public void CertificateProviderGetCertificateDoesNotExist()
        {
            Mock<ICertificateConfiguration> configuration = new Mock<ICertificateConfiguration>();
            configuration.SetupGet(m => m.Issuer).Returns("This certificate does not exist");
            configuration.SetupGet(m => m.Subject).Returns("This certificate does not exist");
            configuration.SetupGet(m => m.Thumbprint).Returns("AFAKETHUMBPRINT");

            CertificateProvider provider = new CertificateProvider(mockLogger.Object);

            Exception actualException = null;
            try
            {
                provider.GetClientCertificate(configuration.Object);
            }
            catch (Exception exception)
            {
                actualException = exception;
            }

            Assert.IsNotNull(actualException);

            ArgumentException argumentException = actualException as ArgumentException;
            Assert.IsNotNull(argumentException);
            Assert.AreEqual("subject", argumentException.ParamName);
            string expectedExceptionMessage = "Certificate Subject: This certificate does not exist was not found in LocalMachine store, or was invalid\r\nParameter name: subject";
            Assert.AreEqual(expectedExceptionMessage, argumentException.Message);
        }

        private static Mock<ILogger> CreateGenevaLogger()
        {
            Mock<ILogger> mockLogger = new Mock<ILogger>();
            mockLogger.Setup((m) => m.Information(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
            mockLogger.Setup((m) => m.Verbose(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
            mockLogger.Setup((m) => m.Error(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
            mockLogger.Setup((m) => m.Warning(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
            return mockLogger;
        }
    }
}
