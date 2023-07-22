namespace Microsoft.PrivacyServices.CommandFeed.Client.Test
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Client.Testing;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Predicates;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class DummyTest
    {
        [TestMethod]
        public async Task TestMethod()
        {
            int deleteCommandCount = 0;
            Mock<IPrivacyDataAgent> mock = new Mock<IPrivacyDataAgent>();

            TaskCompletionSource<bool> deleteReceivedSource = new TaskCompletionSource<bool>();

            mock.Setup(m => m.ProcessDeleteAsync(It.IsAny<IDeleteCommand>()))
                .Callback(() =>
                {
                    deleteCommandCount++;
                    deleteReceivedSource.TrySetResult(true);
                })
                .Returns(Task.FromResult(true));

            InMemoryCommandFeedClient mockClient = new InMemoryCommandFeedClient();

            DeleteCommand testCommand = new DeleteCommand(
                "commandId",
                "assetGroupId",
                "assetGroupQualifier",
                "verifier",
                "correlationVector",
                "leaseReceipt",
                DateTime.UtcNow.AddMinutes(5),
                DateTime.UtcNow,
                new MsaSubject
                {
                    Puid = 1,
                    Anid = "2",
                    Cid = 3,
                    Opid = "4",
                    Xuid = "5",
                },
                "agentState",
                null,
                Policies.Current.DataTypes.Ids.ContentConsumption,
                new TimeRangePredicate
                {
                    StartTime = DateTimeOffset.UtcNow,
                    EndTime = DateTimeOffset.UtcNow,
                },
                null,
                Policies.Current.CloudInstances.Ids.Public.Value);

            mockClient.Enqueue(testCommand);

            PrivacyCommandReceiver receiver = new PrivacyCommandReceiver(
                mock.Object,
                mockClient,
                Mock.Of<CommandFeedLogger>());

            CancellationTokenSource source = new CancellationTokenSource();
            Task t = receiver.BeginReceivingAsync(source.Token);

            await deleteReceivedSource.Task;

            Assert.AreEqual(1, deleteCommandCount);
            source.Cancel();

            await t;
            
            Assert.IsTrue(t.IsCompleted || t.IsFaulted);
        }

        [TestMethod]
        public async Task BackoffTest()
        {
            int getCommandsCount = 0;
            Mock<IPrivacyDataAgent> mock = new Mock<IPrivacyDataAgent>();

            TaskCompletionSource<bool> deleteReceivedSource = new TaskCompletionSource<bool>();

            var mockClient = new Mock<ICommandFeedClient>();
            mockClient.Setup(m => m.GetCommandsAsync(It.IsAny<CancellationToken>())).Callback(() =>
            {
                getCommandsCount++;
                if (getCommandsCount > 3)
                {
                    deleteReceivedSource.TrySetResult(true);
                }
            }).Throws(new DivideByZeroException("This is only a test"));

            PrivacyCommandReceiver receiver = new PrivacyCommandReceiver(
                mock.Object,
                mockClient.Object,
                Mock.Of<CommandFeedLogger>());

            CancellationTokenSource source = new CancellationTokenSource();

            DateTime startTime = DateTime.UtcNow;
            Task t = receiver.BeginReceivingAsync(source.Token);

            await deleteReceivedSource.Task;

            Assert.IsTrue(DateTime.UtcNow - startTime > TimeSpan.FromSeconds(3));
            source.Cancel();

            await t;
            Assert.IsTrue(t.IsCompleted || t.IsFaulted);
        }

        [TestMethod]
        public async Task CancellationTest()
        {
            int getCommandsCount = 0;
            bool cancellationRequested = false;

            Mock<IPrivacyDataAgent> mock = new Mock<IPrivacyDataAgent>();

            TaskCompletionSource<bool> deleteReceivedSource = new TaskCompletionSource<bool>();

            var mockClient = new Mock<ICommandFeedClient>();
            mockClient.Setup(m => m.GetCommandsAsync(It.IsAny<CancellationToken>())).Callback(() =>
            {
                getCommandsCount++;
                if (getCommandsCount > 3)
                {
                    deleteReceivedSource.TrySetResult(true);
                }
            }).Throws(new DivideByZeroException("This is only a test"));

            var mockLogger = new Mock<CommandFeedLogger>();
            mockLogger.Setup(m => m.CancellationException(It.IsAny<Exception>())).Callback(() => { cancellationRequested = true; });

            PrivacyCommandReceiver receiver = new PrivacyCommandReceiver(
                mock.Object,
                mockClient.Object,
                mockLogger.Object);

            CancellationTokenSource source = new CancellationTokenSource();

            DateTime startTime = DateTime.UtcNow;
            Task t = receiver.BeginReceivingAsync(source.Token);

            await deleteReceivedSource.Task;

            Assert.IsTrue(DateTime.UtcNow - startTime > TimeSpan.FromSeconds(3));
            source.Cancel();

            await t;
            Assert.IsTrue(t.IsCompleted || t.IsFaulted);
            Assert.IsTrue(cancellationRequested);
        }
    }
}
