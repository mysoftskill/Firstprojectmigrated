namespace Microsoft.PrivacyServices.AnaheimId.Blob
{
    using System.Collections.Generic;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// A class used for fake interaction with the blob client
    /// </summary>
    public class MockTestBlobClient : ITestBlobClient
    {
        private const string ComponentName = nameof(TestBlobClient);
        private readonly Dictionary<string, string> blobs = new Dictionary<string, string>();
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockTestBlobClient" /> class.
        /// </summary>
        /// <param name="logger">The logger used to output test messages.</param>
        public MockTestBlobClient(ILogger logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Creates a new blob with the provided name and message contents.
        /// </summary>
        /// <param name="blobName">The name of the blob to create.</param>
        /// <param name="message">The message to include in the blob content.</param>
        public void CreateTextBlob(string blobName, string message)
        {
            this.blobs.Add(blobName, message);
            this.logger.Information(ComponentName, $"Creating the blob: {blobName} with message content: {message}.");
        }

        /// <summary>
        /// Check if a blob with a given name exists in the container
        /// </summary>
        /// <param name="blobName">The name of the blob to look for.</param>
        /// <returns>The existence of the blob in the container.</returns>
        public bool CheckIfBlobExists(string blobName)
        {
            this.logger.Information(ComponentName, $"Checking if the blob: {blobName} exists.");
            return this.blobs.ContainsKey(blobName);
        }

        /// <summary>
        /// Downloads a blob with the provided name and extracts the message contents.
        /// </summary>
        /// <param name="blobName">The name of the blob to read.</param>
        /// <returns>The message contents of the blob.</returns>
        public string ReadTextBlob(string blobName)
        {
            this.logger.Information(ComponentName, $"Downloading the blob: {blobName}.");
            this.blobs.TryGetValue(blobName, out string message);
            this.logger.Information(ComponentName, $"Read the blob: {blobName} with message content: {message}.");
            return message;
        }

        /// <summary>
        /// Deletes a blob with the provided name.
        /// </summary>
        /// <param name="blobName">The name of the blob to delete.</param>
        public void DeleteBlob(string blobName)
        {
            this.logger.Information(ComponentName, $"Deleting the blob: {blobName}.");
            this.blobs.Remove(blobName);
        }
    }
}