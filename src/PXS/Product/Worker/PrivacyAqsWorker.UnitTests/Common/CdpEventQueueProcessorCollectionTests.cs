// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.UnitTests.Common
{
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.Common;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.EventProcessors;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.TableStorage;
    using Microsoft.Membership.MemberServices.Privacy.Core.AQS;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Microsoft.PrivacyServices.Common.Azure;

    [TestClass]
    public class CdpEventQueueProcessorCollectionTests : SharedTestFunctions
    {
        private IPrivacyConfigurationManager config;

        private Mock<ICounterFactory> counterFactory;

        private Mock<IAccountCreateWriter> createWriter;

        private Mock<ITable<MsaDeadLetterStorage>> deadLetter;

        private Mock<IAccountDeleteWriter> deleteWriter;

        private Mock<ILogger> logger;

        private CdpEventQueueProcessorFactory queueProcessorFactory;

        private IUserCreateEventProcessorFactory userCreateProcessor;

        private IUserDeleteEventProcessorFactory userDeleteProcessor;

        private Mock<IClock> clock;

        [TestInitialize]
        public void Initialize()
        {
            this.logger = CreateMockGenevaLogger();
            this.deleteWriter = CreateMockAccountDeleteWriter();
            this.createWriter = CreateMockCreateWriter();
            this.counterFactory = CreateMockCounterFactory();
            this.deadLetter = CreateMockMsaDeadLetterStorage();
            this.config = CreateMockConf();
            this.logger = CreateMockGenevaLogger();
            this.clock = new Mock<IClock>();
            
            this.queueProcessorFactory = new CdpEventQueueProcessorFactory();

            var mockMsaAdapter = new Mock<IMsaIdentityServiceAdapter>();
            this.userCreateProcessor = new UserCreateEventProcessorFactory(mockMsaAdapter.Object, this.counterFactory.Object);

            var cdpHelper = new CdpEvent2Helper(this.config, this.logger.Object);

            this.userDeleteProcessor = new UserDeleteEventProcessorFactory(
                cdpHelper,
                this.counterFactory.Object,
                this.logger.Object,
                this.clock.Object);
        }

       }
}
