// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.UnitTests.Cosmos
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.CosmosHelpers;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.Common;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.Cosmos;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.LeaseId;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.Test.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Common.Cosmos;

    [TestClass]
    public class CosmosAccountCreateWriterTests : SharedTestFunctions
    {
        private CosmosAccountCreateWriter cosmosAccountCreateWriter;

        private Mock<ICosmosClient> cosmosClient;

        private Mock<IPuidMappingConfig> cosmosConfig;

        private Mock<ILogger> logger;

        private Mock<IDistributedIdFactory> mockDistributedFactory;

        #region Test Data

        public static IEnumerable<object[]> CosmosAccountCreateWriterTestData()
        {
            var mockIPuidMapping = CreateMockPuidMappingConfig();
            var mockLogger = CreateMockGenevaLogger();
            var mockICosmosClient = CreateMockCosmosClient();
            var factory = CreateMockDistributedIdFactory();
            var data = new List<object[]>
            {
                new object[]
                {
                    null,
                    mockLogger.Object,
                    mockIPuidMapping.Object,
                    factory.Object
                },
                new object[]
                {
                    mockICosmosClient.Object,
                    null,
                    mockIPuidMapping.Object,
                    factory.Object
                },
                new object[]
                {
                    mockICosmosClient.Object,
                    mockLogger.Object,
                    null,
                    factory.Object
                },
                new object[]
                {
                    mockICosmosClient.Object,
                    mockLogger.Object,
                    mockIPuidMapping.Object,
                    null
                }
            };
            return data;
        }

        #endregion

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        [DynamicData(nameof(CosmosAccountCreateWriterTestData), DynamicDataSourceType.Method)]
        public void CosmosAccountCreateWriterNullExceptionHandling(ICosmosClient cosmosClient, ILogger logger, IPuidMappingConfig cosmosConfig, IDistributedIdFactory idFactory)
        {
            //Act
            new CosmosAccountCreateWriter(cosmosClient, logger, cosmosConfig, idFactory);
        }

        [TestInitialize]
        public void Initialize()
        {
            this.cosmosClient = CreateMockCosmosClient();

            this.logger = CreateMockGenevaLogger();

            this.cosmosConfig = CreateMockPuidMappingConfig();

            var mockDistributedId = new Mock<IDistributedId>();
            this.mockDistributedFactory = CreateMockDistributedIdFactory();
            this.mockDistributedFactory
                .Setup(c => c.AcquireIdAsync(It.IsAny<TimeSpan>()))
                .ReturnsAsync(mockDistributedId.Object);

            this.cosmosAccountCreateWriter = new CosmosAccountCreateWriter(
                this.cosmosClient.Object,
                this.logger.Object,
                this.cosmosConfig.Object,
                this.mockDistributedFactory.Object
            );
        }

        [TestMethod]
        public async Task WriteCreatedAccountAsyncSuccess()
        {
            AdapterResponse actualResult = await this.cosmosAccountCreateWriter.WriteCreatedAccountAsync(new AccountCreateInformation()).ConfigureAwait(false);

            Assert.IsTrue(actualResult.IsSuccess);
            Assert.IsNull(actualResult.Error);
        }

        [TestMethod]
        public async Task WriteCreatedAccountsAsyncFail()
        {
            this.mockDistributedFactory.Setup(c => c.AcquireIdAsync(It.IsAny<TimeSpan>())).ReturnsAsync(null);

            var data = new List<AccountCreateInformation>
            {
                new AccountCreateInformation
                {
                    Cid = long.MaxValue,
                    Puid = ulong.MaxValue
                }
            };
            AdapterResponse actualResult = await this.cosmosAccountCreateWriter.WriteCreatedAccountsAsync(data).ConfigureAwait(false);

            Assert.IsFalse(actualResult.IsSuccess);
            Assert.IsNotNull(actualResult.Error);
        }

        [TestMethod]
        public async Task WriteCreatedAccountsAsyncSuccess()
        {
            AdapterResponse actualResult = await this.cosmosAccountCreateWriter.WriteCreatedAccountsAsync(new List<AccountCreateInformation>()).ConfigureAwait(false);

            Assert.IsTrue(actualResult.IsSuccess);
            Assert.IsNull(actualResult.Error);
        }
    }
}
