namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.Azure.Storage;
    using Microsoft.Azure.Storage.Queue;
    using Microsoft.PrivacyServices.Common.Azure;
    using Newtonsoft.Json;

    /// <summary>
    /// Interface that supports publishing to a queue.
    /// </summary>
    public interface IAzureWorkItemQueuePublisher<TWorkItem>
    {
        /// <summary>
        /// Publishes the given work item to the queue.
        /// </summary>
        Task PublishAsync(TWorkItem workItem, TimeSpan? visibilityDelay = null);

        /// <summary>
        /// Publishes a work item that can be split into multiple sub-items.
        /// </summary>
        /// <param name="items">The total set of items to be published. May be recursively split across separate queue items.</param>
        /// <param name="workItemBuilder">A work item constructor that accepts a set of sub-items.</param>
        /// <param name="getVisibilityDelay">The a function to get the desired visibility delay according to the work item's absolute position in the binary tree.</param>
        Task PublishWithSplitAsync<T>(
            IEnumerable<T> items,
            Func<IEnumerable<T>, TWorkItem> workItemBuilder,
            Func<int, TimeSpan> getVisibilityDelay);
    }

    /// <summary>
    /// An interface that handles work items in an Azure Queue.
    /// </summary>
    public interface IAzureWorkItemQueueHandler<TWorkItem>
    {
        /// <summary>
        /// The relative priority of this work item.
        /// </summary>
        SemaphorePriority WorkItemPriority { get; }

        /// <summary>
        /// Processes the given work item.
        /// </summary>
        Task<QueueProcessResult> ProcessWorkItemAsync(QueueWorkItemWrapper<TWorkItem> wrapper);
    }

    /// <summary>
    /// A class for an Azure-queue based work item queue. This class acts as both a processor harness and a publisher,
    /// which can be invoked independently depending on the use case. For processing, the logic is delegated
    /// to an internal IAzureWorkItemQueueHandler.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    public class AzureWorkItemQueue<TWorkItem> : IAzureWorkItemQueuePublisher<TWorkItem>
    {
        // Thresholds for how frequently we'll pull from the queue.
        private const int MinQueueBackoffMs = 0;

        private const int MaxWaitInSeconds = 60;

        /// <summary>
        /// Max bytes Azure will let us publish. The true limit is 65536, but we round down for safety.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes")]
        public const int MaxAzureQueueMessageSize = 40000;

        private readonly IAzureCloudQueue[] azureQueues;

        // Queues that are having issues. Anything in here is disabled for publishes.
        private readonly HashSet<IAzureCloudQueue> deadQueues = new HashSet<IAzureCloudQueue>();
        private readonly object syncRoot = new object();

        public AzureWorkItemQueue(string queueName = null) : this(TimeSpan.FromMinutes(5), queueName)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="defaultLeasePeriod">The default lease period for the message in the queue</param>
        /// <param name="queueName">The name of the queue. If not specified, it defaults to the work item name.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase")]
        public AzureWorkItemQueue(TimeSpan defaultLeasePeriod, string queueName = null)
        {
            this.deadQueues = new HashSet<IAzureCloudQueue>();

            if (string.IsNullOrEmpty(queueName))
            {
                queueName = typeof(TWorkItem).Name;
            }

            queueName = queueName.ToLowerInvariant();

            this.azureQueues =
                Config.Instance.AzureStorageAccounts
                    .Select(CloudStorageAccount.Parse)
                    .Select(x => x.CreateCloudQueueClient())
                    .Select(x => x.GetQueueReference(queueName))
                    .Select(x => new AzureQueueCloudQueue(x, defaultLeasePeriod))
                    .ToArray();
        }

        /// <summary>
        /// Test hook constructor.
        /// </summary>
        internal AzureWorkItemQueue(IAzureCloudQueue[] innerQueues)
        {
            this.azureQueues = innerQueues;
        }

        /// <summary>
        /// A soft limit on the number of pending work items. If above this value, we won't fetch new work items, 
        /// but will work on everything we have. This value plus batch size indicates the maximum degree of 
        /// concurrent processing.
        /// </summary>
        public int SoftPendingWorkItemLimit { get; set; } = 100;

        /// <summary>
        /// The number of items to retrieve in a call.
        /// </summary>
        /// <remarks>
        /// 32 is the max batch size that Azure supports.
        /// </remarks>
        public int BatchSize { get; set; } = 32;

        /// <summary>
        /// Minimum number of seconds to back off in the event of a processing exception.
        /// </summary>
        public int MinExceptionBackoffSeconds { get; set; } = 60;

        /// <summary>
        /// Maximum number of seconds to back off in the event of a processing exception.
        /// </summary>
        public int MaxExceptionBackoffSeconds { get; set; } = (int)TimeSpan.FromHours(1).TotalSeconds;

        /// <summary>
        /// Amount of time to back off in the event of a processing exception. This is a random value designed to spread failures out.
        /// </summary>
        public TimeSpan ExceptionBackoff => TimeSpan.FromSeconds(RandomHelper.Next(this.MinExceptionBackoffSeconds, this.MaxExceptionBackoffSeconds));

        /// <summary>
        /// Publishes a work item that can be split into multiple sub-items.
        /// </summary>
        /// <param name="items">The total set of items to be published. May be recursively split across separate queue items.</param>
        /// <param name="workItemBuilder">A work item constructor that accepts a set of sub-items.</param>
        /// <param name="getVisibilityDelay">The a function to get the desired visibility delay according to the work item's absolute position in the binary tree.</param>   
        /// <remarks>
        /// Azure queues have a limit of ~64kb. We often get batches of events that are larger than that.
        /// In order to accommodate large batches, we split this work item into multiple work items. Each
        /// sub-item therefore has a unique position in the binary tree:
        ///
        ///     0
        ///    / \
        ///   1   2
        ///  / \ / \
        /// 3  4 5  6
        /// 
        /// If N is the current node, the left child's position is 2N + 1, and the right child's position is 2N + 2.
        /// 
        /// The "position" parameter indicates the position of the given node in the tree. This is useful
        /// to use because it allows us to set a delay on the amount of time before a given work item executes,
        /// avoiding conflicts between different chunks of work. This is useful, since these work items all
        /// touch the same cold storage record, so we don't want them to all run concurrently.
        /// 
        /// See the "PublishWithSplitAsync" method for more details.
        /// </remarks>
        public Task PublishWithSplitAsync<T>(
            IEnumerable<T> items,
            Func<IEnumerable<T>, TWorkItem> workItemBuilder,
            Func<int, TimeSpan> getVisibilityDelay)
        {
            return this.RecursivePublishWithSplitAsync(0, items, items.Count(), workItemBuilder, getVisibilityDelay);
        }

        private async Task RecursivePublishWithSplitAsync<T>(
            int position,
            IEnumerable<T> items,
            int count,
            Func<IEnumerable<T>, TWorkItem> workItemBuilder,
            Func<int, TimeSpan> getVisibilityDelay)
        {
            TWorkItem workItem = workItemBuilder(items);
            TimeSpan? delay = getVisibilityDelay?.Invoke(position);
            byte[] publishBytes = Package(workItem);

            if (count == 0)
            {
                // Nothing to publish.
                // Intentionally left empty.
            }
            else if (count == 1 || publishBytes.Length <= MaxAzureQueueMessageSize)
            {
                // If there is only one item, then we can't split.
                // If the total length is less than the max, then we don't need to split.
                await this.PublishBytesAsync(publishBytes, delay);
            }
            else
            {
                // Try to split into equal halves.
                int leftCount = count / 2;
                int rightCount = count - leftCount;

                IEnumerable<T> leftItems = items.Take(leftCount);
                IEnumerable<T> rightItems = items.Skip(leftCount);

                Task leftPublish = this.RecursivePublishWithSplitAsync((2 * position) + 1, leftItems, leftCount, workItemBuilder, getVisibilityDelay);
                Task rightPublish = this.RecursivePublishWithSplitAsync((2 * position) + 2, rightItems, rightCount, workItemBuilder, getVisibilityDelay);

                await Task.WhenAll(leftPublish, rightPublish);
            }
        }

        /// <summary>
        /// Publishes to a random queue.
        /// </summary>
        public Task PublishAsync(TWorkItem workItem, TimeSpan? visibilityDelay = null)
        {
            byte[] body = Package(workItem);
            return this.PublishBytesAsync(body, visibilityDelay);
        }

        /// <summary>
        /// Publishes the given byte array to a random queue.
        /// </summary>
        private async Task PublishBytesAsync(byte[] body, TimeSpan? visibilityDelay)
        {
            if (body.Length > MaxAzureQueueMessageSize)
            {
                throw new MessageTooLargeException();
            }

            IncomingEvent.Current?.SetProperty("BodyLength", body.Length.ToString());

            int startIndex = RandomHelper.Next();
            int max = this.azureQueues.Length;
            int count = 0;

            while (count < max)
            {
                var queue = this.azureQueues[(startIndex + count) % max];
                CloudQueueMessage message = null;

                try
                {
                    count++;

                    if (this.deadQueues.Contains(queue))
                    {
                        // This queue is having issues (or we are having issues with it).
                        // Don't publish for now.
                        continue;
                    }

                    // Flight to temporarily disable publishing to specific azure queues
                    if (FlightingUtilities.IsStringValueEnabled(FlightingNames.AzureQueuePublisherDisabled, queue.AccountName))
                    {
                        continue;
                    }


                    await Logger.InstrumentAsync(
                        new OutgoingEvent(SourceLocation.Here()),
                        async ev =>
                        {
                            message = new CloudQueueMessage(body);

                            ev["AccountName"] = queue.AccountName;
                            ev["QueueName"] = queue.QueueName;
                            ev["MessageId"] = message.Id;

                            // Throws if doesn't exist; completes synchronously most of the time.
                            await queue.EnsureQueueExistsAsync();

                            await AddMessageAsync(queue, message, visibilityDelay);
                        });

                    return;
                }
                catch (Exception ex)
                {
                    DualLogger.Instance.Error(
                        nameof(AzureWorkItemQueue<TWorkItem>), 
                        ex, 
                        "Error adding message to queue: "
                        + $"AccountName:{queue.AccountName};"
                        + $"QueueName:{queue.QueueName}"
                        + $"MessageId:{message?.Id ?? "null"}");

                    // Error publishing. Add this queue to the dead queues set and then remove 10 seconds later.
                    lock (this.syncRoot)
                    {
                        var newDeadQueue = queue;
                        bool added = this.deadQueues.Add(newDeadQueue);

                        if (added)
                        {
                            Task.Run(async () =>
                            {
                                await Task.Delay(TimeSpan.FromSeconds(RandomHelper.Next(10, MaxWaitInSeconds)));
                                lock (this.syncRoot)
                                {
                                    this.deadQueues.Remove(newDeadQueue);
                                }
                            });
                        }
                    }

                    if (count >= max)
                    {
                        DualLogger.Instance.Error(
                        nameof(AzureWorkItemQueue<TWorkItem>),
                        $"No more queues to try Count={count}, Max={max}, Last tried queue deails: "
                        + $"AccountName:{queue.AccountName};"
                        + $"QueueName:{queue.QueueName}"
                        + $"MessageId:{message?.Id ?? "null"}");

                        throw;
                    }
                }
            }

            if (count >= max)
            {
                DualLogger.Instance.Error(
                        nameof(AzureWorkItemQueue<TWorkItem>),
                        $"all queues are dead, waiting for dead queue cleanup. Count={count}, Max={max}");
                
                // Wait for queue flush.
                await Task.Delay(TimeSpan.FromSeconds(MaxWaitInSeconds));

                // Throw exception.
                throw new IndexOutOfRangeException($"Exhausted all queues. count={count}, max={max}");
            }
        }

        /// <summary>
        /// Begins processing messages from the queue.
        /// </summary>
        public async Task BeginProcessAsync(IAzureWorkItemQueueHandler<TWorkItem> workItemHandler, CancellationToken token)
        {
            string typeName = workItemHandler.GetType().Name;

            // A threadsafe pipe between the producer and consumer.
            var bufferedWorkItems = new ConcurrentQueue<(IAzureCloudQueue queue, CloudQueueMessage message)>();

            // Start task but don't await.
            Task monitorQueueDepth = this.ReportQueueDepthAsync(token);

            List<Task> pollQueueTasks = new List<Task>();
            foreach (var queue in this.azureQueues)
            {
                pollQueueTasks.Add(this.PollQueueAsync(queue, bufferedWorkItems, token));
            }

            // List of work items we are currently processing.
            List<Task> runningTasks = new List<Task>();

            while (true)
            {
                try
                {
                    while (runningTasks.Count >= this.SoftPendingWorkItemLimit)
                    {
                        Task completedTask = await Task.WhenAny(runningTasks);
                        runningTasks.Remove(completedTask);
                    }

                    // We can now start another work item once we have it.
                    while (bufferedWorkItems.Count == 0 && !token.IsCancellationRequested)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(50));
                    }

                    while (runningTasks.Count < this.SoftPendingWorkItemLimit && bufferedWorkItems.TryDequeue(out var nextWorkItem))
                    {
                        runningTasks.Add(this.ProcessMessage(nextWorkItem.queue, nextWorkItem.message, workItemHandler, typeName));
                    }

                    // If cancellation requested and all subtasks have finished, then we're good to cancel.
                    if (token.IsCancellationRequested && pollQueueTasks.All(t => t.IsCompleted) && bufferedWorkItems.Count == 0)
                    {
                        await Task.WhenAll(runningTasks);
                        break;
                    }
                }
                catch
                {
                    PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "QueueEventProcessingErrors").Increment(this.GetType().Name);
                }
            }
        }

        private async Task PollQueueAsync(
            IAzureCloudQueue pollQueue,
            ConcurrentQueue<(IAzureCloudQueue, CloudQueueMessage)> resultQueue,
            CancellationToken token)
        {
            await Task.Yield();
            int nextBackoffMs = 1000;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(nextBackoffMs);

                    while (resultQueue.Count >= this.SoftPendingWorkItemLimit)
                    {
                        await Task.Delay(50);
                    }

                    // Delay processing if the Key value matches the work item name
                    // and the random percentage allows.
                    bool delayProcessing = FlightingUtilities.IsKeyEnabledForFlight(
                        FlightingNames.AzureWorkItemQueueDelayProcessing,
                        typeof(TWorkItem).Name);

                    // Emergency switches to back off.
                    if (delayProcessing)
                    {
                        int delaySecs = 30;
                        DualLogger.Instance.Information(
                            "PollQueueAsync", 
                            $"Delay queue poll for {delaySecs} secs. AccountName:{pollQueue.AccountName} QueueName:{pollQueue.QueueName}");
                        await Task.Delay(TimeSpan.FromSeconds(delaySecs), token);
                        continue;
                    }

                    // Disable processing.
                    bool disableProcessing = FlightingUtilities.IsStringValueEnabled(
                        FlightingNames.AzureWorkItemQueueDisableProcessing,
                        pollQueue.QueueName);

                    if (disableProcessing)
                    {
                        continue;
                    }

                    await pollQueue.EnsureQueueExistsAsync();
                    var results = await pollQueue.GetMessagesAsync(this.BatchSize);

                    int count = 0;
                    foreach (var item in results)
                    {
                        count++;
                        resultQueue.Enqueue((pollQueue, item));
                    }

                    if (count == 0)
                    {
                        // Increase delay up to maximum.
                        nextBackoffMs = (nextBackoffMs + Config.Instance.Worker.AzureQueueMaxBackoffMs) / 2;
                    }
                    else if (count >= this.BatchSize)
                    {
                        // Reduce delay down to minimum;
                        nextBackoffMs = (nextBackoffMs + MinQueueBackoffMs) / 2;
                    }
                }
                catch (Exception ex)
                {
                    DualLogger.Instance.Error(nameof(AzureWorkItemQueue<TWorkItem>), $"Failed to poll queue: AccountName:{ pollQueue.AccountName}, QueueName:{pollQueue.QueueName}: Ex: {ex.Message}");

                    // Already instrumented; don't break our loop.
                    nextBackoffMs = Config.Instance.Worker.AzureQueueMaxBackoffMs;
                }
            }
        }

        private async Task ReportQueueDepthAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    foreach (var queue in this.azureQueues)
                    {
                        int count = await queue.GetCountAsync(token);
                        Logger.Instance?.AzureWorkerQueueDepth(queue.AccountName, queue.QueueName, count);
                    }
                }
                catch
                {
                }

                // Sleep randomly.
                await Task.Delay(TimeSpan.FromSeconds(RandomHelper.Next(60, 180)));
            }
        }

        /// <summary>
        /// Takes the next batch of messages from the given queue.
        /// </summary>
        private async Task<IEnumerable<(CloudQueueMessage, IAzureCloudQueue)>> TakeMessagesAsync(IAzureCloudQueue queue)
        {
            var result = await Logger.InstrumentAsync(
                new OutgoingEvent(SourceLocation.Here()),
                async ev => await queue.GetMessagesAsync(this.BatchSize));

            return result.Select(x => (x, queue));
        }

        /// <summary>
        /// Processes a single message from the queue.
        /// </summary>
        private async Task ProcessMessage(IAzureCloudQueue queue, CloudQueueMessage message, IAzureWorkItemQueueHandler<TWorkItem> workItemHandler, string typeName)
        {
            var shouldIgnoreIfNotFound = FlightingUtilities.IsEnabled(FlightingNames.IgnoreMessageIfNotFoundEnabled);

            try
            {
                using (await PrioritySemaphore.Instance.WaitAsync(workItemHandler.WorkItemPriority))
                {
                    await Logger.InstrumentAsync(
                        new IncomingEvent(SourceLocation.Here()),
                        async ev =>
                        {
                            ev.OperationName = $"{typeName}.{nameof(workItemHandler.ProcessWorkItemAsync)}";
                            ev["AccountName"] = queue.AccountName;
                            ev["QueueName"] = queue.QueueName;

                            LogCloudQueueMessage(ev, message);

                            var workItem = new QueueWorkItemWrapper<TWorkItem>(
                                Unpackage(message.AsBytes),
                                queue,
                                message,
                                Package);

                            var result = await workItemHandler.ProcessWorkItemAsync(workItem);

                            if (result == null)
                            {
                                DualLogger.Instance.Error(nameof(AzureWorkItemQueue<TWorkItem>), "ProcessWorkItemAsync returned null");
                                PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "NullWorkItemResults").Increment(typeName);
                                result = QueueProcessResult.RetryAfter(this.ExceptionBackoff);
                            }

                            if (result.Complete)
                            {
                                // If complete, then delete it from the queue.
                                await DeleteMessageAsync(queue, message);
                            }
                            else
                            {
                                // Otherwise, modify the visibility and the content.
                                message.SetMessageContent2(Package(workItem.WorkItem));
                                await UpdateMessageAsync(queue, message, result.Delay, MessageUpdateFields.Visibility | MessageUpdateFields.Content);
                            }

                            ev.OperationStatus = OperationStatus.Succeeded;
                            ev.StatusCode = HttpStatusCode.OK;
                        });
                }
            }
            catch (Exception ex)
            {
                // Exception already instrumented by the Incoming instrumentation above. Just need to swallow it here and back off.
                // Note: don't modify original message in this case.
                DualLogger.Instance.Error(nameof(AzureWorkItemQueue<TWorkItem>), ex, $"Exception encountered while processing message in {queue.AccountName}:{queue.QueueName}. Backing off! : {ex.Message}");

                // If the message does not exist, then don't update the message further.
                if (shouldIgnoreIfNotFound && ex.Message.Contains("The specified message does not exist"))
                {
                    DualLogger.Instance.Information(nameof(AzureWorkItemQueue<TWorkItem>), $"Ignoring to update the message in {queue.AccountName}:{queue.QueueName} for: {ex.Message}");
                    return;
                }

                // Don't observe this task. If it throws an exception, that's OK. It'll be measured by the outgoing instrumentation.
                await UpdateMessageAsync(queue, message, this.ExceptionBackoff, MessageUpdateFields.Visibility).ConfigureAwait(false);
            }
        }

        private static async Task AddMessageAsync(IAzureCloudQueue queue, CloudQueueMessage message, TimeSpan? visibilityDelay)
        {
            await Logger.InstrumentAsync(
                new OutgoingEvent(SourceLocation.Here()),
                async ev =>
                {
                    LogCloudQueueMessage(ev, message);
                    await queue.AddMessageAsync(message, visibilityDelay);
                }).ConfigureAwait(false);
        }

        private static async Task DeleteMessageAsync(IAzureCloudQueue queue, CloudQueueMessage message)
        {
            await Logger.InstrumentAsync(
                new OutgoingEvent(SourceLocation.Here()),
                async ev =>
                {
                    LogCloudQueueMessage(ev, message);
                    await queue.DeleteMessageAsync(message).ConfigureAwait(false);
                }).ConfigureAwait(false);
        }

        private static async Task UpdateMessageAsync(IAzureCloudQueue queue, CloudQueueMessage message, TimeSpan visibilityTimeout, MessageUpdateFields updateFields)
        {
             await Logger.InstrumentAsync(
                new OutgoingEvent(SourceLocation.Here()),
                async ev =>
                {
                    LogCloudQueueMessage(ev, message);
                    ev["VisibilityTimeoutSeconds"] = ((int)visibilityTimeout.TotalSeconds).ToString();
                    await queue.UpdateMessageAsync(message, visibilityTimeout, updateFields).ConfigureAwait(false);
                }).ConfigureAwait(false);
        }

        private static TWorkItem Unpackage(byte[] body)
        {
            byte[] decompressedBytes = CompressionTools.Gzip.Decompress(body);
            string text = Encoding.UTF8.GetString(decompressedBytes);
            return JsonConvert.DeserializeObject<TWorkItem>(text);
        }

        private static byte[] Package(TWorkItem workItem)
        {
            string text = JsonConvert.SerializeObject(workItem);
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            byte[] compressedBytes = CompressionTools.Gzip.Compress(bytes);

            PerfCounterUtility.GetOrCreate(PerformanceCounterType.Number, "InverseCompressionRatio").Set(typeof(TWorkItem).Name, bytes.Length / compressedBytes.Length);

            return compressedBytes;
        }

        private static void LogCloudQueueMessage(OperationEvent @event, CloudQueueMessage message)
        {
            if (@event != null)
            {
                @event.SetProperty("WorkItemId", message.Id);

                if (message.ExpirationTime != null)
                {
                    int hoursUntilExpiration = (int)(message.ExpirationTime.Value - DateTimeOffset.UtcNow).TotalHours;
                    @event.SetProperty("HoursUntilExpiration", hoursUntilExpiration.ToString());
                }

                if (message.InsertionTime != null)
                {
                    int hoursPending = (int)(DateTimeOffset.UtcNow - message.InsertionTime.Value).TotalHours;
                    @event.SetProperty("HoursPending", hoursPending.ToString());
                }
            }
        }
    }
}
