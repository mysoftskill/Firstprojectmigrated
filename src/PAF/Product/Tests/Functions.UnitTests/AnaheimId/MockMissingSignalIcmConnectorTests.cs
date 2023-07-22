namespace Functions.UnitTests.AnaheimId
{
    using System.Collections.Generic;
    using Microsoft.PrivacyServices.AnaheimId.Blob;
    using Microsoft.PrivacyServices.AnaheimId.Icm;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    /// <summary>
    /// Mock Missing Signal Icm Connector unit tests.
    /// </summary>
    [TestClass]
    public class MockMissingSignalIcmConnectorTests
    {
        /// <summary>
        /// Test AnaheimTestICMConnector.
        /// The mock icm connector should run if the correct inputs are provided and should utilize the blob client.
        /// </summary>
        /// <param name="blobUrl">The link to the test file.</param>
        /// <param name="expectedResult">The result of the ICM creation process.</param>
        [DataTestMethod]
        [DataRow(null, false)]
        [DataRow("MissingSignal_2021-11-23_22_34_16.avro", true)]
        public void MockAnaheimICMConnectorTests(string blobUrl, bool expectedResult)
        {
            Mock<ILogger> mockLogger = new Mock<ILogger>();
            ITestBlobClient testBlockBlobClient = new MockTestBlobClient(mockLogger.Object);
            MockMissingSignalIcmConnector missingSignalIcmConnector = new MockMissingSignalIcmConnector(testBlockBlobClient, "DEV", 4, true);
            List<long> requestIds = new List<long>() { 1, 2, 3, 4 };
            bool testResult = missingSignalIcmConnector.CreateMissingSignalIncident(blobUrl, requestIds, mockLogger.Object);
            Assert.AreEqual(expectedResult, testResult);
        }
    }
}