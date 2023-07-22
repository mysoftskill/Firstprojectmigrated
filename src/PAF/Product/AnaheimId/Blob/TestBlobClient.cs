namespace Microsoft.PrivacyServices.AnaheimId.Blob
{
    using System;
    using System.IO;
    using System.Text;
    using global::Azure;
    using global::Azure.Core;
    using global::Azure.Storage.Blobs;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// A class for interacting with the blob client
    /// </summary>
    public class TestBlobClient : ITestBlobClient
    {
        private const string ComponentName = nameof(TestBlobClient);
        private readonly ILogger logger;
        private readonly BlobContainerClient containerClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestBlobClient" /> class.
        /// </summary>
        /// <param name="logger">The logger used to output test messages.</param>
        /// <param name="containerEndpoint">The blob storage container url.</param>
        /// <param name="credential">The credential for authentication.</param>
        public TestBlobClient(ILogger logger, string containerEndpoint, TokenCredential credential)
        {
            this.logger = logger;

            // Get a credential and create a service client object for the blob container.
            this.logger.Information(ComponentName, "Creating the test blob client.");
            this.containerClient = new BlobContainerClient(
                new Uri(containerEndpoint),
                credential);

            // Create the container if it does not exist.
            this.logger.Information(ComponentName, $"Creating the container {containerEndpoint} if it does not yet exist.");
            this.containerClient.CreateIfNotExists();
        }

        /// <summary>
        /// Creates a new blob with the provided name and message contents.
        /// </summary>
        /// <param name="blobName">The name of the blob to create.</param>
        /// <param name="message">The message to include in the blob content.</param>
        public void CreateTextBlob(string blobName, string message)
        {
            try
            {
                this.logger.Information(ComponentName, $"Creating the blob: {blobName} with message content: {message}.");
                var content = Encoding.ASCII.GetBytes(message);
                using (var stream = new MemoryStream(content))
                {
                    this.containerClient.UploadBlob(blobName, stream);
                }
            }
            catch (RequestFailedException e)
            {
                this.logger.Error(ComponentName, e.Message);

                // We should expect conflict errors(409) when the blob already exists due to the duplicate azure function instance issue
                if (e.Status != 409)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Check if a blob with a given name exists in the container
        /// </summary>
        /// <param name="blobName">The name of the blob to look for.</param>
        /// <returns>The existence of the blob in the container.</returns>
        public bool CheckIfBlobExists(string blobName)
        {
            try
            {
                this.logger.Information(ComponentName, $"Checking if the blob: {blobName} exists.");
                return this.containerClient.GetBlobClient(blobName).Exists();
            }
            catch (RequestFailedException e)
            {
                this.logger.Error(ComponentName, e.Message);
                throw;
            }
        }

        /// <summary>
        /// Downloads a blob with the provided name and extracts the message contents.
        /// </summary>
        /// <param name="blobName">The name of the blob to read.</param>
        /// <returns>The message contents of the blob.</returns>
        public string ReadTextBlob(string blobName)
        {
            try
            {
                // Retrieve reference to a blob
                this.logger.Information(ComponentName, $"Downloading the blob: {blobName}.");
                var blockBlob = this.containerClient.GetBlobClient(blobName);

                // Extract the message from the blob
                string message;
                using (var memoryStream = new MemoryStream())
                {
                    blockBlob.DownloadTo(memoryStream);
                    message = Encoding.ASCII.GetString(memoryStream.ToArray());
                }

                this.logger.Information(ComponentName, $"Read the blob: {blobName} with message content: {message}.");
                return message;
            }
            catch (RequestFailedException e)
            {
                this.logger.Error(ComponentName, e.Message);
                throw;
            }
        }

        /// <summary>
        /// Deletes a blob with the provided name.
        /// </summary>
        /// <param name="blobName">The name of the blob to delete.</param>
        public void DeleteBlob(string blobName)
        {
            try
            {
                // Delete the blob if it exists
                this.logger.Information(ComponentName, $"Deleting the blob: {blobName}.");
                this.containerClient.GetBlobClient(blobName).DeleteIfExists();
            }
            catch (RequestFailedException e)
            {
                this.logger.Error(ComponentName, e.Message);
                throw;
            }
        }
    }
}