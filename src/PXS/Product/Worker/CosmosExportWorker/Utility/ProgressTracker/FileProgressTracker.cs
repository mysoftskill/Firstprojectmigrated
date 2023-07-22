// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem;

    /// <summary>
    ///     file progress tracker
    /// </summary>
    public class FileProgressTracker : IFileProgressTracker
    {
        private readonly Utility.TraceLoggerAction errLogger;
        private readonly IFileSystemManager fileSysMgr;
        private readonly SemaphoreSlim asyncLock = new SemaphoreSlim(1);
        private readonly IClock clock;
        private readonly string filePath;
        private readonly string cv = Guid.NewGuid().ToString("N");
        private readonly int maxPendingBufferSize;

        private ConcurrentBag<LogItem> currentItems = new ConcurrentBag<LogItem>();
        private ICollection<LogItem> pendingItems;
        private IFile file;
        private int id;

        /// <summary>
        ///     Initializes a new instance of the FileProgressTracker class
        /// </summary>
        /// <param name="fileSystemManager">file system manager</param>
        /// <param name="clock">clock</param>
        /// <param name="agentId">agent id</param>
        /// <param name="name">name</param>
        /// <param name="errorLogger">error logger</param>
        /// <param name="maxPendingBufferSize">maximum pending buffer size</param>
        public FileProgressTracker(
            IFileSystemManager fileSystemManager,
            IClock clock,
            string agentId,
            string name,
            Utility.TraceLoggerAction errorLogger,
            int maxPendingBufferSize)
        {
            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(agentId, nameof(agentId));
            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(name, nameof(name));

            this.fileSysMgr = fileSystemManager ?? throw new ArgumentNullException(nameof(fileSystemManager));
            this.errLogger = errorLogger ?? throw new ArgumentNullException(nameof(errorLogger));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));

            this.filePath =
                this.fileSysMgr.ActivityLog.RootDirectory +
                Utility.EnsureHasTrailingSlashButNoLeadingSlash(agentId) +
                Utility.EnsureNoLeadingSlash(name) + 
                ".tsv";

            this.maxPendingBufferSize = Math.Max(0, maxPendingBufferSize);
        }

        /// <summary>
        ///     Adds the message to the internal message buffer
        /// </summary>
        /// <param name="type">message type</param>
        /// <param name="format">message format string</param>
        /// <param name="args">message replacement parameters</param>
        public void AddMessage(
            string type, 
            string format, 
            params object[] args)
        {
            string msg;

            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(format, nameof(format));
            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(type, nameof(type));

            try
            {
                msg = args?.Length > 0 ? format.FormatInvariant(args) : format;
            }
            catch (FormatException)
            {
                msg = "FORMAT STRING PROBLEM: " + format;
            }

            // Cosmos gives special meaning to these characters, so escape them
            msg = msg
                .Replace("\r", "##LF##")
                .Replace("\n", "##CR##")
                .Replace("\t", "##TAB##")
                .Replace("#", "##HASH##");

            this.currentItems.Add(new LogItem(this.clock.UtcNow, type, msg, this.id++));
        }

        /// <summary>
        ///     Persists the internal message buffer to storage
        /// </summary>
        /// <returns>resulting value</returns>
        public async Task PersistAsync()
        {
            ICollection<LogItem> toWrite = null;
            string errCtx = "starting";

            // use a semaphore becuase it isn't owned by a thread and therefore can survive async operations
            await this.asyncLock.WaitAsync().ConfigureAwait(false);

            try
            {
                ConcurrentBag<LogItem> current = this.currentItems;
                StringBuilder sb = new StringBuilder();

                // ensure no one is adding stuff to the list of messages by swapping the 'write' bag with a new one 
                this.currentItems = new ConcurrentBag<LogItem>();

                toWrite = this.pendingItems;

                // open the file before attempting to construct the data to write to allow some time to pass to minimize the risk
                //  of missing an entry that was in the middle of being added when we swapped out the existing bag
                if (this.file == null)
                {
                    errCtx = "creating or opening file";

                    this.file = await this.fileSysMgr.ActivityLog
                        .CreateFileAsync(this.filePath, this.fileSysMgr.ActivityLog.DefaultLifetime, FileCreateMode.OpenExisting);
                }
                else
                {
                    // if we don't spend the time attempting to create the file, then just wait a bit
                    await Task.Delay(50);
                }

                toWrite = (toWrite?.Union(current) ?? current).OrderBy(o => o.Id).ToList();

                if (toWrite.Count == 0)
                {
                    return;
                }

                errCtx = "building string to write";

                foreach (LogItem item in toWrite)
                {
                    sb.Append(item.Time.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss"));
                    sb.Append('\t');
                    sb.Append(this.cv);
                    sb.Append('\t');
                    sb.Append(item.Type);
                    sb.Append('\t');
                    sb.AppendLine(item.Message);

                    if (sb.Length > this.maxPendingBufferSize)
                    {
                        errCtx = "writing to file";

                        await this.file.AppendAsync(sb.ToString());

                        sb.Clear();

                        errCtx = "building string to write";
                    }
                }

                if (sb.Length > 0)
                {
                    errCtx = "writing to file";

                    await this.file.AppendAsync(sb.ToString());
                }

                this.pendingItems = null;
                toWrite = null;
            }
            catch(Exception e)
            {
                this.errLogger($"Failed to write progress tracking data to {this.filePath} while {errCtx}: {e}");

                // don't fail processing if we can't write to the store as we might be able to try again later
                if (e is IOException == false)
                {
                    throw;
                }
            }
            finally
            {
                this.pendingItems = toWrite;
                this.asyncLock.Release();
            }
        }

        /// <summary>
        ///     a single activity log item
        /// </summary>
        private class LogItem
        {
            /// <summary>
            ///     Initializes a new instance of the LogItem class
            /// </summary>
            /// <param name="time">time of the message</param>
            /// <param name="type">message type</param>
            /// <param name="message">message text</param>
            /// <param name="id">unique id for proper sorting</param>
            public LogItem(
                DateTimeOffset time,
                string type,
                string message,
                int id)
            {
                this.Message = message;
                this.Type = type;
                this.Time = time;
                this.Id = id;
            }

            public DateTimeOffset Time { get; }
            public string Message { get; }
            public string Type { get; }
            public int Id { get; }
        }
    }
}
