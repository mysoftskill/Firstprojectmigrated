// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.UnitTests.Utility.Incidents
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.DataManagementAdapter;
    using Microsoft.PrivacyServices.DataManagement.Client;
    using Microsoft.PrivacyServices.DataManagement.Client.V2;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Utility;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Utility.Incidents;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Newtonsoft.Json;

    using RequestContext = Microsoft.PrivacyServices.DataManagement.Client.RequestContext;

    [TestClass]
    public class IncidentCreatorTests
    {
        private class IncidentCreatorTestException : Exception {  }

        private readonly Mock<IDataManagementClientFactory> mockPdmsClientFact = new Mock<IDataManagementClientFactory>();
        private readonly Mock<IPrivacyConfigurationManager> mockCfg = new Mock<IPrivacyConfigurationManager>();
        private readonly Mock<IDataManagementClient> mockPdmsClient = new Mock<IDataManagementClient>();
        private readonly Mock<IHttpResult<Incident>> mockHttpResult = new Mock<IHttpResult<Incident>>();
        private readonly Mock<IIncidentClient> mockIncidentClient = new Mock<IIncidentClient>();
        private readonly Mock<ICounterFactory> mockCounterFact = new Mock<ICounterFactory>();
        private readonly Mock<IAadAuthManager> mockAuthMgr = new Mock<IAadAuthManager>();

        private readonly AgentIncident inputIncident = new AgentIncident
        {
            AgentId = "8DB1BDFD-6598-4FF0-A868-C5D8E443C029",
            Keywords = "words of keyness",
        };

        private IncidentCreator testObj;
        private Incident responseIncident;

        [TestInitialize]
        public void Init()
        {
            this.mockPdmsClientFact
                .Setup(
                    o => o.Create(
                        It.IsAny<IAadAuthManager>(),
                        It.IsAny<IPrivacyConfigurationManager>(),
                        It.IsAny<ICounterFactory>()))
                .Returns(this.mockPdmsClient.Object);

            this.mockPdmsClient.SetupGet(o => o.Incidents).Returns(this.mockIncidentClient.Object);

            this.mockIncidentClient
                .Setup(o => o.CreateAsync(It.IsAny<Incident>(), It.IsAny<RequestContext>()))
                .ReturnsAsync(this.mockHttpResult.Object);

            this.mockAuthMgr.Setup(o => o.GetAccessTokenAsync(It.IsAny<string>())).ReturnsAsync("TOKEN");

            this.mockHttpResult.SetupGet(o => o.Response).Returns(() => this.responseIncident);

            this.testObj = new IncidentCreator(
                this.mockPdmsClientFact.Object,
                this.mockCfg.Object,
                this.mockCounterFact.Object,
                this.mockAuthMgr.Object);
        }

        [TestMethod]
        public async Task CreateIncidentGetsClientFromFactoryAndInvokesIt()
        {
            const long Id = 10101;

            this.responseIncident = new Incident { Id = Id };

            // test
            await this.testObj.CreateIncidentAsync(CancellationToken.None, this.inputIncident);

            // verify
            this.mockPdmsClientFact.Verify(
                o => o.Create(this.mockAuthMgr.Object, this.mockCfg.Object, this.mockCounterFact.Object),
                Times.Once);

            this.mockPdmsClient.VerifyGet(o => o.Incidents, Times.Once);

            this.mockIncidentClient.Verify(
                o => o.CreateAsync(
                    It.Is<Incident>(
                        p => p.Keywords.Equals(this.inputIncident.Keywords) && 
                             p.Routing.AgentId.HasValue && 
                             p.Routing.AgentId.Value == Guid.Parse(this.inputIncident.AgentId)),
                    It.IsAny<RequestContext>()));

            this.mockHttpResult.VerifyGet(o => o.Response, Times.Once);
        }

        [TestMethod]
        public async Task CreateIncidentReturnsIdAndStatusWhenIncidentFileCallDoesNotThrowAndReturnsNonNull()
        {
            const long Id = 10101;

            IncidentCreateResult result;

            this.responseIncident = new Incident { Id = Id };

            // test
            result = await this.testObj.CreateIncidentAsync(CancellationToken.None, this.inputIncident);

            // verify
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Id.HasValue);
            Assert.AreEqual(IncidentFileStatus.Created, result.Status);
        }

        [TestMethod]
        public async Task CreateIncidentReturnsNullIdAndFailedStatusWhenIncidentFileCallDoesNotThrowAndReturnsNull()
        {
            IncidentCreateResult result;

            this.responseIncident = null;

            // test
            result = await this.testObj.CreateIncidentAsync(CancellationToken.None, this.inputIncident);

            // verify
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Id.HasValue);
            Assert.AreEqual(IncidentFileStatus.FailedToFile, result.Status);
        }
        
        [TestMethod]
        [ExpectedException(typeof(SEHException))]
        public async Task CreateIncidentThrowsActualExceptionWhenFatalExceptionOccurs()
        {
            this.mockIncidentClient
                .Setup(o => o.CreateAsync(It.IsAny<Incident>(), It.IsAny<RequestContext>()))
                .Returns(Task.FromException<IHttpResult<Incident>>(new SEHException()));

            await this.testObj.CreateIncidentAsync(CancellationToken.None, this.inputIncident);
        }
        
        [TestMethod]
        public async Task CreateIncidentWrapsNonFatalExceptionInActionException()
        {
            this.mockIncidentClient
                .Setup(o => o.CreateAsync(It.IsAny<Incident>(), It.IsAny<RequestContext>()))
                .Returns(Task.FromException<IHttpResult<Incident>>(new IncidentCreatorTestException()));

            try
            {
                await this.testObj.CreateIncidentAsync(CancellationToken.None, this.inputIncident);
                Assert.Fail("Did not throw expected DataActionException");
            }
            catch(DataActionException e)
            {
                Assert.IsFalse(e.IsFatal);
            }
        }

        [TestMethod]
        public async Task CreateExtractsResponseFromJsonExceptionIfPresent()
        {
            const string Response = "RESPONSE";

            JsonException exTest = new JsonException();
            exTest.Data["DataManagmentClient.RawResponse"] = Response;

            this.mockIncidentClient
                .Setup(o => o.CreateAsync(It.IsAny<Incident>(), It.IsAny<RequestContext>()))
                .Returns(Task.FromException<IHttpResult<Incident>>(exTest));

            try
            {
                await this.testObj.CreateIncidentAsync(CancellationToken.None, this.inputIncident);
                Assert.Fail("Did not throw expected DataActionException");
            }
            catch (DataActionException e)
            {
                Assert.IsFalse(e.IsFatal);
                Assert.IsTrue(e.Data.Contains(DataActionConsts.ExceptionDataIncidentRawResponse));
                Assert.AreEqual((string)e.Data[DataActionConsts.ExceptionDataIncidentRawResponse], Response);
            }
        }

        [TestMethod]
        public async Task CreateDoesNotPopulateDataIfNoResponsePresentInJsonException()
        {
            this.mockIncidentClient
                .Setup(o => o.CreateAsync(It.IsAny<Incident>(), It.IsAny<RequestContext>()))
                .Returns(Task.FromException<IHttpResult<Incident>>(new JsonException()));

            try
            {
                await this.testObj.CreateIncidentAsync(CancellationToken.None, this.inputIncident);
                Assert.Fail("Did not throw expected DataActionException");
            }
            catch (DataActionException e)
            {
                Assert.IsFalse(e.IsFatal);
                Assert.IsFalse(e.Data.Contains(DataActionConsts.ExceptionDataIncidentRawResponse));
            }
        }

        [TestMethod]
        public async Task CreateIgnoresResponseInNonJsonException()
        {
            const string Response = "RESPONSE";

            IncidentCreatorTestException exTest = new IncidentCreatorTestException();
            exTest.Data["DataManagmentClient.RawResponse"] = Response;

            this.mockIncidentClient
                .Setup(o => o.CreateAsync(It.IsAny<Incident>(), It.IsAny<RequestContext>()))
                .Returns(Task.FromException<IHttpResult<Incident>>(exTest));

            try
            {
                await this.testObj.CreateIncidentAsync(CancellationToken.None, this.inputIncident);
                Assert.Fail("Did not throw expected DataActionException");
            }
            catch (DataActionException e)
            {
                Assert.IsFalse(e.IsFatal);
                Assert.IsFalse(e.Data.Contains(DataActionConsts.ExceptionDataIncidentRawResponse));
            }
        }

        //// cannot test the exception cases because they only have internal constructors in so not possible to create them in the test
    }
}
