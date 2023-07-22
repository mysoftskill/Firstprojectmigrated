// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExportWorker.UnitTests.Tasks.FileProcessor.DataWriters
{
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem.Cosmos;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Data;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Tasks;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExportWorker.UnitTests.TestUtility;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class CommandDataWriterFactoryTests
    {
        private readonly Mock<ICosmosExportPipelineFactory> mockPipelineFactory = new Mock<ICosmosExportPipelineFactory>();
        private readonly Mock<ICommandObjectFactory> mockCmdFactory = new Mock<ICommandObjectFactory>();
        private readonly Mock<ITable<CommandState>> mockCmdState = new Mock<ITable<CommandState>>();
        private readonly Mock<IFileSystemManager> mockFileSystemMgr = new Mock<IFileSystemManager>();
        private readonly Mock<ICosmosFileSystem> mockFileSystem = new Mock<ICosmosFileSystem>();
        private readonly Mock<IPrivacyCommand> mockPrivCommand = new Mock<IPrivacyCommand>();
        private readonly Mock<IExportPipeline> mockPipeline = new Mock<IExportPipeline>();
        private readonly Mock<IExportCommand> mockExportCommand = new Mock<IExportCommand>();
        private readonly Mock<ICommandClient> mockClient = new Mock<ICommandClient>();
        private readonly MockLogger mockLog = new MockLogger();

        private class ThresholdConfig : ICosmosFileSizeThresholds
        {
            public long Oversized { get; set; } = 5000;
            public long Medium { get; set; } = 1000;
            public long Large { get; set; } = 2000;
        }

        public class TestPaths : ICosmosRelativePathsAndExpiryTimes
        {
            public string BasePath { get; } = "BASE/";
            public string AgentOutput { get; } = "OUTPUT/";
            public string PostProcessHolding { get; } = "HOLDING/";
            public string ActivityLog { get; } = "ACTIVITY/";
            public string DeadLetter { get; } = "DEADLETTER/";
            public int ActivityLogExpiryHours { get; } = 1;
            public int DeadLetterExpiryHours { get; } = 1;
            public int HoldingExpiryHours { get; } = 1;
            public string StatsLog { get; } = "STATS/";
            public int StatsLogExpiryHours { get; } = 1;
            public int ManifestHoldingExpiryHours { get; } = 1;
        }

        private readonly HttpRequestException testException = new HttpRequestException();
        private readonly CommandState testState = new CommandState();

        private CommandDataWriterFactory testObj;
        
        [TestInitialize]
        public void Init()
        {
            this.mockFileSystemMgr.Setup(o => o.GetFileSystem(It.IsAny<string>())).Returns(this.mockFileSystem.Object);
            this.mockFileSystemMgr.SetupGet(o => o.CosmosPathsAndExpiryTimes).Returns(new TestPaths());
            this.mockFileSystemMgr.SetupGet(o => o.FileSizeThresholds).Returns(new ThresholdConfig());
            this.mockFileSystemMgr.SetupGet(o => o.DeadLetter).Returns(this.mockFileSystem.Object);

            this.mockFileSystem.SetupGet(o => o.RootDirectory).Returns("ROOT");

            this.testObj = new CommandDataWriterFactory(
                this.mockPipelineFactory.Object,
                this.mockCmdFactory.Object,
                this.mockFileSystemMgr.Object,
                this.mockCmdState.Object,
                this.mockLog.Object);

            this.mockCmdFactory.Setup(o => o.CreateCommandFeedClient()).Returns(this.mockClient.Object);
            this.mockCmdState.Setup(o => o.GetItemAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(this.testState);

            this.mockClient
                .Setup(o => o.QueryCommandAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockExportCommand.Object);

            this.mockPipelineFactory
                .Setup(o => o.Create(It.IsAny<string>(), It.IsAny<IExportCommand>()))
                .Returns(this.mockPipeline.Object);
        }

        [TestMethod]
        public async Task CreateTriesToFetchStatusAndReturnsDeadLetterWriterIfFailed()
        {
            const string CommandId = "CMDID";
            const string FileName = "FILENAME";
            const string AgentId = "AGENTID";

            ICommandDataWriter result;

            this.mockCmdState.Setup(o => o.GetItemAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((CommandState)null);

            // test
            result = await this.testObj.CreateAsync(CancellationToken.None, AgentId, CommandId, FileName);

            // verify
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(DeadLetterDataWriter));
            this.mockCmdState.Verify(o => o.GetItemAsync(AgentId, CommandId), Times.Once);
        }

        [TestMethod]
        public async Task CreateFetchesStatusdAndReturnsNoOpWriterIfCommandShouldBeIgnored()
        {
            const string CommandId = "CMDID";
            const string FileName = "FILENAME";
            const string AgentId = "AGENTID";

            ICommandDataWriter result;

            this.testState.IgnoreCommand = true;

            // test
            result = await this.testObj.CreateAsync(CancellationToken.None, AgentId, CommandId, FileName);

            // verify
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(NoOpCommandDataWriter));
            this.mockCmdState.Verify(o => o.GetItemAsync(AgentId, CommandId), Times.Once);
        }

        [TestMethod]
        public async Task CreateFetchesStatusdAndReturnsNoOpWriterIfCommandIsNotApplicable()
        {
            const string CommandId = "CMDID";
            const string FileName = "FILENAME";
            const string AgentId = "AGENTID";

            ICommandDataWriter result;

            this.testState.IgnoreCommand = true;

            // test
            result = await this.testObj.CreateAsync(CancellationToken.None, AgentId, CommandId, FileName);

            // verify
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(NoOpCommandDataWriter));
            this.mockCmdState.Verify(o => o.GetItemAsync(AgentId, CommandId), Times.Once);
        }

        [TestMethod]
        public async Task CreateFetchesStatusdAndReturnsNoOpWriterIfCommandIsComplete()
        {
            const string CommandId = "CMDID";
            const string FileName = "FILENAME";
            const string AgentId = "AGENTID";

            ICommandDataWriter result;

            this.testState.IgnoreCommand = true;

            // test
            result = await this.testObj.CreateAsync(CancellationToken.None, AgentId, CommandId, FileName);

            // verify
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(NoOpCommandDataWriter));
            this.mockCmdState.Verify(o => o.GetItemAsync(AgentId, CommandId), Times.Once);
        }

        [TestMethod]
        public async Task CreateTriesToFetchCommandAndReturnsNoOpWriterIfQueryCommandReturnsNull()
        {
            const string LeaseReceipt = "LEASE";
            const string CommandId = "CMDID";
            const string FileName = "FILENAME";
            const string AgentId = "AGENTID";

            ICommandDataWriter result;

            this.testState.LeaseReceipt = LeaseReceipt;

            this.mockClient
                .Setup(o => o.QueryCommandAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IPrivacyCommand)null);

            // test
            result = await this.testObj.CreateAsync(CancellationToken.None, AgentId, CommandId, FileName);

            // verify
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(NoOpCommandDataWriter));
            Assert.IsTrue(result.Statuses.HasFlag(WriterStatuses.AbandonedNoCommand));

            this.mockCmdFactory.Verify(o => o.CreateCommandFeedClient(), Times.Once);
            this.mockClient.Verify(o => o.QueryCommandAsync(CommandId, LeaseReceipt, CancellationToken.None), Times.Once);
            this.mockCmdState.Verify(o => o.GetItemAsync(AgentId, CommandId), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(HttpRequestException))]
        public async Task CreateTriesToFetchCommandAndRetriesAfterExceptionBeforeGivingUpWhenNonNotFoundStatusCodeProvided()
        {
            const string LeaseReceipt = "LEASE";
            const string CommandId = "CMDID";
            const string FileName = "FILENAME";
            const string AgentId = "AGENTID";

            this.testException.Data.Add("StatusCode", HttpStatusCode.InternalServerError);

            this.testState.LeaseReceipt = LeaseReceipt;

            this.mockClient
                .Setup(o => o.QueryCommandAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<IPrivacyCommand>(this.testException));

            try
            {
                // test
                await this.testObj.CreateAsync(CancellationToken.None, AgentId, CommandId, FileName);
            }
            catch (HttpRequestException)
            {
                // verify
                this.mockCmdFactory.Verify(o => o.CreateCommandFeedClient(), Times.Exactly(1));
                this.mockClient.Verify(o => o.QueryCommandAsync(CommandId, LeaseReceipt, CancellationToken.None), Times.Exactly(1));
                this.mockCmdState.Verify(o => o.GetItemAsync(AgentId, CommandId), Times.Once);

                throw;
            }
        }
        
        [TestMethod]
        [ExpectedException(typeof(HttpRequestException))]
        public async Task CreateTriesToFetchCommandAndRetriesAfterExceptionBeforeGivingUpWhenNoStatusCodeProvided()
        {
            const string LeaseReceipt = "LEASE";
            const string CommandId = "CMDID";
            const string FileName = "FILENAME";
            const string AgentId = "AGENTID";

            this.testState.LeaseReceipt = LeaseReceipt;

            this.mockClient
                .Setup(o => o.QueryCommandAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<IPrivacyCommand>(this.testException));

            try
            {
                // test
                await this.testObj.CreateAsync(CancellationToken.None, AgentId, CommandId, FileName);
            }
            catch (HttpRequestException)
            {
                // verify
                this.mockCmdFactory.Verify(o => o.CreateCommandFeedClient(), Times.Exactly(1));
                this.mockClient.Verify(o => o.QueryCommandAsync(CommandId, LeaseReceipt, CancellationToken.None), Times.Exactly(1));
                this.mockCmdState.Verify(o => o.GetItemAsync(AgentId, CommandId), Times.Once);

                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(UnexpectedCommandException))]
        public async Task CreateThrowsUnexpectedCommandWhenIfFetchesNonExportCommand()
        {
            const string LeaseReceipt = "LEASE";
            const string CommandId = "CMDID";
            const string FileName = "FILENAME";
            const string AgentId = "AGENTID";

            this.testState.LeaseReceipt = LeaseReceipt;

            this.mockClient
                .Setup(o => o.QueryCommandAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockPrivCommand.Object);

            try
            {
                // test
                await this.testObj.CreateAsync(CancellationToken.None, AgentId, CommandId, FileName);
            }
            catch (UnexpectedCommandException)
            {
                // verify
                this.mockCmdFactory.Verify(o => o.CreateCommandFeedClient(), Times.Once);
                this.mockClient.Verify(o => o.QueryCommandAsync(CommandId, LeaseReceipt, CancellationToken.None), Times.Once);
                this.mockCmdState.Verify(o => o.GetItemAsync(AgentId, CommandId), Times.Once);

                throw;
            }
        }

        [TestMethod]
        public async Task CreateReturnsCommandDataWriterIfCommandSuccessfullyFetched()
        {
            const string LeaseReceipt = "LEASE";
            const string CommandId = "CMDID";
            const string FileName = "FILENAME";
            const string AgentId = "AGENTID";

            ICommandDataWriter result;

            this.testState.LeaseReceipt = LeaseReceipt;

            // test
            result = await this.testObj.CreateAsync(CancellationToken.None, AgentId, CommandId, FileName);

            // verify
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(CommandDataWriter));
            this.mockCmdFactory.Verify(o => o.CreateCommandFeedClient(), Times.Once);
            this.mockClient.Verify(o => o.QueryCommandAsync(CommandId, LeaseReceipt, CancellationToken.None), Times.Once);
            this.mockCmdState.Verify(o => o.GetItemAsync(AgentId, CommandId), Times.Once);
            this.mockPipelineFactory.Verify(o => o.Create(CommandId, this.mockExportCommand.Object), Times.Once);
        }
    }
}
