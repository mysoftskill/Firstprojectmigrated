// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.UnitTests.AzureQueueProcessor
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.AzureQueueProcessor;
    using Microsoft.Membership.MemberServices.Privacy.Core.AQS;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.Membership.MemberServices.Privacy.Core.VerificationTokenValidation;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts;
    using Microsoft.Membership.MemberServices.Test.Common;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class MsaAccountDeleteQueueProcessorTests
    {
        private readonly Mock<IMsaAccountDeleteQueueProcessorConfiguration> config = new Mock<IMsaAccountDeleteQueueProcessorConfiguration>(MockBehavior.Strict);

        private readonly Mock<ICounterFactory> counterFactory = new Mock<ICounterFactory>();

        private readonly Mock<ILogger> logger = new Mock<ILogger>(MockBehavior.Loose);

        private readonly Mock<IMsaIdentityServiceAdapter> msaIdentityServiceAdapter = new Mock<IMsaIdentityServiceAdapter>(MockBehavior.Strict);

        private readonly Mock<IAccountDeleteWriter> pcfWriter = new Mock<IAccountDeleteWriter>(MockBehavior.Strict);

        private readonly Mock<IMsaAccountDeleteQueue> queue = new Mock<IMsaAccountDeleteQueue>(MockBehavior.Strict);

        private readonly Mock<IVerificationTokenValidationService> tokenValidationService = new Mock<IVerificationTokenValidationService>(MockBehavior.Strict);

        private readonly Mock<IXboxAccountsAdapter> xboxAccountsAdapter = new Mock<IXboxAccountsAdapter>(MockBehavior.Strict);

        private IList<IQueueItem<AccountDeleteInformation>> messages = new List<IQueueItem<AccountDeleteInformation>>
            { new InMemoryQueueItem<AccountDeleteInformation>(new InMemoryQueue<AccountDeleteInformation>("name"), new AccountDeleteInformation()) };

        [TestInitialize]
        public void Init()
        {
            this.config.Setup(c => c.RequesterId).Returns("TestRequester");
            this.config.Setup(c => c.IgnoreVerifierErrors).Returns(false);

            this.queue
                .Setup(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.messages);

            var counter = new Mock<ICounter>(MockBehavior.Loose);
            this.counterFactory
                .Setup(c => c.GetCounter(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CounterType>()))
                .Returns(counter.Object);

            var msaResponse = new AdapterResponse<string>();
            this.msaIdentityServiceAdapter
                .Setup(c => c.GetGdprAccountCloseVerifierAsync(It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(msaResponse);

            var tokenValidationResponse = new AdapterResponse();
            this.tokenValidationService
                .Setup(c => c.ValidateVerifierAsync(It.IsAny<PrivacyRequest>(), It.IsAny<string>()))
                .ReturnsAsync(tokenValidationResponse);

            var xuidDictionary = new AdapterResponse<Dictionary<long, string>>();
            this.xboxAccountsAdapter
                .Setup(c => c.GetXuidsAsync(It.IsAny<IEnumerable<long>>()))
                .ReturnsAsync(xuidDictionary);

            var pcfResponse = new AdapterResponse<IList<AccountDeleteInformation>>();
            this.pcfWriter
                .Setup(c => c.WriteDeletesAsync(It.IsAny<IList<AccountDeleteInformation>>(), It.IsAny<string>()))
                .ReturnsAsync(pcfResponse);
        }

        [TestMethod]
        public async Task DoWorkSuccessReadFromQueue()
        {
            // Arrange: happy path where queue gives us a message
            MsaAccountDeleteQueueProcessor worker = this.CreateMsaAccountDeleteQueueProcessor();

            // Act
            Assert.IsTrue(await worker.DoWorkAsync().ConfigureAwait(false));

            // Assert
            this.queue.Verify(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task DoWorkEmptyQueue()
        {
            // Arrange: return null messages
            this.queue
                .Setup(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(null);
            MsaAccountDeleteQueueProcessor worker = this.CreateMsaAccountDeleteQueueProcessor();

            // Act
            Assert.IsFalse(await worker.DoWorkAsync().ConfigureAwait(false));

            // Assert
            this.queue.Verify(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task DoWorkErrorFromQueue()
        {
            // Arrange: throw an exception
            this.queue
                .Setup(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("No one expected this..."));
            MsaAccountDeleteQueueProcessor worker = this.CreateMsaAccountDeleteQueueProcessor();

            // Act
            Assert.IsFalse(await worker.DoWorkAsync().ConfigureAwait(false));

            // Assert
            this.queue.Verify(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task DoWorkCompleteQueueMessages()
        {
            // Arrange: happy path where queue has a message and it's completed
            var mockQueueItem = new Mock<IQueueItem<AccountDeleteInformation>>(MockBehavior.Strict);
            mockQueueItem.Setup(c => c.Data).Returns(new AccountDeleteInformation());
            mockQueueItem.Setup(c => c.CompleteAsync()).Returns(Task.CompletedTask);
            mockQueueItem.SetupGet(c => c.DequeueCount).Returns(1);

            this.messages = new List<IQueueItem<AccountDeleteInformation>> { mockQueueItem.Object };
            this.queue
                .Setup(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.messages);
            MsaAccountDeleteQueueProcessor worker = this.CreateMsaAccountDeleteQueueProcessor();

            // Act
            Assert.IsTrue(await worker.DoWorkAsync().ConfigureAwait(false));

            // Assert
            this.queue.Verify(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
            mockQueueItem.Verify(c => c.CompleteAsync(), Times.Once);
        }

        [TestMethod]
        public async Task DoWorkFailBuildRequestMsaVerifier()
        {
            // Arrange: msa verifier failure
            this.msaIdentityServiceAdapter
                .Setup(c => c.GetGdprAccountCloseVerifierAsync(It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new AdapterResponse<string> { Error = new AdapterError(AdapterErrorCode.Unknown, "oops broken partner", 500) });
            MsaAccountDeleteQueueProcessor worker = this.CreateMsaAccountDeleteQueueProcessor();

            // Act
            Assert.IsFalse(await worker.DoWorkAsync().ConfigureAwait(false));

            // Assert
            this.queue.Verify(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
            this.msaIdentityServiceAdapter
                .Verify(c => c.GetGdprAccountCloseVerifierAsync(It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            this.logger.Verify(c => c.Error(nameof(MsaAccountDeleteQueueProcessor), "Failed to acquire verifier tokens. Errors countered: 1"), Times.Once);
        }

        [TestMethod]
        public async Task DoWorkFailBuildRequestXuidLookup()
        {
            // Arrange: xuid lookup failure
            string xboxErrorMessage = $"Xbox is broken. {Guid.NewGuid().ToString()}";
            this.xboxAccountsAdapter
                .Setup(c => c.GetXuidsAsync(It.IsAny<IEnumerable<long>>()))
                .ReturnsAsync(new AdapterResponse<Dictionary<long, string>>() { Error = new AdapterError(AdapterErrorCode.Unknown, xboxErrorMessage, 500) });
            MsaAccountDeleteQueueProcessor worker = this.CreateMsaAccountDeleteQueueProcessor();

            // Act
            Assert.IsFalse(await worker.DoWorkAsync().ConfigureAwait(false));

            // Assert
            this.queue.Verify(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
            this.xboxAccountsAdapter
                .Verify(c => c.GetXuidsAsync(It.IsAny<IEnumerable<long>>()), Times.Once);
            this.logger.Verify(c => c.Error(nameof(MsaAccountDeleteQueueProcessor), xboxErrorMessage), Times.Once);
        }

        [TestMethod]
        public async Task DoWorkSuccessBuildRequest()
        {
            // Arrange: success from build request and responses from msa and xbox succeeded (for build request)
            MsaAccountDeleteQueueProcessor worker = this.CreateMsaAccountDeleteQueueProcessor();

            // Act
            Assert.IsTrue(await worker.DoWorkAsync().ConfigureAwait(false));

            // Assert
            this.queue.Verify(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
            this.msaIdentityServiceAdapter
                .Verify(c => c.GetGdprAccountCloseVerifierAsync(It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            this.xboxAccountsAdapter
                .Verify(c => c.GetXuidsAsync(It.IsAny<IEnumerable<long>>()), Times.Once);
        }

        [TestMethod]
        public async Task DoWorkFailSendToPcf()
        {
            // Arrange: pcf write failure
            string pcfErrorMessage = $"PCF is broken. {Guid.NewGuid().ToString()}";
            this.pcfWriter
                .Setup(c => c.WriteDeletesAsync(It.IsAny<IList<AccountDeleteInformation>>(), It.IsAny<string>()))
                .ReturnsAsync(new AdapterResponse<IList<AccountDeleteInformation>> { Error = new AdapterError(AdapterErrorCode.Unknown, pcfErrorMessage, 500) });
            MsaAccountDeleteQueueProcessor worker = this.CreateMsaAccountDeleteQueueProcessor();

            // Act
            Assert.IsFalse(await worker.DoWorkAsync().ConfigureAwait(false));

            // Assert
            this.queue.Verify(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
            this.msaIdentityServiceAdapter
                .Verify(c => c.GetGdprAccountCloseVerifierAsync(It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            this.xboxAccountsAdapter
                .Verify(c => c.GetXuidsAsync(It.IsAny<IEnumerable<long>>()), Times.Once);
            this.pcfWriter
                .Verify(c => c.WriteDeletesAsync(It.IsAny<IList<AccountDeleteInformation>>(), It.IsAny<string>()), Times.Once);
            this.logger.Verify(c => c.Error(nameof(MsaAccountDeleteQueueProcessor), pcfErrorMessage), Times.Once);
        }

        [TestMethod]
        public async Task DoWorkSuccessSendToPcf()
        {
            // Arrange: pcf write success
            MsaAccountDeleteQueueProcessor worker = this.CreateMsaAccountDeleteQueueProcessor();

            // Act
            Assert.IsTrue(await worker.DoWorkAsync().ConfigureAwait(false));

            // Assert
            this.queue.Verify(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
            this.msaIdentityServiceAdapter
                .Verify(c => c.GetGdprAccountCloseVerifierAsync(It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            this.xboxAccountsAdapter
                .Verify(c => c.GetXuidsAsync(It.IsAny<IEnumerable<long>>()), Times.Once);
            this.pcfWriter
                .Verify(c => c.WriteDeletesAsync(It.IsAny<IList<AccountDeleteInformation>>(), It.IsAny<string>()), Times.Once);
        }

        private MsaAccountDeleteQueueProcessor CreateMsaAccountDeleteQueueProcessor()
        {
            return new MsaAccountDeleteQueueProcessor(
                this.config.Object,
                this.queue.Object,
                this.xboxAccountsAdapter.Object,
                this.msaIdentityServiceAdapter.Object,
                this.tokenValidationService.Object,
                this.pcfWriter.Object,
                this.counterFactory.Object,
                this.logger.Object);
        }
    }
}
