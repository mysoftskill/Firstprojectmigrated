// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.UnitTests
{
    using System;
    using System.Net.Http;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Extensions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using HttpClient = Microsoft.OSGS.HttpClientCommon.HttpClient;

    /// <summary>
    /// HttpMessageHandlerExtensions Test
    /// </summary>
    [TestClass]
    public class HttpMessageHandlerExtensionsTest
    {
        [TestMethod]
        public void HttpMessageHandlerExtensionsAttachClientCertificateWebRequestHandlerOnly()
        {
            WebRequestHandler webRequestHandler = new WebRequestHandler();
            HttpClient client = new HttpClient(webRequestHandler);

            X509Certificate2 certificate = new X509Certificate2();

            client.MessageHandler.AttachClientCertificate(certificate);

            Assert.IsTrue(webRequestHandler.ClientCertificates.Contains(certificate));
        }

        [TestMethod]
        public void HttpMessageHandlerExtensionsAttachClientCertificateWebRequestHandlerWithinDelegatingHandler()
        {
            WebRequestHandler webRequestHandler = new WebRequestHandler();
            TestDelegatingHandler delegatingHandler = new TestDelegatingHandler(webRequestHandler);
            HttpClient client = new HttpClient(delegatingHandler);

            X509Certificate2 certificate = new X509Certificate2();
            client.MessageHandler.AttachClientCertificate(certificate);
            Assert.IsTrue(webRequestHandler.ClientCertificates.Contains(certificate));
        }

        [TestMethod]
        public void HttpClientExtensionsAttachClientCertificateWebRequestHandlerWithin2DelegatingHandlers()
        {
            WebRequestHandler webRequestHandler = new WebRequestHandler();
            TestDelegatingHandler delegatingHandler1 = new TestDelegatingHandler(webRequestHandler);
            TestDelegatingHandler delegatingHandler2 = new TestDelegatingHandler(delegatingHandler1);
            HttpClient client = new HttpClient(delegatingHandler2);

            X509Certificate2 certificate = new X509Certificate2();
            client.MessageHandler.AttachClientCertificate(certificate);
            Assert.IsTrue(webRequestHandler.ClientCertificates.Contains(certificate));
        }

        [ExpectedException(typeof(ArgumentNullException))]
        [TestMethod]
        public void HttpClientExtensionsAttachClientCertificateHttpClientNull()
        {
            HttpMessageHandlerExtensions.AttachClientCertificate(null, new X509Certificate2());
        }

        [ExpectedException(typeof(ArgumentNullException))]
        [TestMethod]
        public void HttpClientExtensionsAttachClientCertificateCertificateNull()
        {
            HttpClient client = new HttpClient(new WebRequestHandler());
            client.MessageHandler.AttachClientCertificate(null);
        }

        [ExpectedException(typeof(ArgumentNullException))]
        [TestMethod]
        public void HttpClientExtensionsAttachClientCertificateNoMessageHandler()
        {
            HttpClient client = new HttpClient();
            client.MessageHandler.AttachClientCertificate(new X509Certificate2());
        }

        [ExpectedException(typeof(ArgumentException))]
        [TestMethod]
        public void HttpClientExtensionsAttachClientCertificateOnlyDelegatingHandler()
        {
            HttpClient client = new HttpClient(new TestDelegatingHandler());
            client.MessageHandler.AttachClientCertificate(new X509Certificate2());
        }

        [ExpectedException(typeof(ArgumentException))]
        [TestMethod]
        public void HttpClientExtensionsAttachClientCertificateOnlyHttpClientHandler()
        {
            HttpClient client = new HttpClient(new HttpClientHandler());
            client.MessageHandler.AttachClientCertificate(new X509Certificate2());
        }

        private class TestDelegatingHandler : DelegatingHandler
        {
            public TestDelegatingHandler()
            {
            }

            public TestDelegatingHandler(HttpMessageHandler innerHandler)
                : base(innerHandler)
            {
            }
        }
    }
}