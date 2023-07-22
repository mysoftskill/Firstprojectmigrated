namespace PCF.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandReplay;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Validator;
    using Microsoft.PrivacyServices.SignalApplicability;

    using Moq;

    using Newtonsoft.Json.Linq;

    using Xunit;

    using Common = Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using PXSV1 = Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    [Trait("Category", "UnitTest")]
    public class CommandReplayWorkerTests : INeedDataBuilders
    {
        [Fact]
        public async Task NoActiveReplayJob()
        {
            var agentMapFactory = this.AMockOf<IDataAgentMapFactory>();
            var commandHistory = this.AMockOf<ICommandHistoryRepository>();
            var replayRepo = this.AMockOf<ICommandReplayJobRepository>();
            var validator = this.AMockOf<IValidationService>();
            var publisher = this.AMockOf<IAzureWorkItemQueuePublisher<EnqueueBatchReplayCommandsWorkItem>>();

            var replayWorker = new ReplayWorker(
                agentMapFactory.Object,
                replayRepo.Object,
                commandHistory.Object,
                validator.Object,
                publisher.Object);

            ReplayJobDocument replayJobDoc = null;

            replayRepo.Setup(m => m.PopNextItemAsync(TimeSpan.FromMinutes(It.IsAny<int>()))).ReturnsAsync(replayJobDoc);

            await replayWorker.RunOnceAsync(1);

            agentMapFactory.Verify(m => m.GetDataAgentMap(), Times.Never);
        }

        [Fact]
        public async Task NoMatchingAssetGroupInfo()
        {
            var assetgroupId = this.AnAssetGroupId();
            var agentMapFactory = this.AMockOf<IDataAgentMapFactory>();
            var commandHistory = this.AMockOf<ICommandHistoryRepository>();
            var replayRepo = this.AMockOf<ICommandReplayJobRepository>();
            var validator = this.AMockOf<IValidationService>();
            var publisher = this.AMockOf<IAzureWorkItemQueuePublisher<EnqueueBatchReplayCommandsWorkItem>>();
            var agentMap = this.AMockOf<IDataAgentMap>();

            var replayWorker = new ReplayWorker(
                agentMapFactory.Object,
                replayRepo.Object,
                commandHistory.Object,
                validator.Object,
                publisher.Object);

            ReplayJobDocument replayJob = this.BuildReplayJobDocument(assetgroupId);

            replayRepo.Setup(m => m.PopNextItemAsync(TimeSpan.FromMinutes(10))).ReturnsAsync(replayJob);
            agentMapFactory.Setup(m => m.GetDataAgentMap()).Returns(agentMap.Object);

            await replayWorker.RunOnceAsync(1);
            commandHistory.Verify(m => m.GetCommandsForReplayAsync(
                It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<string>(), false, It.IsAny<string>(), It.IsAny<int>()), Times.Never);
            agentMapFactory.Verify(m => m.GetDataAgentMap(), Times.Once());
            replayRepo.Verify(m => m.ReplaceAsync(replayJob, replayJob.ETag), Times.Exactly(2));
        }

        [Fact]
        public async Task NoActionableReplayDestination()
        {
            var agentId = this.AnAgentId();
            var assetgroupId = this.AnAssetGroupId();

            var agentMapFactory = this.AMockOf<IDataAgentMapFactory>();
            var commandHistory = this.AMockOf<ICommandHistoryRepository>();
            var replayRepo = this.AMockOf<ICommandReplayJobRepository>();
            var validator = this.AMockOf<IValidationService>();
            var publisher = this.AMockOf<IAzureWorkItemQueuePublisher<EnqueueBatchReplayCommandsWorkItem>>();
            var agentMap = this.AMockOf<IDataAgentMap>();
            var dataAgentInfo = this.AMockOf<IDataAgentInfo>();
            var assetGroupInfo = this.AMockOf<IAssetGroupInfo>();
            IAssetGroupInfo agInfo = assetGroupInfo.Object;

            (IEnumerable<JObject> pxsCommands, string continuationToken) commandHistoryQueryResult = (new List<JObject> { }, null);

            var replayWorker = new ReplayWorker(
                agentMapFactory.Object,
                replayRepo.Object,
                commandHistory.Object,
                validator.Object,
                publisher.Object);

            ReplayJobDocument replayJob = this.BuildReplayJobDocument(assetgroupId);

            replayRepo.Setup(m => m.PopNextItemAsync(TimeSpan.FromMinutes(10))).ReturnsAsync(replayJob);
            agentMapFactory.Setup(m => m.GetDataAgentMap()).Returns(agentMap.Object);
            agentMap.Setup(m => m.GetAgentIds()).Returns(new List<AgentId> { agentId });
            agentMap.SetupGet(m => m[agentId]).Returns(dataAgentInfo.Object);
            dataAgentInfo.Setup(m => m.TryGetAssetGroupInfo(It.IsAny<AssetGroupId>(), out agInfo)).Returns(true);
            assetGroupInfo.Setup(m => m.SupportedCommandTypes).Returns(new List<PrivacyCommandType> { PrivacyCommandType.Delete });
            commandHistory.Setup(m => m.GetCommandsForReplayAsync(
                It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<string>(), false, null, It.IsAny<int>())).ReturnsAsync(commandHistoryQueryResult);

            await replayWorker.RunOnceAsync(1);

            agentMapFactory.Verify(m => m.GetDataAgentMap(), Times.Once());
            commandHistory.Verify(
                m => m.GetCommandsForReplayAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<string>(), false, null, It.IsAny<int>()), Times.Once());
            replayRepo.Verify(m => m.ReplaceAsync(replayJob, replayJob.ETag), Times.Exactly(2));
        }

        [Fact]
        public async Task PublishReplayItem()
        {
            var agentId = this.AnAgentId();
            var assetgroupId = this.AnAssetGroupId();

            var agentMapFactory = this.AMockOf<IDataAgentMapFactory>();
            var commandHistory = this.AMockOf<ICommandHistoryRepository>();
            var replayRepo = this.AMockOf<ICommandReplayJobRepository>();
            var validator = this.AMockOf<IValidationService>();
            var publisher = this.AMockOf<IAzureWorkItemQueuePublisher<EnqueueBatchReplayCommandsWorkItem>>();
            var agentMap = this.AMockOf<IDataAgentMap>();
            var dataAgentInfo = this.AMockOf<IDataAgentInfo>();
            var assetGroupInfo = this.AMockOf<IAssetGroupInfo>();
            IAssetGroupInfo agInfo = assetGroupInfo.Object;

            assetGroupInfo.SetupGet(m => m.AgentId).Returns(agentId);
            assetGroupInfo.SetupGet(m => m.AssetGroupId).Returns(assetgroupId);

            var privacyRequest = new PXSV1.PrivacyRequest
            {
                RequestGuid = Guid.NewGuid(),
                Subject = new AadSubject(),
                RequestType = PXSV1.RequestType.AccountClose,
                RequestId = Guid.NewGuid()
            };

            IEnumerable<JObject> raxPxsCommands = Enumerable.Range(1, 101).Select(x => JObject.FromObject(privacyRequest));
            (IEnumerable<JObject> pxsCommands, string continuationToken) commandHistoryQueryResult = (raxPxsCommands, null);

            var replayWorker = new ReplayWorker(
                agentMapFactory.Object,
                replayRepo.Object,
                commandHistory.Object,
                validator.Object,
                publisher.Object);

            ReplayJobDocument replayJob = this.BuildReplayJobDocument(assetgroupId);

            replayRepo.Setup(m => m.PopNextItemAsync(TimeSpan.FromMinutes(10))).ReturnsAsync(replayJob);
            agentMapFactory.Setup(m => m.GetDataAgentMap()).Returns(agentMap.Object);
            agentMap.Setup(m => m.GetAgentIds()).Returns(new List<AgentId> { agentId });
            agentMap.SetupGet(m => m[agentId]).Returns(dataAgentInfo.Object);
            dataAgentInfo.Setup(m => m.TryGetAssetGroupInfo(It.IsAny<AssetGroupId>(), out agInfo)).Returns(true);
            assetGroupInfo.Setup(m => m.SupportedCommandTypes).Returns(new List<PrivacyCommandType> { PrivacyCommandType.Delete });
            commandHistory.Setup(m => m.GetCommandsForReplayAsync(
                It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<string>(), false, null, It.IsAny<int>())).ReturnsAsync(commandHistoryQueryResult);
            validator.Setup(m => m.EnsureValidAsync(It.IsAny<string>(), It.IsAny<CommandClaims>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));

            ApplicabilityResult applicabilityResult = new ApplicabilityResult();
            assetGroupInfo.Setup(m => m.IsCommandActionable(It.IsAny<Common.PrivacyCommand>(), out applicabilityResult)).Returns(true);

            var variantInfo = this.AMockOf<IAssetGroupVariantInfo>();
            assetGroupInfo.SetupGet(m => m.VariantInfosAppliedByPcf).Returns(new List<IAssetGroupVariantInfo> { variantInfo.Object });
            variantInfo.Setup(m => m.IsApplicableToCommand(It.IsAny<Common.PrivacyCommand>(), It.IsAny<bool>())).Returns(false);

            int publishCount = 0;

            // Expected delays in minutes throughout a 6 hour period for 3 batches: 2 of 50 elements, and 1 of 1 element.
            int[] expectedDelays = {0, 180, 360}; 
            var actualDelays = new List<TimeSpan>();
            
            int[] expectedBatchSizes = {50, 50, 1};
            var actualBatchSizes = new List<int>();

            publisher.Setup(m => m.PublishWithSplitAsync(
                It.IsAny<IEnumerable<ReplayCommandDestinationPair>>(),
                It.IsAny<Func<IEnumerable<ReplayCommandDestinationPair>, EnqueueBatchReplayCommandsWorkItem>>(),
                It.IsAny<Func<int, TimeSpan>>()))
            .Callback<IEnumerable<ReplayCommandDestinationPair>, Func<IEnumerable<ReplayCommandDestinationPair>, EnqueueBatchReplayCommandsWorkItem>, Func<int, TimeSpan>>((replayCommands, workItemFactoryFunc, delayFactoryFunc) =>
            {
                publishCount++;
                actualDelays.Add(delayFactoryFunc(0));
                actualBatchSizes.Add(replayCommands.Count());
            })
            .Returns(Task.FromResult(true));

            using(new FlightEnabled(FlightingNames.CommandReplayStaggerEnqueueReplayCommandBatches))
            {
                await replayWorker.RunOnceAsync(1);
            }

            agentMapFactory.Verify(m => m.GetDataAgentMap(), Times.Once());
            commandHistory.Verify(m => m.GetCommandsForReplayAsync(
                It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<string>(), false, null, It.IsAny<int>()), Times.Once());
            replayRepo.Verify(m => m.ReplaceAsync(replayJob, replayJob.ETag), Times.Exactly(2));
            Assert.Equal(3, publishCount);
            Assert.Equal(expectedDelays, actualDelays.Select(d => (int)d.TotalMinutes).ToArray());
            Assert.Equal(expectedBatchSizes, actualBatchSizes.ToArray());
        }

        private ReplayJobDocument BuildReplayJobDocument(AssetGroupId gid)
        {
            return new ReplayJobDocument
            {
                Id = "123.456",
                ReplayDate = DateTimeOffset.UtcNow.GetDate(),
                IsCompleted = false,
                UnixNextVisibleTimeSeconds = 0,
                CreatedTime = DateTimeOffset.UtcNow,
                AssetGroupIds = new[] { gid }
            };
        }
    }
}
