// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem.Cosmos;

    /// <summary>
    ///     periodic file writer
    /// </summary>
    public class PeriodicFileWriter : IPeriodicFileWriter
    {
        private readonly Func<DateTimeOffset, string> fileNameGenerator;
        private readonly ICosmosFileSystem fileSystem;
        private readonly SemaphoreSlim getLock = new SemaphoreSlim(1);
        private readonly SemaphoreSlim flushLock = new SemaphoreSlim(1);
        private readonly string basePath;

        private readonly TimeSpan period;
        private readonly IClock clock;

        private volatile ConcurrentBag<(DateTimeOffset Expires, IQueuedFileWriter Writer)> previous;
        private volatile IQueuedFileWriter current;

        private long writerExpiryTicks;

        /// <summary>
        ///     Initializes a new instance of the PeriodicFileWriter class
        /// </summary>
        /// <param name="fileSystem">file system</param>
        /// <param name="pathSuffix">path suffix</param>
        /// <param name="fileNameGenerator">file name generator</param>
        /// <param name="period">time period after which new files are generated</param>
        /// <param name="clock">time clock</param>
        public PeriodicFileWriter(
            ICosmosFileSystem fileSystem,
            string pathSuffix,
            Func<DateTimeOffset, string> fileNameGenerator,
            TimeSpan period, 
            IClock clock)
        {
            this.fileNameGenerator = fileNameGenerator ?? throw new ArgumentNullException(nameof(fileNameGenerator));
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

            this.basePath = fileSystem.RootDirectory;

            if (string.IsNullOrWhiteSpace(pathSuffix) == false)
            {
                this.basePath += Utility.EnsureHasTrailingSlashButNoLeadingSlash(pathSuffix);
            }
                
            this.period = ArgumentCheck.ReturnIfLessThanOrEqualToElseThrow(period, TimeSpan.Zero, nameof(period));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));

            this.previous = new ConcurrentBag<(DateTimeOffset Expires, IQueuedFileWriter Writer)>();
        }

        /// <summary>
        ///     Gets previous count
        /// </summary>
        public int PreviousCount => this.previous?.Count ?? 0;

        /// <summary>
        ///     Queues the write asynchronous
        /// </summary>
        /// <param name="data">data to write</param>
        /// <returns>resulting value</returns>
        public async Task QueueWriteAsync(string data)
        {
            IQueuedFileWriter writer = await this.GetWriter().ConfigureAwait(false);
            await writer.QueueWriteAsync(data).ConfigureAwait(false);
        }

        /// <summary>
        ///     Flushes the queue
        /// </summary>
        /// <param name="cancelToken">cancel token</param>
        /// <returns>resulting value</returns>
        public async Task FlushQueueAsync(CancellationToken cancelToken)
        {
            ConcurrentBag<(DateTimeOffset Expires, IQueuedFileWriter Writer)> oldPrevious;
            (DateTimeOffset Expires, IQueuedFileWriter Writer) currentItem = (DateTimeOffset.MinValue, null);
            IQueuedFileWriter currentWriter = this.current;
            DateTimeOffset now = this.clock.UtcNow;

            oldPrevious = this.previous;
            this.previous = new ConcurrentBag<(DateTimeOffset Expires, IQueuedFileWriter Writer)>();
            
            await this.flushLock.WaitAsync(cancelToken);

            try
            {
                while (oldPrevious.TryTake(out currentItem))
                {
                    await currentItem.Writer.FlushQueueAsync(cancelToken).ConfigureAwait(false);

                    if (currentItem.Expires > now)
                    {
                        this.previous.Add(currentItem);
                    }
                }

                if (currentWriter != null)
                {
                    await currentWriter.FlushQueueAsync(cancelToken).ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                if (currentItem.Writer != null)
                {
                    this.previous.Add(currentItem);
                }

                foreach ((DateTimeOffset Expires, IQueuedFileWriter Writer) item in oldPrevious)
                {
                    this.previous.Add(item);
                }
            }
            finally
            {
                this.flushLock.Release();
            }
        }

        /// <summary>
        ///     Gets the writer to write the data to
        /// </summary>
        /// <returns>resulting value</returns>
        public async Task<IQueuedFileWriter> GetWriter()
        {
            DateTimeOffset now = this.clock.UtcNow;
            DateTimeOffset expiry;

            // volatile keyword is not supported for 64bit intrinsics. Sadness.
            expiry = new DateTimeOffset(Volatile.Read(ref this.writerExpiryTicks), TimeSpan.Zero);
            
            if (this.current == null || now > expiry)
            {
                await this.getLock.WaitAsync().ConfigureAwait(false);

                try
                {
                    expiry = new DateTimeOffset(Volatile.Read(ref this.writerExpiryTicks), TimeSpan.Zero);

                    // assume period length >> the time to create a new file so that we can just use the already computed 'now'
                    if (this.current == null || now > expiry)
                    {
                        IQueuedFileWriter writer;
                        DateTimeOffset newExpiry = now + this.period;

                        writer = await this.fileSystem
                            .CreateQueuedFileWriterAsync(
                                this.basePath + this.fileNameGenerator(now),
                                this.fileSystem.DefaultLifetime,
                                FileCreateMode.OpenExisting)
                            .ConfigureAwait(false);

                        if (this.current != null)
                        {
                            this.previous.Add((newExpiry, this.current));
                        }

                        this.current = writer;
                        Volatile.Write(ref this.writerExpiryTicks, newExpiry.Ticks);
                    }
                }
                finally
                {
                    this.getLock.Release();
                }
            }

            return this.current;
        }
    }
}
