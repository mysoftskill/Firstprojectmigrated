namespace Functions.UnitTests.AnaheimId
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Azure.ComplianceServices.Common.IfxMetric;
    using Microsoft.Cloud.InstrumentationFramework.Metrics.Extensions;
    using Microsoft.PrivacyServices.AnaheimId.AidFunctions;
    using Microsoft.PrivacyServices.AnaheimId.Avro;
    using Microsoft.PrivacyServices.AnaheimId.Icm;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    /// <summary>
    /// AnaheimId blob storage func unit tests.
    /// </summary>
    [TestClass]
    public class AidBlobStorageFuncTests
    {
        public static IEnumerable<object[]> TestAidBlobStorageFuncData =>
        new List<object[]>
        {
                new object[] { null, false },
                new object[] { new List<long>(), false },
                new object[] { new List<long> { 1, 2, 3 }, true }
        };

        /// <summary>
        /// Test AidBlobStorageFunc.
        /// An ICM trigger should be run when valid request ids are provided.
        /// </summary>
        /// <param name="requestIds">The list of request ids.</param>
        /// <param name="icmCreated">The result of the ICM creation process.</param>
        [DataTestMethod]
        [DynamicData(nameof(TestAidBlobStorageFuncData))]
        public void AidBlobStorageFuncTest(List<long> requestIds, bool icmCreated)
        {
            Mock<ILogger> mockLogger = new Mock<ILogger>();

            // Setting metric should always return true
            Mock<IMetricContainer> mockMetricContainer = new Mock<IMetricContainer>();
            mockMetricContainer.Setup(mock => mock.OutgoingMetric.Set(It.IsAny<DateTime>(), It.IsAny<ulong>(), It.IsAny<DimensionValues5D>())).Returns(true);
            mockMetricContainer.Setup(mock => mock.OutgoingApiErrorCountMetric.Set(It.IsAny<DateTime>(), It.IsAny<ulong>(), It.IsAny<DimensionValues5D>())).Returns(true);
            mockMetricContainer.Setup(mock => mock.OutgoingSuccessLatencyMetric.Set(It.IsAny<DateTime>(), It.IsAny<ulong>(), It.IsAny<DimensionValues4D>())).Returns(true);

            // The CreateMissingSignalIncidentAsync call should be run for ICM creation
            Mock<IMissingSignalIcmConnector> mockMissingSignalIcmConnector = new Mock<IMissingSignalIcmConnector>();
            mockMissingSignalIcmConnector.Setup(mock => mock.CreateMissingSignalIncident(It.IsAny<string>(), It.IsAny<List<long>>(), It.IsAny<ILogger>()));

            // Return the list of request ids given in the input
            Mock<IMissingRequestFileHelper> mockMissingRequestFileHelper = new Mock<IMissingRequestFileHelper>();
            mockMissingRequestFileHelper.Setup(mock => mock.CollectRequestIds(It.IsAny<Stream>(), It.IsAny<int>())).Returns(requestIds);

            // Run the aid blob storage function
            Mock<Stream> mockStream = new Mock<Stream>();
            var aidBlobStorageFunc = new AidBlobStorageFunc(mockMetricContainer.Object, mockMissingRequestFileHelper.Object, mockMissingSignalIcmConnector.Object);
            aidBlobStorageFunc.Run(mockStream.Object, string.Empty, mockLogger.Object);

            // The boolean checks whether an icm creation is expected
            if (icmCreated)
            {
                mockMissingSignalIcmConnector.Verify(mock => mock.CreateMissingSignalIncident(It.IsAny<string>(), It.IsAny<List<long>>(), It.IsAny<ILogger>()), Times.Once());
            }
            else
            {
                mockMissingSignalIcmConnector.Verify(mock => mock.CreateMissingSignalIncident(It.IsAny<string>(), It.IsAny<List<long>>(), It.IsAny<ILogger>()), Times.Never());
            }
        }
    }
}
