// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.UnitTests.Common
{
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Worker;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.Common;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.EventProcessors;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.TableStorage;
    using Microsoft.Membership.MemberServices.Privacy.Core.AQS;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Microsoft.PrivacyServices.Common.Azure;

    [TestClass]
    public class CdpEventQueueProcessorFactoryTests : SharedTestFunctions
    {
        private Mock<IAqsConfiguration> aqsConfig;

        private Mock<ICounterFactory> counterFactory;

        private Mock<ITable<MsaDeadLetterStorage>> deadLetter;

        private Mock<IAccountDeleteWriter> deleteWriter;

        private Mock<ILogger> logger;

        private CdpEventQueueProcessorFactory processor;

        private Mock<IAsyncQueueService2> queue;

        private Mock<IUserCreateEventProcessor> userCreateProcessor;

        private Mock<IUserDeleteEventProcessor> userDeleteProcessor;

        private Mock<IAccountCreateWriter> writer;

        [TestMethod]
        public void CreateSuccess()
        {
            IWorker result = this.processor.Create(
                this.queue.Object,
                this.aqsConfig.Object,
                this.logger.Object,
                this.userCreateProcessor.Object,
                this.userDeleteProcessor.Object,
                this.writer.Object,
                this.deleteWriter.Object,
                this.counterFactory.Object,
                this.deadLetter.Object);

            Assert.IsNotNull(result);
        }

        [TestInitialize]
        public void Initialize()
        {
            this.queue = new Mock<IAsyncQueueService2>(MockBehavior.Strict);
            this.logger = new Mock<ILogger>();
            this.userCreateProcessor = new Mock<IUserCreateEventProcessor>(MockBehavior.Strict);
            this.userDeleteProcessor = new Mock<IUserDeleteEventProcessor>(MockBehavior.Strict);
            this.deleteWriter = new Mock<IAccountDeleteWriter>(MockBehavior.Strict);
            this.counterFactory = new Mock<ICounterFactory>(MockBehavior.Strict);
            this.processor = new CdpEventQueueProcessorFactory();
            this.writer = new Mock<IAccountCreateWriter>(MockBehavior.Strict);
            this.deadLetter = new Mock<ITable<MsaDeadLetterStorage>>(MockBehavior.Strict);
            var config = new Mock<IAqsQueueProcessorConfiguration>();
            this.aqsConfig = new Mock<IAqsConfiguration>(MockBehavior.Strict);
            this.aqsConfig.Setup(a => a.AqsQueueProcessorConfiguration).Returns(config.Object);
        }
    }
}
