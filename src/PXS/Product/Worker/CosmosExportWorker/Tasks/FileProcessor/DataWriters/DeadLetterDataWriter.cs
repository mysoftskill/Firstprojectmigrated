// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Tasks
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem.Cosmos;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>export writer for individual commands that are sent to dead letter storage</summary>
    public sealed class DeadLetterDataWriter : DataWriter
    {
#if ENABLEDEADLETTERWRITER
        private readonly ILogger logger;

        private ICosmosFileSystem fileSystem;
        private string basePath;

        private StringBuilder pending;
        private IFile cosmosFile;
#endif

        /// <summary>
        ///     Initializes a new instance of the DeadLetterDataWriter class
        /// </summary>
        /// <param name="commandId">command id</param>
        /// <param name="agentId">agent id</param>
        /// <param name="fileName">file name</param>
        /// <param name="fileSystem">file system to write to</param>
        /// <param name="logger">Geneva trace logger</param>
        public DeadLetterDataWriter(
            string commandId,
            string agentId,
            string fileName,
            ICosmosFileSystem fileSystem,
            ILogger logger) :
            base(fileName, commandId)
        {
            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(agentId, nameof(agentId));

#if ENABLEDEADLETTERWRITER
            this.fileSystem = ArgumentCheck.ReturnIfNotNullElseThrow(fileSystem, nameof(fileSystem));
            this.logger = ArgumentCheck.ReturnIfNotNullElseThrow(logger, nameof(logger));

            this.basePath = fileSystem.RootDirectory + agentId + "/" + commandId + "/";

            this.Statuses = WriterStatuses.DeadLetterWriter;
#else
            this.Statuses = WriterStatuses.DeadLetterWriter | WriterStatuses.AbandonedNoDeadLetter;
#endif
        }

#if ENABLEDEADLETTERWRITER
        /// <summary>
        ///     Writes a json blob to the export package drop location in the specified file
        /// </summary>
        /// <param name="dataFile">file</param>
        /// <param name="json">json data to add to the export package file</param>
        /// <param name="pendingBytesThreshold">minimum number of bytes to accumulate before sending to storage</param>
        /// <returns>number of bytes added to or subtracted from the writer's pending byte count</returns>
        protected override async Task<long> WriteInternalAsync(
            IFileDataManager dataFile,
            string json,
            int pendingBytesThreshold)
        {
            long prevPending = this.PendingSize;
            int size;

            if (this.fileSystem == null)
            {
                throw new ObjectDisposedException("object has been disposed");
            }

            if (this.cosmosFile == null)
            {
                string fullPath = this.basePath + dataFile.FileName + ".txt";

                this.pending = new StringBuilder();

                this.cosmosFile = await this.fileSystem
                    .CreateFileAsync(fullPath, this.fileSystem.DefaultLifetime, FileCreateMode.CreateAlways)
                    .ConfigureAwait(false);
            }

            this.pending.Append(dataFile.ProductId);
            this.pending.Append("\t");
            this.pending.Append(json);
            this.pending.Append("\n");
           
            size = dataFile.ProductId.Length + json.Length + 2;

            this.PendingSize += size;

            if (this.PendingSize > pendingBytesThreshold)
            {
                await this.FlushInternalAsync(false).ConfigureAwait(false);
            }

            return this.PendingSize - prevPending;
        }

        /// <summary>
        ///     Flushes the asynchronous
        /// </summary>
        /// <param name="isClosing">true to is closing; false otherwise</param>
        /// <returns>resulting value</returns>
        protected override async Task<long> FlushInternalAsync(bool isClosing)
        {
            long prevPending = this.PendingSize;

            if (this.fileSystem == null)
            {
                throw new ObjectDisposedException("object has been disposed");
            }

            try
            {
                await this.cosmosFile.AppendAsync(this.pending.ToString()).ConfigureAwait(false);
            }
            catch (IOException)
            {
                // this is dead letter data we're writing so just ignore any write errors
                this.logger.Warning(
                    nameof(DeadLetterDataWriter),
                    "Error writing {0} bytes to {1} in Cosmos. Some dead letter data may be lost",
                    this.pending.Length,
                    this.cosmosFile.Path);

                this.Statuses |= WriterStatuses.AbandonedWriteError;
            }

            this.PendingSize -= this.pending.Length;

            if (isClosing == false)
            {
                this.pending = new StringBuilder();
            }

            return prevPending - this.PendingSize;
        }

        /// <summary>
        ///     frees, releases, or resets unmanaged resources
        /// </summary>
        /// <param name="disposing">true to releases managed and unmanaged resources; false to release unmanaged resources</param>
        protected override void Dispose(bool disposing)
        {
            this.fileSystem = null;
            this.cosmosFile = null;
            this.basePath = null;
            
            base.Dispose(disposing);
        }
#endif
    }
}