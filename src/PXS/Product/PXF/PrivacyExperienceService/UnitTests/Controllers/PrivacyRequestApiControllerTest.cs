// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.UnitTests.Controllers
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using System.Web.Http;
    using System.Web.Http.Hosting;
    using System.Web.Http.Results;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Aggregation;
    using Microsoft.Membership.MemberServices.Privacy.Core.PCF;
    using Microsoft.Membership.MemberServices.Privacy.Core.TestMsa;
    using Microsoft.Membership.MemberServices.Privacy.Core.Timeline;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.PrivacySubject;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Controllers;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.PrivacyServices.PXS.Command.CommandStatus;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class PrivacyRequestApiControllerTest : ControllerTestBase
    {
        private readonly Mock<DataTypesClassifier> dataTypeClassifierMock = new Mock<DataTypesClassifier>(MockBehavior.Strict, Policies.Current);

        private readonly Mock<ITimelineService> timelineServiceMock = new Mock<ITimelineService>(MockBehavior.Strict);

        private Mock<IPrivacyConfigurationManager> mockConfig;

        private Mock<IPcfProxyService> mockPcfProxyService;

        private Mock<ITestMsaService> mockTestMsaService;

        private Mock<IRequestClassifier> requestClassifierMock;

        private readonly Mock<IAppConfiguration> mockAppConfiguration = new Mock<IAppConfiguration>(MockBehavior.Loose);

        [TestMethod]
        public void ListRequestById_Success()
        {
            var expectedResult = new CommandStatusResponse
            {
                CommandId = Guid.NewGuid(),
                CommandType = "export"
            };
            var expectedServiceResponse = new ServiceResponse<CommandStatusResponse>
            {
                Result = expectedResult
            };
            this.mockPcfProxyService.Setup(s => s.ListRequestByIdAsync(It.IsAny<IRequestContext>(), It.IsAny<Guid>())).Returns(Task.FromResult(expectedServiceResponse));

            var request = new HttpRequestMessage();
            request.RequestUri = new Uri("https://unittest");
            var sut = new PrivacyRequestApiController(
                this.mockPcfProxyService.Object,
                this.mockTestMsaService.Object,
                this.mockConfig.Object,
                this.timelineServiceMock.Object,
                this.dataTypeClassifierMock.Object,
                this.requestClassifierMock.Object,
                this.mockAppConfiguration.Object)
            {
                Request = request
            };

            var actual = sut.ListRequestByIdAsync(Guid.NewGuid()).Result;
            var conNegResult = actual as OkNegotiatedContentResult<CommandStatusResponse>;
            Assert.IsNotNull(conNegResult);
            Assert.IsNotNull(conNegResult.Content);
            Assert.AreEqual(expectedResult.CommandId, conNegResult.Content.CommandId);
            Assert.AreEqual(expectedResult.CommandType, conNegResult.Content.CommandType);
        }

        [TestInitialize]
        public void TestInit()
        {
            this.mockPcfProxyService = new Mock<IPcfProxyService>();
            this.mockTestMsaService = new Mock<ITestMsaService>();
            this.mockConfig = new Mock<IPrivacyConfigurationManager>();
            this.mockConfig.Setup(c => c.PrivacyExperienceServiceConfiguration.CloudInstance).Returns(CloudInstanceType.PublicProd);
            this.requestClassifierMock = new Mock<IRequestClassifier>(MockBehavior.Strict);
            this.requestClassifierMock.Setup(c => c.IsTestRequest(It.IsAny<string>(), It.IsAny<IIdentity>(), It.IsAny<string>())).Returns(false);
            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(ConfigNames.PXS.PRCMakeEmailMandatory, false)).ReturnsAsync(false);
        }

        [TestMethod]
        public void TestMsaCloseAsync_Returns_Error_When_PostTestMsaCloseAsync_Fails()
        {
            // Arrange
            var expectedError = new Error(ErrorCode.InvalidClientCredentials, "any string");
            this.mockTestMsaService.Setup(_ => _.PostTestMsaCloseAsync(It.IsAny<IRequestContext>()))
                .Returns(
                    Task.FromResult(
                        new ServiceResponse<Guid>
                        {
                            Error = expectedError
                        }));

            var request = new HttpRequestMessage { RequestUri = new Uri("https://example.com") };
            request.Properties.Add(HttpPropertyKeys.HttpConfigurationKey, new HttpConfiguration());

            var sut = new PrivacyRequestApiController(
                this.mockPcfProxyService.Object,
                this.mockTestMsaService.Object,
                this.mockConfig.Object,
                this.timelineServiceMock.Object,
                this.dataTypeClassifierMock.Object,
                this.requestClassifierMock.Object,
                this.mockAppConfiguration.Object)
            {
                Request = request
            };

            // Act
            var actual = sut.TestMsaCloseAsync().Result as ResponseMessageResult;

            // Assert
            Assert.AreEqual(HttpStatusCode.Forbidden, actual.Response.StatusCode, "Invalid status code.");
            string responseContentValue = actual.Response.Content.ReadAsStringAsync().Result;
            Assert.AreEqual($"{{\"code\":\"{expectedError.Code}\",\"message\":\"{expectedError.Message}\"}}", responseContentValue, "Invalid content.");
        }

        [TestMethod]
        public void TestMsaCloseAsync_Returns_RequestId_When_PostTestMsaCloseAsync_Succeeds()
        {
            // Arrange
            var expectedRequestId = Guid.NewGuid();
            this.mockTestMsaService.Setup(_ => _.PostTestMsaCloseAsync(It.IsAny<IRequestContext>()))
                .Returns(
                    Task.FromResult(
                        new ServiceResponse<Guid>
                        {
                            Result = expectedRequestId
                        }));

            var request = new HttpRequestMessage { RequestUri = new Uri("https://example.com") };
            var sut = new PrivacyRequestApiController(
                this.mockPcfProxyService.Object,
                this.mockTestMsaService.Object,
                this.mockConfig.Object,
                this.timelineServiceMock.Object,
                this.dataTypeClassifierMock.Object,
                this.requestClassifierMock.Object,
                this.mockAppConfiguration.Object)
            {
                Request = request
            };

            // Act
            var actual = sut.TestMsaCloseAsync().Result as OkNegotiatedContentResult<OperationResponse>;

            // Assert
            Assert.AreEqual(expectedRequestId, actual.Content.Ids[0], "Invalid content.");
        }
    }
}
