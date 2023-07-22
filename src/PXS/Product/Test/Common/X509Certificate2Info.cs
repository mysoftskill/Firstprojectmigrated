// <copyright>
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Membership.MemberServices.Test.Common
{
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Configuration;
    using Moq;

    using Microsoft.PrivacyServices.Common.Azure;

    public class X509Certificate2Info
    {
        /// <summary>
        /// Constructor for X509Certificate2Info which specifies which certificate you want to load from store.
        /// </summary>
        /// <param name="subject">The subject of the certificate you want to retrieve. Comma-separated pairs without spaces between key and value.</param>
        /// <param name="issuer">The issuer of the certificate you want to retrieve. Comma-separated pairs without spaces between key and value.</param>
        /// <param name="fileName">The file name of the certificate on local disk.</param>
        /// <param name="password">The password on the certificate on local disk.</param>
        /// <param name="thumbprint">The thumbprint on the certificate on local disk.</param>
        public X509Certificate2Info(string subject, string issuer, string fileName, string password, string thumbprint)
        {
            this.Subject = subject;
            this.Issuer = issuer;
            this.FileName = fileName;
            this.Password = password;
            this.Thumbprint = thumbprint;
        }

        public string Subject { get; set; }

        public string Issuer { get; set; }

        public string FileName { get; set; }

        public string Password { get; set; }

        public string Thumbprint { get; set; }

        public X509Certificate2 LoadFromStore()
        {
            ILogger logger = new ConsoleLogger();
            CertificateProvider certProvider = new CertificateProvider(logger);
            return this.LoadFromStore(certProvider);
        }

        public X509Certificate2 LoadFromStore(ICertificateProvider certProvider)
        {
            Mock<ICertificateConfiguration> mockCertificateConfiguration = new Mock<ICertificateConfiguration>(MockBehavior.Strict);
            mockCertificateConfiguration.SetupGet(m => m.Subject).Returns(this.Subject);
            mockCertificateConfiguration.SetupGet(m => m.Issuer).Returns(this.Issuer);
            mockCertificateConfiguration.SetupGet(m => m.Thumbprint).Returns(this.Thumbprint);
            mockCertificateConfiguration.SetupGet(m => m.CheckValidity).Returns(true);
            return certProvider.GetClientCertificate(mockCertificateConfiguration.Object);
        }

        public X509Certificate2 LoadFromFile()
        {
            return new X509Certificate2(this.FileName, this.Password);
        }
    }
}
