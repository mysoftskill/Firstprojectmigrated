namespace Microsoft.PrivacyServices.DataManagement.Worker.ChangeFeedReader.UnitTest
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Storage.Queue;
    using Microsoft.PrivacyServices.DataManagement.Common;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.AzureQueue;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb.UnitTest;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Scheduler;
    using Microsoft.PrivacyServices.DataManagement.Worker.ChangeFeedReader;
    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;

    using ChangeFeedReaderModels = Microsoft.PrivacyServices.DataManagement.DataAccess.ChangeFeedReader.Models;
    using Models = Microsoft.PrivacyServices.DataManagement.Models;

    public class ChangeFeedReaderWorkerTest
    {
        [Theory(DisplayName = "When data owner is in the feed, then copy it to the queue."), ValidData]
        public async Task When_DataOwnerInFeed_Then_SaveInQueue(
            int logicalSequenceNumber,
            [Frozen] Mock<IChangeFeedReader> changeFeedReader,
            [Frozen] Mock<ICloudQueue> cloudQueue,
            ChangeFeedReaderWorker worker)
        {
            // Create Data owner object.
            var lockState = new Lock<ChangeFeedReaderLockState>();
            Models.V2.ServiceTree serviceTree = new Models.V2.ServiceTree();
            serviceTree.ServiceId = "ServiceTreeId";
            Models.V2.DataOwner dataOwner = new Models.V2.DataOwner();
            dataOwner.ServiceTree = serviceTree;

            // Mock change feed.
            var document = DocumentModule.Create(dataOwner);
            document.SetPropertyValue("_lsn", logicalSequenceNumber);
            changeFeedReader.Setup(m => m.ReadItemsAsync(null)).ReturnsAsync(new[] { document });

            // Make worker call.
            await worker.DoLockWorkAsync(lockState, CancellationToken.None).ConfigureAwait(false);

            // Verify that message with correct properties got added.
            Action<CloudQueueMessage> verify = m =>
            {
                var queuedDataOwner = JsonConvert.DeserializeObject<ChangeFeedReaderModels.DataOwner>(m.AsString);
                Assert.Equal(dataOwner.Description, queuedDataOwner.Description);
                Assert.Equal(dataOwner.Id, queuedDataOwner.Id);
                Assert.Equal(dataOwner.IsDeleted, queuedDataOwner.IsDeleted);
                Assert.Equal(dataOwner.Name, queuedDataOwner.Name);
                Assert.Equal(dataOwner.ServiceTree.ServiceId, queuedDataOwner.ServiceTree.ServiceId);
                Assert.Equal(logicalSequenceNumber, queuedDataOwner.LogicalSequenceNumber);
                Assert.Equal(dataOwner.WriteSecurityGroups, queuedDataOwner.WriteSecurityGroups);
                Assert.Equal(dataOwner.TagSecurityGroups, queuedDataOwner.TagSecurityGroups);
                Assert.Equal(dataOwner.TagApplicationIds, queuedDataOwner.TagApplicationIds);
            };

            cloudQueue.Verify(m => m.AddMessageAsync(Is.Value(verify), It.IsAny<TimeSpan?>()), Times.Exactly(1));
            Assert.Equal(logicalSequenceNumber.ToString(), lockState.State.ContinuationToken);
        }

        [Theory(DisplayName = "When asset group is in the feed, then copy it to the queue."), ValidData]
        public async Task When_AssetGroupInFeed_Then_SaveInQueue(
            int logicalSequenceNumber,
            [Frozen] Mock<IChangeFeedReader> changeFeedReader,
            [Frozen] Mock<ICloudQueue> cloudQueue,
            ChangeFeedReaderWorker worker)
        {
            var lockState = new Lock<ChangeFeedReaderLockState>();

            Models.V2.AssetGroup assetGroup = new Models.V2.AssetGroup();

            var document = DocumentModule.Create(assetGroup);
            document.SetPropertyValue("_lsn", logicalSequenceNumber);
            changeFeedReader.Setup(m => m.ReadItemsAsync(null)).ReturnsAsync(new[] { document });

            await worker.DoLockWorkAsync(lockState, CancellationToken.None).ConfigureAwait(false);

            Action<CloudQueueMessage> verify = m =>
            {
                var queuedAssetGroup = JsonConvert.DeserializeObject<ChangeFeedReaderModels.AssetGroup>(m.AsString);
                Assert.Equal(assetGroup.Id, queuedAssetGroup.Id);
                Assert.Equal(assetGroup.IsDeleted, queuedAssetGroup.IsDeleted);
                Assert.Equal(logicalSequenceNumber, queuedAssetGroup.LogicalSequenceNumber);
                Assert.Equal(assetGroup.OwnerId, queuedAssetGroup.OwnerId);
                Assert.Equal(assetGroup.QualifierParts, queuedAssetGroup.QualifierParts);
                Assert.Equal(assetGroup.DeleteAgentId, queuedAssetGroup.DeleteAgentId);
                Assert.Equal(assetGroup.ExportAgentId, queuedAssetGroup.ExportAgentId);
                Assert.Equal(assetGroup.IsVariantsInheritanceBlocked, queuedAssetGroup.IsVariantsInheritanceBlocked);
                Assert.Equal(assetGroup.IsDeleteAgentInheritanceBlocked, queuedAssetGroup.IsDeleteAgentInheritanceBlocked);
                Assert.Equal(assetGroup.IsExportAgentInheritanceBlocked, queuedAssetGroup.IsExportAgentInheritanceBlocked);
            };

            cloudQueue.Verify(m => m.AddMessageAsync(Is.Value(verify), It.IsAny<TimeSpan?>()), Times.Exactly(1));

            Assert.Equal(logicalSequenceNumber.ToString(), lockState.State.ContinuationToken);
        }

        [Theory(DisplayName = "When an unknown entity is in the feed, then do not queue it but save its continuation token."), AutoMoqData(true)]
        public async Task When_UnknownEntityInFeed_Then_DoNotQueueButSaveLSN(
            int logicalSequenceNumber,
            Models.V2.SharingRequest sharingRequest,
            [Frozen] Mock<IChangeFeedReader> changeFeedReader,
            [Frozen] Mock<ICloudQueue> cloudQueue,
            ChangeFeedReaderWorker worker)
        {
            var lockState = new Lock<ChangeFeedReaderLockState>();
            var document = DocumentModule.Create(sharingRequest);
            document.SetPropertyValue("_lsn", logicalSequenceNumber);
            changeFeedReader.Setup(m => m.ReadItemsAsync(null)).ReturnsAsync(new[] { document });

            await worker.DoLockWorkAsync(lockState, CancellationToken.None).ConfigureAwait(false);

            cloudQueue.Verify(m => m.AddMessageAsync(It.IsAny<CloudQueueMessage>(), It.IsAny<TimeSpan?>()), Times.Never);

            Assert.Equal(logicalSequenceNumber.ToString(), lockState.State.ContinuationToken);
        }

        [Theory(DisplayName = "When no continuation token exist, then pass null to change feed reader."), AutoMoqData(true)]
        public async Task When_NoContinuationToken_Then_PassInNull(
            [Frozen] Mock<IChangeFeedReader> changeFeedReader,
            ChangeFeedReaderWorker worker)
        {
            await worker.DoLockWorkAsync(new Lock<ChangeFeedReaderLockState>(), CancellationToken.None).ConfigureAwait(false);

            changeFeedReader.Verify(m => m.ReadItemsAsync(null), Times.Once);
        }

        [Theory(DisplayName = "When continuation token exist, then pass it to change feed reader."), AutoMoqData(true)]
        public async Task When_ContinuationTokenExists_Then_PassItIn(
            Lock<ChangeFeedReaderLockState> lockState,
            [Frozen] Mock<IChangeFeedReader> changeFeedReader,
            ChangeFeedReaderWorker worker)
        {
            lockState.State.SyncContinuationToken = "1";
            lockState.State.ContinuationToken = "10";
            await worker.DoLockWorkAsync(lockState, CancellationToken.None).ConfigureAwait(false);
            changeFeedReader.Verify(m => m.ReadItemsAsync("10"), Times.Once);
        }

        [Theory(DisplayName = "When lock state is null, then use null continuation token."), AutoMoqData(true)]
        public async Task When_LockStateNull_Then_NullContinuationToken(
            Lock<ChangeFeedReaderLockState> lockState,
            [Frozen] Mock<IChangeFeedReader> changeFeedReader,
            ChangeFeedReaderWorker worker)
        {
            lockState.State = null;
            await worker.DoLockWorkAsync(lockState, CancellationToken.None).ConfigureAwait(false);
            changeFeedReader.Verify(m => m.ReadItemsAsync(null), Times.Once);
        }

        [Theory(DisplayName = "When the day is Sunday, then trigger a full sync."), AutoMoqData(true)]
        public async Task When_Sunday_Then_TriggerFullSync(
            Lock<ChangeFeedReaderLockState> lockState,
            [Frozen] Mock<IDateFactory> dateFactory,
            [Frozen] Mock<IChangeFeedReader> changeFeedReader,
            ChangeFeedReaderWorker worker)
        {
            // Return a day that is Sunday.
            var date = new DateTimeOffset(2018, 4, 22, 1, 1, 1, TimeSpan.Zero);
            var endDate = new DateTimeOffset(2018, 4, 28, 1, 1, 1, TimeSpan.Zero);
            dateFactory.Setup(m => m.GetCurrentTime()).Returns(date);

            // Start with no initial sync time.
            lockState.State.ContinuationToken = "10";
            lockState.State.LastSyncTime = default(DateTimeOffset);
            lockState.State.FullSyncInProgress = false;

            await worker.DoLockWorkAsync(lockState, CancellationToken.None).ConfigureAwait(false);
            changeFeedReader.Verify(m => m.ReadItemsAsync(null), Times.Once);

            Assert.Equal(endDate, lockState.State.LastSyncTime);
            Assert.True(lockState.State.FullSyncInProgress);
            Assert.Equal("10", lockState.State.SyncContinuationToken);
        }

        [Theory(DisplayName = "When the day is the Sunday after the last sync, then trigger a full sync."), AutoMoqData(true)]
        public async Task When_NextSunday_Then_TriggerFullSync(
            Lock<ChangeFeedReaderLockState> lockState,
            [Frozen] Mock<IDateFactory> dateFactory,
            [Frozen] Mock<IChangeFeedReader> changeFeedReader,
            ChangeFeedReaderWorker worker)
        {
            // Return a day that is Sunday.
            var sunday1 = new DateTimeOffset(2018, 4, 15, 1, 1, 1, TimeSpan.Zero);
            var sunday2 = new DateTimeOffset(2018, 4, 22, 1, 1, 1, TimeSpan.Zero);
            dateFactory.Setup(m => m.GetCurrentTime()).Returns(sunday2);

            // Start with no initial sync time.
            lockState.State.ContinuationToken = "10";
            lockState.State.LastSyncTime = sunday1;
            lockState.State.FullSyncInProgress = false;

            await worker.DoLockWorkAsync(lockState, CancellationToken.None).ConfigureAwait(false);
            changeFeedReader.Verify(m => m.ReadItemsAsync(null), Times.Once);

            Assert.Equal(sunday2.AddDays(6), lockState.State.LastSyncTime);
            Assert.True(lockState.State.FullSyncInProgress);
        }

        [Theory(DisplayName = "When a full sync has already completed, then do not trigger another one."), AutoMoqData(true)]
        public async Task When_FullSyncComplete_Then_DoNotTriggerAnotherOne(
            Lock<ChangeFeedReaderLockState> lockState,
            [Frozen] Mock<IDateFactory> dateFactory,
            [Frozen] Mock<IChangeFeedReader> changeFeedReader,
            ChangeFeedReaderWorker worker)
        {
            // Return a day that is Sunday.
            var date = new DateTimeOffset(2018, 4, 21, 1, 1, 1, TimeSpan.Zero);
            dateFactory.Setup(m => m.GetCurrentTime()).Returns(date);

            // Start with a sync time in the future (this is what we set as part of processing a sync).
            var originalContinuationToken = lockState.State.ContinuationToken;
            lockState.State.LastSyncTime = date.AddDays(.5);
            lockState.State.FullSyncInProgress = false;

            await worker.DoLockWorkAsync(lockState, CancellationToken.None).ConfigureAwait(false);
            changeFeedReader.Verify(m => m.ReadItemsAsync(originalContinuationToken), Times.Once);

            Assert.Equal(date.AddDays(.5), lockState.State.LastSyncTime);
            Assert.False(lockState.State.FullSyncInProgress);
        }

        [Theory(DisplayName = "When a full sync reaches the old continuation token, then mark the sync as complete."), AutoMoqData(true)]
        public async Task When_OldStateReached_Then_StopTheSync(
            Lock<ChangeFeedReaderLockState> lockState,
            Models.V2.DataOwner dataOwner,
            [Frozen] Mock<IChangeFeedReader> changeFeedReader,
            ChangeFeedReaderWorker worker)
        {
            // Ensure the final continuation token is returned.
            var document = DocumentModule.Create(dataOwner);
            document.SetPropertyValue("_lsn", "123");

            // Set the final continuation token value.
            lockState.State.SyncContinuationToken = "123";
            lockState.State.FullSyncInProgress = true;

            changeFeedReader.Setup(m => m.ReadItemsAsync(lockState.State.ContinuationToken)).ReturnsAsync(new[] { document });

            var x = new System.Diagnostics.Stopwatch();
            x.Start();
            await worker.DoLockWorkAsync(lockState, CancellationToken.None).ConfigureAwait(false);

            x.Stop();

            Assert.True(x.ElapsedMilliseconds >= 500, $"x.ElapsedMilliseconds ({x.ElapsedMilliseconds}) < 500");
            Assert.False(lockState.State.FullSyncInProgress);
        }

        [Theory(DisplayName = "Verify the correct queues are called for each entity type."), ValidData]
        public async Task EnsureCorrectQueuesAreCalled(
            Mock<IChangeFeedReader> changeFeedReader,
            Mock<ICloudQueue> dataOwnerQueue,
            Mock<ICloudQueue> assetGroupQueue,
            Mock<ICloudQueue> variantDefinitionQueue,
            IFixture fixture)
        {
            // Manually create the object so that we can set the queues independently.
            var worker = new ChangeFeedReaderWorker(
                fixture.Create<Guid>(),
                fixture.Create<IDateFactory>(),
                fixture.Create<ILockConfig>(),
                fixture.Create<ILockDataAccess<ChangeFeedReaderLockState>>(),
                fixture.Create<ISessionFactory>(),
                fixture.Create<IEventWriterFactory>(),
                changeFeedReader.Object,
                dataOwnerQueue.Object,
                assetGroupQueue.Object,
                variantDefinitionQueue.Object);

            Models.V2.DataOwner dataOwner = new Models.V2.DataOwner();
            Models.V2.AssetGroup assetGroup = new Models.V2.AssetGroup();
            Models.V2.VariantDefinition variantDefinition = new Models.V2.VariantDefinition();
            Models.V2.DeleteAgent deleteAgent = new Models.V2.DeleteAgent();

            var documents = new[]
            {
                DocumentModule.Create(variantDefinition),
                DocumentModule.Create(dataOwner),
                DocumentModule.Create(assetGroup)
            };
            changeFeedReader.Setup(m => m.ReadItemsAsync(null)).ReturnsAsync(documents);

            await worker.DoLockWorkAsync(new Lock<ChangeFeedReaderLockState>(), CancellationToken.None).ConfigureAwait(false);

            Func<Guid, Action<CloudQueueMessage>> verify = id => m =>
            {
                var obj = JObject.Parse(m.AsString);
                Assert.Equal(id.ToString(), obj.GetValue("id").ToString());
            };

            dataOwnerQueue.Verify(m => m.AddMessageAsync(Is.Value(verify(dataOwner.Id)), It.IsAny<TimeSpan?>()), Times.Once);
            assetGroupQueue.Verify(m => m.AddMessageAsync(Is.Value(verify(assetGroup.Id)), It.IsAny<TimeSpan?>()), Times.Once);
            variantDefinitionQueue.Verify(m => m.AddMessageAsync(Is.Value(verify(variantDefinition.Id)), It.IsAny<TimeSpan?>()), Times.Once);
        }

        [Theory(DisplayName = "Verify only data grid queues are called for non bootstrap full sync."), ValidData]
        public async Task VerifyNonBootstrapFullSyncOnlyEnqueueDataGridQueues(
            Mock<IChangeFeedReader> changeFeedReader,
            Mock<ICloudQueue> dataOwnerQueue,
            Mock<ICloudQueue> assetGroupQueue,
            Mock<ICloudQueue> variantDefinitionQueue,
            [Frozen] Mock<IDateFactory> dateFactory,
            Lock<ChangeFeedReaderLockState> lockState,
            IFixture fixture)
        {
            // Return a day that is Sunday.
            var date = new DateTimeOffset(2019, 3, 24, 1, 1, 1, TimeSpan.Zero);
            dateFactory.Setup(m => m.GetCurrentTime()).Returns(date);

            // Start with a sync time in the past, so this is a full sync.
            lockState.State.ContinuationToken = "10";
            lockState.State.SyncContinuationToken = "0";
            lockState.State.LastSyncTime = date.AddDays(-2);
            lockState.State.FullSyncInProgress = false;

            // Manually create the object so that we can set the queues independently.
            var worker = new ChangeFeedReaderWorker(
                fixture.Create<Guid>(),
                dateFactory.Object,
                fixture.Create<ILockConfig>(),
                fixture.Create<ILockDataAccess<ChangeFeedReaderLockState>>(),
                fixture.Create<ISessionFactory>(),
                fixture.Create<IEventWriterFactory>(),
                changeFeedReader.Object,
                dataOwnerQueue.Object,
                assetGroupQueue.Object,
                variantDefinitionQueue.Object);

            Models.V2.DataOwner dataOwner = new Models.V2.DataOwner();
            Models.V2.AssetGroup assetGroup = new Models.V2.AssetGroup();
            Models.V2.VariantDefinition variantDefinition = new Models.V2.VariantDefinition();

            // Setup LSN and document1 should be enqueued in data grid queue in full sync.
            var doc1 = DocumentModule.Create(variantDefinition);
            doc1.SetPropertyValue("_lsn", "9");

            // Setup LSN and document2 should be enqueued in data grid queue in full sync.
            var doc2 = DocumentModule.Create(dataOwner);
            doc2.SetPropertyValue("_lsn", "10");

            // Setup LSN and document3 should be enqueued in data grid queue in incremental sync.
            var doc3 = DocumentModule.Create(assetGroup);
            doc3.SetPropertyValue("_lsn", "11");
 
            var documents = new[] { doc1, doc2, doc3 };
            changeFeedReader.Setup(m => m.ReadItemsAsync(null)).ReturnsAsync(documents);

            await worker.DoLockWorkAsync(lockState, CancellationToken.None).ConfigureAwait(false);

            Func<Guid, Action<CloudQueueMessage>> verify = id => m =>
            {
                var obj = JObject.Parse(m.AsString);
                Assert.Equal(id.ToString(), obj.GetValue("id").ToString());
            };

            dataOwnerQueue.Verify(m => m.AddMessageAsync(Is.Value(verify(dataOwner.Id)), It.IsAny<TimeSpan?>()), Times.Once);
            assetGroupQueue.Verify(m => m.AddMessageAsync(Is.Value(verify(assetGroup.Id)), It.IsAny<TimeSpan?>()), Times.Once);
            variantDefinitionQueue.Verify(m => m.AddMessageAsync(Is.Value(verify(variantDefinition.Id)), It.IsAny<TimeSpan?>()), Times.Once);
        }

        public class ValidDataAttribute : AutoMoqDataAttribute
        {
            public ValidDataAttribute() : base(false)
            {
            }
        }
    }
}
