namespace Functions.UnitTests.AnaheimId
{
    using Microsoft.PrivacyServices.AnaheimId.Blob;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    /// <summary>
    /// Mock Blob Client unit tests.
    /// </summary>
    [TestClass]
    public class MockBlobClientTests
    {
        /// <summary>
        /// Test the MockTestBlobClient which is used in place of blob storage read/writes for running locally.
        /// </summary>
        [TestMethod]
        public void MockBlobClientIteractionTests()
        {
            Mock<ILogger> mockLogger = new Mock<ILogger>();
            MockTestBlobClient testBlockBlobClient = new MockTestBlobClient(mockLogger.Object);
            string testBlobName = "TestBlob";
            string testBlobMessage = "TestBlobMessage";
            Assert.AreEqual(false, testBlockBlobClient.CheckIfBlobExists(testBlobName));

            // Mock blob should exist and be read properly
            testBlockBlobClient.CreateTextBlob(testBlobName, testBlobMessage);
            Assert.AreEqual(true, testBlockBlobClient.CheckIfBlobExists(testBlobName));
            Assert.AreEqual(testBlobMessage, testBlockBlobClient.ReadTextBlob(testBlobName));

            // Mock blob should not exist after removal
            testBlockBlobClient.DeleteBlob(testBlobName);
            Assert.AreNotEqual(true, testBlockBlobClient.CheckIfBlobExists(testBlobName));
            Assert.AreNotEqual(testBlobMessage, testBlockBlobClient.ReadTextBlob(testBlobName));
        }
    }
}