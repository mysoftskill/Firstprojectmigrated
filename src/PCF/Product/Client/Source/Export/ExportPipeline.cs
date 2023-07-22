namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Microsoft.PrivacyServices.Policy;

    /// <summary>
    /// This class is the main entry point for client code to export data, and handles both
    /// serializing the data and uploading it in a pipeline. Serialization happens
    /// synchronously per record and then is queued to a worker task to upload. If the
    /// upload task gets too backed up, serialization will halt and wait. This is designed
    /// as such to ensure streaming data at a maximum rate by serializing and uploading
    /// happening in parallel to maximize total throughput.
    /// 
    /// You MUST <see cref="Dispose"/> this class before completing the command, as Dispose
    /// will ensure all data that is being uploaded has completed.
    /// </summary>
    public sealed class ExportPipeline : IDisposable
    {
        /// <summary>
        /// Use this productId only for testing. This will put files into a 'Miscellaneous' folder in the final output for the user.
        /// </summary>
        public const int UnknownProductId = 0;

        private readonly IExportDestination destination;
        private readonly IExportSerializer serializer;
        private readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1);
        private readonly ConcurrentDictionary<string, ExportFilePump> openFiles = new ConcurrentDictionary<string, ExportFilePump>();

        private readonly bool isCompressed;

        /// <summary>
        /// Creates a new export pipeline with a particular serializer to a specific destination.
        /// </summary>
        /// <param name="serializer">The serializer to use to export. <see cref="JsonExportSerializer" /> is recommended.</param>
        /// <param name="destination">The <see cref="IExportDestination" /> to export to.</param>'
        /// <param name="isCompressed">Compression status</param>
        public ExportPipeline(
            IExportSerializer serializer,
            IExportDestination destination,
            bool isCompressed = false)
        {
            this.serializer = serializer;
            this.destination = destination;
            this.isCompressed = isCompressed;
        }

        /// <summary>
        /// How much we're willing to buffer per-file.
        /// </summary>
        internal long MaxInternalBufferPerFile { get; set; } = 10 * 1024 * 1024;

        /// <summary>
        ///     Disposes the pipeline, gracefully waits for all queued data to finish.
        /// 
        ///     Dispose MUST be called before completing the export command, in order to have
        ///     a complete set of data uploaded.
        /// </summary>
        public void Dispose()
        {
            this.semaphoreSlim.Dispose();

            List<Task> disposeTasks = new List<Task>();
            foreach (ExportFilePump pump in this.openFiles.Values)
            {
                disposeTasks.Add(Task.Run(() => pump.Dispose()));
            }

            // Wait for all.
            Task.WhenAll(disposeTasks).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Exports as a particular data type a particular record.
        /// </summary>
        /// <param name="dataType">The data type being exported.</param>
        /// <param name="data">The piece of data to append to the file.</param>
        /// <returns>A task that completes when the export has been successful.</returns>
        [Obsolete("Use ExportAsync() with timestamp and correlationId parameters")]
        public Task ExportAsync(DataTypeId dataType, object data)
        {
            return this.ExportAsync(ExportProductId.Unknown, dataType, DateTimeOffset.UtcNow, string.Empty, data);
        }

        /// <summary>
        /// Exports as a particular data type a particular record.
        /// </summary>
        /// <param name="dataType">The data type being exported.</param>
        /// <param name="timestamp">The timestamp of the data.</param>
        /// <param name="correlationId">The correlation id of the data.</param>
        /// <param name="data">The piece of data to append to the file.</param>
        /// <returns>A task that completes when the export has been successful.</returns>
        [Obsolete("Use ExportAsync() with a product id")]
        public Task ExportAsync(DataTypeId dataType, DateTimeOffset timestamp, string correlationId, object data)
        {
            return this.ExportAsync(ExportProductId.Unknown, $"{dataType.Value}.json", timestamp, correlationId, data);
        }

        /// <summary>
        /// Exports as a particular data type a particular record.
        /// </summary>
        /// <param name="productId">The productId this data is from.</param>
        /// <param name="dataType">The data type being exported.</param>
        /// <param name="timestamp">The timestamp of the data.</param>
        /// <param name="correlationId">The correlation id of the data.</param>
        /// <param name="data">The piece of data to append to the file.</param>
        /// <param name="originalFileSize">Original File size in case of isCompressed true</param>
        /// <returns>A task that completes when the export has been successful.</returns>
        public Task<ExportedFileSizeDetails> ExportAsync(
            ExportProductId productId, 
            DataTypeId dataType, 
            DateTimeOffset timestamp, 
            string correlationId, 
            object data, 
            long originalFileSize = 0)
        {
            return this.ExportAsync(productId, $"{dataType.Value}.json", timestamp, correlationId, data, originalFileSize);
        }

        /// <summary>
        ///     Exports to a particular filename a particular record.
        /// </summary>
        /// <param name="fileName">The name of the file to export to.</param>
        /// <param name="data">The piece of data to append to the file.</param>
        /// <returns>A task that completes when the export has been successful.</returns>
        [Obsolete("Use ExportAsync() with timestamp and correlationId parameters")]
        public Task ExportAsync(string fileName, object data)
        {
            return this.ExportAsync(ExportProductId.Unknown, fileName, DateTimeOffset.UtcNow, string.Empty, data);
        }

        /// <summary>
        /// Exports to a particular filename a particular record.
        /// </summary>
        /// <param name="fileName">The name of the file to export to.</param>
        /// <param name="timestamp">The timestamp of the data.</param>
        /// <param name="correlationId">The correlation id of the data.</param>
        /// <param name="data">The piece of data to append to the file.</param>
        /// <returns>A task that completes when the export has been successful.</returns>
        [Obsolete("Use ExportAsync() with a product id")]
        public Task ExportAsync(string fileName, DateTimeOffset timestamp, string correlationId, object data)
        {
            return this.ExportAsync(ExportProductId.Unknown, fileName, timestamp, correlationId, data);
        }

        /// <summary>
        /// Exports to a particular filename a particular record.
        /// </summary>
        /// <param name="productId">The productId this data is from.</param>
        /// <param name="fileName">The name of the file to export to.</param>
        /// <param name="timestamp">The timestamp of the data.</param>
        /// <param name="correlationId">The correlation id of the data.</param>
        /// <param name="data">The piece of data to append to the file.</param>
        /// <param name="originalFileSize">Original File size in case of isCompressed true</param>
        /// <returns>A task that completes when the export has been successful.</returns>
        public Task<ExportedFileSizeDetails> ExportAsync(
            ExportProductId productId, 
            string fileName, 
            DateTimeOffset timestamp, 
            string correlationId, 
            object data, 
            long originalFileSize = 0)
        {
            // Since this code happens before any await, it is done on the calling thread.
            var stream = new MemoryStream();
            this.serializer.Serialize(timestamp, correlationId, data, stream);

            return this.EnqueueStreamAsync(productId, fileName, stream, originalFileSize);
        }

        /// <summary>
        /// Exports to a particular filename a particular record.
        /// </summary>
        /// <param name="productId">The productId this data is from.</param>
        /// <param name="dataType">The data type being exported.</param>
        /// <param name="jsonData">The piece of data to append to the file as a properly formatted JSON serialized string.</param>
        /// <param name="originalFileSize">Original File size in case of isCompressed true</param>
        /// <returns>A task that completes when the export has been successful.</returns>
        public Task<ExportedFileSizeDetails> ExportAsync(
            ExportProductId productId, 
            DataTypeId dataType, 
            string jsonData, 
            long originalFileSize = 0)
        {
            return this.ExportAsync(productId, $"{dataType.Value}.json", jsonData, originalFileSize);
        }

        /// <summary>
        /// Exports to a particular filename a particular record.
        /// </summary>
        /// <param name="productId">The productId this data is from.</param>
        /// <param name="fileName">The name of the file to export to.</param>
        /// <param name="jsonData">The piece of data to append to the file as a properly formatted JSON serialized string.</param>
        /// <param name="originalFileSize">Original File size in case of isCompressed true</param>
        /// <returns>A task that completes when the export has been successful.</returns>
        public Task<ExportedFileSizeDetails> ExportAsync(
            ExportProductId productId, 
            string fileName, 
            string jsonData, 
            long originalFileSize = 0)
        {
            // Since this code happens before any await, it is done on the calling thread.
            var stream = new MemoryStream(JsonExportSerializer.Utf8NoBom.GetBytes(jsonData));
            return this.EnqueueStreamAsync(productId, fileName, stream, originalFileSize);
        }

        /// <summary>
        /// Export a specific file with binary data.
        /// </summary>
        /// <param name="fileName">Filename in the container to write.</param>
        /// <param name="stream">The binary stream to write.</param>
        /// <returns>A <see cref="Task" /> indicating when the operation has completed.</returns>
        [Obsolete("Use ExportAsync() with a product id")]
        public Task ExportAsync(string fileName, Stream stream)
        {
            return this.ExportAsync(ExportProductId.Unknown, fileName, stream);
        }

        /// <summary>
        /// Export a specific file with binary data.
        /// </summary>
        /// <param name="productId">The productId this data is from.</param>
        /// <param name="fileName">Filename in the container to write.</param>
        /// <param name="stream">The binary stream to write.</param>
        /// <param name="originalFileSize">Original File size in case of isCompressed true</param>
        /// <returns>A <see cref="Task" /> indicating when the operation has completed.</returns>
        public async Task<ExportedFileSizeDetails> ExportAsync(
            ExportProductId productId, 
            string fileName, 
            Stream stream, 
            long originalFileSize = 0)
        {
            fileName = $"{productId.Id}/{fileName}";

            // Open the destination file
            using (IExportFile file = await this.destination.GetOrCreateFileAsync(fileName).ConfigureAwait(false))
            {
                // Write the actual data
                long streamLength = await file.AppendAsync(stream).ConfigureAwait(false);

                return new ExportedFileSizeDetails(fileName, streamLength, this.isCompressed, originalFileSize);
            }
        }

        /// <summary>
        /// Enqueues a memory stream for writing to the file store
        /// </summary>
        /// <param name="productId">The productId this data is from.</param>
        /// <param name="fileName">Filename in the container to write.</param>
        /// <param name="stream">stream</param>
        /// <param name="originalFileSize">Original File size in case of isCompressed true</param>
        /// <exception cref="TimeoutException">If the queue is stalled or the background worker has died.</exception>
        /// <returns>A task that completes when the export has been successful.</returns>
        private async Task<ExportedFileSizeDetails> EnqueueStreamAsync(ExportProductId productId, string fileName, MemoryStream stream, long originalFileSize = 0)
        {
            fileName = $"{productId.Id}/{fileName}";

            stream.Seek(0, SeekOrigin.Begin);

            if (!this.openFiles.TryGetValue(fileName, out ExportFilePump pump))
            {
                await this.semaphoreSlim.WaitAsync().ConfigureAwait(false);
                try
                {
                    if (!this.openFiles.TryGetValue(fileName, out pump))
                    {
                        IExportFile file = await this.destination.GetOrCreateFileAsync(fileName).ConfigureAwait(false);

                        pump = new ExportFilePump(file, this.serializer, this.MaxInternalBufferPerFile);
                        this.openFiles[fileName] = pump;
                    }
                }
                finally
                {
                    this.semaphoreSlim.Release();
                }
            }

            long streamLength = await pump.WriteAsync(stream).ConfigureAwait(false);
            return new ExportedFileSizeDetails(fileName, streamLength, this.isCompressed, originalFileSize);
        }

        private sealed class ExportFilePump : IDisposable
        {
            private readonly long maxPendingBytes;

            // Small fragments, waiting to be aggregated together.
            private readonly ConcurrentQueue<MemoryStream> pendingWrites = new ConcurrentQueue<MemoryStream>();

            // Wake up the flush thread when there is data available.
            private readonly AutoResetEvent flushAvailableEvent = new AutoResetEvent(false);

            private readonly IExportSerializer serializer;
            private readonly IExportFile file;
            private readonly Task flushTask;

            private bool isFirstWrite = true;
            private bool disposed = false;
            private long currentPendingBytes = 0;

            public ExportFilePump(IExportFile file, IExportSerializer serializer, long maxBuffer)
            {
                this.maxPendingBytes = maxBuffer;
                this.file = file;
                this.serializer = serializer;
                this.flushTask = this.FlushTask();
            }

            public void Dispose()
            {
                try
                {
                    this.flushAvailableEvent.Set();

                    while (this.pendingWrites.Any())
                    {
                        // Wait for outstanding writes to complete. Throw if the flush task fails.
                        this.ThrowExceptionIfFlushTaskFaulted();
                        Thread.Sleep(100);
                    }

                    this.disposed = true;
                    this.flushTask.GetAwaiter().GetResult();
                    this.serializer.WriteFilePostfixAsync(this.file).GetAwaiter().GetResult();
                }
                finally
                {
                    this.disposed = true;

                    // Dispose the file and the auto reset event.
                    this.file.Dispose();
                    this.flushAvailableEvent.Dispose();
                }
            }

            public async Task<long> WriteAsync(MemoryStream data)
            {
                long streamLength = data.Length;
                DateTimeOffset startTime = DateTimeOffset.UtcNow;

                while (this.currentPendingBytes > this.maxPendingBytes)
                {
                    this.ThrowExceptionIfFlushTaskFaulted();

                    if (DateTimeOffset.UtcNow - startTime >= TimeSpan.FromMinutes(2))
                    {
                        throw new TimeoutException("Couldn't push to queue. Exporting has likely stalled.");
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
                }

                Interlocked.Add(ref this.currentPendingBytes, data.Length);
                this.pendingWrites.Enqueue(data);

                // Signal that there is work to be done.
                this.flushAvailableEvent.Set();

                return streamLength;
            }

            private void ThrowExceptionIfFlushTaskFaulted()
            {
                if (this.flushTask.IsFaulted)
                {
                    throw new TimeoutException(
                        this.flushTask.Exception.InnerException.Message,
                        this.flushTask.Exception.InnerException);
                }
            }

            private async Task FlushTask()
            {
                await Task.Yield();

                while (true)
                {
                    if (this.pendingWrites.Count == 0 && this.disposed)
                    {
                        return;
                    }

                    if (this.pendingWrites.TryDequeue(out MemoryStream stream))
                    {
                        stream.Position = 0;

                        if (this.isFirstWrite)
                        {
                            await this.serializer.WriteFilePrefixAsync(this.file).ConfigureAwait(false);
                            this.isFirstWrite = false;
                        }
                        else
                        {
                            await this.serializer.WriteEntryDelimiterAsync(this.file).ConfigureAwait(false);
                        }

                        await this.file.AppendAsync(stream).ConfigureAwait(false);

                        Interlocked.Add(ref this.currentPendingBytes, -1 * stream.Length);
                        stream.Dispose();
                    }
                    else if (this.pendingWrites.Count == 0)
                    {
                        this.flushAvailableEvent.WaitOne(100);
                    }
                }
            }
        }
    }
}
