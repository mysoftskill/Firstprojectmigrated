namespace Functions.UnitTests.AnaheimId
{
    using System;
    using System.Collections.Generic;
    using Microsoft.AzureAd.Icm.Types;
    using Microsoft.PrivacyServices.AnaheimId.Icm;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    /// <summary>
    /// AnaheimId Missing Signal ICM Connector unit tests.
    /// </summary>
    [TestClass]
    public class MissingSignalIcmConnectorTests
    {
        /// <summary>
        /// Test AnaheimTestICMConnector.
        /// The icm should be sent only when a valid file is provided.
        /// </summary>
        /// <param name="blobUrl">The link to the test file.</param>
        /// <param name="expectedResult">The result of the ICM creation process.</param>
        [DataTestMethod]
        [DataRow(null, false)]
        [DataRow("bad_file_url.avro", false)]
        [DataRow("BadDate_2020-10-23_37_8-00002.avro", false)]
        [DataRow("MissingSignal_2021-11-23_22_34_16.avro", true)]
        [DataRow("MissingSignal_2021-11-23_22_34_16-00004.avro", true)]
        [DataRow("MissingSignal_2021-11-23_3_3_1-00004.avro", true)]
        public void AnaheimICMConnectorTests(string blobUrl, bool expectedResult)
        {
            Mock<ILogger> mockLogger = new Mock<ILogger>();
            List<long> requestIds = new List<long>() { 1, 2, 3, 4 };
            MissingSignalIcmConnector missingSignalIcmConnector = this.CreateMissingSignalICMConnector();
            bool testResult = missingSignalIcmConnector.CreateMissingSignalIncident(blobUrl, requestIds, mockLogger.Object);
            Assert.AreEqual(expectedResult, testResult);
        }

        /// <summary>
        /// Creates the Missing Signal ICM Connector.
        /// </summary>
        private MissingSignalIcmConnector CreateMissingSignalICMConnector()
        {
            IncidentAddUpdateResult incidentAddUpdateResult = new IncidentAddUpdateResult
            {
                IncidentId = 1234
            };
            Mock<IConnectorIncidentManager> mockIncidentManager = new Mock<IConnectorIncidentManager>();
            mockIncidentManager.Setup(f => f.AddOrUpdateIncident2(
                It.IsAny<Guid>(),
                It.IsAny<AlertSourceIncident>(),
                It.IsAny<RoutingOptions>()))
               .Returns(incidentAddUpdateResult);
            return new MissingSignalIcmConnector(mockIncidentManager.Object, new Guid("11111111-1111-1111-1111-111111111111"), "TestConnectorName", "DEV", 4, "https://test");
        }
    }
}