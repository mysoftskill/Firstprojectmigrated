// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Utilities;

    /// <summary>
    ///     implementation of a queued writer for Cosmos
    /// </summary>
    public class QueuedFileWriter : IQueuedFileWriter
    {
        private readonly ConcurrentQueue<string> pending = new ConcurrentQueue<string>();
        private readonly SemaphoreSlim writerLock = new SemaphoreSlim(1);
        private readonly IFile target;
        private readonly int writeChunkSize;

        /// <summary>
        ///     Initializes a new instance of the QueuedWriter class
        /// </summary>
        /// <param name="target">target file</param>
        /// <param name="writeChunkSize">size of data to write in a single chunk</param>
        public QueuedFileWriter(
            IFile target,
            int writeChunkSize)
        {
            this.writeChunkSize = Math.Max(writeChunkSize, 0);
            this.target = target ?? throw new ArgumentNullException(nameof(target));
        }

        /// <summary>
        ///     queues data to be written to the store
        /// </summary>
        /// <param name="data">data to queue for writing</param>
        /// <returns>resulting value</returns>
        public Task QueueWriteAsync(string data)
        {
            if (string.IsNullOrWhiteSpace(data) == false)
            {
                this.pending.Enqueue(data);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        ///     sends the pending data to the store
        /// </summary>
        /// <param name="cancelToken">cancel token</param>
        /// <returns>resulting value</returns>
        public async Task FlushQueueAsync(CancellationToken cancelToken)
        {
            await this.writerLock.WaitAsync(cancelToken).ConfigureAwait(false);

            try
            {
                StringBuilder sb = null;

                async Task Transmit(string data)
                {
                    try
                    {
                        await this.target.AppendAsync(data).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        this.pending.Enqueue(data);
                        throw;
                    }
                }

                for (; ; )
                {
                    string item;

                    if (cancelToken.IsCancellationRequested)
                    {
                        if (sb != null)
                        {
                            this.pending.Enqueue(sb.ToString());
                        }

                        cancelToken.ThrowIfCancellationRequested();
                    }

                    if (this.pending.TryDequeue(out item) == false)
                    {
                        if (sb != null)
                        {
                            await Transmit(sb.ToString()).ConfigureAwait(false);
                        }

                        break;
                    }

                    // skip allocating and copying into a stringbuilder if we don't already have one and the data size already
                    //  meets the threshold at which we'll write.
                    if (sb == null && item.Length >= this.writeChunkSize)
                    {
                        await Transmit(item).ConfigureAwait(false);
                        continue;
                    }

                    sb = sb ?? new StringBuilder();
                    sb.Append(item);

                    if (sb.Length >= this.writeChunkSize)
                    {
                        await Transmit(sb.ToString()).ConfigureAwait(false);
                        sb = null;
                    }
                }
            }
            finally
            {
                this.writerLock.Release();
            }
        }
    }
}
