// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.UnitTests.MsaIdentityService
{
    using System;
    using System.Threading.Tasks;
    using System.Web.Services.Protocols;
    using System.Xml;

    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    /// <summary>
    ///     LogicalOperationMsaIdentityServiceHandler
    /// </summary>
    [TestClass]
    public class LogicalOperationMsaIdentityServiceHandlerTests : TestBase
    {
        public LogicalOperationMsaIdentityServiceHandler serviceHandler;

        private Mock<OutgoingApiEventWrapper> mockApi;

        [TestMethod]
        public async Task ExecuteAsyncExpectedException()
        {
            //Arrange
            string errorMessage = "errormessage";
            try
            {
                //Act
                Exception actualResult = await this.serviceHandler.ExecuteAsync(() => this.GenerateTaskException(errorMessage));
                Assert.Fail("shouldn't get here");
            }
            catch (Exception)
            {
                //Assert
                Assert.AreEqual(errorMessage, this.mockApi.Object.ErrorMessage);
                Assert.AreEqual(500, this.mockApi.Object.ServiceErrorCode);
                Assert.IsFalse(this.mockApi.Object.Success);
            }
        }

        [TestMethod]
        [DataRow("80045024", "401")]
        [DataRow("80048101", "401")]
        [DataRow("80048105", "401")]
        [DataRow("baderrorcode", null)]
        [DataRow("401", "500")]
        public async Task ExecuteAsyncExpectedSoapException(string errorcode, string expectedErrorCode)
        {
            try
            {
                SoapException actualResult = await this.serviceHandler.ExecuteAsync(() => this.GenerateTaskSoapException(errorcode));
                Assert.Fail("shouldn't get here");
            }
            catch (SoapException)
            {
                Assert.IsFalse(this.mockApi.Object.Success);
                if (!string.IsNullOrWhiteSpace(expectedErrorCode))
                    Assert.AreEqual(expectedErrorCode, this.mockApi.Object.ProtocolStatusCode);
            }
        }

        [TestMethod]
        public async Task ExecuteAsyncSuccess()
        {
            //Arrange
            string dummayValue = "123456";

            //Act
            string actualResult = await this.serviceHandler.ExecuteAsync(() => this.GenerateLogicalOperationData(dummayValue));

            //Assert
            Assert.IsNotNull(actualResult);
            Assert.AreEqual(dummayValue, actualResult);
            Assert.IsTrue(this.mockApi.Object.Success);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task ExecuteAsyncWithNoAction()
        {
            try
            {
                //Act
                await this.serviceHandler.ExecuteAsync(null).ConfigureAwait(false);
                Assert.Fail("This should have failed");
            }
            catch (ArgumentNullException e)
            {
                //Assert
                Assert.AreEqual("Value cannot be null.\r\nParameter name: action", e.Message);
                throw;
            }
        }

        [TestInitialize]
        public void Initialize()
        {
            this.mockApi = new Mock<OutgoingApiEventWrapper>();
            this.serviceHandler = new LogicalOperationMsaIdentityServiceHandler(this.mockApi.Object);
        }

        [TestMethod]
        public void TryParseFail()
        {
            MsaIdentityServiceException idsapiException;
            var actualResult = this.serviceHandler.TryParse(this.GenerateSoapException("baderrorcode"), out idsapiException);

            Assert.IsNotNull(idsapiException);
            Assert.IsFalse(actualResult);
        }

        [TestMethod]
        public void TryParseSuccess()
        {
            MsaIdentityServiceException idsapiException;
            var actualResult = this.serviceHandler.TryParse(this.GenerateSoapException("12345"), out idsapiException);

            Assert.IsNotNull(idsapiException);
            Assert.IsTrue(actualResult);
        }

        [TestMethod]
        public void TryParseWithNoNode()
        {
            MsaIdentityServiceException idsapiException;
            var actualResult = this.serviceHandler.TryParse(new Mock<SoapException>().Object, out idsapiException);

            Assert.IsNotNull(idsapiException);
            Assert.IsTrue(actualResult);
        }

        private async Task<string> GenerateLogicalOperationData(string dummayValue)
        {
            return await Task.FromResult(dummayValue);
        }

        private MsaIdentityServiceException GenerateSoapException(string errorCode)
        {
            var doc = new XmlDocument();
            XmlNode parentNode = doc;
            var namespaceManager = new XmlNamespaceManager(new NameTable());
            namespaceManager.AddNamespace("psf", "http://schemas.microsoft.com/Passport/SoapServices/SOAPFault");
            doc.LoadXml(
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>" +
                "<psf:error xmlns:psf=\"http://schemas.microsoft.com/Passport/SoapServices/SOAPFault\">" +
                "<psf:description xmlns:psf=\"http://schemas.microsoft.com/Passport/SoapServices/SOAPFault\">" +
                "<psf:text xmlns:psf=\"http://schemas.microsoft.com/Passport/SoapServices/SOAPFault\">" +
                "This is a dummay error message" +
                "</psf:text>" +
                "</psf:description>" +
                "<psf:value xmlns:psf=\"http://schemas.microsoft.com/Passport/SoapServices/SOAPFault\">" +
                "" + errorCode + "" +
                "</psf:value>" +
                "<psf:internalerror xmlns:psf=\"http://schemas.microsoft.com/Passport/SoapServices/SOAPFault\">" +
                "<psf:text xmlns:psf=\"http://schemas.microsoft.com/Passport/SoapServices/SOAPFault\">" +
                "This is a dummay internal error message" +
                "</psf:text>" +
                "<psf:code xmlns:psf=\"http://schemas.microsoft.com/Passport/SoapServices/SOAPFault\">" +
                "" + errorCode + "" +
                "</psf:code>" +
                "</psf:internalerror>" +
                "</psf:error>");

            XmlNode root = doc.DocumentElement;

            var msaIdentityServiceException = new MsaIdentityServiceException(
                "bad soap exception",
                SoapException.ServerFaultCode,
                "https://www.msn.com",
                "",
                root,
                It.IsAny<SoapFaultSubCode>(),
                It.IsAny<Exception>());

            return msaIdentityServiceException;
        }

        private async Task<Exception> GenerateTaskException(string errorMessage)
        {
            return await Task.FromException<Exception>(new Exception(errorMessage, null));
        }

        private async Task<MsaIdentityServiceException> GenerateTaskSoapException(string errorcode)
        {
            return await Task.FromException<MsaIdentityServiceException>(this.GenerateSoapException(errorcode));
        }
    }
}
