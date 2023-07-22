// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.UnitTests.EventProcessors
{
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.EventProcessors;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class UserCreateEventProcessorFactoryTests : SharedTestFunctions
    {
        private Mock<IMsaIdentityServiceAdapter> mockAdapter;

        private Mock<ICounter> mockCounter;

        private Mock<ICounterFactory> mockFactory;

        private UserCreateEventProcessorFactory processorFactory;

        [TestMethod]
        public void CreateSuccess()
        {
            //Arrange
            var config = CreateMockAqsQueueProcessorConfig();

            //Act
            IUserCreateEventProcessor result = this.processorFactory.Create(config.Object);

            //Assert
            Assert.IsNotNull(result);
        }

        [TestInitialize]
        public void Initialize()
        {
            this.mockCounter = CreateMockCounter();
            this.mockFactory = CreateMockCounterFactory();
            this.mockAdapter = new Mock<IMsaIdentityServiceAdapter>(MockBehavior.Strict);
            this.processorFactory = new UserCreateEventProcessorFactory(this.mockAdapter.Object, this.mockFactory.Object);
        }

        [TestMethod]
        public void UserCreateEventProcessorFactorySuccess()
        {
            //Act
            var result = new UserCreateEventProcessorFactory(this.mockAdapter.Object, this.mockFactory.Object);

            //Assert
            Assert.IsNotNull(result);
        }
    }
}
