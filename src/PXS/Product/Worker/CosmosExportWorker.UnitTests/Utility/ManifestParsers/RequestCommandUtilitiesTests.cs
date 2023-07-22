// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExportWorker.UnitTests.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Data;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility.ManifestParsers;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.PcfAdapter;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class RequestCommandUtilitiesTests
    {
        private const string TestAgentId = "AGENTID";
        private const string CmdId = "commandid";

        private class RequestCommandUtilitiesTestsException : Exception { }

        private class TestConfig : ICommandMonitorConfig
        {
            public CommandFeedStockEndpointType StockEndpointType => CommandFeedStockEndpointType.Prod;
            public ICertificateConfiguration PcfMsaCertificate => null;
            public IList<string> LeaseExtensionMinuteSet => new List<string>();
            public IList<string> SyntheticCommandAgents => new List<string>();
            public PcfAuthMode AuthMode => PcfAuthMode.Aad;
            public string TaskType => "TASKTYPE";
            public string AgentId => "AGENTID";
            public string Tag => "TAG";
            public long PcfMsaSiteId => 1;
            public bool SuppressIsTestCommandFailures => true;
            public int DelayOnExceptionMinutes => 1;
            public int InstanceCount => 1;
        }

        private readonly Mock<ICommandObjectFactory> mockCmdClientFact = new Mock<ICommandObjectFactory>();
        private readonly Mock<ITable<CommandState>> mockCmdState = new Mock<ITable<CommandState>>();
        private readonly Mock<IPrivacyCommand> mockCmd = new Mock<IPrivacyCommand>();
        private readonly Mock<ICommandClient> mockCmdClient = new Mock<ICommandClient>();
        private readonly Mock<ILeaseRenewer> mockRenewer = new Mock<ILeaseRenewer>();

        private RequestCommandUtilities testObj;

        private readonly List<CommandState> queryResponse = new List<CommandState>();
        private readonly OperationContext ctx = new OperationContext("TASKID", 10101);
        private readonly TestConfig config = new TestConfig();

        private QueryCommandResult pcfResponse;
        private string pcfLeaseReceipt;

        [TestInitialize]
        public void Init()
        {
            this.pcfResponse = new QueryCommandResult
            {
                ResponseCode = ResponseCode.OK,
                Command = this.mockCmd.Object
            };
            
            this.mockCmdClientFact.Setup(o => o.CreateCommandFeedClient()).Returns(this.mockCmdClient.Object);

            this.mockCmdClient
                .Setup(
                    o => o.QueryCommandAsync(
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.pcfResponse);

            this.mockCmdState.Setup(o => o.QueryAsync(It.IsAny<string>())).ReturnsAsync(this.queryResponse);

            this.mockCmdState
                .Setup(o => o.GetItemAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(this.queryResponse?.FirstOrDefault());

            this.mockCmdState.Setup(o => o.InsertAsync(It.IsAny<CommandState>())).ReturnsAsync(true);

            this.mockCmd.SetupGet(o => o.LeaseReceipt).Returns(() => this.pcfLeaseReceipt);
            this.mockCmd.SetupGet(o => o.CommandId).Returns(RequestCommandUtilitiesTests.CmdId);

            var mockConfig = new Mock<IAppConfiguration>();
            mockConfig.Setup(o => o.IsFeatureFlagEnabledAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(false);

            this.testObj = new RequestCommandUtilities(this.config, this.mockCmdClientFact.Object, this.mockCmdState.Object, mockConfig.Object, 1);
        }

        // "1" is specified multiple times below because we can then use it in the Moq verify call as all the data values
        //  are the same
        private static IEnumerable<object[]> PopulatesResultsTest =>
            new List<object[]>
            {
                // normal pending command response
                new object[]
                {
                    false, false, false,
                    false, ResponseCode.OK,
                    CommandStatusCode.Actionable,
                },
                // already complete
                new object[]
                {
                    true, false, false,
                    false, ResponseCode.OK,
                    CommandStatusCode.Completed,
                },
                // already found not applicable
                new object[]
                {
                    false, true, false,
                    false, ResponseCode.OK,
                    CommandStatusCode.NotApplicable,
                },
                // was previously ignored
                new object[]
                {
                    false, false, true,
                    false, ResponseCode.OK,
                    CommandStatusCode.Ignored,
                },
                // missing- pcf returns valid command
                new object[]
                {
                    false, false, false,
                    true, ResponseCode.OK,
                    CommandStatusCode.Actionable,
                },
                // missing- pcf returns not found
                new object[]
                {
                    false, false, false,
                    true, ResponseCode.CommandNotFound,
                    CommandStatusCode.UnknownCommand,
                },
                // missing- pcf returns not found
                new object[]
                {
                    false, false, false,
                    true, ResponseCode.CommandNotApplicable,
                    CommandStatusCode.NotApplicable,
                },
                // missing- pcf returns not applicable
                new object[]
                {
                    false, false, false,
                    true, ResponseCode.CommandNotYetDelivered,
                    CommandStatusCode.NotAvailable,
                },
                // missing- pcf returns that it hasn't finished prepping the command yet
                new object[]
                {
                    false, false, false,
                    true, ResponseCode.CommandNotFoundInQueue,
                    CommandStatusCode.Completed,
                },
                // missing- pcf returns the command was already completed
                new object[]
                {
                    false, false, false,
                    true, ResponseCode.CommandAlreadyCompleted,
                    CommandStatusCode.Completed,
                },
                // missing- pcf returns that the command is unknown
                new object[]
                {
                    false, false, false,
                    true, ResponseCode.UnableToResolveLocation,
                    CommandStatusCode.Missing,
                },
            };

        [TestMethod]
        [DynamicData(nameof(RequestCommandUtilitiesTests.PopulatesResultsTest))]
        public async Task DetermineCommandStatusPopulatesResultsCorrectlyBasedOnInput(
            bool isComplete,
            bool isNotApplicable,
            bool isIgnored,
            bool callPcf,
            ResponseCode pcfResponseCode,
            CommandStatusCode expectedCode)
        {
            RequestCommandsInfo result;
            
            if (callPcf)
            {
                this.pcfResponse.ResponseCode = pcfResponseCode;
                this.pcfLeaseReceipt = "LEASE";
            }

            // if we want to call PCF, the command must be missing from the response
            else
            {
                this.queryResponse.Add(
                    new CommandState
                    {
                        AgentId = RequestCommandUtilitiesTests.TestAgentId,
                        CommandId = RequestCommandUtilitiesTests.CmdId,
                        IgnoreCommand = isIgnored,
                        NotApplicable = isNotApplicable,
                        IsComplete = isComplete
                    });
            }

            // test
            result = await this.testObj.DetermineCommandStatusAsync(
                this.ctx,
                RequestCommandUtilitiesTests.TestAgentId,
                new[] { RequestCommandUtilitiesTests.CmdId },
                this.mockRenewer.Object,
                CancellationToken.None,
                false);

            // verify
            Assert.IsNotNull(result);

            Assert.AreEqual(1, result.CommandCount);
            Assert.AreEqual(expectedCode == CommandStatusCode.NotAvailable, result.HasNotAvailable);
            Assert.AreEqual(expectedCode == CommandStatusCode.Undetermined, result.HasUndetermined);
            Assert.AreEqual(expectedCode == CommandStatusCode.Missing, result.HasMissing);
            Assert.AreEqual(1, result.Commands.Count);
            Assert.AreEqual(expectedCode, result.Commands.Keys.First());
            Assert.AreEqual(1, result.Commands[expectedCode].Count);
            Assert.AreEqual(RequestCommandUtilitiesTests.CmdId, result.Commands[expectedCode].First());
        }

        [TestMethod]
        public async Task DetermineCommandStatusQueriesForCommandsInMultipleBatches()
        {
            const string ExpectedQueryFmt = "PartitionKey eq '{0}' and (RowKey eq '{1}')";
            const string CmdId2 = "commandid2";

            RequestCommandsInfo result;
            CommandState result1;
            CommandState result2;
            string expectedQuery1;
            string expectedQuery2;

            expectedQuery1 = ExpectedQueryFmt.FormatInvariant(
                RequestCommandUtilitiesTests.TestAgentId, RequestCommandUtilitiesTests.CmdId);

            expectedQuery2 = ExpectedQueryFmt.FormatInvariant(
                RequestCommandUtilitiesTests.TestAgentId, CmdId2);

            result1 = new CommandState
            {
                AgentId = RequestCommandUtilitiesTests.TestAgentId,
                CommandId = RequestCommandUtilitiesTests.CmdId,
            };

            result2 = new CommandState
            {
                AgentId = RequestCommandUtilitiesTests.TestAgentId,
                CommandId = CmdId2,
            };

            this.mockCmdState
                .SetupSequence(o => o.QueryAsync(It.IsAny<string>()))
                .Returns(Task.FromResult<ICollection<CommandState>>(new[] { result1 }))
                .Returns(Task.FromResult<ICollection<CommandState>>(new[] { result2 }));

            this.queryResponse.Add(
                new CommandState
                {
                    AgentId = RequestCommandUtilitiesTests.TestAgentId,
                    CommandId = RequestCommandUtilitiesTests.CmdId,
                });

            this.queryResponse.Add(
                new CommandState
                {
                    AgentId = RequestCommandUtilitiesTests.TestAgentId,
                    CommandId = CmdId2,
                });

            // test
            result = await this.testObj.DetermineCommandStatusAsync(
                this.ctx,
                RequestCommandUtilitiesTests.TestAgentId,
                new[] { RequestCommandUtilitiesTests.CmdId, CmdId2 },
                this.mockRenewer.Object,
                CancellationToken.None,
                false);

            // verify
            Assert.IsNotNull(result);

            Assert.AreEqual(2, result.CommandCount);
            Assert.AreEqual(1, result.Commands.Count);
            Assert.AreEqual(CommandStatusCode.Actionable, result.Commands.Keys.First());
            Assert.AreEqual(2, result.Commands[CommandStatusCode.Actionable].Count);
            Assert.IsTrue(result.Commands[CommandStatusCode.Actionable].Contains(RequestCommandUtilitiesTests.CmdId));
            Assert.IsTrue(result.Commands[CommandStatusCode.Actionable].Contains(CmdId2));

            this.mockCmdState.Verify(o => o.QueryAsync(It.IsAny<string>()), Times.Exactly(2));
            this.mockCmdState.Verify(o => o.QueryAsync(expectedQuery1), Times.Once);
            this.mockCmdState.Verify(o => o.QueryAsync(expectedQuery2), Times.Once);
        }

        // "1" is specified multiple times below because we can then use it in the Moq verify call as all the data values
        //  are the same
        private static IEnumerable<object[]> CallsPcfTest =>
            new List<object[]>
            {
                // missing- pcf returns valid command
                new object[]
                {
                    true,
                    false, false, false,
                    "LEASE", ResponseCode.OK,
                    CommandStatusCode.Actionable,
                },
                // missing- pcf returns not found
                new object[]
                {
                    false,
                    false, true, false,
                    string.Empty, ResponseCode.CommandNotFound,
                    CommandStatusCode.UnknownCommand,
                },
                // missing- pcf returns not found
                new object[]
                {
                    false,
                    false, true, false,
                    string.Empty, ResponseCode.CommandNotApplicable,
                    CommandStatusCode.NotApplicable,
                },
                // missing- pcf returns the command was already completed
                new object[]
                {
                    false,
                    true, false, false,
                    string.Empty, ResponseCode.CommandAlreadyCompleted,
                    CommandStatusCode.Completed,
                },
                // missing- pcf returns that it hasn't finished prepping the command yet
                new object[]
                {
                    false,
                    true, false, false,
                    string.Empty, ResponseCode.CommandNotFoundInQueue,
                    CommandStatusCode.Completed,
                },
                // missing- pcf returns not applicable
                new object[]
                {
                    false,
                    false, false, false,
                    null, ResponseCode.CommandNotYetDelivered,
                    CommandStatusCode.NotAvailable,
                },
                // missing- pcf returns that the command is unknown
                new object[]
                {
                    false,
                    false, false, false,
                    null, ResponseCode.UnableToResolveLocation,
                    CommandStatusCode.Missing,
                },
            };
        
        [TestMethod]
        [DynamicData(nameof(RequestCommandUtilitiesTests.CallsPcfTest))]
        public async Task DetermineCommandStatusCallsPcfAndWritesToTableStoreAsAppropriateWhenCommandMissing(
            bool writesToTableStore,
            bool isComplete,
            bool isNotApplicable,
            bool isIgnored,
            string leaseReceipt,
            ResponseCode pcfResponseCode,
            CommandStatusCode expectedCode)
        {
            RequestCommandsInfo result;

            this.pcfResponse.ResponseCode = pcfResponseCode;
            this.pcfLeaseReceipt = leaseReceipt;

            // test
            result = await this.testObj.DetermineCommandStatusAsync(
                this.ctx,
                RequestCommandUtilitiesTests.TestAgentId,
                new[] { RequestCommandUtilitiesTests.CmdId },
                this.mockRenewer.Object,
                CancellationToken.None,
                false);

            // verify
            Assert.IsNotNull(result);

            Assert.AreEqual(1, result.CommandCount);
            Assert.AreEqual(1, result.Commands.Count);
            Assert.AreEqual(expectedCode, result.Commands.Keys.First());
            Assert.AreEqual(1, result.Commands[expectedCode].Count);
            Assert.AreEqual(RequestCommandUtilitiesTests.CmdId, result.Commands[expectedCode].First());

            this.mockCmdClient
                .Verify(
                    o => o.QueryCommandAsync(
                        this.config.AgentId,
                        RequestCommandUtilitiesTests.TestAgentId,
                        RequestCommandUtilitiesTests.CmdId,
                        CancellationToken.None),
                    Times.Once);

            if (writesToTableStore)
            {
                Func<CommandState, bool> verifier =
                    o =>
                    {
                        Assert.AreEqual(RequestCommandUtilitiesTests.TestAgentId, o.AgentId);
                        Assert.AreEqual(RequestCommandUtilitiesTests.CmdId, o.CommandId);
                        Assert.AreEqual(isNotApplicable, o.NotApplicable);
                        Assert.AreEqual(leaseReceipt, o.LeaseReceipt);
                        Assert.AreEqual(isIgnored, o.IgnoreCommand);
                        Assert.AreEqual(isComplete, o.IsComplete);
                        return true;
                    };

                this.mockCmdState.Verify(o => o.InsertAsync(It.IsAny<CommandState>()), Times.Once);
                this.mockCmdState.Verify(o => o.InsertAsync(It.Is<CommandState>(p => verifier(p))), Times.Once);
            }
            else
            {
                this.mockCmdState.Verify(o => o.InsertAsync(It.IsAny<CommandState>()), Times.Never);
            }
        }

        [TestMethod]
        public async Task DetermineCommandStatusFetchesFromStateStoreIfInsertAfterQueryingPcfFails()
        {
            RequestCommandsInfo result;
            CommandState state;

            this.pcfResponse.ResponseCode = ResponseCode.OK;
            this.pcfLeaseReceipt = "LEASE";

            Func<CommandState, bool> verifier =
                o =>
                {
                    Assert.AreEqual(RequestCommandUtilitiesTests.TestAgentId, o.AgentId);
                    Assert.AreEqual(RequestCommandUtilitiesTests.CmdId, o.CommandId);
                    Assert.AreEqual("LEASE", o.LeaseReceipt);
                    Assert.AreEqual(false, o.IgnoreCommand);
                    Assert.AreEqual(false, o.NotApplicable);
                    Assert.AreEqual(false, o.IsComplete);
                    return true;
                };

            state = new CommandState
            {
                AgentId = RequestCommandUtilitiesTests.TestAgentId,
                CommandId = RequestCommandUtilitiesTests.CmdId,
            };

            this.mockCmdState.Setup(o => o.GetItemAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(state);
            this.mockCmdState.Setup(o => o.InsertAsync(It.IsAny<CommandState>())).ReturnsAsync(false);

            // test
            result = await this.testObj.DetermineCommandStatusAsync(
                this.ctx,
                RequestCommandUtilitiesTests.TestAgentId,
                new[] { RequestCommandUtilitiesTests.CmdId },
                this.mockRenewer.Object,
                CancellationToken.None,
                false);

            // verify
            Assert.IsNotNull(result);

            Assert.AreEqual(1, result.CommandCount);
            Assert.AreEqual(1, result.Commands.Count);
            Assert.AreEqual(CommandStatusCode.Actionable, result.Commands.Keys.First());
            Assert.AreEqual(1, result.Commands[CommandStatusCode.Actionable].Count);
            Assert.AreEqual(RequestCommandUtilitiesTests.CmdId, result.Commands[CommandStatusCode.Actionable].First());

            this.mockCmdState.Verify(o => o.InsertAsync(It.IsAny<CommandState>()), Times.Once);
            this.mockCmdState.Verify(o => o.InsertAsync(It.Is<CommandState>(p => verifier(p))), Times.Once);

            this.mockCmdState.Verify(
                o => o.GetItemAsync(RequestCommandUtilitiesTests.TestAgentId, RequestCommandUtilitiesTests.CmdId),
                Times.Once);
        }
        
        [TestMethod]
        [DataRow(ResponseCode.UnableToResolveLocation, CommandStatusCode.Missing)]
        [DataRow(ResponseCode.CommandNotYetDelivered, CommandStatusCode.NotAvailable)]
        public async Task DetermineCommandStatusObeysAbortOnMissingParameterWhenItIsFalse(
            ResponseCode pcfCode,
            CommandStatusCode expectedCode)
        {
            const string CmdId2 = "commandid2";

            RequestCommandsInfo result;

            this.pcfResponse.ResponseCode = pcfCode;
            this.pcfLeaseReceipt = null;

            this.mockCmdClient
                .SetupSequence(
                    o => o.QueryCommandAsync(
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new QueryCommandResult { ResponseCode = pcfCode, Command = this.mockCmd.Object }))
                .Returns(Task.FromResult(new QueryCommandResult { ResponseCode = ResponseCode.OK, Command = this.mockCmd.Object }));

            // test
            result = await this.testObj.DetermineCommandStatusAsync(
                this.ctx,
                RequestCommandUtilitiesTests.TestAgentId,
                new[] { RequestCommandUtilitiesTests.CmdId, CmdId2 },
                this.mockRenewer.Object,
                CancellationToken.None,
                false);

            // verify
            Assert.IsNotNull(result);

            Assert.AreEqual(2, result.CommandCount);
            Assert.AreEqual(2, result.Commands.Count);
            Assert.IsTrue(result.Commands.ContainsKey(CommandStatusCode.Actionable));
            Assert.IsTrue(result.Commands.ContainsKey(expectedCode));
            Assert.AreEqual(1, result.Commands[CommandStatusCode.Actionable].Count);
            Assert.AreEqual(1, result.Commands[expectedCode].Count);
            Assert.AreEqual(CmdId2, result.Commands[CommandStatusCode.Actionable].First());
            Assert.AreEqual(RequestCommandUtilitiesTests.CmdId, result.Commands[expectedCode].First());

            this.mockCmdClient.Verify(
                o => o.QueryCommandAsync(
                    this.config.AgentId,
                    RequestCommandUtilitiesTests.TestAgentId,
                    RequestCommandUtilitiesTests.CmdId,
                    CancellationToken.None),
                Times.Once);

            this.mockCmdClient.Verify(
                o => o.QueryCommandAsync(
                    this.config.AgentId,
                    RequestCommandUtilitiesTests.TestAgentId,
                    CmdId2,
                    CancellationToken.None),
                Times.Once);

            this.mockCmdClient.Verify(
                o => o.QueryCommandAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [TestMethod]
        [DataRow(ResponseCode.UnableToResolveLocation, CommandStatusCode.Missing)]
        [DataRow(ResponseCode.CommandNotYetDelivered, CommandStatusCode.NotAvailable)]
        public async Task DetermineCommandStatusObeysAbortOnMissingParameterWhenItIsTrue(
            ResponseCode pcfCode,
            CommandStatusCode expectedCode)
        {
            const string CmdId2 = "commandid2";

            RequestCommandsInfo result;

            this.pcfResponse.ResponseCode = pcfCode;
            this.pcfLeaseReceipt = null;

            this.mockCmdClient
                .SetupSequence(
                    o => o.QueryCommandAsync(
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new QueryCommandResult { ResponseCode = pcfCode, Command = this.mockCmd.Object }))
                .Returns(Task.FromException<QueryCommandResult>(new RequestCommandUtilitiesTestsException()));

            // test
            result = await this.testObj.DetermineCommandStatusAsync(
                this.ctx,
                RequestCommandUtilitiesTests.TestAgentId,
                new[] { RequestCommandUtilitiesTests.CmdId, CmdId2 },
                this.mockRenewer.Object,
                CancellationToken.None,
                true);

            // verify
            Assert.IsNotNull(result);

            Assert.AreEqual(2, result.CommandCount);
            Assert.AreEqual(2, result.Commands.Count);
            Assert.IsTrue(result.Commands.ContainsKey(CommandStatusCode.Undetermined));
            Assert.IsTrue(result.Commands.ContainsKey(expectedCode));
            Assert.AreEqual(1, result.Commands[CommandStatusCode.Undetermined].Count);
            Assert.AreEqual(1, result.Commands[expectedCode].Count);
            Assert.AreEqual(CmdId2, result.Commands[CommandStatusCode.Undetermined].First());
            Assert.AreEqual(RequestCommandUtilitiesTests.CmdId, result.Commands[expectedCode].First());

            this.mockCmdClient.Verify(
                o => o.QueryCommandAsync(
                    this.config.AgentId,
                    RequestCommandUtilitiesTests.TestAgentId,
                    RequestCommandUtilitiesTests.CmdId,
                    CancellationToken.None),
                Times.Once);

            this.mockCmdClient.Verify(
                o => o.QueryCommandAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
