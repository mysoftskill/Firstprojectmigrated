// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.Vortex
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.ComplianceServices.AnaheimIdLib.Schema;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.AnaheimIdAdapter;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.Membership.MemberServices.Privacy.Core.Vortex;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.PcfAdapter;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    /// <summary>
    ///     Tests for the <see cref="VortexEventService" />
    /// </summary>
    [TestClass]
    public class VortexEventServiceTests
    {
        private readonly Policy policy = Policies.Current;

        private List<IQueueItem<DeviceDeleteRequest>> actualRequests;

        private Mock<ICounterFactory> mockICounterFactory;

        private Mock<IMsaIdentityServiceAdapter> mockIMsaIdentityServiceAdapter;

        private Mock<IPrivacyConfigurationManager> mockIPrivacyConfig;

        private Mock<IVortexEndpointConfiguration> mockIVortexEndpointConfig;

        private Mock<ILogger> mockLogger;

        private Mock<IPcfAdapter> mockPcfAdapter;

        private Mock<IPrivacyExperienceServiceConfiguration> mockPxsConfig;

        private VortexEventService service;

        private Mock<IAnaheimIdAdapter> mockAIdAdapter;

        [TestInitialize]
        public void Init()
        {
            this.mockLogger = GenerateMockGenevaLogger();
            this.mockPcfAdapter = CreateMockPcfAdapter();
            this.mockIMsaIdentityServiceAdapter = CreateMockMsaIdentityServiceAdapter();
            this.mockIMsaIdentityServiceAdapter
                .Setup(c => c.GetGdprDeviceDeleteVerifierAsync(It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<string>()))
                .ReturnsAsync(new AdapterResponse<string> { Result = "i_am_a_device_delete_verifier_token" });

            this.mockICounterFactory = CreateMockCounterFactory();

            this.mockIVortexEndpointConfig = CreateMockVortexEndpointConfiguration();

            this.mockPxsConfig = CreateMockPrivacyExperienceServiceConfiguration(this.mockIVortexEndpointConfig);

            this.mockIPrivacyConfig = CreateMockPrivacyConfigurationManager(this.mockPxsConfig);

            this.mockAIdAdapter = CreateMockAnaheimIdAdapter();

            Mock<IVortexDeviceDeleteQueueManager> queueMock = CreateMockVortexDeviceDeleteQueueManager();
            this.actualRequests = new List<IQueueItem<DeviceDeleteRequest>>();
            queueMock.Setup(q => q.EnqueueAsync(It.IsAny<DeviceDeleteRequest>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask)
                .Callback<DeviceDeleteRequest, TimeSpan?, CancellationToken>(
                    (ddr, ts, ct) =>
                    {
                        this.actualRequests.Add(
                            new FakeQueueItem<DeviceDeleteRequest>
                            {
                                Data = ddr
                            });
                    });

            this.service = new VortexEventService(
                this.mockPcfAdapter.Object,
                queueMock.Object,
                this.policy,
                this.mockIMsaIdentityServiceAdapter.Object,
                this.mockLogger.Object,
                this.mockIPrivacyConfig.Object,
                this.mockICounterFactory.Object,
                new InMemoryRedisClient(),
                this.mockAIdAdapter.Object,
                CreateMockAppConfiguration(true).Object);
        }

        [TestMethod]
        public async Task QueueInvalidEventsAsyncSuccess()
        {
            //Arrange
            var info = new VortexRequestInformation { IsWatchdogRequest = false, WasCompressed = false };
            string json = VortexTestSettings.BadJsonEvent;

            //Act
            ServiceResponse results = await this.service.QueueValidEventsAsync(Encoding.UTF8.GetBytes(json), info).ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(results);
            Assert.IsFalse(results.IsSuccess);
            Assert.AreEqual(ErrorCode.InvalidInput.ToString(), results.Error.Code);
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public async Task QueueValidEventsAsyncSuccess(bool isWatchdogRequest, bool wasCompressed)
        {
            //Arrange
            var info = new VortexRequestInformation { IsWatchdogRequest = isWatchdogRequest, WasCompressed = wasCompressed };

            //Act
            ServiceResponse results = await this.service.QueueValidEventsAsync(Encoding.UTF8.GetBytes(VortexTestSettings.JsonEvents), info).ConfigureAwait(false);

            //Assert
            Assert.IsTrue(results.IsSuccess);
            Assert.IsNull(results.Error);
            Assert.IsNotNull(results);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        [DynamicData(nameof(VortexEventServiceConstructorTestData), DynamicDataSourceType.Method)]
        public void VortexEventServiceNullHandlingSuccess(
            IPcfAdapter pcfAdapter,
            IVortexDeviceDeleteQueueManager deviceDeleteRequestQueue,
            Policy policy,
            IMsaIdentityServiceAdapter msaIdentityServiceAdapter,
            ILogger logger,
            IPrivacyConfigurationManager config,
            ICounterFactory counterFactory,
            IRedisClient redisClient,
            IAnaheimIdAdapter anaheimIdAdapter,
            IAppConfiguration appConfiguration)
        {
            new VortexEventService(pcfAdapter, deviceDeleteRequestQueue, policy, msaIdentityServiceAdapter, logger, config, counterFactory, redisClient, anaheimIdAdapter, appConfiguration);
        }

        /// <summary>
        ///     Verify that the <see cref="VortexEventService" /> handles parsing events
        /// </summary>
        /// <returns>Test task</returns>
        [DataTestMethod]
        [DynamicData(nameof(VortexEventServiceShouldHandEventsTestData), DynamicDataSourceType.Method)]
        public async Task VortexEventServiceShouldHandleEvents(string json, int eventsProcessed, int timeBetweenRequestsLimitMinutes)
        {
            Policy policy = Policies.Current;
            DataTypes.KnownIds ids = policy.DataTypes.Ids;
            VortexTestSettings.CreateSignalWriterMocks(
                ids,
                out Mock<IPcfAdapter> pcfAdapter,
                out Mock<IMsaIdentityServiceAdapter> msaIdentityServiceAdapter);

            var vortexConfigMock = new Mock<IVortexEndpointConfiguration>(MockBehavior.Strict);
            vortexConfigMock.Setup(vec => vec.MaxTimeoutCacheCount).Returns(100);
            vortexConfigMock.Setup(vec => vec.TimeBetweenUserRequestsLimitMinutes).Returns(timeBetweenRequestsLimitMinutes);
            vortexConfigMock.Setup(vec => vec.TimeBetweenNonUserRequestsLimitMinutes).Returns(timeBetweenRequestsLimitMinutes);

            var configMock = new Mock<IPrivacyExperienceServiceConfiguration>(MockBehavior.Strict);
            configMock.Setup(c => c.VortexEndpointConfiguration).Returns(vortexConfigMock.Object);

            var configManagerMock = new Mock<IPrivacyConfigurationManager>(MockBehavior.Strict);
            configManagerMock.Setup(c => c.PrivacyExperienceServiceConfiguration).Returns(configMock.Object);

            var queueMock = new Mock<IVortexDeviceDeleteQueueManager>(MockBehavior.Strict);

            var counterFactoryMock = new Mock<ICounterFactory>(MockBehavior.Strict);
            counterFactoryMock.Setup(c => c.GetCounter(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CounterType>()))
                .Returns(new Mock<ICounter>().Object);

            var actualRequests = new List<IQueueItem<DeviceDeleteRequest>>();
 
            queueMock.Setup(q => q.EnqueueAsync(It.IsAny<DeviceDeleteRequest>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask)
                .Callback<DeviceDeleteRequest, TimeSpan?, CancellationToken>(
                    (ddr, ts, ct) =>
                    {
                        actualRequests.Add(
                            new FakeQueueItem<DeviceDeleteRequest>
                            {
                                Data = ddr
                            });
                    });

            var appConfig = CreateMockAppConfiguration(true);

            IVortexEventService service = new VortexEventService(
                pcfAdapter.Object,
                queueMock.Object,
                policy,
                msaIdentityServiceAdapter.Object,
                new ConsoleLogger(),
                configManagerMock.Object,
                counterFactoryMock.Object,
                new InMemoryRedisClient(),
                this.mockAIdAdapter.Object,
                appConfig.Object);
            var info = new VortexRequestInformation { IsWatchdogRequest = false, WasCompressed = false };

            for (int x = 0; x < 2; ++x)
            {
                // First call should succeed.
                // Second call is expected to no-op due to dedup logic (unless timeBetweenRequestsLimitMinutes is set to 0).
                ServiceResponse response = await service.QueueValidEventsAsync(Encoding.UTF8.GetBytes(json), info).ConfigureAwait(false);
                Assert.IsTrue(response.IsSuccess, response.Error?.Message);

                IEnumerable<ServiceResponse<IQueueItem<DeviceDeleteRequest>>> results = (await service.DeleteDevicesAsync(actualRequests).ConfigureAwait(false)).ToList();
                Assert.IsTrue(results.All(r => r.IsSuccess), results.FirstOrDefault(r => !r.IsSuccess)?.Error?.Message);

                actualRequests.Clear();

                // have a small wait to increase the stability of the test
                Thread.Sleep(500);
            }

            // Check for exact number of calls to partners
            VortexTestSettings.VerifyDeleteRequestProcessed(pcfAdapter, msaIdentityServiceAdapter, ids, eventsProcessed: eventsProcessed);
        }

        private class FakeQueueItem<T> : IQueueItem<T>
        {
            public T Data { get; set; }

            public int DequeueCount { get; set; }

            public DateTimeOffset? ExpirationTime { get; set; }

            /// <inheritdoc />
            public string Id { get; }

            public DateTimeOffset? InsertionTime { get; set; }

            public DateTimeOffset? NextVisibleTime { get; set; }

            /// <inheritdoc />
            public string PopReceipt { get; }

            public Task CompleteAsync()
            {
                throw new NotImplementedException();
            }

            public Task ReleaseAsync()
            {
                throw new NotImplementedException();
            }

            public Task<bool> RenewLeaseAsync(TimeSpan duration)
            {
                throw new NotImplementedException();
            }

            /// <inheritdoc />  
            public Task<bool> UpdateAsync(TimeSpan leaseDuration)
            {
                throw new NotImplementedException();
            }
        }

        #region Test Data

        public static IEnumerable<object[]> VortexEventServiceShouldHandEventsTestData()
        {
            return new List<object[]>
            {
                new object[]
                {
                    VortexTestSettings.JsonEvents,
                    1,
                    10
                },
                new object[]
                {
                    VortexTestSettings.CreateEventsString(VortexTestSettings.BadJsonEvent),
                    0,
                    10
                },
                new object[]
                {
                    VortexTestSettings.CreateEventsString(VortexTestSettings.JsonEvent, VortexTestSettings.BadJsonEvent),
                    1,
                    10
                },
                new object[]
                {
                    VortexTestSettings.CreateEventsString(VortexTestSettings.LegacyJsonEvent),
                    1,
                    10
                },
                new object[]
                {
                    VortexTestSettings.JsonEvents,
                    2,
                    0
                },
            };
        }

        public static IEnumerable<object[]> VortexEventServiceConstructorTestData()
        {
            Mock<IVortexEndpointConfiguration> mockIVortextEndpointConfig = CreateMockVortexEndpointConfiguration();
            Mock<IPrivacyExperienceServiceConfiguration> mockPxsConfig = CreateMockPrivacyExperienceServiceConfiguration(mockIVortextEndpointConfig);
            Mock<IPcfAdapter> mockPcfAdapter = CreateMockPcfAdapter();
            Mock<IVortexDeviceDeleteQueueManager> mockIVortextDeviceDeleteQueueManager = CreateMockVortexDeviceDeleteQueueManager();
            Mock<IMsaIdentityServiceAdapter> mockMsaIdentity = CreateMockMsaIdentityServiceAdapter();
            Mock<IPrivacyConfigurationManager> mockIPrivacyConfig = CreateMockPrivacyConfigurationManager(mockPxsConfig);
            Mock<ILogger> mockILogger = GenerateMockGenevaLogger();
            Mock<ICounterFactory> mockICounterFactory = CreateMockCounterFactory();
            Mock<IRedisClient> mockRedisClient = new Mock<IRedisClient>(MockBehavior.Strict);
            Mock<IAnaheimIdAdapter> mockAIdAdapter = CreateMockAnaheimIdAdapter();
            Mock<IAppConfiguration> mockAppConfig = CreateMockAppConfiguration(true);

            var data = new List<object[]>
            {
                new object[]
                {
                    null,
                    mockIVortextDeviceDeleteQueueManager.Object,
                    Policies.Current,
                    mockMsaIdentity.Object,
                    mockILogger.Object,
                    mockIPrivacyConfig.Object,
                    mockICounterFactory.Object,
                    mockRedisClient.Object,
                    mockAIdAdapter.Object,
                    mockAppConfig.Object
                },
                new object[]
                {
                    mockPcfAdapter.Object,
                    null,
                    Policies.Current,
                    mockMsaIdentity.Object,
                    mockILogger.Object,
                    mockIPrivacyConfig.Object,
                    mockICounterFactory.Object,
                    mockRedisClient.Object,
                    mockAIdAdapter.Object,
                    mockAppConfig.Object
                },
                new object[]
                {
                    mockPcfAdapter.Object,
                    mockIVortextDeviceDeleteQueueManager.Object,
                    Policies.Current,
                    null,
                    mockILogger.Object,
                    mockIPrivacyConfig.Object,
                    mockICounterFactory.Object,
                    mockRedisClient.Object,
                    mockAIdAdapter.Object,
                    mockAppConfig.Object
                },
                new object[]
                {
                    mockPcfAdapter.Object,
                    mockIVortextDeviceDeleteQueueManager.Object,
                    Policies.Current,
                    mockMsaIdentity.Object,
                    null,
                    mockIPrivacyConfig.Object,
                    mockICounterFactory.Object,
                    mockRedisClient.Object,
                    mockAIdAdapter.Object,
                    mockAppConfig.Object
                },
                new object[]
                {
                    mockPcfAdapter.Object,
                    mockIVortextDeviceDeleteQueueManager.Object,
                    Policies.Current,
                    mockMsaIdentity.Object,
                    mockILogger.Object,
                    null,
                    mockICounterFactory.Object,
                    mockRedisClient.Object,
                    mockAIdAdapter.Object,
                    mockAppConfig.Object
                },
                new object[]
                {
                    mockPcfAdapter.Object,
                    mockIVortextDeviceDeleteQueueManager.Object,
                    Policies.Current,
                    mockMsaIdentity.Object,
                    mockILogger.Object,
                    mockIPrivacyConfig.Object,
                    null,
                    mockRedisClient.Object,
                    mockAIdAdapter.Object,
                    mockAppConfig.Object
                },
                new object[]
                {
                    mockPcfAdapter.Object,
                    mockIVortextDeviceDeleteQueueManager.Object,
                    Policies.Current,
                    mockMsaIdentity.Object,
                    mockILogger.Object,
                    mockIPrivacyConfig.Object,
                    mockICounterFactory.Object,
                    null,
                    mockAIdAdapter.Object,
                    mockAppConfig.Object
                },
                new object[]
                {
                    mockPcfAdapter.Object,
                    mockIVortextDeviceDeleteQueueManager.Object,
                    Policies.Current,
                    mockMsaIdentity.Object,
                    mockILogger.Object,
                    mockIPrivacyConfig.Object,
                    mockICounterFactory.Object,
                    mockRedisClient.Object,
                    null,
                    mockAppConfig.Object
                },
                new object[]
                {
                    mockPcfAdapter.Object,
                    mockIVortextDeviceDeleteQueueManager.Object,
                    Policies.Current,
                    mockMsaIdentity.Object,
                    mockILogger.Object,
                    mockIPrivacyConfig.Object,
                    mockICounterFactory.Object,
                    mockRedisClient.Object,
                    mockAIdAdapter.Object,
                    null
                }
            };
            return data;
        }

        private static Mock<IPcfAdapter> CreateMockPcfAdapter()
        {
            return new Mock<IPcfAdapter>(MockBehavior.Strict);
        }

        private static Mock<IVortexDeviceDeleteQueueManager> CreateMockVortexDeviceDeleteQueueManager()
        {
            return new Mock<IVortexDeviceDeleteQueueManager>(MockBehavior.Strict);
        }

        private static Mock<IMsaIdentityServiceAdapter> CreateMockMsaIdentityServiceAdapter()
        {
            return new Mock<IMsaIdentityServiceAdapter>(MockBehavior.Strict);
        }

        private static Mock<ILogger> GenerateMockGenevaLogger()
        {
            return new Mock<ILogger>();
        }

        private static Mock<IPrivacyConfigurationManager> CreateMockPrivacyConfigurationManager(Mock<IPrivacyExperienceServiceConfiguration> mockPxsConfig)
        {
            var mockConfig = new Mock<IPrivacyConfigurationManager>(MockBehavior.Strict);
            mockConfig.Setup(c => c.PrivacyExperienceServiceConfiguration).Returns(mockPxsConfig.Object);
            return mockConfig;
        }

        private static Mock<ICounterFactory> CreateMockCounterFactory()
        {
            var mockICounterFactory = new Mock<ICounterFactory>(MockBehavior.Strict);
            mockICounterFactory.Setup(c => c.GetCounter(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CounterType>()))
                .Returns(new Mock<ICounter>().Object);
            return mockICounterFactory;
        }

        private static Mock<IVortexEndpointConfiguration> CreateMockVortexEndpointConfiguration()
        {
            var mockIPrivacy = new Mock<IVortexEndpointConfiguration>(MockBehavior.Strict);
            mockIPrivacy.Setup(vec => vec.MaxTimeoutCacheCount).Returns(100);
            mockIPrivacy.Setup(vec => vec.TimeBetweenUserRequestsLimitMinutes).Returns(10);
            mockIPrivacy.Setup(vec => vec.TimeBetweenNonUserRequestsLimitMinutes).Returns(10);
            return mockIPrivacy;
        }

        private static Mock<IPrivacyExperienceServiceConfiguration> CreateMockPrivacyExperienceServiceConfiguration(
            Mock<IVortexEndpointConfiguration> mockIVortexEndpointConfig)
        {
            var mockIPxsConfig = new Mock<IPrivacyExperienceServiceConfiguration>(MockBehavior.Strict);
            mockIPxsConfig.Setup(c => c.VortexEndpointConfiguration).Returns(mockIVortexEndpointConfig.Object);
            return mockIPxsConfig;
        }


        private static Mock<IAnaheimIdAdapter> CreateMockAnaheimIdAdapter()
        {
            var mockAIdAdapter = new Mock<IAnaheimIdAdapter>(MockBehavior.Strict);
            mockAIdAdapter.Setup(a => a.SendDeleteDeviceIdRequestAsync(It.IsAny<DeleteDeviceIdRequest>())).ReturnsAsync(new AdapterResponse());
            return mockAIdAdapter;
        }

        private static Mock<IAppConfiguration> CreateMockAppConfiguration(bool isFeatureEnabled)
        {
            var mockAppConfig = new Mock<IAppConfiguration>();
            mockAppConfig.Setup(c => c.IsFeatureFlagEnabledAsync(FeatureNames.PXS.DeleteDeviceRequestEnabled, true)).ReturnsAsync(isFeatureEnabled);
            return mockAppConfig;
        }

        #endregion
    }
}
