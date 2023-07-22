using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.PrivacyServices.CommandFeed.Client.Test
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Predicates;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Validator;
    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;
    using Microsoft.PrivacyServices.Policy;

    using Moq;

    using Newtonsoft.Json;

    [TestClass]
    public class PrivacyCommandReceiverTests
    {
        [TestMethod]
        [Ignore]
        public async Task PrivacyCommandReceiverParallelism()
        {
            async Task WaitForConditionAsync(Func<bool> condition, int maxDelay = 50000)
            {
                Stopwatch sw = Stopwatch.StartNew();
                while (!condition() && sw.ElapsedMilliseconds < maxDelay)
                {
                    await Task.Delay(100);
                }

                if (!condition())
                {
                    Assert.Fail();
                }
            }

            TestDataAgent dataAgent = new TestDataAgent();

            Mock<ICommandFeedClient> mockClient = new Mock<ICommandFeedClient>();

            int returnedCount = 0;

            // Always returns 100 commands.
            mockClient
                .Setup(m => m.GetCommandsAsync(It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    returnedCount += 10;
                    return Task.FromResult(Enumerable.Range(1, 10).Select(x => this.GetCommand()).ToList());
                });

            var cancellationToken = new CancellationTokenSource();

            PrivacyCommandReceiver receiver = new PrivacyCommandReceiver(dataAgent, mockClient.Object, new ConsoleCommandFeedLogger());
            receiver.ConcurrencyLimit = 10;

            Task receiveTask = receiver.BeginReceivingAsync(cancellationToken.Token);

            await WaitForConditionAsync(() => returnedCount >= 20);

            // Expect that "GetCommands" has been called twice, 10 commands are currently processing (per the concurrency limit),
            // and that 10 are in queue waiting to be processed.
            Assert.AreEqual(20, returnedCount);
            Assert.AreEqual(10, dataAgent.PendingCount);
            Assert.AreEqual(0, dataAgent.ProcessedCount);

            // Discharge a single command. This has a couple of effects.
            // 1) One command is drained from the queue and starts processing.
            // 2) A new batch of commands is requested from PCF.
            dataAgent.HandleNextCommand();

            await WaitForConditionAsync(() => returnedCount >= 30);

            // Receiver is buffering 19 commands internally. It won't fetch more until it's buffering less than 10.
            // So, let's complete 9 commands.
            Assert.AreEqual(30, returnedCount);
            Assert.AreEqual(10, dataAgent.PendingCount);
            Assert.AreEqual(1, dataAgent.ProcessedCount);

            for (int i = 0; i < 9; ++i)
            {
                dataAgent.HandleNextCommand();
            }

            await WaitForConditionAsync(() => dataAgent.ProcessedCount >= 10);

            // Reciever is now buffering only 10 commands.
            Assert.AreEqual(30, returnedCount);
            Assert.AreEqual(10, dataAgent.PendingCount);
            Assert.AreEqual(10, dataAgent.ProcessedCount);

            dataAgent.HandleNextCommand();

            await WaitForConditionAsync(() => returnedCount >= 40);

            // Reciever is now buffering 19 commands again.
            Assert.AreEqual(40, returnedCount);
            Assert.AreEqual(10, dataAgent.PendingCount);
            Assert.AreEqual(11, dataAgent.ProcessedCount);

            // Set the cancellation token.
            cancellationToken.Cancel();

            bool stopping = true;
            Task t = Task.Run(() =>
            {
                while (stopping)
                {
                    dataAgent.HandleNextCommand();
                }
            });

            await receiveTask;
            stopping = false;

            // Asser that all commands got handled on shutdown.
            Assert.AreEqual(40, returnedCount);
            Assert.AreEqual(0, dataAgent.PendingCount);
            Assert.AreEqual(40, dataAgent.ProcessedCount);
        }

        private IPrivacyCommand GetCommand()
        {
            return new DeleteCommand(
                Guid.NewGuid().ToString("n"),
                "assetGroup1",
                "assetGroupQ1",
                string.Empty,
                "cv1",
                "lr1",
                DateTime.UtcNow.AddMinutes(5),
                DateTime.UtcNow,
                new MsaSubject
                {
                    Puid = 12345,
                    Anid = "12345",
                    Cid = 12345,
                    Opid = "12345",
                    Xuid = "12345",
                },
                "state",
                new BrowsingHistoryPredicate(),
                Policies.Current.DataTypes.Ids.BrowsingHistory,
                new TimeRangePredicate
                {
                    StartTime = DateTime.UtcNow.AddDays(30),
                    EndTime = DateTimeOffset.UtcNow,
                },
                null,
                Policies.Current.CloudInstances.Ids.Public.Value);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
        private class TestDataAgent : IPrivacyDataAgent
        {
            private int pendingCount = 0;
            private int processedCount = 0;
            private AutoResetEvent acceptCommandEvent = new AutoResetEvent(false);

            public int PendingCount => this.pendingCount;

            public int ProcessedCount => this.processedCount;

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public void HandleNextCommand()
            {
                this.acceptCommandEvent.Set();
            }

            public Task ProcessAccountClosedAsync(IAccountCloseCommand command) => this.Handle();

            public Task ProcessAgeOutAsync(IAgeOutCommand command) => this.Handle();

            public Task ProcessDeleteAsync(IDeleteCommand command) => this.Handle();

            public Task ProcessExportAsync(IExportCommand command) => this.Handle();

            private Task Handle()
            {
                Interlocked.Increment(ref this.pendingCount);

                this.acceptCommandEvent.WaitOne();

                Interlocked.Decrement(ref this.pendingCount);
                Interlocked.Increment(ref this.processedCount);

                return Task.FromResult(true);
            }
        }
    }
}
