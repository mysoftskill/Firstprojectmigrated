// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.UnitTests.Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Serialization;

    using Live.Mesh.Service.AsyncQueueService.Interface;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.Common;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.EventProcessors;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.TableStorage;
    using Microsoft.Membership.MemberServices.Privacy.Core.AQS;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Microsoft.PrivacyServices.Common.Azure;

    [TestClass]
    public class CdpEventQueueProcessorTests : SharedTestFunctions
    {
        private AccountCreateInformation accountCreateInfo;

        private Mock<ICounterFactory> counterFactory;

        private Mock<ITable<MsaDeadLetterStorage>> deadLetter;

        private Mock<IAccountDeleteWriter> deleteWriter;

        private Mock<ILogger> logger;

        private CdpEventQueueProcessor processor;

        private Mock<IAsyncQueueService2> queue;

        private Mock<IUserCreateEventProcessor> userCreateProcessor;

        private Mock<IUserDeleteEventProcessor> userDeleteProcessor;

        private Mock<IAccountCreateWriter> writer;

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        [DynamicData(nameof(GenerateCdpEventQueueProcessorConstructorTestData), DynamicDataSourceType.Method)]
        public void CreateNewCdpEventQueueProcessorExpectedException(
            IAsyncQueueService2 aqsClient,
            IAqsQueueProcessorConfiguration processorConfiguration,
            ILogger logger,
            IUserCreateEventProcessor createEventProcessor,
            IUserDeleteEventProcessor deleteEventProcessor,
            IAccountCreateWriter accountCreateWriter,
            IAccountDeleteWriter accountDeleteWriter,
            ICounterFactory counterFactory,
            ITable<MsaDeadLetterStorage> deadLetterTable,
            string expectedErrorMessage)
        {
            //Act
            new CdpEventQueueProcessor(
                aqsClient,
                processorConfiguration,
                logger,
                createEventProcessor,
                deleteEventProcessor,
                accountCreateWriter,
                accountDeleteWriter,
                counterFactory,
                deadLetterTable);
        }

        [TestMethod]
        public void GetCsvStringSuccess()
        {
            //Arrange
            const string expectedResult = "6543210,9EC5DB78D9EB442E1BFAE226FFFFFFFF,MmjTGr0j8uJVxq2sM5Z4OA2,123321";
            this.accountCreateInfo.Puid = 6543210;
            this.accountCreateInfo.Cid = 123321;

            //Act
            string result = this.accountCreateInfo.GetCsvString();

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedResult, result);
        }

        [TestInitialize]
        public void Initialize()
        {
            this.accountCreateInfo = new AccountCreateInformation();
            this.queue = CreateMockQueueService2();
            this.logger = CreateMockGenevaLogger();
            this.userCreateProcessor = CreateMockCreateEventProcessor();
            this.userDeleteProcessor = CreateMockDeleteEventProcessor();
            this.deleteWriter = CreateMockAccountDeleteWriter();
            this.counterFactory = CreateMockCounterFactory();
            this.writer = CreateMockCreateWriter();
            this.deadLetter = CreateMockMsaDeadLetterStorage();

            this.processor = new CdpEventQueueProcessor(
                this.queue.Object,
                CreateMockConf().AqsWorkerConfiguration.AqsConfiguration[0].AqsQueueProcessorConfiguration,
                this.logger.Object,
                this.userCreateProcessor.Object,
                this.userDeleteProcessor.Object,
                this.writer.Object,
                this.deleteWriter.Object,
                this.counterFactory.Object,
                this.deadLetter.Object);
        }

        [TestMethod]
        public async Task ShouldCompleteWorkOnSuccess()
        {
            const long createPuid = 123456;
            const long deletePuid = 654321;

            string createEventId = createPuid.ToString("X16");
            string deleteEventId = deletePuid.ToString("X16");

            this.queue.Setup(q => q.TakeWorkAsync(It.IsAny<string>(), It.IsAny<short>(), It.IsAny<int>()))
                .ReturnsAsync(GenerateGroups((createEventId, true, true), (deleteEventId, false, true)));

            this.writer.Setup(
                w => w.WriteCreatedAccountsAsync(
                    It.Is<IList<AccountCreateInformation>>(e => !e.Any()))).ReturnsAsync(new AdapterResponse<IList<AccountCreateInformation>>());

            this.writer.Setup(
                w => w.WriteCreatedAccountsAsync(
                    It.Is<IList<AccountCreateInformation>>(
                        mappings =>
                            1 == mappings.Count() &&
                            createPuid == mappings.FirstOrDefault().Puid &&
                            deletePuid == mappings.FirstOrDefault().Cid))).ReturnsAsync(
                new AdapterResponse<IList<AccountCreateInformation>>
                {
                    Result = new List<AccountCreateInformation>
                    {
                        new AccountCreateInformation
                        {
                            Puid = createPuid,
                            Cid = deletePuid
                        }
                    }
                });

            this.deleteWriter.Setup(
                dw => dw.WriteDeletesAsync(
                    It.Is<IList<AccountDeleteInformation>>(
                        deletes =>
                            1 == deletes.Count() &&
                            deletes.FirstOrDefault().Puid == deletePuid &&
                            deletes.FirstOrDefault().Cid == createPuid),
                    It.IsAny<string>())).ReturnsAsync(
                new AdapterResponse<IList<AccountDeleteInformation>>
                {
                    Result = new List<AccountDeleteInformation>
                    {
                        new AccountDeleteInformation
                        {
                            Puid = deletePuid,
                            Cid = createPuid
                        }
                    }
                });

            this.userCreateProcessor.Setup(
                    ucm => ucm.ProcessCreateItemsAsync(It.Is<IEnumerable<CDPEvent2>>(evts => evts.All(evt => string.Equals(evt.AggregationKey, createEventId)))))
                .ReturnsAsync(
                    new AdapterResponse<IList<AccountCreateInformation>>
                    {
                        Result = new List<AccountCreateInformation>
                        {
                            new AccountCreateInformation
                            {
                                Puid = createPuid,
                                Cid = deletePuid
                            }
                        }
                    });

            this.userDeleteProcessor.Setup(udp => udp.ProcessDeleteItemsAsync(It.Is<IEnumerable<CDPEvent2>>(evts => string.Equals(deleteEventId, evts.First().AggregationKey)), CancellationToken.None)).ReturnsAsync(
                new AdapterResponse<IList<AdapterResponse<AccountDeleteInformation>>>
                {
                    Result = new[] {
                        new AdapterResponse<AccountDeleteInformation>
                        {
                            Result = new AccountDeleteInformation
                            {
                                Puid = deletePuid,
                                Cid = createPuid
                            }
                        }
                    }.ToList()
                });

            this.userDeleteProcessor.Setup(udp => udp.EventHelper).Returns(new CdpEvent2Helper(CreateMockConf(), this.logger.Object));

            // Success group should end up Completing work
            this.queue.Setup(q => q.CompleteWorkAsync(It.IsAny<string>(), It.Is<string>(s => s == createEventId)))
                .Returns(Task.CompletedTask);

            this.queue.Setup(q => q.CompleteWorkAsync(It.IsAny<string>(), It.Is<string>(s => s == deleteEventId)))
                .Returns(Task.CompletedTask);

            Assert.IsTrue(await this.processor.DoWorkAsync().ConfigureAwait(false));

            this.queue.Verify(q => q.CompleteWorkAsync(It.IsAny<string>(), It.Is<string>(s => s == createEventId)));

            this.writer.Verify(
                w => w.WriteCreatedAccountsAsync(
                    It.Is<IList<AccountCreateInformation>>(
                        mappings =>
                            1 == mappings.Count() &&
                            createPuid == mappings.FirstOrDefault().Puid &&
                            deletePuid == mappings.FirstOrDefault().Cid)));
        }

        [TestMethod]
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

            MsaDeadLetterStorage deadLettered = null;
            this.deadLetter.Setup(dl => dl.InsertAsync(It.IsAny<MsaDeadLetterStorage>()))
                .ReturnsAsync(true)
                .Callback<MsaDeadLetterStorage>(dl => deadLettered = dl);

            // Error group should release the work
            this.queue.Setup(q => q.CompleteWorkAsync(It.IsAny<string>(), errorGroupId)).Returns(Task.CompletedTask);

            Assert.IsTrue(await this.processor.DoWorkAsync().ConfigureAwait(false));
            this.queue.Verify(q => q.CompleteWorkAsync(It.IsAny<string>(), errorGroupId));

            Assert.IsNotNull(deadLettered);
            Assert.IsNotNull(deadLettered.PartitionKey);
            Assert.IsNotNull(deadLettered.RowKey);
            Assert.AreEqual(puid, deadLettered.DataActual.Puid);
        }

        [TestMethod]
        public async Task ShouldDeleteInvalidDeleteItem()
        {
            const long puid = 654321;
            string errorGroupId = puid.ToString("X16");

            this.queue.Setup(q => q.TakeWorkAsync(It.IsAny<string>(), It.IsAny<short>(), It.IsAny<int>()))
                .ReturnsAsync(GenerateGroups((errorGroupId, false, false)));

            this.userDeleteProcessor.Setup(udp => udp.EventHelper).Returns(new CdpEvent2Helper(CreateMockConf(), this.logger.Object));
            this.userDeleteProcessor.Setup(udp => udp.ProcessDeleteItemsAsync(It.Is<IEnumerable<CDPEvent2>>(e => !e.Any()), It.IsAny<CancellationToken>())).ReturnsAsync(
                new AdapterResponse<IList<AdapterResponse<AccountDeleteInformation>>>
                {
                    Result = new List<AdapterResponse<AccountDeleteInformation>>()
                });

            this.deleteWriter.Setup(dw => dw.WriteDeletesAsync(It.Is<IList<AccountDeleteInformation>>(infos => !infos.Any()), It.IsAny<string>())).ReturnsAsync(
                new AdapterResponse<IList<AccountDeleteInformation>>
                {
                    Result = new List<AccountDeleteInformation>()
                });

            // Error group should release the work
            this.queue.Setup(q => q.CompleteWorkAsync(It.IsAny<string>(), errorGroupId))
                .Returns(Task.CompletedTask);

            MsaDeadLetterStorage deadLettered = null;
            this.deadLetter.Setup(dl => dl.InsertAsync(It.IsAny<MsaDeadLetterStorage>()))
                .ReturnsAsync(true)
                .Callback<MsaDeadLetterStorage>(dl => deadLettered = dl);

            Assert.IsTrue(await this.processor.DoWorkAsync().ConfigureAwait(false));

            this.queue.Verify(q => q.CompleteWorkAsync(It.IsAny<string>(), errorGroupId));

            Assert.IsNotNull(deadLettered);
            Assert.IsNotNull(deadLettered.PartitionKey);
            Assert.IsNotNull(deadLettered.RowKey);
            Assert.AreEqual(puid, deadLettered.DataActual.Puid);
        }

        [TestMethod]
        public async Task ShouldFlagNoWorkWhenNoEventsAreReturned()
        {
            this.queue.Setup(q => q.TakeWorkAsync(It.IsAny<string>(), It.IsAny<short>(), It.IsAny<int>()))
                .ReturnsAsync((AggregationGroup[])null);

            this.logger.Setup(
                l => l.Information(nameof(CdpEventQueueProcessor), "Received no work from the queue"));

            Assert.IsFalse(await this.processor.DoWorkAsync().ConfigureAwait(false));
        }

        [TestMethod]
        public async Task ShouldReleaseWorkOnError()
        {
            const long createPuid = 123456;
            const long deletePuid = 654321;
            const string debugInfo = "Processing Failed";

            string createEventId = createPuid.ToString("X16");
            string deleteEventId = deletePuid.ToString("X16");

            this.queue.Setup(q => q.TakeWorkAsync(It.IsAny<string>(), It.IsAny<short>(), It.IsAny<int>()))
                .ReturnsAsync(GenerateGroups((createEventId, true, true), (deleteEventId, false, true)));

            this.queue.Setup(c => c.ReleaseWorkAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                .Returns(Task.FromResult(Task.CompletedTask));

            this.writer.Setup(
                w => w.WriteCreatedAccountsAsync(
                    It.Is<IList<AccountCreateInformation>>(e => !e.Any()))).ReturnsAsync(new AdapterResponse<IList<AccountCreateInformation>>());

            this.writer.Setup(
                w => w.WriteCreatedAccountsAsync(
                    It.Is<IList<AccountCreateInformation>>(
                        mappings =>
                            1 == mappings.Count() &&
                            createPuid == mappings.FirstOrDefault().Puid &&
                            deletePuid == mappings.FirstOrDefault().Cid))).ReturnsAsync(
                new AdapterResponse<IList<AccountCreateInformation>>
                {
                    Result = new List<AccountCreateInformation>
                    {
                        new AccountCreateInformation
                        {
                            Puid = createPuid,
                            Cid = deletePuid
                        }
                    }
                });

            this.deleteWriter.Setup(
                dw => dw.WriteDeletesAsync(
                    It.Is<IList<AccountDeleteInformation>>(
                        deletes =>
                            1 == deletes.Count() &&
                            deletes.FirstOrDefault().Puid == deletePuid &&
                            deletes.FirstOrDefault().Cid == createPuid),
                    It.IsAny<string>())).ReturnsAsync(
                new AdapterResponse<IList<AccountDeleteInformation>>
                {
                    Result = new List<AccountDeleteInformation>
                    {
                        new AccountDeleteInformation
                        {
                            Puid = createPuid,
                            Cid = deletePuid
                        }
                    }
                });

            this.userCreateProcessor.Setup(
                    ucm => ucm.ProcessCreateItemsAsync(It.Is<IEnumerable<CDPEvent2>>(evts => evts.All(evt => string.Equals(evt.AggregationKey, createEventId)))))
                .ReturnsAsync(
                    new AdapterResponse<IList<AccountCreateInformation>>
                    {
                        Error = new AdapterError(AdapterErrorCode.BadRequest, "doesnotmatter", (int)HttpStatusCode.InternalServerError)
                    });

            this.userDeleteProcessor.Setup(udp => udp.ProcessDeleteItemsAsync(It.Is<IEnumerable<CDPEvent2>>(evts => string.Equals(deleteEventId, evts.First().AggregationKey)), CancellationToken.None)).ReturnsAsync(
                new AdapterResponse<IList<AdapterResponse<AccountDeleteInformation>>>
                {
                    Result = new[] {
                        new AdapterResponse<AccountDeleteInformation>
                        {
                            Result = new AccountDeleteInformation
                            {
                                Puid = deletePuid,
                                Cid = createPuid
                            }
                        }
                    }.ToList()
                });

            this.userDeleteProcessor.Setup(udp => udp.EventHelper).Returns(new CdpEvent2Helper(CreateMockConf(), this.logger.Object));

            Assert.IsTrue(await this.processor.DoWorkAsync().ConfigureAwait(false));

            this.queue.Verify(q => q.ReleaseWorkAsync(It.IsAny<string>(), It.Is<string>(s => s == createEventId), It.IsAny<int>(), It.Is<string>(s => s == debugInfo)));
        }

        [TestMethod]
        public void StartSuccess()
        {
          this.processor.Start();

        }

        private static IEnumerable<object[]> GenerateCdpEventQueueProcessorConstructorTestData()
        {
            var queue = CreateMockQueueService2().Object;
            var config = CreateMockConf().AqsWorkerConfiguration.AqsConfiguration[0].AqsQueueProcessorConfiguration;
            var logger = CreateMockGenevaLogger().Object;
            var userCreateProcessor = CreateMockCreateEventProcessor().Object;
            var userDeleteProcessor = CreateMockDeleteEventProcessor().Object;
            var createWriter = CreateMockCreateWriter().Object;
            var deleteWriter = CreateMockAccountDeleteWriter().Object;
            var counterFactory = CreateMockCounterFactory().Object;
            var deadLetter = CreateMockMsaDeadLetterStorage().Object;

            var data = new List<object[]>
            {
                new object[]
                {
                    null,
                    config,
                    logger,
                    userCreateProcessor,
                    userDeleteProcessor,
                    createWriter,
                    deleteWriter,
                    counterFactory,
                    deadLetter,
                    "Value cannot be null.\r\nParameter name: aqsClient"
                },
                new object[]
                {
                    queue,
                    null,
                    logger,
                    userCreateProcessor,
                    userDeleteProcessor,
                    createWriter,
                    deleteWriter,
                    counterFactory,
                    deadLetter,
                    "Value cannot be null.\r\nParameter name: processorConfiguration"
                },
                new object[]
                {
                    queue,
                    config,
                    null,
                    userCreateProcessor,
                    userDeleteProcessor,
                    createWriter,
                    deleteWriter,
                    counterFactory,
                    deadLetter,
                    "Value cannot be null.\r\nParameter name: logger"
                },
                new object[]
                {
                    queue,
                    config,
                    logger,
                    null,
                    userDeleteProcessor,
                    createWriter,
                    deleteWriter,
                    counterFactory,
                    deadLetter,
                    "Value cannot be null.\r\nParameter name: createEventProcessor"
                },
                new object[]
                {
                    queue,
                    config,
                    logger,
                    userCreateProcessor,
                    null,
                    createWriter,
                    deleteWriter,
                    counterFactory,
                    deadLetter,
                    "Value cannot be null.\r\nParameter name: deleteEventProcessor"
                },
                new object[]
                {
                    queue,
                    config,
                    logger,
                    userCreateProcessor,
                    userDeleteProcessor,
                    null,
                    deleteWriter,
                    counterFactory,
                    deadLetter,
                    "Value cannot be null.\r\nParameter name: accountCreateWriter"
                },
                new object[]
                {
                    queue,
                    config,
                    logger,
                    userCreateProcessor,
                    userDeleteProcessor,
                    createWriter,
                    null,
                    counterFactory,
                    deadLetter,
                    "Value cannot be null.\r\nParameter name: accountDeleteWriter"
                },
                new object[]
                {
                    queue,
                    config,
                    logger,
                    userCreateProcessor,
                    userDeleteProcessor,
                    createWriter,
                    deleteWriter,
                    null,
                    deadLetter,
                    "Value cannot be null.\r\nParameter name: counterFactory"
                },
                new object[]
                {
                    queue,
                    config,
                    logger,
                    userCreateProcessor,
                    userDeleteProcessor,
                    createWriter,
                    deleteWriter,
                    counterFactory,
                    null,
                    "Value cannot be null.\r\nParameter name: deadLetterTable"
                }
            };

            return data;
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
    }
}
