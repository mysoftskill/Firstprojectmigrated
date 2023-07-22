// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExportWorker.UnitTests.Utility.CommandFeed
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExportWorker.UnitTests.TestUtility;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.PcfAdapter;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.PXS.Command.CommandStatus;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class CommandFeedClientWrapperTests
    {
        private class CommandFeedClientWrapperTestsException : Exception { }

        private readonly Mock<ICommandMonitorConfig> mockConfig = new Mock<ICommandMonitorConfig>();
        private readonly Mock<ICommandFeedClient> mockCmdClient = new Mock<ICommandFeedClient>();
        private readonly Mock<IPcfAdapter> mockPcfClient = new Mock<IPcfAdapter>();
        private readonly MockLogger mockLog = new MockLogger();

        private CommandFeedClientWrapper testObj;

        private void SetupTestObject()
        {
            this.testObj = new CommandFeedClientWrapper(
                this.mockConfig.Object,
                this.mockCmdClient.Object,
                this.mockPcfClient.Object,
                this.mockLog.Object);
        }

        [TestInitialize]
        public void Init()
        {
            this.SetupTestObject();
        }

        [TestMethod]
        public async Task QueryCommandCallsCmdClientQueryCommandAndReturnsResult()
        {
            const string CommandId = "TobyDog";
            const string Receipt = "LEASERECEIPT";

            Mock<IPrivacyCommand> mockCmd = new Mock<IPrivacyCommand>();

            IPrivacyCommand result;

            this.mockCmdClient
                .Setup(o => o.QueryCommandAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCmd.Object);

            // test
            result = await this.testObj.QueryCommandAsync(CommandId, Receipt, CancellationToken.None);

            // verify
            Assert.AreSame(mockCmd.Object, result);
            this.mockCmdClient.Verify(o => o.QueryCommandAsync(Receipt, CancellationToken.None), Times.Once);
        }

        [TestMethod]
        [DataRow(HttpStatusCode.BadRequest)]
        [DataRow("404")]
        [DataRow(404)]
        [DataRow(404L)]
        public async Task QueryCommandReturnsNullWhenCmeClientQueryReturnsExceptionWithExpectedStatusCode(object code)
        {
            const string CommandId = "TobyDog";
            const string Receipt = "LEASERECEIPT";

            HttpRequestException ex = new HttpRequestException();

            IPrivacyCommand result;

            ex.Data.Add((object)"StatusCode", (object)code);

            this.mockCmdClient
                .Setup(o => o.QueryCommandAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<IPrivacyCommand>(ex));

            // test
            result = await this.testObj.QueryCommandAsync(CommandId, Receipt, CancellationToken.None);

            // verify
            Assert.IsNull(result);
            this.mockCmdClient.Verify(o => o.QueryCommandAsync(Receipt, CancellationToken.None), Times.Once);
        }

        [TestMethod]
        public async Task IsTestCommandReturnsFalseIfCannotFetchRequestById()
        {
            Guid id = Guid.NewGuid();

            bool? result;

            AdapterResponse<CommandStatusResponse> response = new AdapterResponse<CommandStatusResponse>
            { 
                Error = new AdapterError(AdapterErrorCode.BadRequest, string.Empty, 400)
            };

            this.mockPcfClient.Setup(o => o.GetRequestByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>())).ReturnsAsync(response);

            // test 
            result = await this.testObj.IsTestCommandAsync(id.ToString());

            // verify
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Value);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task IsTestCommandReturnsResponseIfFetchingRequestReturnsSuccessfully(bool value)
        {
            Guid id = Guid.NewGuid();

            bool? result;

            AdapterResponse<CommandStatusResponse> response = new AdapterResponse<CommandStatusResponse>
            { 
                Result = new CommandStatusResponse { IsSyntheticCommand = value }
            };

            this.mockPcfClient.Setup(o => o.GetRequestByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>())).ReturnsAsync(response);

            // test 
            result = await this.testObj.IsTestCommandAsync(id.ToString());

            // verify
            Assert.IsNotNull(result);
            Assert.AreEqual(value, result.Value);
        }

        [TestMethod]
        [DataRow(typeof(InvalidOperationException))]
        [DataRow(typeof(HttpRequestException))]
        [DataRow(typeof(OperationCanceledException))]
        public async Task IsTestCommandReturnsNullIfFetchingRequestThrowsSupportedExceptionAndSuppressIsEnabled(Type exceptionType)
        {
            Guid id = Guid.NewGuid();

            bool? result;

            this.mockPcfClient
                .Setup(o => o.GetRequestByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>()))
                .Returns(
                    Task.FromException<AdapterResponse<CommandStatusResponse>>(
                        (Exception)Activator.CreateInstance(exceptionType)));

            this.mockConfig.SetupGet(o => o.SuppressIsTestCommandFailures).Returns(true);

            this.SetupTestObject();

            // test 
            result = await this.testObj.IsTestCommandAsync(id.ToString());

            // verify
            Assert.IsNull(result);
        }

        [TestMethod]
        [DataRow(typeof(InvalidOperationException))]
        [DataRow(typeof(HttpRequestException))]
        [DataRow(typeof(OperationCanceledException))]
        public async Task IsTestCommandThrowsIfFetchingRequestThrowsSupportedExceptionAndSuppressIsDisabled(Type exceptionType)
        {
            Guid id = Guid.NewGuid();

            bool? result;

            this.mockPcfClient
                .Setup(o => o.GetRequestByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>()))
                .Returns(
                    Task.FromException<AdapterResponse<CommandStatusResponse>>(
                        (Exception)Activator.CreateInstance(exceptionType)));

            this.mockConfig.SetupGet(o => o.SuppressIsTestCommandFailures).Returns(true);

            this.SetupTestObject();

            // test 
            result = await this.testObj.IsTestCommandAsync(id.ToString());

            // verify
            Assert.IsNull(result);
        }

        [TestMethod]
        [ExpectedException(typeof(CommandFeedClientWrapperTestsException))]
        public async Task IsTestCommandThrowsIfFetchingRequestThrowsNonSupportedException()
        {
            Guid id = Guid.NewGuid();

            bool? result;

            this.mockPcfClient
                .Setup(o => o.GetRequestByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>()))
                .Returns(
                    Task.FromException<AdapterResponse<CommandStatusResponse>>(
                        new CommandFeedClientWrapperTestsException()));

            this.mockConfig.SetupGet(o => o.SuppressIsTestCommandFailures).Returns(true);

            this.SetupTestObject();

            // test 
            result = await this.testObj.IsTestCommandAsync(id.ToString());

            // verify
            Assert.IsNull(result);
        }
    }
}
