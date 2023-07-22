namespace Functions.UnitTests.AnaheimId
{
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using Microsoft.PrivacyServices.AnaheimId.Avro;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// AnaheimId Missing Request File Helper unit tests.
    /// </summary>
    [TestClass]
    public class MissingRequestFileHelperTests
    {
        // Path to Sample Avro File for Testing
        private static readonly MissingRequestFileHelper MissingRequestFileHelper = new MissingRequestFileHelper();
        private static readonly string FileName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\AnaheimId\AvroTestFile.avro";

        /// <summary>
        /// Test AnaheimIdGetSampleRequestIds.
        /// The correct sample ids should be extracted from the test avro file.
        /// </summary>
        [TestMethod]
        public void AnaheimIdGetSampleRequestIds()
        {
            List<long> requestIds;
            using (FileStream fs = File.OpenRead(FileName))
            {
                requestIds = MissingRequestFileHelper.CollectRequestIds(fs, 5);
            }

            // Ensure the correct device ids are parsed from the file
            Assert.AreEqual(5, requestIds.Count);
            Assert.AreEqual(6790418099107362, requestIds[0]);
            Assert.AreEqual(6795373755989827, requestIds[1]);
            Assert.AreEqual(6842481315917121, requestIds[2]);
            Assert.AreEqual(6965280033909610, requestIds[3]);
            Assert.AreEqual(6947609965797394, requestIds[4]);
        }

        /// <summary>
        /// Test AnaheimIdExceedMaxSampleIds.
        /// The number of ids returned should not exceed the number of requests in the test file.
        /// </summary>
        [TestMethod]
        public void AnaheimIdExceedsMaxSampleIds()
        {
            List<long> requestIds;
            using (FileStream fs = File.OpenRead(FileName))
            {
                requestIds = MissingRequestFileHelper.CollectRequestIds(fs, 20);
            }

            // Ensure no more ids are extracted than exist in the file
            Assert.AreEqual(11, requestIds.Count);
        }

        /// <summary>
        /// Test AnaheimIdExceedMinSampleIds.
        /// The mininum number of returned ids should be zero (an empty list).
        /// </summary>
        [TestMethod]
        public void AnaheimIdExceedsMinSampleIds()
        {
            List<long> requestIds;
            using (FileStream fs = File.OpenRead(FileName))
            {
                requestIds = MissingRequestFileHelper.CollectRequestIds(fs, -1);
            }

            // Ensure that the minimum ids are extracted from the file
            Assert.AreEqual(0, requestIds.Count);
        }
    }
}
