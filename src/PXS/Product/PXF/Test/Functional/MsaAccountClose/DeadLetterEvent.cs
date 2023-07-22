// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.FunctionalTests.MsaAccountClose
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Xml.Serialization;

    using Live.Mesh.Service.AsyncQueueService.Interface;

    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.Common;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.EventProcessors;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.TableStorage;
    using Microsoft.Membership.MemberServices.Privacy.Core.AQS;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     Summary description for DeadLetterEvent
    /// </summary>
    [TestClass]
    public class DeadLetterEvent : StorageEmulatorBase
    {
        private Mock<ICounter> counter;

        private Mock<ICounterFactory> counterFactory;

        private ITable<MsaDeadLetterStorage> deadLetter;

        private Mock<IAccountDeleteWriter> deleteWriter;

        private Mock<ILogger> logger;

        private CdpEventQueueProcessor processor;

        private Mock<IAsyncQueueService2> queue;

        private Mock<IUserCreateEventProcessor> userCreateProcessor;

        private Mock<IUserDeleteEventProcessor> userDeleteProcessor;

        private Mock<IAccountCreateWriter> writer;

        [TestInitialize]
        public void Initialize()
        {
            this.mockAzureStorageConfiguration.SetupGet(c => c.UseEmulator).Returns(true);
            this.StartEmulator();
            this.queue = new Mock<IAsyncQueueService2>(MockBehavior.Strict);
            this.logger = new Mock<ILogger>();
            this.userCreateProcessor = new Mock<IUserCreateEventProcessor>(MockBehavior.Strict);
            this.userDeleteProcessor = new Mock<IUserDeleteEventProcessor>(MockBehavior.Strict);
            this.deleteWriter = new Mock<IAccountDeleteWriter>(MockBehavior.Strict);

            this.counter = new Mock<ICounter>();
            this.counterFactory = new Mock<ICounterFactory>(MockBehavior.Strict);
            this.counterFactory.Setup(cf => cf.GetCounter(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CounterType>())).Returns(this.counter.Object);

            this.writer = new Mock<IAccountCreateWriter>(MockBehavior.Strict);

            this.deadLetter = new AzureTable<MsaDeadLetterStorage>(this.StorageProvider, this.Logger, nameof(MsaDeadLetterStorage));

            this.processor = new CdpEventQueueProcessor(
                this.queue.Object,
                CreateMockConf().AqsWorkerConfiguration.AqsConfiguration[0].AqsQueueProcessorConfiguration,
                this.logger.Object,
                this.userCreateProcessor.Object,
                this.userDeleteProcessor.Object,
                this.writer.Object,
                this.deleteWriter.Object,
                this.counterFactory.Object,
                this.deadLetter);
        }

        [TestMethod]
        [TestCategory("FCT"), Ignore]
        public async Task ShouldDeleteInvalidCreateItem()
        {
            const long puid = 123456;
            string errorGroupId = puid.ToString("X16");

            this.queue.Setup(q => q.TakeWorkAsync(It.IsAny<string>(), It.IsAny<short>(), It.IsAny<int>()))
                .ReturnsAsync(GenerateGroups((errorGroupId, true, true)));

            this.userCreateProcessor.Setup(u => u.ProcessCreateItemsAsync(It.IsAny<IEnumerable<CDPEvent2>>())).ReturnsAsync(
                new AdapterResponse<IList<AccountCreateInformation>>
                {
                    Result = new List<AccountCreateInformation>()
                });

            this.writer.Setup(w => w.WriteCreatedAccountsAsync(It.Is<IList<AccountCreateInformation>>(evts => !evts.Any()))).ReturnsAsync(
                new AdapterResponse<IList<AccountCreateInformation>>
                {
                    Result = new List<AccountCreateInformation>()
                });

            // Error group should release the work
            this.queue.Setup(q => q.CompleteWorkAsync(It.IsAny<string>(), errorGroupId)).Returns(Task.CompletedTask);

            Assert.IsTrue(await this.processor.DoWorkAsync().ConfigureAwait(false));
            this.queue.Verify(q => q.CompleteWorkAsync(It.IsAny<string>(), errorGroupId));
        }

        [TestMethod]
        [TestCategory("FCT"), Ignore]
        public async Task ShouldDeleteInvalidDeleteItem()
        {
            const long puid = 654321;
            string errorGroupId = puid.ToString("X16");

            this.queue.Setup(q => q.TakeWorkAsync(It.IsAny<string>(), It.IsAny<short>(), It.IsAny<int>()))
                .ReturnsAsync(GenerateGroups((errorGroupId, false, false)));

            this.userDeleteProcessor.Setup(udp => udp.EventHelper).Returns(new CdpEvent2Helper(CreateMockConf(), this.logger.Object));

            this.deleteWriter.Setup(dw => dw.WriteDeletesAsync(It.Is<IList<AccountDeleteInformation>>(infos => !infos.Any()), It.IsAny<string>())).ReturnsAsync(
                new AdapterResponse<IList<AccountDeleteInformation>>
                {
                    Result = new List<AccountDeleteInformation>()
                });

            // Error group should release the work
            this.queue.Setup(q => q.CompleteWorkAsync(It.IsAny<string>(), errorGroupId))
                .Returns(Task.CompletedTask);

            Assert.IsTrue(await this.processor.DoWorkAsync().ConfigureAwait(false));

            this.queue.Verify(q => q.CompleteWorkAsync(It.IsAny<string>(), errorGroupId));
        }

        protected static IPrivacyConfigurationManager CreateMockConf()
        {
            var config = new Mock<IAqsQueueProcessorConfiguration>(MockBehavior.Strict);
            config.Setup(c => c.RequesterId).Returns("AqsUnitTests");
            config.Setup(c => c.QueueName).Returns("UnitTestQueue");
            config.Setup(c => c.GroupsToTake).Returns(50);
            config.Setup(c => c.LeaseTimeoutSeconds).Returns(50);
            config.Setup(c => c.ReleaseWaitIntervalSeconds).Returns(50);
            var aqsConfig = new Mock<IAqsConfiguration>(MockBehavior.Strict);
            aqsConfig.Setup(a => a.AqsQueueProcessorConfiguration).Returns(config.Object);

            var workerConfig = new Mock<IPrivacyAqsWorkerConfiguration>(MockBehavior.Strict);
            workerConfig.Setup(a => a.AqsConfiguration).Returns(new[] { aqsConfig.Object }.ToList());
            workerConfig.Setup(a => a.EnableExtraLogging).Returns(true);

            var privacyConfig = new Mock<IPrivacyConfigurationManager>(MockBehavior.Strict);
            privacyConfig.Setup(p => p.AqsWorkerConfiguration).Returns(workerConfig.Object);

            return privacyConfig.Object;
        }

        private static AggregationGroup[] GenerateGroups(params (string Id, bool IsCreateEvent, bool IsValidData)[] infos)
        {
            return infos.Select(
                info =>
                    new AggregationGroup
                    {
                        Id = info.Id,
                        WorkItems = new[]
                        {
                            new WorkItem
                            {
                                Id = info.Id,
                                Payload = GenerateXmlEvent(info.Id, info.IsCreateEvent, info.IsValidData),
                                TakenCount = 5
                            }
                        }
                    }).ToArray();
        }

        private static byte[] GenerateXmlEvent(string id, bool isCreateEvent, bool isValidData)
        {
            var evt = new CDPEvent2
            {
                AggregationKey = id,
                EventData = isCreateEvent
                    ? new UserCreate()
                    : (EventData)new UserDelete
                    {
                        Property = isValidData
                            ? new[]
                            {
                                ValidDeleteData()
                            }
                            : null
                    }
            };

            var serializer = new XmlSerializer(typeof(CDPEvent2));
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, evt);
                return stream.GetBuffer();
            }
        }

        protected static EventDataBaseProperty ValidDeleteData(int reason = 2) => new EventDataBaseProperty
        {
            Name = "CredentialName",
            ExtendedData = $"UserDeleteReason:{reason},cid:123abc,GdprPreVerifier:gdpr"
        };
    }
}
