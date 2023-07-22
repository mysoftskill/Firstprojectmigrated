namespace Microsoft.PrivacyServices.DataManagement.Worker.ChangeFeedReader
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Storage.Queue;
    using Microsoft.PrivacyServices.DataManagement.Common;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.AzureQueue;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Scheduler;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;
    using Microsoft.PrivacyServices.DataManagement.Worker.Scheduler;

    using Newtonsoft.Json;

    using Models = Microsoft.PrivacyServices.DataManagement.DataAccess.ChangeFeedReader.Models;

    /// <summary>
    /// Implements the worker class.
    /// </summary>
    public class ChangeFeedReaderWorker : LockWorker<ChangeFeedReaderLockState>, IInitializer
    {
        private readonly IChangeFeedReader changeFeedReader;
        private readonly ICloudQueue dataOwnersQueue;
        private readonly ICloudQueue assetGroupsQueue;
        private readonly ICloudQueue variantDefinitionsQueue;
        private readonly IEventWriterFactory eventWriterFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeFeedReaderWorker"/> class.
        /// </summary>
        /// <param name="id">Worker id.</param>
        /// <param name="dateFactory">Date factory instance.</param>
        /// <param name="lockConfig">Lock configuration.</param>
        /// <param name="dataAccess">Lock data access.</param>
        /// <param name="sessionFactory">The session factory.</param>
        /// <param name="eventWriterFactory">The event writer factory.</param>
        /// <param name="changeFeedReader">The change feed reader.</param>
        /// <param name="dataOwnersQueue">The data owners queue for data grid.</param>
        /// <param name="assetGroupsQueue">The asset groups queue for data grid.</param>
        /// <param name="variantDefinitionsQueue">The variant definitions queue for data grid.</param>
        public ChangeFeedReaderWorker(
            Guid id,
            IDateFactory dateFactory,
            ILockConfig lockConfig,
            ILockDataAccess<ChangeFeedReaderLockState> dataAccess,
            ISessionFactory sessionFactory,
            IEventWriterFactory eventWriterFactory,
            IChangeFeedReader changeFeedReader,
            ICloudQueue dataOwnersQueue,
            ICloudQueue assetGroupsQueue,
            ICloudQueue variantDefinitionsQueue)
            : base(id, dateFactory, dataAccess, sessionFactory)
        {
            this.changeFeedReader = changeFeedReader;
            this.dataOwnersQueue = dataOwnersQueue;
            this.assetGroupsQueue = assetGroupsQueue;
            this.variantDefinitionsQueue = variantDefinitionsQueue;
            this.eventWriterFactory = eventWriterFactory;

            this.EnableAcquireLock = lockConfig.EnableChangeFeedReaderLock;
            this.LockName = "DataGrid-ChangeFeedReader";
        }

        /// <inheritdoc />
        public override int IdleTimeBetweenCallsInMilliseconds => 5000;

        public override bool EnableAcquireLock { get; set; }

        public override string LockName { get; set; }

        public override double LockExpiryTimeInMilliseconds => 300000;

        public override int LockMaxFailureCountPerInstance => 10;

        // Adding initializer for worker
        public Task InitializeAsync()
        {
            string workerName = this.GetType().Name;

            eventWriterFactory.Trace(workerName, $"Beginning {workerName} initialization.");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Generates a new configuration entry for the given time window.
        /// </summary>
        /// <param name="lockStatus">The current lock status.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Null string when host process should invoke DoWorkAsync immediately, otherwise a string message indicating why it should back off.</returns>
        public override async Task<string> DoLockWorkAsync(Lock<ChangeFeedReaderLockState> lockStatus, CancellationToken cancellationToken)
        {
            var currentTime = this.DateFactory.GetCurrentTime();

            lockStatus.State = lockStatus.State ?? new ChangeFeedReaderLockState();

            string callbackResponse = "WaitingForChanges";

            Task delay()
            {
                if (lockStatus.State.FullSyncInProgress)
                {
                    // When doing a full sync, introduce a throttle to avoid DDOSing DataGrid.
                    // This is still needed because processing SoftDelete values are expensive for them.
                    return Task.Delay(TimeSpan.FromMilliseconds(500));
                }
                else
                {
                    return Task.CompletedTask;
                }
            }

            // On Sundays (or immediately if in the bootstrap case)) perform a full sync.
            var initialLastSyncTime = lockStatus.State.LastSyncTime;
            if ((currentTime.DayOfWeek == DayOfWeek.Sunday ||
                 lockStatus.State.LastSyncTime == default) &&
                currentTime > lockStatus.State.LastSyncTime &&
                !lockStatus.State.FullSyncInProgress)
            {
                lockStatus.State.FullSyncInProgress = true;
                lockStatus.State.SyncContinuationToken = lockStatus.State.ContinuationToken;
                lockStatus.State.LastSyncTime = currentTime.AddDays(6); // Gives us 1 week to do the sync before a new one is triggered.
                lockStatus.State.ContinuationToken = null; // Clear the continuation to start at the beginning.

                var triggerEvent = new FullSyncTriggerEvent
                {
                    startTime = currentTime.ToString()
                };

                this.eventWriterFactory.WriteEvent(nameof(ChangeFeedReaderWorker), triggerEvent);
            }

            var response = await this.changeFeedReader.ReadItemsAsync(lockStatus.State.ContinuationToken).ConfigureAwait(false);

            foreach (var obj in response)
            {
                var entityTypeString = obj.GetPropertyValue<string>("entityType");

                if (Enum.TryParse<EntityType>(entityTypeString, out EntityType entityType))
                {
                    var contractVersion = obj.GetPropertyValue<string>("contractVersion");

                    if (entityType == EntityType.DataOwner &&
                        contractVersion == EntityFilterCriteria<DataOwner>.ContractVersion.Value)
                    {
                        callbackResponse = null;

                        await this.EnqueueAsync<Models.DataOwner>(obj, this.dataOwnersQueue).ConfigureAwait(false);

                        await delay().ConfigureAwait(false);
                    }
                    else if (entityType == EntityType.AssetGroup &&
                        contractVersion == EntityFilterCriteria<AssetGroup>.ContractVersion.Value)
                    {
                        callbackResponse = null;

                        await this.EnqueueAsync<Models.AssetGroup>(obj, this.assetGroupsQueue).ConfigureAwait(false);

                        await delay().ConfigureAwait(false);
                    }
                    else if (entityType == EntityType.VariantDefinition &&
                        contractVersion == EntityFilterCriteria<VariantDefinition>.ContractVersion.Value)
                    {
                        callbackResponse = null;

                        await this.EnqueueAsync<Models.VariantDefinition>(obj, this.variantDefinitionsQueue).ConfigureAwait(false);

                        await delay().ConfigureAwait(false);
                    }
                }

                // Ultimately, we want the logical sequence number of the last item in the batch.
                // That is the value of the continuation token.
                lockStatus.State.ContinuationToken = obj.GetPropertyValue<string>("_lsn");

                // If we have started a full sync, determine if we should stop.
                // We stop when we have reached the original continuation token (prior to the sync).
                if (lockStatus.State.FullSyncInProgress)
                {
                    var previousState = long.Parse(lockStatus.State.SyncContinuationToken ?? "0");
                    var currentState = long.Parse(lockStatus.State.ContinuationToken ?? "0");

                    if (previousState <= currentState)
                    {
                        lockStatus.State.FullSyncInProgress = false;

                        var triggerEvent = new FullSyncTriggerEvent
                        {
                            endTime = currentTime.ToString()
                        };

                        this.eventWriterFactory.WriteEvent(nameof(ChangeFeedReaderWorker), triggerEvent);
                    }
                }
            }

            await this.DataAccess.UpdateAsync(lockStatus).ConfigureAwait(false);

            return callbackResponse;
        }

        private async Task EnqueueAsync<T>(Document document, ICloudQueue queue)
        {
            var obj = (T)(dynamic)document;
            var data = JsonConvert.SerializeObject(obj);
            var message = new CloudQueueMessage(data);

            string entityType = string.Empty;
            Type type = typeof(T);

            if (type == typeof(Models.DataOwner))
            {
                entityType = "DataOwner";
            }
            else if (type == typeof(Models.AssetGroup))
            {
                entityType = "AssetGroup";
            }
            else if (type == typeof(Models.VariantDefinition))
            {
                entityType = "VariantDefinition";
            }

            var enqueuingEvent = new EnqueuingMessageEvent
            {
                id = document.Id,
                lsn = document.GetPropertyValue<string>("_lsn"),
                storageUri = queue.GetStorageUri()
            };

            // Write as Sll event.
            this.eventWriterFactory.WriteEvent(nameof(ChangeFeedReaderWorker), enqueuingEvent);

            try
            {
                await queue.AddMessageAsync(message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (ex is System.ArgumentException)
                {
                    this.eventWriterFactory.Trace(nameof(ChangeFeedReaderWorker), "=====================Invalid doc details=====================");
                    this.eventWriterFactory.Trace(nameof(ChangeFeedReaderWorker), $"DocumentId: {document.Id}, DocumentType: {entityType}, TimeStamp: {document.Timestamp}");
                }

                throw;
            }

            var enqueuedEvent = new EnqueuedMessageEvent
            {
                id = document.Id,
                lsn = document.GetPropertyValue<string>("_lsn"),
                storageUri = queue.GetStorageUri()
            };

            // Write as Sll event.
            this.eventWriterFactory.WriteEvent(nameof(ChangeFeedReaderWorker), enqueuedEvent);
        }
    }
}
