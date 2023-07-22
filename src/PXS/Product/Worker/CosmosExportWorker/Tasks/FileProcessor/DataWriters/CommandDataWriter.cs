// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility;
    using Microsoft.Azure.Storage;

    /// <summary>
    ///    the export writer for individual commands
    /// </summary>
    public sealed class CommandDataWriter : DataWriter
    {
        // https://docs.microsoft.com/en-us/rest/api/storageservices/common-rest-api-error-codes
        // https://docs.microsoft.com/en-us/rest/api/storageservices/blob-service-error-codes
        private static readonly HashSet<string> TerminalAzureStorageErrorCodes = 
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "ContainerBeingDeleted",
                "ContainerDisabled",
                "ContainerNotFound",
                "AccountIsDisabled",
                "AuthenticationFailed",
                "InsufficientAccountPermissions",
                "InvalidAuthenticationInfo",
                "AuthorizationFailure",
                "403",
                "401",
            };

        /// <summary>
        ///     a collection of error codes that are possibly terminal
        /// </summary>
        private static readonly HashSet<string> PossiblyTerminalErrorCodes =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                WebExceptionStatus.NameResolutionFailure.ToStringInvariant(),
                WebExceptionStatus.ProxyNameResolutionFailure.ToStringInvariant(),
            };

        /// <summary>
        ///     Initializes a new instance of the CommandDataWriter class
        /// </summary>
        /// <param name="commandId">command id</param>
        /// <param name="fileName">file name to write to</param>
        /// <param name="exportPipeline">export pipeline</param>
        public CommandDataWriter(
            string commandId,
            string fileName,
            IExportPipeline exportPipeline) :
            base(fileName, commandId)
        {
            this.ExportPipeline = exportPipeline ?? throw new ArgumentNullException(nameof(exportPipeline));
            this.Statuses = WriterStatuses.NormalDataWriter;
        }

        /// <summary>
        ///     Gets a value indicating whether to log size and/or row data for command feed
        /// </summary>
        public override bool LogForCommandFeed => true;

        /// <summary>
        ///     Gets or sets export pipeline
        /// </summary>
        private IExportPipeline ExportPipeline { get; set; }

        /// <summary>
        ///     Creates the specified file name
        /// </summary>
        /// <param name="fileName">file name</param>
        /// <param name="productId">product id</param>
        /// <returns>resulting value</returns>
        protected override IFileDataManager CreateFileDetails(
            string fileName,
            string productId)
        {
            return new BlobFileDetails(fileName, productId);
        }

        /// <summary>
        ///     Writes a json blob to the export package drop location in the specified file
        /// </summary>
        /// <param name="dataFile">data file object</param>
        /// <param name="json">json data to add to the export package file</param>
        /// <param name="pendingBytesThreshold">minimum number of bytes to accumulate before sending to storage</param>
        /// <returns>number of bytes added to or subtracted from the writer's pending byte count</returns>
        protected override async Task<long> WriteInternalAsync(
            IFileDataManager dataFile, 
            string json,
            int pendingBytesThreshold)
        {
            BlobFileDetails file = dataFile as BlobFileDetails;
            long prevPending = this.PendingSize;

            ArgumentCheck.ThrowIfNull(
                file, nameof(dataFile), "dataFile is not the expected type " + typeof(BlobFileDetails).FullName);

            if (this.ExportPipeline == null)
            {
                throw new ObjectDisposedException("object has been disposed");
            }

            dataFile.AddRow(json, this.LastErrorDetails != null);

            if (this.LastErrorDetails != null)
            {
                return 0;
            }
            
            this.PendingSize += json.Length;

            if (this.PendingSize > pendingBytesThreshold)
            {
                await this.FlushInternalAsync(false).ConfigureAwait(false);
            }

            return this.PendingSize - prevPending;
        }

        /// <summary>
        ///      Flushes any pending data
        /// </summary>
        /// <returns>number of bytes written</returns>
        protected override async Task<long> FlushInternalAsync(bool isClosing)
        {
            long prevPending = this.PendingSize;

            if (this.ExportPipeline == null)
            {
                throw new ObjectDisposedException("object has been disposed");
            }

            foreach (BlobFileDetails file in this.FileDetails.OfType<BlobFileDetails>())
            {
                this.PendingSize -= await file.FlushAsync(this).ConfigureAwait(false);
            }

            return prevPending - this.PendingSize;
        }

        /// <summary>
        ///     frees, releases, or resets unmanaged resources
        /// </summary>
        /// <param name="disposing">true to releases managed and unmanaged resources; false to release unmanaged resources</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    this.ExportPipeline?.Dispose();
                }
                catch (StorageException e)
                {
                    this.HandleStorageException(e);
                }

                this.ExportPipeline = null;
            }
        }

        /// <summary>
        ///      Handles a storage exception
        /// </summary>
        /// <param name="e">exception to handle</param>
        private void HandleStorageException(StorageException e)
        {
            string message;
            string code;
                
            (code, message) = this.ExtractStatusCodeAndSetErrorInfo(e);

            if (code != null && 
                (CommandDataWriter.TerminalAzureStorageErrorCodes.Contains(code) ||
                 (this.TransientFailureMode == TransientFailureMode.AssumeNonTransient &&
                  CommandDataWriter.PossiblyTerminalErrorCodes.Contains(code))))
            {
                this.Statuses |= WriterStatuses.AbandonedNoStorage;
                throw new NonTransientStorageException(e.Message, e);
            }

            throw new IOException(message, e);
        }

        /// <summary>
        ///     Sets the error state
        /// </summary>
        /// <param name="code">code to set LastErrorCode to</param>
        /// <param name="message">message to set LastErrorDetails to</param>
        private void SetErrorInfo(
            string code,
            string message)
        {
            // replace hashes with '-' in case this gets written to Cosmos, which will require escaping '#' characters
            this.LastErrorDetails = message.Replace('#', '-');
            this.LastErrorCode = code;
            this.PendingSize = 0;
        }

        /// <summary>
        ///      Sets the error state
        /// </summary>
        /// <param name="e">StrorageException instance</param>
        private (string Code, string Msg) ExtractStatusCodeAndSetErrorInfo(StorageException e)
        {
            StorageExtendedErrorInformation info = e.RequestInformation?.ExtendedErrorInformation;
            string messageFull;
            string errorFrom = e.InnerException?.GetType().Name ?? "UNKNOWN";
            string message = e.GetMessageAndInnerMessages();
            string code = "UnknownError";

            if (info != null)
            {
                errorFrom = "Storage." + info.GetType().Name;

                if (string.IsNullOrWhiteSpace(info.ErrorMessage) == false)
                {
                    message = info.ErrorMessage;
                }

                if (string.IsNullOrWhiteSpace(info.ErrorCode) == false)
                {
                    code = info.ErrorCode;
                }
            }
            else if (e.InnerException is WebException webEx)
            {
                errorFrom = "WebEx." + webEx.GetType().Name;
                code = webEx.Status.ToStringInvariant();
            }

            messageFull = $"{errorFrom}.{code}: {message}";

            this.SetErrorInfo(code, messageFull);

            return (code, messageFull);
        }

        /// <summary>
        ///     FileDetails object for writing blob files
        /// </summary>
        private class BlobFileDetails : FileDetails
        {
            private readonly ICollection<string> pending;

            /// <summary>
            ///     Initializes a new instance of the BlobFileDetails class
            /// </summary>
            /// <param name="fileName">file name</param>
            /// <param name="productId">product id</param>
            public BlobFileDetails(
                string fileName, 
                string productId) : 
                base(fileName, productId)
            {
                this.pending = new List<string>();
            }

            /// <summary>
            ///     Adds a row to the file
            /// </summary>
            /// <param name="json">json to add</param>
            /// <param name="onlyRecordStats">true to only record stats; false to record stats and any other required data</param>
            public override void AddRow(
                string json,
                bool onlyRecordStats)
            {
                if (onlyRecordStats == false)
                {
                    this.pending.Add(json);
                }

                base.AddRow(json, onlyRecordStats);
            }

            /// <summary>
            ///     Flushes the asynchronous
            /// </summary>
            /// <param name="writer">writer</param>
            /// <returns>resulting value</returns>
            public async Task<long> FlushAsync(CommandDataWriter writer)
            {
                long result = 0;

                if (this.pending?.Count > 0)
                {
                    string output = string.Join(",", this.pending);

                    try
                    {
                        await writer.ExportPipeline.ExportAsync(this.ProductId,this.FileName, output).ConfigureAwait(false);
                    }
                    catch (TimeoutException e)
                    {
                        // for now, consider a timeout exception to be a non-transient exception as ExportAsync can throw it
                        //  when the write queue backs up and the most common case of that is that the write location is no
                        //  longer valid.

                        if (e.InnerException is StorageException storageErr)
                        {
                            writer.HandleStorageException(storageErr);
                        }
                        else
                        {
                            writer.SetErrorInfo("StorageError", "Timeout: Error queueing messages to writer (likely storage issue)");
                            writer.Statuses |= WriterStatuses.AbandonedNoStorage;
                            throw new NonTransientStorageException(e.Message, e);
                        }
                    }
                    catch (StorageException e)
                    {
                        writer.HandleStorageException(e);
                    }

                    // output contains an extra comma between rows; there are pending.Count rows and thus (pending.Count - 1) commas
                    //  between them.  Subtract that to get the actual pending size decrease
                    result = output.Length - this.pending.Count + 1;

                    this.pending.Clear();
                }

                return result;
            }
        }
    }
}
