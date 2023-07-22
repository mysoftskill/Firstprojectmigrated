namespace Microsoft.PrivacyServices.AnaheimId.Icm
{
    using System.Collections.Generic;
    using Microsoft.PrivacyServices.AnaheimId.Blob;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// A class for simulating the operation of creating ICM incidents for missing signals.
    /// </summary>
    public class MockMissingSignalIcmConnector : IMissingSignalIcmConnector
    {
        private const string ComponentName = nameof(MockMissingSignalIcmConnector);
        private readonly int severity;
        private readonly bool removeTestFiles;
        private readonly string serviceLocation;
        private readonly ITestBlobClient testBlockBlobClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockMissingSignalIcmConnector" /> class.
        /// </summary>
        /// <param name="testBlockBlobClient">The test blob storage client.</param>
        /// <param name="serviceLocation">The alert test environment.</param>
        /// <param name="severity">The severity of the test alert.</param>
        /// <param name="removeTestFiles">Remove the test output file from blob storage after verification.</param>
        public MockMissingSignalIcmConnector(ITestBlobClient testBlockBlobClient, string serviceLocation, int severity, bool removeTestFiles)
        {
            this.testBlockBlobClient = testBlockBlobClient;
            this.serviceLocation = serviceLocation;
            this.severity = severity;
            this.removeTestFiles = removeTestFiles;
        }

        /// <summary>
        /// Logs a test message to blob storage that is representative some information that would be sent to the ICM portal.
        /// </summary>
        /// <param name="name">The name of the blob containing missing signals.</param>
        /// <param name="requestIds">A sample of request ids found in the missing signal file.</param>
        /// <param name="logger">The logger instance.</param>
        /// <returns>An incident create/update success status.</returns>
        public bool CreateMissingSignalIncident(string name, List<long> requestIds, ILogger logger)
        {
            if (name != null)
            {
                // The test blob that will be created for verifying
                string filename = CreateBlobNameForTesting(name);
                logger.Information(ComponentName, "Expected File: " + filename);
                string message = CreateBlobTextForTesting(this.serviceLocation, this.severity, string.Join(", ", requestIds));
                logger.Information(ComponentName, "Expected Message: " + message);

                // A new blob will be created with the information needed for the ICM
                this.testBlockBlobClient.CreateTextBlob(filename, message);
                if (this.testBlockBlobClient.CheckIfBlobExists(filename))
                {
                    // Read the test file back from blob storage and later verify the contents match
                    logger.Information(ComponentName, "The Blob: " + filename + " exists.");
                    string testMessage = this.testBlockBlobClient.ReadTextBlob(filename);
                    logger.Information(ComponentName, "Read the message: " + testMessage + " from the file: " + filename);

                    // Flag to determine whether test file should be left or removed
                    if (this.removeTestFiles)
                    {
                        logger.Information(ComponentName, $"Removing the test file from the container: {filename}.");
                        this.testBlockBlobClient.DeleteBlob(filename);
                    }

                    if (testMessage == message)
                    {
                        logger.Information(ComponentName, "The test file in the blob matches the expected ICM trigger content.");
                        return true;
                    }
                    else
                    {
                        logger.Error(ComponentName, "The test blob contents does not match the expected message.");
                    }
                }
                else
                {
                    logger.Error(ComponentName, "The test blob could not be uploaded.");
                }
            }
            else
            {
                logger.Error(ComponentName, "The original blob url has not been provided.");
            }

            return false;
        }

        /// <summary>
        /// Creates a name for a test blob for verification.
        /// </summary>
        /// <param name="blobName">The name of the original blob.</param>
        /// <returns>The name of the test blob.</returns>
        private static string CreateBlobNameForTesting(string blobName)
        {
            // Construct the message which will be written to the test blobs
            return string.Format(
                "{0}.test",
                blobName);
        }

        /// <summary>
        /// Creates the file content for a test blob for verification.
        /// </summary>
        /// <param name="serviceLocation">The name of the test environment.</param>
        /// <param name="severity">The serverity of the test alert.</param>
        /// <param name="commaSeperatedIds">The list of test request ids.</param>
        /// <returns>The message to write to a blob.</returns>
        private static string CreateBlobTextForTesting(string serviceLocation, int severity, string commaSeperatedIds)
        {
            // Construct the message which will be written to the test blobs
            return string.Format(
                "Anaheim Missing Signals for {0} with serverity {1} containing request ids: {2}",
                serviceLocation,
                severity,
                commaSeperatedIds);
        }
    }
}