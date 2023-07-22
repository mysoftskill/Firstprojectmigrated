// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.QuickExportWorker.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.DataContracts.ExportTypes;
    using Microsoft.Membership.MemberServices.Privacy.QuickExportWorker.CsvSerializer;
    using Microsoft.Membership.MemberServices.Privacy.QuickExportWorker.DataProcessingAgents;
    using Microsoft.Membership.MemberServices.PrivacyAdapters;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class PdApiDataResourceAgentTests
    {
        private Mock<ICounterFactory> counterFactoryMock;
        private Mock<IPxfDispatcher> pxfDispatcherMock;
        private Mock<ISerializer> serializerMock;
        private Mock<IPrivacyExportConfiguration> exportConfigMock;
        private ILogger logger;

        private PdApiDataResourceAgent resourceAgent;

        [TestInitialize]
        public void Init()
        {
            this.pxfDispatcherMock = new Mock<IPxfDispatcher>(MockBehavior.Loose);
            this.pxfDispatcherMock.Setup(
                c => c.GetAdaptersForResourceType(It.IsAny<IPxfRequestContext>(), It.IsAny<ResourceType>(), It.IsAny<PxfAdapterCapability>()))
                .Returns(new List<PartnerAdapter>());

            var configManagerMock = new Mock<IPrivacyConfigurationManager>(MockBehavior.Loose);
            var configurationMock = new Mock<IPrivacyExperienceServiceConfiguration>(MockBehavior.Loose);
            this.exportConfigMock = new Mock<IPrivacyExportConfiguration>(MockBehavior.Loose);
            var retryManagerMock = new Mock<IRetryStrategyConfiguration>(MockBehavior.Loose);
            var retryConfigurationMock = new Mock<IFixedIntervalRetryConfiguration>(MockBehavior.Loose);

            configManagerMock.Setup(c => c.PrivacyExperienceServiceConfiguration).Returns(configurationMock.Object);
            configurationMock.Setup(c => c.PrivacyExportConfiguration).Returns(this.exportConfigMock.Object);
            this.exportConfigMock.Setup(c => c.RetryStrategy).Returns(retryManagerMock.Object);
            retryManagerMock.Setup(c => c.RetryMode).Returns(RetryMode.FixedInterval);
            retryManagerMock.Setup(c => c.FixedIntervalRetryConfiguration).Returns(retryConfigurationMock.Object);
            retryConfigurationMock.Setup(c => c.RetryCount).Returns(1);
            retryConfigurationMock.Setup(c => c.RetryIntervalInMilliseconds).Returns(1);

            this.counterFactoryMock = new Mock<ICounterFactory>(MockBehavior.Loose);
            this.counterFactoryMock
                .Setup(c => c.GetCounter(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CounterType>()))
                .Returns(new Mock<ICounter>(MockBehavior.Loose).Object);
            this.logger = new ConsoleLogger();

            this.serializerMock = new Mock<ISerializer>(MockBehavior.Loose);

            resourceAgent = new PdApiDataResourceAgent(
                pxfDispatcherMock.Object, 
                counterFactoryMock.Object, 
                this.serializerMock.Object, 
                configManagerMock.Object, 
                this.logger);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task ProcessExportAsyncThrowsForInvalidUserId()
        {
            var stagingHelper = new Mock<IExportStagingStorageHelper>(MockBehavior.Strict);

            ExportStatusRecord statusRecord = new ExportStatusRecord("export1")
            {
                UserId = "notANumber",
            };
            ExportDataResourceStatus resourceStatus = new ExportDataResourceStatus();
            await this.resourceAgent.ProcessExportAsync(statusRecord, resourceStatus, stagingHelper.Object);
        }

        [TestMethod]
        public async Task ProcessExportAsyncDoesNotWriteIfNoBeaconOrLocationData()
        {
            await ProcessExportCore(addLocationData: false, addBeaconData: false, isBeaconDataWritten: false);
        }

        [TestMethod]
        public async Task ProcessExportAsyncWritesLocationData()
        {
            await ProcessExportCore(addLocationData: true, addBeaconData: false, isBeaconDataWritten: false);
        }

        [TestMethod]
        public async Task ProcessExportAsyncWritesBeaconAndLocationData()
        {
            await ProcessExportCore(addLocationData: true, addBeaconData: true, isBeaconDataWritten: true);
        }

        [TestMethod]
        public async Task ProcessExportAsyncWritesBeaconDataAsync()
        {
            await ProcessExportCore(addLocationData: false, addBeaconData: true, isBeaconDataWritten: true);
        }

        private async Task ProcessExportCore(bool addLocationData, bool addBeaconData, bool isBeaconDataWritten)
        {
            var stagingHelper = new Mock<IExportStagingStorageHelper>();
            var stagingFile1 = new Mock<IExportStagingFile>();
            var stagingFileForUserVisitLocation = new Mock<IExportStagingFile>();
            var stagingFileForRawUserLocation = new Mock<IExportStagingFile>();

            var locationAdapterMock = new Mock<IPxfAdapter>();
            var beaconAdapterMock = new Mock<IPxfAdapter>();

            stagingHelper.Setup(
                c => c.GetStagingFile($"{Policies.Current.DataTypes.Ids.PreciseUserLocation.Value}.csv"))
                .Returns(stagingFile1.Object);
            stagingHelper.Setup(
                c => c.GetStagingFile(PdApiDataResourceAgent.UserVisitLocationsFileName))
                .Returns(stagingFileForUserVisitLocation.Object);
            stagingHelper.Setup(
                c => c.GetStagingFile(PdApiDataResourceAgent.RawUserLocationsFileName))
                .Returns(stagingFileForRawUserLocation.Object);

            var header = "DateTime, DeviceId, Latitude, Longitude, Name";
            this.serializerMock.Setup(c => c.WriteHeader(It.IsAny<IEnumerable<string>>())).Returns(header);

            stagingFileForUserVisitLocation.Setup(c => c.AddBlockAsync(header)).Returns(Task.CompletedTask);
            stagingFileForRawUserLocation.Setup(c => c.AddBlockAsync(header)).Returns(Task.CompletedTask);

            var locationAdapter = new PartnerAdapter
            {
                Adapter = locationAdapterMock.Object,
                PartnerId = "pd",
                RealTimeDelete = false,
                RealTimeView = true,
            };

            var beaconAdapter = new PartnerAdapter
            {
                Adapter = beaconAdapterMock.Object,
                PartnerId = "beacon",
                RealTimeDelete = false,
                RealTimeView = true,
            };

            this.pxfDispatcherMock.Setup(c => c.GetAdaptersForResourceType(
                It.IsAny<IPxfRequestContext>(),
                ResourceType.Location,
                PxfAdapterCapability.View)).Returns(new List<PartnerAdapter> { locationAdapter });

            this.pxfDispatcherMock.Setup(c => c.GetAdaptersForResourceType(
                It.IsAny<IPxfRequestContext>(),
                ResourceType.LocationTransit,
                PxfAdapterCapability.View)).Returns(new List<PartnerAdapter> { beaconAdapter });

            var content = "12/12/2012, dev1, 765, 12345, name1";
            this.serializerMock.Setup(c => c.ConvertResource(It.IsAny<Resource>())).Returns(content);

            var locationResponse = new PagedResponse<LocationResource>()
            {
                Items = null,
                NextLink = null
            };

            var beaconResponse = new PagedResponse<LocationResource>()
            {
                Items = null,
                NextLink = null
            };

            if (addLocationData)
            {
                locationResponse = new PagedResponse<LocationResource>()
                {
                    Items = new List<LocationResource>()
                {
                    new LocationResource()
                    {
                        DateTime = DateTimeOffset.UtcNow,
                        DeviceId = "dev1",
                        AccuracyRadius = 123,
                        Longitude = 123,
                        Latitude = 12,
                        LocationType = LocationType.Device
                    }
                },
                    NextLink = null
                };
            }

            if (addBeaconData)
            {
                beaconResponse = new PagedResponse<LocationResource>()
                {
                    Items = new List<LocationResource>()
                {
                    new LocationResource()
                    {
                        DateTime = DateTimeOffset.UtcNow,
                        DeviceId = "dev1",
                        AccuracyRadius = 123,
                        Longitude = 123,
                        Latitude = 12,
                        LocationType = LocationType.Device
                    }
                },
                    NextLink = null
                };
            }

            locationAdapterMock.Setup(c => c.GetLocationHistoryAsync(
                It.IsAny<IPxfRequestContext>(),
                OrderByType.DateTime,
                DateOption.Between,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>())).Returns(Task.FromResult(locationResponse));

            beaconAdapterMock.Setup(c => c.GetLocationHistoryAsync(
                It.IsAny<IPxfRequestContext>(),
                OrderByType.DateTime,
                DateOption.Between,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>())).Returns(Task.FromResult(beaconResponse));

            ExportStatusRecord statusRecord = new ExportStatusRecord("export1")
            {
                UserId = "12345",
                ExportId = "234",
                Ticket = "ticket"
            };

            var resourceStatus = new ExportDataResourceStatus()
            {
                ResourceDataType = Policies.Current.DataTypes.Ids.PreciseUserLocation.Value
            };

            stagingFileForUserVisitLocation.Setup(c => c.AddBlockAsync(content)).Returns(Task.CompletedTask).Verifiable();
            stagingFileForUserVisitLocation.Setup(c => c.CommitAsync()).Returns(Task.CompletedTask).Verifiable();
            stagingFileForRawUserLocation.Setup(c => c.AddBlockAsync(content)).Returns(Task.CompletedTask).Verifiable();
            stagingFileForRawUserLocation.Setup(c => c.CommitAsync()).Returns(Task.CompletedTask).Verifiable();

            await this.resourceAgent.ProcessExportAsync(statusRecord, resourceStatus, stagingHelper.Object);

            stagingFileForUserVisitLocation.Verify(c => c.CommitAsync(), addLocationData ? Times.Once() : Times.Never());

            stagingFileForRawUserLocation.Verify(c => c.CommitAsync(), isBeaconDataWritten ? Times.Once() : Times.Never());

            Assert.IsTrue(resourceStatus.IsComplete);
        }
    }
}
