// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.UnitTests
{
    using System;
    using System.Net;

    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Clients.Implementations;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Clients.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    /// <summary>
    /// PrivacyExperienceClient Test
    /// </summary>
    [TestClass]
    public class PrivacyExperienceClientTest : ClientTestBase
    {
        [ExpectedException(typeof(ArgumentNullException))]
        [TestMethod]
        public void PrivacyExperienceClientThrowExceptionIfEndpointNull()
        {
            try
            {
                PrivacyExperienceClient client = new PrivacyExperienceClient(
                    serviceEndpoint: null,
                    httpClient: this.MockHttpClient.Object,
                    authClient: this.MockAuthClient.Object);
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Value cannot be null.\r\nParameter name: serviceEndpoint", ex.Message);
                throw;
            }

            Assert.Fail("An exception should have been thrown.");
        }

        [ExpectedException(typeof(ArgumentNullException))]
        [TestMethod]
        public void PrivacyExperienceClientThrowExceptionIfHttpClientNull()
        {
            try
            {
                PrivacyExperienceClient client = new PrivacyExperienceClient(
                    serviceEndpoint: this.TestEndpoint,
                    httpClient: null,
                    authClient: this.MockAuthClient.Object);
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Value cannot be null.\r\nParameter name: httpClient", ex.Message);
                throw;
            }

            Assert.Fail("An exception should have been thrown.");
        }

        [ExpectedException(typeof(ArgumentNullException))]
        [TestMethod]
        public void PrivacyExperienceClientThrowExceptionIfNoWebRequestHandlerExists()
        {
            this.MockHttpClient.Setup(c => c.MessageHandler).Returns(value: null);

            try
            {
                PrivacyExperienceClient client = new PrivacyExperienceClient(
                    serviceEndpoint: this.TestEndpoint,
                    httpClient: this.MockHttpClient.Object,
                    authClient: this.MockAuthClient.Object);
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Value cannot be null.\r\nParameter name: messageHandler", ex.Message);
                throw;
            }

            Assert.Fail("An exception should have been thrown.");
        }

        [ExpectedException(typeof(ArgumentNullException))]
        [TestMethod]
        public void PrivacyExperienceClientThrowExceptionIfAuthClientNull()
        {
            try
            {
                PrivacyExperienceClient client = new PrivacyExperienceClient(
                    serviceEndpoint: this.TestEndpoint,
                    httpClient: this.MockHttpClient.Object,
                    authClient: null);
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Value cannot be null.\r\nParameter name: authClient", ex.Message);
                throw;
            }

            Assert.Fail("An exception should have been thrown.");
        }

        [ExpectedException(typeof(ArgumentNullException))]
        [TestMethod]
        public void PrivacyExperienceClientThrowExceptionIfAuthClientMissingCertificate()
        {
            this.MockAuthClient.SetupGet(c => c.ClientCertificate).Returns(value: null);

            try
            {
                PrivacyExperienceClient client = new PrivacyExperienceClient(
                    serviceEndpoint: this.TestEndpoint,
                    httpClient: this.MockHttpClient.Object,
                    authClient: this.MockAuthClient.Object);
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Value cannot be null.\r\nParameter name: clientCertificate", ex.Message);
                throw;
            }

            Assert.Fail("An exception should have been thrown.");
        }

        [TestMethod]
        public void PrivacyExperienceClientShouldSetEndpointInConstructor()
        {
            var client = this.CreateBasicClient();

            // Assert
            this.MockHttpClient.VerifySet(c => c.BaseAddress = this.TestEndpoint, Times.Once);
        }

        [TestMethod]
        public void ClientSetsServicePointPropertiesToDefaultValues()
        {
            // becuase we're testing a process global value, make sure to use a different URL for each test
            Uri testUrl = new Uri("https://defaultsettings.example1.org");

            this.MockHttpClient.SetupGet(o => o.BaseAddress).Returns(testUrl);

            // Create a client and verify that the service point properties are set to default value
            var client = new PrivacyExperienceClient(
                serviceEndpoint: testUrl,
                httpClient: this.MockHttpClient.Object,
                authClient: this.MockAuthClient.Object);

            ServicePoint servicePoint = ServicePointManager.FindServicePoint(testUrl);

            // Check that the values were updated to the default values
            var defaultConfig = new DefaultServicePointManagerConfig();
            Assert.AreEqual(defaultConfig.ConnectionLeaseTimeout, servicePoint.ConnectionLeaseTimeout);
            Assert.AreEqual(defaultConfig.Expect100Continue, servicePoint.Expect100Continue);
            Assert.AreEqual(defaultConfig.MaxIdleTime, servicePoint.MaxIdleTime);
            Assert.AreEqual(defaultConfig.UseNagleAlgorithm, servicePoint.UseNagleAlgorithm);
        }

        [TestMethod]
        public void ClientOverridesServicePointPropertiesToCustomValues()
        {
            // becuase we're testing a process global value, make sure to use a different URL for each test
            Uri testUrl = new Uri("https://customsettings.example2.org");

            Mock<IServicePointManagerConfig> mockServicePointManagerConfig = new Mock<IServicePointManagerConfig>();
            mockServicePointManagerConfig.Setup(c => c.ConnectionLeaseTimeout).Returns(50);
            mockServicePointManagerConfig.Setup(c => c.Expect100Continue).Returns(true);
            mockServicePointManagerConfig.Setup(c => c.MaxIdleTime).Returns(120);
            mockServicePointManagerConfig.Setup(c => c.UseNagleAlgorithm).Returns(true);

            this.MockHttpClient.SetupGet(o => o.BaseAddress).Returns(testUrl);

            // Create a client and verify that the service point properties changes to custom values
            var client = new PrivacyExperienceClient(
                serviceEndpoint: testUrl,
                httpClient: this.MockHttpClient.Object,
                authClient: this.MockAuthClient.Object,
                servicePointManagerConfig: mockServicePointManagerConfig.Object);

            ServicePoint servicePoint = ServicePointManager.FindServicePoint(testUrl);

            Assert.AreEqual(50, servicePoint.ConnectionLeaseTimeout);
            Assert.AreEqual(true, servicePoint.Expect100Continue);
            Assert.AreEqual(120, servicePoint.MaxIdleTime);
            Assert.AreEqual(true, servicePoint.UseNagleAlgorithm);
        }
    }
}