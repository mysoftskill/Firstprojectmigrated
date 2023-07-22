// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExportWorker.UnitTests.Tasks.CommandMonitor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Data;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Tasks;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExportWorker.UnitTests.TestUtility;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Newtonsoft.Json;

    [TestClass]
    public class CosmosDataAgentTests
    {
        private const string LeaseReciept = "LEASEREECIEPT";
        private const string CommandId = "commandid";
        private const string AgentId = "AGENTID";
        private const int LeaseMinutes = 30;

        private readonly Mock<ITable<CommandFileState>> mockCmdFileState = new Mock<ITable<CommandFileState>>();
        private readonly Mock<ICommandMonitorConfig> mockCfg = new Mock<ICommandMonitorConfig>();
        private readonly Mock<ITable<CommandState>> mockCmdState = new Mock<ITable<CommandState>>();
        private readonly Mock<ICounterFactory> mockCounterFact = new Mock<ICounterFactory>();
        private readonly Mock<ICommandClient> mockCmdClient = new Mock<ICommandClient>();
        private readonly Mock<IExportCommand> mockExportCmd = new Mock<IExportCommand>();
        private readonly Mock<ICounter> mockCounter = new Mock<ICounter>();
        private readonly Mock<IClock> mockClock = new Mock<IClock>();
        private readonly MockLogger mockLog = new MockLogger();

        private readonly TimeSpan leaseExtension = TimeSpan.FromMinutes(CosmosDataAgentTests.LeaseMinutes);

        private CosmosDataAgent testObj;

        private string leaseReceipt = CosmosDataAgentTests.LeaseReciept;

        [TestInitialize]
        public void Init()
        {
            this.mockCfg.SetupGet(o => o.LeaseExtensionMinuteSet).Returns(new[] { "30*12", "60*6" });

            this.mockCfg.SetupGet(o => o.SyntheticCommandAgents).Returns(null as IList<string>);

            this.testObj = new CosmosDataAgent(
                this.mockCfg.Object,
                this.mockCmdFileState.Object,
                this.mockCmdState.Object,
                this.mockCounterFact.Object,
                this.mockCmdClient.Object,
                this.mockLog.Object, 
                this.mockClock.Object,
                "TaskId");
            
            this.mockExportCmd
                .Setup(
                    o => o.CheckpointAsync(
                        It.IsAny<CommandStatus>(),
                        It.IsAny<int>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<IEnumerable<ExportedFileSizeDetails>>()))
                .Returns(Task.CompletedTask);

            this.mockExportCmd
                .SetupGet(o => o.AssetGroupQualifier)
                .Returns(
                    "AssetType=AzureBlob;" +
                    "AccountName=an;" +
                    "CustomProperties={'OriginalAgentId':'" + CosmosDataAgentTests.AgentId + "'}");

            this.mockCounterFact
                .Setup(o => o.GetCounter(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CounterType>()))
                .Returns(this.mockCounter.Object);

            this.mockExportCmd.SetupGet(o => o.LeaseReceipt).Returns(() => this.leaseReceipt);
            this.mockExportCmd.SetupGet(o => o.AgentState).Returns((string)null);
            this.mockExportCmd.SetupGet(o => o.CommandId).Returns(CosmosDataAgentTests.CommandId);

            this.mockCmdClient.Setup(o => o.IsTestCommandAsync(It.IsAny<string>())).ReturnsAsync(false);
        }

        [TestMethod]
        public async Task ProcessDeleteSignalsUnexpectedCommand()
        {
            Mock<IDeleteCommand> mockCmd = new Mock<IDeleteCommand>();

            mockCmd
                .Setup(
                    o => o.CheckpointAsync(
                        It.IsAny<CommandStatus>(),
                        It.IsAny<int>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<IEnumerable<ExportedFileSizeDetails>>()))
                .Returns(Task.CompletedTask);

            // test 
            await this.testObj.ProcessDeleteAsync(mockCmd.Object);

            // verify
            this.mockCmdClient.Verify(
                o => o.CheckpointAsync(mockCmd.Object, CommandStatus.UnexpectedCommand, null, 0, null, null), Times.Once);
        }

        [TestMethod]
        public async Task ProcessAccountCloseSignalsUnexpectedCommand()
        {
            Mock<IAccountCloseCommand> mockCmd = new Mock<IAccountCloseCommand>();

            mockCmd
                .Setup(
                    o => o.CheckpointAsync(
                        It.IsAny<CommandStatus>(),
                        It.IsAny<int>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<IEnumerable<ExportedFileSizeDetails>>()))
                .Returns(Task.CompletedTask);

            // test 
            await this.testObj.ProcessAccountClosedAsync(mockCmd.Object);

            // verify
            this.mockCmdClient.Verify(
                o => o.CheckpointAsync(mockCmd.Object, CommandStatus.UnexpectedCommand, null, 0, null, null), Times.Once);
        }

        [TestMethod]
        public async Task ProcessExportInsertsRowAndExtendsLeaseWhenCommandCanBeInserted()
        {
            Func<CommandState, bool> verifier = 
                o =>
                {
                    Assert.AreEqual(CosmosDataAgentTests.AgentId, o.AgentId);
                    Assert.AreEqual(this.mockExportCmd.Object.CommandId, o.CommandId);
                    Assert.IsFalse(o.IsComplete);
                    Assert.AreEqual(this.mockExportCmd.Object.LeaseReceipt, o.LeaseReceipt);
                    return true;
                };

            this.mockCmdState.Setup(o => o.InsertAsync(It.IsAny<CommandState>())).ReturnsAsync(true);
            this.mockCmdState.Setup(o => o.ReplaceAsync(It.IsAny<CommandState>())).ReturnsAsync(true);

            // test 
            await this.testObj.ProcessExportAsync(this.mockExportCmd.Object);

            // verify
            this.mockCmdClient.Verify(
                o => o.CheckpointAsync(this.mockExportCmd.Object, CommandStatus.Pending, this.leaseExtension, 0, null, null), 
                Times.Once);

            this.mockCmdState.Verify(o => o.InsertAsync(It.Is<CommandState>(p => verifier(p))));
            this.mockCmdState.Verify(o => o.ReplaceAsync(It.Is<CommandState>(p => verifier(p))));
            this.mockCmdState.Verify(o => o.GetItemAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
        
        [TestMethod]
        public async Task ProcessExportMarksCommandAsTestWhenSyntheticAgentListIsNullAndCommandMarkedTest()
        {
            Func<CommandState, bool> verifier = o => { Assert.IsTrue(o.IgnoreCommand); return true; };

            this.mockCmdState.Setup(o => o.InsertAsync(It.IsAny<CommandState>())).ReturnsAsync(true);
            this.mockCfg.SetupGet(o => o.SyntheticCommandAgents).Returns(null as IList<string>);
            this.mockCmdClient.Setup(o => o.IsTestCommandAsync(It.IsAny<string>())).ReturnsAsync(true);

            // test 
            await this.testObj.ProcessExportAsync(this.mockExportCmd.Object);

            // verify
            this.mockCmdState.Verify(o => o.InsertAsync(It.Is<CommandState>(p => verifier(p))));
            this.mockCmdClient.Verify(o => o.IsTestCommandAsync(CosmosDataAgentTests.CommandId), Times.Once);
        }

        [TestMethod]
        public async Task ProcessExportMarksCommandAsNonTestWhenSyntheticAgentListIsNullAndCommandMarkedNonTest()
        {
            Func<CommandState, bool> verifier = o => { Assert.IsFalse(o.IgnoreCommand); return true; };

            this.mockCmdState.Setup(o => o.InsertAsync(It.IsAny<CommandState>())).ReturnsAsync(true);
            this.mockCfg.SetupGet(o => o.SyntheticCommandAgents).Returns(null as IList<string>);
            this.mockCmdClient.Setup(o => o.IsTestCommandAsync(It.IsAny<string>())).ReturnsAsync(false);

            // test 
            await this.testObj.ProcessExportAsync(this.mockExportCmd.Object);

            // verify
            this.mockCmdState.Verify(o => o.InsertAsync(It.Is<CommandState>(p => verifier(p))));
            this.mockCmdClient.Verify(o => o.IsTestCommandAsync(CosmosDataAgentTests.CommandId), Times.Once);
        }

        [TestMethod]
        public async Task ProcessExportMarksCommandAsNonTestWhenSyntheticAgentListContainsAgentAndCommandMarkedTest()
        {
            Func<CommandState, bool> verifier = o => { Assert.IsFalse(o.IgnoreCommand); return true; };

            this.mockCmdState.Setup(o => o.InsertAsync(It.IsAny<CommandState>())).ReturnsAsync(true);
            this.mockCfg.SetupGet(o => o.SyntheticCommandAgents).Returns(new[] { CosmosDataAgentTests.AgentId });
            this.mockCmdClient.Setup(o => o.IsTestCommandAsync(It.IsAny<string>())).ReturnsAsync(true);

            // test 
            await this.testObj.ProcessExportAsync(this.mockExportCmd.Object);

            // verify
            this.mockCmdState.Verify(o => o.InsertAsync(It.Is<CommandState>(p => verifier(p))));
            this.mockCmdClient.Verify(o => o.IsTestCommandAsync(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        [DataRow(true, null, false, false)]
        [DataRow(true, true, true, true)]
        [DataRow(true, false, false, true)]
        [DataRow(false, null, false, false)]
        [DataRow(false, true, true, true)]
        [DataRow(false, false, false, true)]
        public async Task ProcessExportCallsSetsAgentStateCorrectly(
            bool hasExistingState,
            bool? returnValue,
            bool expectedIsTestValue,
            bool expectedHasCalledIsTestValue)
        {
            Func<string, bool> verifier =
                s =>
                {
                    CosmosDataAgent.CosmosExportAgentState obj =
                        JsonConvert.DeserializeObject<CosmosDataAgent.CosmosExportAgentState>(s);

                    Assert.AreEqual(3, obj.Version);
                    Assert.AreEqual(expectedHasCalledIsTestValue, obj.HasTestCommandBeenRetrieved);
                    Assert.AreEqual(expectedIsTestValue, obj.IsTestCommand);
                    return true;
                };

            string existingState = hasExistingState ?
                JsonConvert.SerializeObject(new CosmosDataAgent.CosmosExportAgentState { Version = 0 }) :
                null;

            this.mockExportCmd.SetupSet(o => o.AgentState = It.IsAny<string>()).Verifiable();
            this.mockCmdClient.Setup(o => o.IsTestCommandAsync(It.IsAny<string>())).ReturnsAsync(returnValue);
            this.mockExportCmd.SetupGet(o => o.AgentState).Returns(existingState);

            // test 
            await this.testObj.ProcessExportAsync(this.mockExportCmd.Object);

            // verify
            this.mockExportCmd.VerifySet(o => o.AgentState = It.Is<string>(p => verifier(p)));
        }


        [TestMethod]
        public async Task ProcessExportCallsIsTestApiWhenAgentStateDoesNotExist()
        {
            this.mockCmdClient.Setup(o => o.IsTestCommandAsync(It.IsAny<string>())).ReturnsAsync(true);
            this.mockExportCmd.SetupGet(o => o.AgentState).Returns((string)null);

            // test 
            await this.testObj.ProcessExportAsync(this.mockExportCmd.Object);

            // verify
            this.mockCmdClient.Verify(o => o.IsTestCommandAsync(It.IsAny<string>()), Times.Once);
        }
        
        [TestMethod]
        public async Task ProcessExportCallsIsTestApiWhenAgentStateExistsAndVersionIs0()
        {
            CosmosDataAgent.CosmosExportAgentState agentState = new CosmosDataAgent.CosmosExportAgentState
            {
                Version = 0,
            };
            
            this.mockCmdClient.Setup(o => o.IsTestCommandAsync(It.IsAny<string>())).ReturnsAsync(true);
            this.mockExportCmd.SetupGet(o => o.AgentState).Returns(JsonConvert.SerializeObject(agentState));

            // test 
            await this.testObj.ProcessExportAsync(this.mockExportCmd.Object);

            // verify
            this.mockCmdClient.Verify(o => o.IsTestCommandAsync(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task ProcessExportDoesNotCallIsTestApiWhenAgentStateExistsAndVersionIs1AndIsTestCommandIsTrue()
        {
            CosmosDataAgent.CosmosExportAgentState agentState = new CosmosDataAgent.CosmosExportAgentState
            {
                Version = 1,
                IsTestCommand = true,
            };

            this.mockCmdClient.Setup(o => o.IsTestCommandAsync(It.IsAny<string>())).ReturnsAsync(true);
            this.mockExportCmd.SetupGet(o => o.AgentState).Returns(JsonConvert.SerializeObject(agentState));

            // test 
            await this.testObj.ProcessExportAsync(this.mockExportCmd.Object);

            // verify
            this.mockCmdClient.Verify(o => o.IsTestCommandAsync(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task ProcessExportCallsIsTestApiWhenAgentStateExistsAndVersionIs1AndIsTestCommandIsFalse()
        {
            CosmosDataAgent.CosmosExportAgentState agentState = new CosmosDataAgent.CosmosExportAgentState
            {
                Version = 1,
                IsTestCommand = false,
            };

            this.mockCmdClient.Setup(o => o.IsTestCommandAsync(It.IsAny<string>())).ReturnsAsync(true);
            this.mockExportCmd.SetupGet(o => o.AgentState).Returns(JsonConvert.SerializeObject(agentState));

            // test 
            await this.testObj.ProcessExportAsync(this.mockExportCmd.Object);

            // verify
            this.mockCmdClient.Verify(o => o.IsTestCommandAsync(It.IsAny<string>()), Times.Once);
        }


        [TestMethod]
        public async Task ProcessExportDoesNotCallIsTestApiWhenAgentStateExistsAndVersionIs2AndHasCalledTestCommandIsTrue()
        {
            CosmosDataAgent.CosmosExportAgentState agentState = new CosmosDataAgent.CosmosExportAgentState
            {
                Version = 2,
                HasTestCommandBeenRetrieved = true,
            };

            this.mockCmdClient.Setup(o => o.IsTestCommandAsync(It.IsAny<string>())).ReturnsAsync(true);
            this.mockExportCmd.SetupGet(o => o.AgentState).Returns(JsonConvert.SerializeObject(agentState));

            // test 
            await this.testObj.ProcessExportAsync(this.mockExportCmd.Object);

            // verify
            this.mockCmdClient.Verify(o => o.IsTestCommandAsync(It.IsAny<string>()), Times.Never);
        }


        [TestMethod]
        public async Task ProcessExportCallsIsTestApiWhenAgentStateExistsAndVersionIs2AndHasCalledTestCommandIsFalse()
        {
            CosmosDataAgent.CosmosExportAgentState agentState = new CosmosDataAgent.CosmosExportAgentState
            {
                Version = 2,
                HasTestCommandBeenRetrieved = false,
            };

            this.mockCmdClient.Setup(o => o.IsTestCommandAsync(It.IsAny<string>())).ReturnsAsync(true);
            this.mockExportCmd.SetupGet(o => o.AgentState).Returns(JsonConvert.SerializeObject(agentState));

            // test 
            await this.testObj.ProcessExportAsync(this.mockExportCmd.Object);

            // verify
            this.mockCmdClient.Verify(o => o.IsTestCommandAsync(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task ProcessExportMarksCommandAsNonTestWhenSyntheticAgentListDoesNotContainsAgentAndCommandMarkedNonTest()
        {
            Func<CommandState, bool> verifier = o => { Assert.IsFalse(o.IgnoreCommand); return true; };

            this.mockCmdState.Setup(o => o.InsertAsync(It.IsAny<CommandState>())).ReturnsAsync(true);
            this.mockCfg.SetupGet(o => o.SyntheticCommandAgents).Returns(new[] { "IAmNOTTheVeryModelOfAModernMajorDataThing" });
            this.mockCmdClient.Setup(o => o.IsTestCommandAsync(It.IsAny<string>())).ReturnsAsync(false);

            // test 
            await this.testObj.ProcessExportAsync(this.mockExportCmd.Object);

            // verify
            this.mockCmdState.Verify(o => o.InsertAsync(It.Is<CommandState>(p => verifier(p))));
            this.mockCmdClient.Verify(o => o.IsTestCommandAsync(CosmosDataAgentTests.CommandId), Times.Once);
        }

        [TestMethod]
        public async Task ProcessExportMarksCommandAsTestWhenSyntheticAgentListDoesNotContainsAgentAndCommandMarkedTest()
        {
            Func<CommandState, bool> verifier = o => { Assert.IsTrue(o.IgnoreCommand); return true; };

            this.mockCmdState.Setup(o => o.InsertAsync(It.IsAny<CommandState>())).ReturnsAsync(true);
            this.mockCfg.SetupGet(o => o.SyntheticCommandAgents).Returns(new[] { "IAmNOTTheVeryModelOfAModernMajorDataThing" });
            this.mockCmdClient.Setup(o => o.IsTestCommandAsync(It.IsAny<string>())).ReturnsAsync(true);

            // test 
            await this.testObj.ProcessExportAsync(this.mockExportCmd.Object);

            // verify
            this.mockCmdState.Verify(o => o.InsertAsync(It.Is<CommandState>(p => verifier(p))));
            this.mockCmdClient.Verify(o => o.IsTestCommandAsync(CosmosDataAgentTests.CommandId), Times.Once);
        }

        [TestMethod]
        public async Task ProcessExportMarksCommandAsCompleteWhenDeterminedToBeATestCommand()
        {
            Func<CommandState, bool> verifier = o => { Assert.IsTrue(o.IsComplete); return true; };

            this.mockCmdState.Setup(o => o.InsertAsync(It.IsAny<CommandState>())).ReturnsAsync(true);
            this.mockCfg.SetupGet(o => o.SyntheticCommandAgents).Returns(null as IList<string>);
            this.mockCmdClient.Setup(o => o.IsTestCommandAsync(It.IsAny<string>())).ReturnsAsync(true);

            // test 
            await this.testObj.ProcessExportAsync(this.mockExportCmd.Object);

            // verify
            this.mockCmdState.Verify(o => o.InsertAsync(It.Is<CommandState>(p => verifier(p))), Times.Once);

            this.mockCmdClient.Verify(
                o => o.CheckpointAsync(this.mockExportCmd.Object, CommandStatus.Complete, null, 0, null, null),
                Times.Once);
        }

        [TestMethod]
        public async Task ProcessExportReportsPendingAndExtendsLeaseWhenCommandExistsButIsNotComplete()
        {
            this.mockExportCmd.SetupGet(o => o.AgentState).Returns("{'DequeueCount':1}");

            this.mockCmdState
                .Setup(o => o.GetItemAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(
                    new CommandState
                    {
                        AgentId = CosmosDataAgentTests.AgentId,
                        CommandId = CosmosDataAgentTests.CommandId,
                        IsComplete = false,
                        LeaseReceipt = CosmosDataAgentTests.LeaseReciept,
                        IgnoreCommand = false,
                    });

            this.mockCmdState.Setup(o => o.ReplaceAsync(It.IsAny<CommandState>())).ReturnsAsync(true);

            // test 
            await this.testObj.ProcessExportAsync(this.mockExportCmd.Object);

            // verify
            this.mockCmdClient.Verify(
                o => o.CheckpointAsync(this.mockExportCmd.Object, CommandStatus.Pending, this.leaseExtension, 0, null, null),
                Times.Once);

            this.mockCmdState.Verify(o => o.InsertAsync(It.IsAny<CommandState>()), Times.Never);
            this.mockCmdState.Verify(
                o => o.GetItemAsync(CosmosDataAgentTests.AgentId, this.mockExportCmd.Object.CommandId), Times.Once);
        }

        [TestMethod]
        public async Task ProcessExportReportsFailedWhenCannotInsertNewOrFetchExistingCommandState()
        {
            this.mockExportCmd.SetupGet(o => o.AgentState).Returns("{'DequeueCount':1}");

            this.mockCmdState.Setup(o => o.GetItemAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((CommandState)null);
            this.mockCmdState.Setup(o => o.InsertAsync(It.IsAny<CommandState>())).ReturnsAsync(false);

            // test 
            await this.testObj.ProcessExportAsync(this.mockExportCmd.Object);

            // verify
            this.mockCmdClient.Verify(
                o => o.CheckpointAsync(this.mockExportCmd.Object, CommandStatus.Failed, this.leaseExtension, 0, null, null),
                Times.Once);

            this.mockCmdState.Verify(o => o.InsertAsync(It.IsAny<CommandState>()), Times.Once);
            this.mockCmdState.Verify(
                o => o.GetItemAsync(CosmosDataAgentTests.AgentId, this.mockExportCmd.Object.CommandId), Times.Exactly(2));
        }

        [TestMethod]
        public async Task ProcessExportReportsCompleteWhenCommandExistsAndIsComplete()
        {
            this.mockExportCmd.SetupGet(o => o.AgentState).Returns("{'DequeueCount':1}");

            this.mockCmdState
                .Setup(o => o.GetItemAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(
                    new CommandState
                    {
                        AgentId = CosmosDataAgentTests.AgentId,
                        CommandId = CosmosDataAgentTests.CommandId,
                        IsComplete = true,
                        LeaseReceipt = CosmosDataAgentTests.LeaseReciept,
                        IgnoreCommand = false,
                    });

            this.mockCmdFileState.Setup(o => o.QueryAsync(It.IsAny<string>())).ReturnsAsync(new List<CommandFileState>());

            // test 
            await this.testObj.ProcessExportAsync(this.mockExportCmd.Object);

            // verify
            this.mockCmdClient.Verify(
                o => o.CheckpointAsync(this.mockExportCmd.Object, CommandStatus.Complete, null, It.IsAny<int>(), null, It.IsAny<IEnumerable<ExportedFileSizeDetails>>()),
                Times.Once);

            this.mockCmdState.Verify(o => o.InsertAsync(It.IsAny<CommandState>()), Times.Never);

            this.mockCmdState.Verify(
                o => o.GetItemAsync(CosmosDataAgentTests.AgentId, this.mockExportCmd.Object.CommandId), 
                Times.Once);
        }

        [TestMethod]
        public async Task EnsureExportedFileSizeDetailsArePassedToCheckPointForCompletedCommands()
        {
            this.mockExportCmd.SetupGet(o => o.AgentState).Returns("{'DequeueCount':1}");
            
            this.mockCmdState
                .Setup(o => o.GetItemAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(
                    new CommandState
                    {
                        AgentId = CosmosDataAgentTests.AgentId,
                        CommandId = CosmosDataAgentTests.CommandId,
                        IsComplete = true,
                        LeaseReceipt = CosmosDataAgentTests.LeaseReciept,
                        IgnoreCommand = false,
                    });

            this.mockCmdFileState.Setup(o => o.QueryAsync(It.IsAny<string>())).ReturnsAsync(new List<CommandFileState>()
            {
                new CommandFileState
                {
                    FilePath = "343434.json",
                    AgentId = CosmosDataAgentTests.AgentId,
                    ByteCount = 2345,
                    CommandId = CosmosDataAgentTests.CommandId,
                    DataFilePathAndCommand = "datafilepathandcommand",
                    ETag = "etag",
                    NonTransientErrorInfo = null,
                    RowCount = 5,
                    PartitionKey = "12",
                    RowKey = "34"
                }
            });

            await this.testObj.ProcessExportAsync(this.mockExportCmd.Object);

            // Ensure that Exported File Size details are passed to checkpoint
            this.mockCmdClient.Verify(o => o.CheckpointAsync(this.mockExportCmd.Object, CommandStatus.Complete, null, 5, null, It.IsAny<IEnumerable<ExportedFileSizeDetails>>()));
        }

        [TestMethod]
        public async Task EnsureExportedFileSizeDetailsAreNotPassedToCheckPointForPendingCommands()
        {
            this.mockExportCmd.SetupGet(o => o.AgentState).Returns("{'DequeueCount':1}");

            this.mockCmdState
                .Setup(o => o.GetItemAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(
                    new CommandState
                    {
                        AgentId = CosmosDataAgentTests.AgentId,
                        CommandId = CosmosDataAgentTests.CommandId,
                        IsComplete = false,
                        LeaseReceipt = CosmosDataAgentTests.LeaseReciept,
                        IgnoreCommand = false,
                    });

            this.mockCmdFileState.Setup(o => o.QueryAsync(It.IsAny<string>())).ReturnsAsync(new List<CommandFileState>()
            {
                new CommandFileState
                {
                    FilePath = "343434.json",
                    AgentId = CosmosDataAgentTests.AgentId,
                    ByteCount = 2345,
                    CommandId = CosmosDataAgentTests.CommandId,
                    DataFilePathAndCommand = "datafilepathandcommand",
                    ETag = "etag",
                    NonTransientErrorInfo = null,
                    RowCount = 5,
                    PartitionKey = "12",
                    RowKey = "34"
                }
            });

            await this.testObj.ProcessExportAsync(this.mockExportCmd.Object);

            // Ensure that Exported File Size details are not passed to checkpoint for pending commands
            this.mockCmdClient.Verify(o => o.CheckpointAsync(this.mockExportCmd.Object, CommandStatus.Pending, TimeSpan.FromMinutes(30), 0, null, null), Times.Once);
        }

        [TestMethod]
        public async Task ProcessExportCorrectlySetsRowCountWhenCommandExistsAndIsComplete()
        {
            const int RowCount2 = 20160415;
            const int RowCount1 = 5;

            this.mockExportCmd.SetupGet(o => o.AgentState).Returns("{'DequeueCount':2}");

            this.mockCmdState
                .Setup(o => o.GetItemAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(
                    new CommandState
                    {
                        AgentId = CosmosDataAgentTests.AgentId,
                        CommandId = CosmosDataAgentTests.CommandId,
                        IsComplete = true,
                        LeaseReceipt = CosmosDataAgentTests.LeaseReciept,
                        IgnoreCommand = false,
                    });

            this.mockCmdFileState
                .Setup(o => o.QueryAsync(It.IsAny<string>()))
                .ReturnsAsync(
                    new[] { new CommandFileState { RowCount = RowCount1 }, new CommandFileState { RowCount = RowCount2 } });

            // test 
            await this.testObj.ProcessExportAsync(this.mockExportCmd.Object);

            // verify
            this.mockCmdFileState
                .Verify(
                    o => o.QueryAsync(
                        $"PartitionKey eq '{CosmosDataAgentTests.AgentId}' and CommandId eq '{CosmosDataAgentTests.CommandId}'"),
                    Times.Once);

            this.mockCmdClient.Verify(
                o => o.CheckpointAsync(this.mockExportCmd.Object, CommandStatus.Complete, null, RowCount1 + RowCount2, null, It.IsAny<IEnumerable<ExportedFileSizeDetails>>()),
                Times.Once);
        }

        [TestMethod]
        public async Task ProcessExportDeletesCommandAndCommandFileStateWhenIsComplete()
        {
            CommandFileState state = new CommandFileState { RowCount = 0 };

            Func<ICollection<CommandFileState>, bool> verifier = 
                c =>
                {
                    Assert.AreEqual(1, c.Count);
                    Assert.AreSame(state, c.First());
                    return true;
                };

            this.mockExportCmd.SetupGet(o => o.AgentState).Returns("{'DequeueCount':2}");

            this.mockCmdState
                .Setup(o => o.GetItemAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(
                    new CommandState
                    {
                        AgentId = CosmosDataAgentTests.AgentId,
                        CommandId = CosmosDataAgentTests.CommandId,
                        IsComplete = true,
                        LeaseReceipt = CosmosDataAgentTests.LeaseReciept,
                        IgnoreCommand = false,
                    });

            this.mockCmdFileState
                .Setup(o => o.QueryAsync(It.IsAny<string>()))
                .ReturnsAsync(new[] { state });

            // test 
            await this.testObj.ProcessExportAsync(this.mockExportCmd.Object);

            // verify
            this.mockCmdFileState.Verify(o => o.DeleteBatchAsync(It.IsAny<ICollection<CommandFileState>>()), Times.Once);
            this.mockCmdFileState.Verify(o => o.DeleteBatchAsync(It.Is<ICollection<CommandFileState>>(p => verifier(p))), Times.Once);
        }

        [TestMethod]
        public async Task ProcessExportPersistsNewLeaseReceiptAfterCheck()
        {
            const string NewLeaseReceiept = CosmosDataAgentTests.LeaseReciept + ".NEWANDIMPROVED";

            this.mockExportCmd.SetupGet(o => o.AgentState).Returns("{'DequeueCount':1}");

            this.mockCmdState
                .Setup(o => o.GetItemAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(
                    new CommandState
                    {
                        AgentId = CosmosDataAgentTests.AgentId,
                        CommandId = CosmosDataAgentTests.CommandId,
                        IsComplete = false,
                        LeaseReceipt = CosmosDataAgentTests.LeaseReciept,
                        IgnoreCommand = false,
                    });

            this.mockCmdState.Setup(o => o.InsertAsync(It.IsAny<CommandState>())).ReturnsAsync(false);

            this.mockCmdClient
                .Setup(
                    o => o.CheckpointAsync(
                        It.IsAny<IPrivacyCommand>(),
                        It.IsAny<CommandStatus>(),
                        It.IsAny<TimeSpan?>(),
                        It.IsAny<int>(),
                        It.IsAny<IEnumerable<string>>(),
                        null))
                .Returns(
                    () =>
                    {
                        this.leaseReceipt = NewLeaseReceiept;
                        return Task.CompletedTask;
                    });

            // test 
            await this.testObj.ProcessExportAsync(this.mockExportCmd.Object);

            // verify
            this.mockCmdState.Verify(
                o => o.ReplaceAsync(It.Is<CommandState>(p => NewLeaseReceiept.EqualsIgnoreCase(p.LeaseReceipt))));
        }

        [TestMethod]
        public async Task ProcessExportDoesNotPersistsNewLeaseReceiptIfCommandComplete()
        {
            const string NewLeaseReceiept = CosmosDataAgentTests.LeaseReciept + ".NEWANDIMPROVED";

            this.mockExportCmd.SetupGet(o => o.AgentState).Returns("{'DequeueCount':1}");

            this.mockCmdState
                .Setup(o => o.GetItemAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(
                    new CommandState
                    {
                        AgentId = CosmosDataAgentTests.AgentId,
                        CommandId = CosmosDataAgentTests.CommandId,
                        IsComplete = true,
                        LeaseReceipt = CosmosDataAgentTests.LeaseReciept,
                        IgnoreCommand = false,
                    });

            this.mockCmdState.Setup(o => o.InsertAsync(It.IsAny<CommandState>())).ReturnsAsync(false);

            Task ValueFunction()
            {
                this.leaseReceipt = NewLeaseReceiept;
                return Task.CompletedTask;
            }

            this.mockCmdClient
                .Setup(
                    o => o.CheckpointAsync(
                        It.IsAny<IPrivacyCommand>(),
                        It.IsAny<CommandStatus>(),
                        It.IsAny<TimeSpan?>(),
                        It.IsAny<int>(),
                        It.IsAny<IEnumerable<string>>(),
                        null))
                .Returns(
                    ValueFunction);

            // test 
            await this.testObj.ProcessExportAsync(this.mockExportCmd.Object);

            // verify
            this.mockCmdState.Verify(o => o.ReplaceAsync(It.IsAny<CommandState>()), Times.Never);
        }
    }
}
