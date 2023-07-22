// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem.Cosmos
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.Common.Cosmos;
    using Microsoft.Membership.MemberServices.Common.Utilities;


    /// <summary>
    ///     a stream for reading data from Cosmos in chunks
    /// </summary>
    public class CosmosChunkedReadStream : Stream
    {
        private static readonly TimeSpan RetryDelayIncrementDefault = TimeSpan.FromSeconds(5);

        private static readonly TimeSpan RetryDelayInitialDefault = TimeSpan.FromSeconds(1);

        private const int MaxRetriesDefault = 3;

        private const int ChunkSize = 25 * 1024 * 1024;

        private readonly ICosmosClient client;

        private readonly int chunkSize;

        private readonly TimeSpan delayIncrement;

        private readonly TimeSpan delayInitial;

        private readonly int maxRetries;

        private MemoryStream currentStream;

        private CosmosStreamInfo cosmosInfo;

        private long nextCosmosFetchOffset;

        private long overallPosition;

        private bool done;

        /// <summary>
        ///     Initializes a new instance of the CosmosChunkedReadStream class
        /// </summary>
        /// <param name="cosmosInfo">cosmos stream info</param>
        /// <param name="client">cosmos client</param>
        /// <param name="appConfig">App config to read lease time and chuck size dynamically</param>
        public CosmosChunkedReadStream(
            CosmosStreamInfo cosmosInfo,
            ICosmosClient client,
            IAppConfiguration appConfig)
        {
            ArgumentCheck.ThrowIfNull(cosmosInfo, nameof(cosmosInfo));
            ArgumentCheck.ThrowIfNull(client, nameof(client));

            this.delayIncrement = CosmosChunkedReadStream.RetryDelayIncrementDefault;
            this.delayInitial = CosmosChunkedReadStream.RetryDelayInitialDefault;
            this.cosmosInfo = cosmosInfo;
            this.chunkSize = Math.Max(1, appConfig.GetConfigValue<int>(ConfigNames.PXS.CosmosWorkerChunkReadSizeMultiple, 1)) * CosmosChunkedReadStream.ChunkSize;
            this.client = client;
        }

        /// <summary>
        ///     Initializes a new instance of the CosmosChunkedReadStream class
        /// </summary>
        /// <param name="cosmosInfo">stream info</param>
        /// <param name="client">cosmos client</param>
        /// <param name="chunkSize">chunk size</param>
        /// <param name="retryDelayInitial">retry delay initial</param>
        /// <param name="retryDelayIncrement">retry delay increment</param>
        /// <param name="maxRetries">maximum retries</param>
        public CosmosChunkedReadStream(
            CosmosStreamInfo cosmosInfo,
            ICosmosClient client,
            int chunkSize,
            TimeSpan? retryDelayInitial,
            TimeSpan? retryDelayIncrement,
            int? maxRetries)
        {
            ArgumentCheck.ThrowIfNull(cosmosInfo, nameof(cosmosInfo));
            ArgumentCheck.ThrowIfNull(client, nameof(client));

            this.delayIncrement = retryDelayInitial ?? CosmosChunkedReadStream.RetryDelayIncrementDefault;
            this.delayInitial = retryDelayIncrement ?? CosmosChunkedReadStream.RetryDelayInitialDefault;
            this.cosmosInfo = cosmosInfo;
            this.maxRetries = maxRetries ?? CosmosChunkedReadStream.MaxRetriesDefault;
            this.chunkSize = Math.Max(chunkSize, 1);
            this.client = client;
        }

        /// <summary>
        ///     Gets a value indicating whether the current stream supports reading
        /// </summary>
        public override bool CanRead => this.cosmosInfo != null;

        /// <summary>
        ///     Gets a value indicating whether the current stream supports seeking
        /// </summary>
        public override bool CanSeek => false;

        /// <summary>
        ///     Gets a value indicating whether the current stream supports writing
        /// </summary>
        public override bool CanWrite => false;

        /// <summary>
        ///     Gets the length in bytes of the stream
        /// </summary>
        public override long Length => this.cosmosInfo.Length;

        /// <summary>
        ///     Gets or sets the position within the current stream
        /// </summary>
        public override long Position
        {
            get => this.overallPosition;
            set => throw new NotSupportedException();
        }

        /// <summary>
        ///     Clears all buffers for this stream and causes any buffered data to be written to the underlying device
        /// </summary>
        public override void Flush()
        {
        }

        /// <summary>
        ///     When overridden in a derived class, sets the position within the current stream
        /// </summary>
        /// <param name="offset">byte offset relative to the origin parameter</param>
        /// <param name="origin">indicates the reference point used to obtain the new position</param>
        /// <returns>new position within the current stream</returns>
        public override long Seek(
            long offset, 
            SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Sets the length of the current stream
        /// </summary>
        /// <param name="value">desired length of the current stream in bytes</param>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes
        ///      read
        /// </summary>
        /// <param name="buffer">
        ///     array of bytes whose contents in the range of offset and (offset + count - 1) will be replaced by the bytes read from 
        ///      the current source
        /// </param>
        /// <param name="offset">zero-based byte offset in buffer to start writing data</param>
        /// <param name="count">maximum number of bytes to be read</param>
        /// <returns>
        ///     total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes 
        ///      are not currently available, or zero if the end of the stream has been reached
        /// </returns>
        public override int Read(
            byte[] buffer, 
            int offset, 
            int count)
        {
            return this.ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        ///     Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes
        ///      read
        /// </summary>
        /// <param name="buffer">
        ///     array of bytes whose contents in the range of offset and (offset + count - 1) will be replaced by the bytes read from 
        ///      the current source
        /// </param>
        /// <param name="offset">zero-based byte offset in buffer to start writing data</param>
        /// <param name="count">maximum number of bytes to be read</param>
        /// <param name="cancellationToken">token to monitor for cancellation requests</param>
        /// <returns>
        ///     total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes 
        ///      are not currently available, or zero if the end of the stream has been reached
        /// </returns>
        public override async Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
           
                return await this.ReadAsyncInternal(buffer, offset, count, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        ///     writes a sequence of bytes to the current stream and advances the current position within this stream by the number 
        ///      of bytes written
        /// </summary>
        /// <param name="buffer">
        ///     array of bytes whose contents from offset to (offset + count - 1) will be written to the current stream.
        /// </param>
        /// <param name="offset">zero-based byte offset in buffer at which copying will be started</param>
        /// <param name="count">number of bytes to be written to the current stream</param>
        public override void Write(
            byte[] buffer,
            int offset, 
            int count)
        {
            throw new NotSupportedException();
        }

        /// <summary>frees, releases, or resets resources</summary>
        /// <param name="disposing">true to release managed and unmanaged resources; false to release only unmanaged</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.currentStream?.Dispose();
                this.currentStream = null;
                this.cosmosInfo = null;
            }
        }

        /// <summary>
        ///     determines if we should attempt to retry a call to Cosmos that resulted in an empty stream being returned
        /// </summary>
        /// <returns>false if the end of file appears legitimate, true if the read should be retried</returns>
        private async Task<bool> ShouldRetryEmptyResultReadAsync(
            long offset,
            long size)
        {
            // if the length of the stream we got back from ReadStreamAsync is 0, then it *should* mean that the
            //  server has no data for us for that range, so we *should* be able to treat it as end-of-file
            //  and exit if we hit the condition.
            // Sadly, this isn't the case as sometimes, Cosmos can return 0 bytes even though quite a lot of the
            //  file remains to be read, so we have to double check it against the expected size. Even better,
            //  though, the file size we get from Cosmos is not always reliable, so use the following guesses:
            //  1. if the size is exactly what we expect, return
            //  2. if the size read is less than the expected size, issue the request again a few times and then
            //      throw if we still get back empty
            //  3. if the size is less than what we've already read, attempt to fetch the size again

            CosmosStreamInfo newInfo;
            string name;

            if (this.cosmosInfo.Length == this.overallPosition)
            {
                return false;
            }

            // refresh the info we have about the stream as the file may have changed while we were reading it (unfortunate, but 
            //  may happen)
            newInfo = await this.client.GetStreamInfoAsync(this.cosmosInfo.StreamName, false, false);

            if (newInfo.Length == this.overallPosition)
            {
                this.cosmosInfo = newInfo;
                return false;
            }

            if (newInfo.Length > this.overallPosition)
            {
                return true;
            }

            (_, name) = CosmosFileSystemUtility.SplitNameAndPath(this.cosmosInfo.StreamName);

            throw new ChunkedReadException(
                errorCode: ChunkedReadErrorCode.ExtendedStreamLength,
                requestOffset: offset,
                requestSize: size,
                path: this.cosmosInfo.StreamName,
                name: name,
                size: newInfo.Length,
                message: "Error reading from stream {0}: read {1} bytes of {2} reported bytes".FormatInvariant(
                    this.cosmosInfo.StreamName,
                    this.overallPosition,
                    newInfo.Length));
        }

        /// <summary>
        ///     Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes
        ///      read
        /// </summary>
        /// <param name="buffer">
        ///     array of bytes whose contents in the range of offset and (offset + count - 1) will be replaced by the bytes read from 
        ///      the current source
        /// </param>
        /// <param name="offset">zero-based byte offset in buffer to start writing data</param>
        /// <param name="count">maximum number of bytes to be read</param>
        /// <param name="cancellationToken">token to monitor for cancellation requests</param>
        /// <returns>
        ///     total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes 
        ///      are not currently available, or zero if the end of the stream has been reached
        /// </returns>
        private async Task<int> ReadAsyncInternal(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            TimeSpan retryDelay = this.delayInitial;
            int retryAttempt = 0;
            int totalRead = 0;

            ArgumentCheck.ThrowIfNull(buffer, nameof(buffer));
            ArgumentCheck.ThrowIfLessThan(offset, 0, nameof(offset));
            ArgumentCheck.ThrowIfLessThan(count, 0, nameof(count));
            ArgumentCheck.ThrowIfLessThanOrEqualTo(buffer.Length, offset, nameof(offset), "offset must be less than buffer length");
            ArgumentCheck.ThrowIfLessThan(
                buffer.Length - offset, count, nameof(offset), "count must be less or equal to the buffer length minus offset");

            if (this.cosmosInfo == null)
            {
                throw new ObjectDisposedException(nameof(CosmosChunkedReadStream));
            }

            if (this.done)
            {
                return 0;
            }

            for (;;)
            {
                // if we don't have a current stream, fetch one
                if (this.currentStream == null)
                {
                    MemoryStream nextChunkLocal = null;
                    long fetchCount = Math.Max(count, this.chunkSize);

                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        nextChunkLocal = new MemoryStream();

                        DataInfo result =
                            await this.client
                                .ReadStreamAsync(
                                    this.cosmosInfo.StreamName,
                                    this.nextCosmosFetchOffset,
                                    fetchCount,
                                    false)
                                .ConfigureAwait(false);

                        if (result != null && result.Length > 0)
                        {
                            nextChunkLocal.Write(result.Data, 0, result.Length);
                        }
                        
                        if (nextChunkLocal.Length == 0)
                        {
                            if (await this.ShouldRetryEmptyResultReadAsync(this.nextCosmosFetchOffset, fetchCount))
                            {
                                if (retryAttempt >= this.maxRetries)
                                {
                                    const string ErrFmt =
                                        "Error reading from stream {0}: read {1} bytes of {2} reported bytes, but Cosmos is " +
                                        "handing us back an empty stream for {3} bytes at offset {4}";

                                    string name;

                                    (_, name) = CosmosFileSystemUtility.SplitNameAndPath(this.cosmosInfo.StreamName);

                                    throw new ChunkedReadException(
                                        errorCode: ChunkedReadErrorCode.EarlyStreamEnd,
                                        requestOffset: this.nextCosmosFetchOffset,
                                        requestSize: fetchCount,
                                        path: this.cosmosInfo.StreamName,
                                        name: name,
                                        size: this.cosmosInfo.Length,
                                        message: ErrFmt.FormatInvariant(
                                            this.cosmosInfo.StreamName, 
                                            this.overallPosition, 
                                            this.cosmosInfo.Length,
                                            fetchCount,
                                            this.nextCosmosFetchOffset));
                                }

                                await Task.Delay(retryDelay, cancellationToken);

                                retryDelay += this.delayIncrement;

                                ++retryAttempt;

                                continue;
                            }

                            this.currentStream = null;
                            this.done = true;

                            return totalRead;
                        }

                        // if we have a non-empty response stream, reset the retry info
                        retryDelay = this.delayInitial;
                        retryAttempt = 0;

                        nextChunkLocal.Seek(0, SeekOrigin.Begin);
                        this.currentStream = nextChunkLocal;
                        nextChunkLocal = null;

                        this.nextCosmosFetchOffset += this.currentStream.Length;
                    }
                    finally
                    {
                        // Close() isn't really required for a MemoryStream, but it is good practice to call Close() on streams
                        nextChunkLocal?.Close();
                    }
                }

                // if we still have pending data to add, pull it from the stream into the provided buffer
                if (count > 0)
                {
                    int countRead =
                        await this.currentStream.ReadAsync(buffer, offset, count, cancellationToken)
                            .ConfigureAwait(false);

                    this.overallPosition += countRead;
                    totalRead += countRead;

                    if (countRead == 0)
                    {
                        // Close() isn't really required for a MemoryStream, but it is good practice to call Close() on streams
                        this.currentStream.Close();
                        this.currentStream = null;
                    }

                    count -= countRead;
                    offset += countRead;
                }

                // if we've fulfilled the caller's request, return
                if (count == 0)
                {
                    return totalRead;
                }
            }
        }
    }
}
