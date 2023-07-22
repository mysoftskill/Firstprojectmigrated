// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.UnitTests.EventHubProcessor
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.SecretStore;
    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.EventHubProcessor;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Microsoft.PrivacyServices.Common.Azure;

    [TestClass]
    public class EventHubHelpersTests : SharedTestFunctions
    {
        private EventHubHelpers eventHubHelper;

        private Mock<IAadAuthenticationHelper> mockAadAuthHelper;

        private Mock<ILogger> mockLogger;

        private Mock<ISecretStoreReader> mockSecretStoreReader;

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task GetAzureStorageConnectionStringTest(bool useEmulator)
        {
            this.CreateEventHubHelper(true, true, useEmulator);
            var result = await this.eventHubHelper.GetAzureStorageConnectionStringAsync().ConfigureAwait(false);
            Assert.IsNotNull(result);
            if (useEmulator)
            {
                Assert.AreEqual(result, "UseDevelopmentStorage=true;");
            }
        }

        [TestInitialize]
        public void Initialize()
        {
            this.mockAadAuthHelper = new Mock<IAadAuthenticationHelper>();
            this.mockAadAuthHelper
                .Setup(c => c.GetAccessTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult("dummytoken"));

            this.mockSecretStoreReader = new Mock<ISecretStoreReader>();

            this.mockLogger = CreateMockGenevaLogger();
        }

        [TestMethod]
        [DynamicData(nameof(CreateConstructorTestData), DynamicDataSourceType.Method)]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldHandleConstructorException(
            IPrivacyConfigurationManager config,
            ISecretStoreReader secretStoreReader,
            ILogger logger)
        {
            try
            {
                this.eventHubHelper = new EventHubHelpers(
                    config,
                    this.mockAadAuthHelper.Object,
                    secretStoreReader,
                    logger);

                Assert.Fail("Should have thrown exception");
            }
            catch (ArgumentNullException)
            {
                throw;
            }
        }

        private void CreateEventHubHelper(
            bool setupPxsConfig = false,
            bool setupAadAccountCloseConfig = false,
            bool useEmulator = false,
            bool authKeyEncryptedFilePath = true)
        {
            var privacyConfig = CreateMockPrivacyConfigManager(setupPxsConfig, setupAadAccountCloseConfig, useEmulator);
            this.eventHubHelper = new EventHubHelpers(
                privacyConfig.Object,
                this.mockAadAuthHelper.Object,
                this.mockSecretStoreReader.Object,
                this.mockLogger.Object);
        }

        private static IEnumerable<object[]> CreateConstructorTestData()
        {
            var mockPrivacyConfig = CreateMockPrivacyConfigManager();
            var mockPrivacyConfig2 = CreateMockPrivacyConfigManager(true, true);
            var mockSecretStoreReader = new Mock<ISecretStoreReader>();
            var mockLogger = CreateMockGenevaLogger();

            return new List<object[]>
            {
                new object[]
                {
                    mockPrivacyConfig.Object,
                    mockSecretStoreReader.Object,
                    mockLogger.Object
                },

                new object[]
                {
                    mockPrivacyConfig2.Object,
                    null,
                    mockLogger.Object
                },
                new object[]
                {
                    mockPrivacyConfig2.Object,
                    mockSecretStoreReader.Object,
                    null
                }
            };
        }
    }
}
