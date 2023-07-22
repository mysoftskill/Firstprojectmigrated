// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

using System.Threading.Tasks;

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Tasks
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Membership.MemberServices.Common.Utilities;

    /// <summary>
    ///     base class for data writers
    /// </summary>
    public abstract class DataWriter : ICommandDataWriter
    {
        protected static readonly Task<long> NoOpTask = Task.FromResult(0L);

        private readonly Dictionary<string, IFileDataManager> dataFiles = 
            new Dictionary<string, IFileDataManager>(StringComparer.OrdinalIgnoreCase);

        private string fileName;

        /// <summary>
        ///     Initializes a new instance of the DataWriter class
        /// </summary>
        /// <param name="fileName">file name</param>
        /// <param name="commandId">command id</param>
        protected DataWriter(
            string fileName, 
            string commandId)
        {
            this.CommandId = ArgumentCheck.ReturnIfNotNullEmptyOrWhiteSpace(commandId, nameof(commandId));
            this.fileName = ArgumentCheck.ReturnIfNotNullEmptyOrWhiteSpace(fileName, nameof(fileName));
        }

        /// <summary>
        ///     Gets the command id
        /// </summary>
        public string CommandId { get; }

        /// <summary>
        ///     Gets file details
        /// </summary>
        public IEnumerable<IFileDetails> FileDetails => this.dataFiles.Values;

        /// <summary>
        ///     Gets or sets the transient failure mode
        /// </summary>
        /// <value>the transient failure mode</value>
        public TransientFailureMode TransientFailureMode { get; set; }

        /// <summary>
        ///     Gets the writer statuses
        /// </summary>
        public WriterStatuses Statuses { get; protected set; }

        /// <summary>
        ///     Gets a text representation of the last detected error or null if no errors ocurred
        /// </summary>
        public string LastErrorDetails { get; protected set; } = null;

        /// <summary>
        ///     Gets a text representation of the error code of the last detected error or null if no errors ocurred
        /// </summary>
        public string LastErrorCode { get; protected set; } = null;

        /// <summary>
        ///     Gets the approximate number of bytes waiting to be written
        /// </summary>
        /// <remarks>
        ///     this keeps track of the total number of pending data bytes, including any overhead bytes added by a 
        ///      particular writer (such as extra characters to implement JSON array syntax)
        ///     the behavior difference between this and Size is due to the different use cases.  Size is intended
        ///      for metric tracking while PendingSize is intended for making decisions on when to flush writers to
        ///      storage; thus, PendingSize needs to report a value closer to approximate memory usage.
        /// </remarks>
        public long PendingSize { get; protected set; } = 0;

        /// <summary>
        ///     Gets the approximate number of bytes written by the writer
        /// </summary>
        /// <remarks>
        ///     this keeps track of the number of data bytes written and does NOT include overhead bytes added by a 
        ///      particular writer (such as extra characters to implement JSON array syntax)
        /// </remarks>
        public long Size { get; private set; }

        /// <summary>
        ///     Gets the number of rows written by the writer
        /// </summary>
        public int RowCount { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether to log size and/or row data for command feed
        /// </summary>
        public virtual bool LogForCommandFeed => false;

        /// <summary>
        ///     Writes a json blob to the export package drop location
        /// </summary>
        /// <param name="productId">product id</param>
        /// <param name="json">json to write</param>
        /// <param name="pendingBytesThreshold">minimum number of bytes to accumulate before sending to storage</param>
        /// <returns>number of bytes added to or subtracted from the writer's pending byte count</returns>
        public async Task<long> WriteAsync(
            string productId,
            string json,
            int pendingBytesThreshold)
        {
            IFileDataManager dataFile;
            long result;

            if (this.fileName == null)
            {
                throw new ObjectDisposedException("Object is disposed");
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                return 0;
            }

            if (this.dataFiles.TryGetValue(productId, out dataFile) == false)
            {
                this.dataFiles[productId] = dataFile = this.CreateFileDetails(this.fileName, productId);
            }

            result = await this.WriteInternalAsync(dataFile, json, pendingBytesThreshold).ConfigureAwait(false);
            
            this.RowCount += 1;
            this.Size += json.Length;

            return result;
        }

        /// <summary>
        ///     Flushes any pending data
        /// </summary>
        /// <returns>number of bytes written</returns>
        public async Task<long> FlushAsync()
        {
            if (this.fileName == null)
            {
                throw new ObjectDisposedException("Object is disposed");
            }

            return await this.FlushInternalAsync(false).ConfigureAwait(false);
        }

        /// <summary>
        ///     Closes the writer and flushes any pending data
        /// </summary>
        /// <returns>resulting value</returns>
        public async Task CloseAsync()
        {
            await this.FlushInternalAsync(true).ConfigureAwait(false);

            this.Dispose(true);
        }

        /// <summary>
        ///     frees, releases, or resets unmanaged resources
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        ///     Creates the specified file name
        /// </summary>
        /// <param name="fileName">file name</param>
        /// <param name="productId">product id</param>
        /// <returns>resulting value</returns>
        protected virtual IFileDataManager CreateFileDetails(
            string fileName,
            string productId)
        {
            return new FileDetails(fileName, productId);
        }

        /// <summary>
        ///     Creates the specified file name
        /// </summary>
        /// <param name="dataFile">data file object</param>
        /// <param name="json">json to write</param>
        /// <param name="pendingBytesThreshold">minimum number of bytes to accumulate before sending to storage</param>
        /// <returns>number of bytes added to or subtracted from the writer's pending byte count</returns>
        protected virtual Task<long> WriteInternalAsync(
            IFileDataManager dataFile,
            string json,
            int pendingBytesThreshold)
        {
            dataFile.AddRow(json, false);
            return DataWriter.NoOpTask;
        }

        /// <summary>
        ///     Flushes the asynchronous
        /// </summary>
        /// <param name="isClosing">true to is closing; false otherwise</param>
        /// <returns>resulting value</returns>
        protected virtual Task<long> FlushInternalAsync(bool isClosing)
        {
            return DataWriter.NoOpTask;
        }

        /// <summary>
        ///     frees, releases, or resets unmanaged resources
        /// </summary>
        /// <param name="disposing">true to releases managed and unmanaged resources; false to release unmanaged resources</param>
        protected virtual void Dispose(bool disposing)
        {
            this.fileName = null;
        }
    }
}