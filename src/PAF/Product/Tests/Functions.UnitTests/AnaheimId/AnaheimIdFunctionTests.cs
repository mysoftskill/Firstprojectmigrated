namespace Functions.UnitTests.AnaheimId
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Core;
    using Azure.Storage.Queues;
    using Azure.Storage.Queues.Models;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.IfxMetric;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Timers;
    using Microsoft.Cloud.InstrumentationFramework.Metrics.Extensions;
    using Microsoft.ComplianceServices.AnaheimIdLib.Schema;
    using Microsoft.ComplianceServices.Common.Queues;
    using Microsoft.PrivacyServices.AnaheimId.AidFunctions;
    using Microsoft.PrivacyServices.AnaheimId.Avro;
    using Microsoft.PrivacyServices.AnaheimId.Config;
    using Microsoft.PrivacyServices.AnaheimId.Icm;
    using Microsoft.PrivacyServices.AzureFunctions.Core;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    /// <summary>
    /// AnaheimId Queue Azure Function Container.
    /// </summary>
    [TestClass]
    public class AnaheimIdFunctionTests
    {
        private AnaheimIdFunction anaheimIdFunction;

        private Mock<IAidFunctionsFactory> mockAidFunctionsFactory;

        private Mock<IAidBlobStorageFunc> mockAidBlobStorageFunc;

        private AidTelemetryFunc aidTelemetryFunc;

        private Mock<ILogger> mockLogger;

        private Mock<TimerInfo> mockTimerInfo;

        private Mock<IAppConfiguration> mockAppConfig;

        private Mock<IAidConfig> mockAidConfig;

        private Mock<IMetricContainer> mockMetricContainer;

        [TestInitialize]
        public void Init()
        {
            this.mockAppConfig = new Mock<IAppConfiguration>();

            // Set up Aid config
            var mockQueueAccountInfo = new QueueAccountInfo();
            mockQueueAccountInfo.StorageAccountName = "StorageAccountName";
            mockQueueAccountInfo.QueueName = "QueueName";
            var mockAidMonitoringQueuesStorageAccounts = new List<QueueAccountInfo>();
            mockAidMonitoringQueuesStorageAccounts.Add(mockQueueAccountInfo);
            this.mockAidConfig = new Mock<IAidConfig>();
            this.mockAidConfig.SetupGet(x => x.AidMonitoringQueuesStorageAccounts).Returns(mockAidMonitoringQueuesStorageAccounts);

            this.mockLogger = new Mock<ILogger>();

            this.mockTimerInfo = new Mock<TimerInfo>(It.IsAny<TimerSchedule>(), It.IsAny<ScheduleStatus>(), false);

            // Setting metric should always return true
            this.mockMetricContainer = new Mock<IMetricContainer>();
            this.mockMetricContainer.Setup(mock => mock.IncomingMetric.Set(It.IsAny<DateTime>(), It.IsAny<ulong>(), It.IsAny<DimensionValues4D>())).Returns(true);
            this.mockMetricContainer.Setup(mock => mock.IncomingApiErrorCountMetric.Set(It.IsAny<DateTime>(), It.IsAny<ulong>(), It.IsAny<DimensionValues4D>())).Returns(true);
            this.mockMetricContainer.Setup(mock => mock.IncomingSuccessLatencyMetric.Set(It.IsAny<DateTime>(), It.IsAny<ulong>(), It.IsAny<DimensionValues3D>())).Returns(true);
            this.mockMetricContainer.Setup(mock => mock.OutgoingMetric.Set(It.IsAny<DateTime>(), It.IsAny<ulong>(), It.IsAny<DimensionValues5D>())).Returns(true);
            this.mockMetricContainer.Setup(mock => mock.OutgoingApiErrorCountMetric.Set(It.IsAny<DateTime>(), It.IsAny<ulong>(), It.IsAny<DimensionValues5D>())).Returns(true);
            this.mockMetricContainer.Setup(mock => mock.OutgoingSuccessLatencyMetric.Set(It.IsAny<DateTime>(), It.IsAny<ulong>(), It.IsAny<DimensionValues4D>())).Returns(true);
            var mockCustomMetricDictionary = new Dictionary<string, IMetric>();
            mockCustomMetricDictionary.Add("PAF.FunctionAnaheimQueueDepth", new Mock<IMetric>().Object);
            this.mockMetricContainer.Setup(mock => mock.CustomMetricDictionary).Returns(mockCustomMetricDictionary);

            this.mockAidBlobStorageFunc = new Mock<IAidBlobStorageFunc>();
            this.mockAidBlobStorageFunc.Setup(mock => mock.Run(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<ILogger>()));

            var mockAidQueueMonitoringClient = new Mock<IAidQueueMonitoringClient>();
            mockAidQueueMonitoringClient.Setup(mock => mock.GetQueueName()).Returns(mockQueueAccountInfo.QueueName);
            mockAidQueueMonitoringClient.Setup(mock => mock.GetStorageAccountName()).Returns(mockQueueAccountInfo.StorageAccountName);
            mockAidQueueMonitoringClient.Setup(mock => mock.GetQueueSizeAsync()).Returns(Task.FromResult(1));
            var aidQueueMonitoringClientList = new List<IAidQueueMonitoringClient>();
            aidQueueMonitoringClientList.Add(mockAidQueueMonitoringClient.Object);
            this.aidTelemetryFunc = new AidTelemetryFunc(aidQueueMonitoringClientList, this.mockMetricContainer.Object);
            this.mockAidFunctionsFactory = new Mock<IAidFunctionsFactory>();
            this.mockAidFunctionsFactory.Setup(mock => mock.GetAidBlobStorageFunc()).Returns(this.mockAidBlobStorageFunc.Object);
            this.mockAidFunctionsFactory.Setup(mock => mock.GetAidTelemetryFunc()).Returns(this.aidTelemetryFunc);

            this.anaheimIdFunction = new AnaheimIdFunction(
                this.mockAidFunctionsFactory.Object,
                this.mockAppConfig.Object,
                this.mockMetricContainer.Object,
                this.mockLogger.Object);
        }

        /// <summary>
        /// Test AnaheimIdBlobFunction.
        /// The blob storage function should run on all new missing signal blob files in a container.
        /// </summary>
        /// <param name="filename">The link to the test file.</param>
        /// <param name="runFunction">The boolean to determine whether the function should be run.</param>
        [DataTestMethod]
        [DataRow("MissingSignal_2021-11-23_22_34_16.avro", true)]
        [DataRow("PartialFile_2021-11-23_22_34_16-00004.avro", false)]
        [DataRow("missingsignal_2021-11-23_22_34_16-00004.avro.test", false)]
        [DataRow("MissingSignal_2021-11-23_22_34_16-00004.avro.test", false)]
        public void AnaheimIdBlobFunctionTest(string filename, bool runFunction)
        {
            // Run the Aid Blob Storage function call with the mocked stream
            // Note: This call is triggered by the azure function service when a new file is created in a blob.
            Mock<Stream> mockStream = new Mock<Stream>();
            this.anaheimIdFunction.RunAidBlobStorage(mockStream.Object, filename);

            // The boolean checks whether the Anaheim blob storage function has been run
            // The function should only be run for blobs with "MissingSignal" in the name.
            if (runFunction)
            {
                this.mockAidBlobStorageFunc.Verify(mock => mock.Run(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<ILogger>()), Times.Once());
            }
            else
            {
                this.mockAidBlobStorageFunc.Verify(mock => mock.Run(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<ILogger>()), Times.Never());
            }
        }

        /// <summary>
        /// Test RunTelemetryTimerAsync, timer null should throw.
        /// </summary>
        [ExpectedException(typeof(NullReferenceException))]
        [TestMethod]
        public void AnaheimIdTelemetryTimerNullFunctionTest()
        {
            try
            {
                this.anaheimIdFunction.RunTelemetryTimerAsync(null);
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Object reference not set to an instance of an object.", ex.Message);
                throw;
            }

            Assert.Fail("Exception should have been thrown.");
        }

        /// <summary>
        /// Test RunTelemetryTimerAsync, IAidQueueMonitoringClient list null should throw.
        /// </summary>
        [ExpectedException(typeof(ArgumentNullException))]
        [TestMethod]
        public void AnaheimIdTelemetryTimerFunctionAidQueueMonitoringClientListNullTest()
        {
            try
            {
                var aidTelemetry = new AidTelemetryFunc(null, this.mockMetricContainer.Object);
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Value cannot be null. (Parameter 'aidQueueMonitoringClientList')", ex.Message);
                throw;
            }

            Assert.Fail("Exception should have been thrown.");
        }

        /// <summary>
        /// Test RunTelemetryTimerAsync, AidQueueMonitoringClient list empty should throw.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [ExpectedException(typeof(ArgumentException))]
        [TestMethod]
        public async Task AnaheimIdTelemetryTimerFunctionAidQueueMonitoringClientListEmptyTest()
        {
            try
            {
                var mockList = new List<IAidQueueMonitoringClient>();
                var aidTelemetry = new AidTelemetryFunc(mockList, this.mockMetricContainer.Object);
                await aidTelemetry.RunAsync(this.mockLogger.Object);
            }
            catch (Exception ex)
            {
                Assert.AreEqual("AidTelemetryFunc (Parameter 'aidQueueMonitoringClientList empty')", ex.Message);
                throw;
            }

            Assert.Fail("Exception should have been thrown.");
        }

        /// <summary>
        /// Test RunTelemetryTimerAsync, Queue Account Info empty should throw.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [ExpectedException(typeof(ArgumentException))]
        [DataTestMethod]
        [DataRow(nameof(QueueAccountInfo.QueueName))]
        [DataRow(nameof(QueueAccountInfo.StorageAccountName))]
        public async Task AnaheimIdTelemetryTimerFunctionQueueAccountInfoEmptyTest(string param)
        {
            try
            {
                var mockList = new List<IAidQueueMonitoringClient>();
                var mockClient = new Mock<IAidQueueMonitoringClient>();
                if (param == nameof(QueueAccountInfo.QueueName))
                {
                    mockClient.Setup(mock => mock.GetQueueName()).Returns(string.Empty);
                    mockClient.Setup(mock => mock.GetStorageAccountName()).Returns("storage");
                }
                else
                {
                    mockClient.Setup(mock => mock.GetQueueName()).Returns("queue");
                    mockClient.Setup(mock => mock.GetStorageAccountName()).Returns(string.Empty);
                }

                mockList.Add(mockClient.Object);
                var aidTelemetry = new AidTelemetryFunc(mockList, this.mockMetricContainer.Object);
                await aidTelemetry.RunAsync(this.mockLogger.Object);
            }
            catch (Exception ex)
            {
                Assert.AreEqual($"AidTelemetryFunc (Parameter '{param} not found')", ex.Message);
                throw;
            }

            Assert.Fail("Exception should have been thrown.");
        }

        /// <summary>
        /// Test RunTelemetryTimerAsync.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task AnaheimIdTelemetryTimerFunctionTest()
        {
            try
            {
                await this.anaheimIdFunction.RunTelemetryTimerAsync(this.mockTimerInfo.Object);
            }
            catch (Exception ex)
            {
                Assert.Fail("Expected no exception, but got: " + ex.Message);
            }
        }
    }
}
