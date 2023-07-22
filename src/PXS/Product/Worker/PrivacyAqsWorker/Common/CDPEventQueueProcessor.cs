// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.Common
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    using Live.Mesh.Service.AsyncQueueService.Interface;

    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging.Extensions;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Worker;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.Aqs;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.EventProcessors;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.TableStorage;
    using Microsoft.Membership.MemberServices.Privacy.Core.AQS;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     Used for processing work items from an AQS Queue
    /// </summary>
    public class CdpEventQueueProcessor : BackgroundWorker
    {
        private const string DeadLetterErrorMessage = "Failed to write to dead letter storage";

        private const int MaxRetryCount = 5;

        private readonly IAccountCreateWriter accountCreateWriter;

        private readonly IAccountDeleteWriter accountDeleteWriter;

        private readonly IAsyncQueueService2 aqsClient;

        private readonly ICounterFactory counterFactory;

        private readonly IUserCreateEventProcessor createEventProcessor;

        private readonly ITable<MsaDeadLetterStorage> deadLetterTable;

        private readonly IUserDeleteEventProcessor deleteEventProcessor;

        private readonly ILogger logger;

        private readonly IAqsQueueProcessorConfiguration processorConfiguration;

        private readonly string requesterId;

        /// <summary>
        ///     Creates an instance of <see cref="CdpEventQueueProcessor" />
        /// </summary>
        public CdpEventQueueProcessor(
            IAsyncQueueService2 aqsClient,
            IAqsQueueProcessorConfiguration processorConfiguration,
            ILogger logger,
            IUserCreateEventProcessor createEventProcessor,
            IUserDeleteEventProcessor deleteEventProcessor,
            IAccountCreateWriter accountCreateWriter,
            IAccountDeleteWriter accountDeleteWriter,
            ICounterFactory counterFactory,
            ITable<MsaDeadLetterStorage> deadLetterTable)
        {
            this.aqsClient = aqsClient ?? throw new ArgumentNullException(nameof(aqsClient));
            this.processorConfiguration =
                processorConfiguration ?? throw new ArgumentNullException(nameof(processorConfiguration));
            this.requesterId = processorConfiguration.RequesterId;
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this.createEventProcessor = createEventProcessor ?? throw new ArgumentNullException(nameof(createEventProcessor));
            this.deleteEventProcessor = deleteEventProcessor ?? throw new ArgumentNullException(nameof(deleteEventProcessor));

            this.accountCreateWriter = accountCreateWriter ?? throw new ArgumentNullException(nameof(accountCreateWriter));
            this.accountDeleteWriter = accountDeleteWriter ?? throw new ArgumentNullException(nameof(accountDeleteWriter));

            this.counterFactory = counterFactory ?? throw new ArgumentNullException(nameof(counterFactory));
            this.deadLetterTable = deadLetterTable ?? throw new ArgumentNullException(nameof(deadLetterTable));
        }

        /// <summary>
        ///     Grabs the configured amount of groups and processes them async
        /// </summary>
        /// <returns>
        ///     <c>true</c> if there were events to process, otherwise <c>false</c>
        /// </returns>
        public override async Task<bool> DoWorkAsync()
        {
            try
            {
                Sll.Context.Vector = new CorrelationVector();
                Trace.CorrelationManager.ActivityId = Guid.NewGuid();

                AggregationGroup[] aggregationGroups = await this.aqsClient.TakeWorkAsync(
                    this.processorConfiguration.QueueName,
                    this.processorConfiguration.GroupsToTake,
                    this.processorConfiguration.LeaseTimeoutSeconds).ConfigureAwait(false);
                
                if (aggregationGroups == null || aggregationGroups.Length == 0)
                {
                    // Did not receive work from the queue
                    return false;
                }

                ICounter hoursCounter = this.counterFactory.GetCounter(CounterCategoryNames.Aqs, "hoursinqueue", CounterType.Number);

                // Organize each group with events contained in sub-groups by event-type. All events in a subgroup, we consider dups (same event type).
                var groupsList = new List<(string groupId, IEnumerable<IGrouping<string, CdpEventWrapper>> eventSubgroups)>();
                foreach (var group in aggregationGroups)
                {
                    var wrappedEvents = group.WorkItems.Select(item =>
                    {
                        var evt = Serializer.Deserialize<CDPEvent2>(item.Payload);
                        var timeInQueue = (DateTime.UtcNow - item.SubmissionTime).Hours;

                        hoursCounter.SetValue((ulong)timeInQueue, this.processorConfiguration.QueueName);

                        return new CdpEventWrapper(evt)
                        {
                            AggregationId = group.Id,
                            ParentWorkItem = item
                        };
                    }).ToList();

                    var subGroupsByType = wrappedEvents.GroupBy(we => we.Event.EventData.GetType().Name).ToList();
                   
                    var @event = new AqsWorkReceivedEvent()
                    {
                        GroupId = group.Id,
                        UniqueUsers = wrappedEvents.Select(wi => wi.Puid).Distinct().Count(),
                        EventCounts = subGroupsByType.ToDictionary(sg => sg.Key, sg => sg.Count())
                    };

                    @event.LogWarning();

                    groupsList.Add((groupId: group.Id, eventSubgroups: subGroupsByType));
                }

                // From every sub-group, take one event. Groups with > 1 sub-group will end up having more than one event processed. 
                // This might lead to known issues with multiple events completing/releasing the same AggregationGroup on AQS but this should not be the common case.
                IList<CdpEventWrapper> items = groupsList.SelectMany(g => g.eventSubgroups.Select(sg => sg.FirstOrDefault())).ToList();

                // Divide work items up based on if they're for creates or deletes.
                IList<CdpEventWrapper> createItems = items.Where(item => item.Event.EventData is UserCreate).ToList();
                IList<CdpEventWrapper> deleteItems = items.Where(item => item.Event.EventData is UserDelete).ToList();

                // Start processing the create events and delete events asynchronously
                Task<AdapterResponse<IList<AccountCreateInformation>>> writeTask = this.ProcessCreateItemsAsync(createItems);
                Task<AdapterResponse<IList<AccountDeleteInformation>>> deleteTask = this.ProcessDeleteItemsAsync(deleteItems);

                // Wait for writes and deletes to complete and collect results
                await Task.WhenAll(writeTask, deleteTask).ConfigureAwait(false);

                AdapterResponse<IList<AccountCreateInformation>> writeResult = await writeTask.ConfigureAwait(false);
                AdapterResponse<IList<AccountDeleteInformation>> deleteResult = await deleteTask.ConfigureAwait(false);

                // Create a list of successful IDs to complete
                var completeIds = new List<string>();
                var releaseIds = new List<string>();
                if (writeResult.IsSuccess)
                {
                    IList<ulong> puids = writeResult.Result?.Select(info => info.Puid).ToList() ?? new List<ulong>();
                    completeIds.AddRange(createItems.Where(item => puids.Contains(item.Puid)).Select(item => item.AggregationId));
                }
                else
                {
                    this.logger.Error(nameof(CdpEventQueueProcessor), $"Writing to cosmos failed. Error: {writeResult.Error}");
                    releaseIds = createItems.Select(item => item.AggregationId).ToList();
                }

                if (deleteResult.IsSuccess)
                {
                    IList<ulong> puids = deleteResult.Result?.Select(info => (ulong)info.Puid).ToList() ?? new List<ulong>();
                    completeIds.AddRange(deleteItems.Where(item => puids.Contains(item.Puid)).Select(item => item.AggregationId));
                }
                else
                {
                    this.logger.Error(nameof(CdpEventQueueProcessor), $"Writing deletes failed. Error: {deleteResult.Error}");
                    releaseIds.AddRange(deleteItems.Select(item => item.AggregationId));
                }

                Task completeTasks = Task.WhenAll(completeIds.Select(id => this.aqsClient.CompleteWorkAsync(this.processorConfiguration.QueueName, id)));

                Task releaseTasks = Task.WhenAll(
                    releaseIds.Select(
                        id => this.aqsClient.ReleaseWorkAsync(
                            this.processorConfiguration.QueueName,
                            id,
                            this.processorConfiguration.ReleaseWaitIntervalSeconds,
                            "Processing Failed")));

                await Task.WhenAll(completeTasks, releaseTasks).ConfigureAwait(false);

                return true;
            }
            catch (Exception e)
            {
                this.logger.Error(nameof(CdpEventQueueProcessor), e, e.Message);
            }

            return false; // Don't exit the process, trigger it to wait and try again
        }
        
        /// <summary>
        ///     Starts this instance.
        /// </summary>
        public override void Start()
        {
            base.Start(TimeSpan.FromMilliseconds(this.processorConfiguration.WaitOnQueueEmptyMilliseconds));
        }

        /// <summary>
        ///     Increments a specific counter
        /// </summary>
        /// <param name="category">The counter category</param>
        /// <param name="name">The counter name</param>
        /// <param name="type">The counter type</param>
        /// <param name="value">The value to increment by</param>
        /// <param name="instance">The instance.</param>
        private void IncrementCounter(string category, string name, CounterType type, uint value, string instance = null)
        {
            ICounter counter = this.counterFactory.GetCounter(category, name, type);
            counter.IncrementBy(value);
            if (!string.IsNullOrEmpty(instance))
            {
                counter.IncrementBy(value, instance);
            }
        }

        private async Task<AdapterResponse<IList<AccountCreateInformation>>> ProcessCreateItemsAsync(IList<CdpEventWrapper> createItems)
        {
            if (!createItems.Any())
            {
                return new AdapterResponse<IList<AccountCreateInformation>>();
            }

            AdapterResponse<IList<AccountCreateInformation>> response =
                await this.createEventProcessor.ProcessCreateItemsAsync(createItems.Select(item => item.Event)).ConfigureAwait(false);

            try
            {
                // On success case, complete Puids that had empty responses since the account no longer exists
                if (response.IsSuccess)
                {
                    // Grab all PUIDs that didn't get a response back from MSA
                    IEnumerable<ulong> puids = createItems.Select(evt => evt.Puid);
                    IEnumerable<ulong> successPuids = response.Result.Select(info => info.Puid);
                    IList<ulong> invalidPuids = puids.Except(successPuids).ToList();

                    // Get the Aggregation IDs of the invalid PUIDs
                    // Complete these with AQS, they'll never succeed since MSA does not have entries for them anymore
                    if (invalidPuids.Any())
                    {
                        IList<CdpEventWrapper> failed = createItems.Where(item => invalidPuids.Contains(item.Puid)).ToList();

                        IList<(string AggregationKey, ulong Puid)> invalidIds =
                            failed.Where(item => item.ParentWorkItem.TakenCount >= MaxRetryCount)
                                .Select(item => (AggregationKey: item.AggregationId, Puid: item.Puid)).ToList();

                        // Requeue items that haven't reached the max retry count
                        IEnumerable<string> retryIds = failed.Where(item => item.ParentWorkItem.TakenCount < MaxRetryCount).Select(item => item.AggregationId);
                        Task retryTask = Task.WhenAll(
                            retryIds.Select(
                                id => this.aqsClient.ReleaseWorkAsync(
                                    this.processorConfiguration.QueueName,
                                    id,
                                    this.processorConfiguration.ReleaseWaitIntervalSeconds,
                                    "Partner could not find account")));

                        var cleanOutTasks = new List<Task>();
                        foreach ((string aggregationId, ulong puid) in invalidIds)
                        {
                            var evt = new MsaAccountCreateCidNotFound
                            {
                                Details = "GetSigninNames did not return any information for account"
                            };

                            evt.LogWarning(new MsaId((long)puid));

                            try
                            {
                                // true - added, false - was already in dead letter table, exception is failure
                                await this.deadLetterTable.InsertAsync(
                                    new MsaDeadLetterStorage((long)puid)
                                    {
                                        DataActual = new MsaAccountDeadLetterInformation
                                        {
                                            Puid = (long)puid,
                                            EventType = MsaAccountEventType.AccountCreate
                                        }
                                    }).ConfigureAwait(false);

                                this.IncrementCounter(CounterCategoryNames.Aqs, "deadletter", CounterType.Number, 1, "accountcreate");
                                cleanOutTasks.Add(this.aqsClient.CompleteWorkAsync(this.processorConfiguration.QueueName, aggregationId));
                            }
                            catch (Exception e)
                            {
                                var error = new ErrorEvent
                                {
                                    ComponentName = nameof(CdpEventQueueProcessor),
                                    ErrorMessage = e.Message,
                                    ErrorMethod = nameof(this.ProcessCreateItemsAsync),
                                    ErrorName = e.GetType().Name,
                                    CallStack = e.StackTrace,
                                    ErrorDetails = DeadLetterErrorMessage,
                                    ErrorCode = e.HResult.ToString()
                                };

                                error.LogError();
                            }
                        }

                        Task cleanOutTask = Task.WhenAll(cleanOutTasks);
                        await Task.WhenAll(cleanOutTask, retryTask).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception e)
            {
                // This is an attempt to clean up invalid data, so it's not a critical failure, but we should be aware of it failing
                var error = new ErrorEvent
                {
                    ComponentName = nameof(CdpEventQueueProcessor),
                    ErrorMessage = e.Message,
                    ErrorMethod = nameof(this.ProcessCreateItemsAsync),
                    ErrorName = e.GetType().Name,
                    CallStack = e.StackTrace,
                    ErrorCode = e.HResult.ToString()
                };

                error.LogError();
            }

            return response.IsSuccess ? (await this.accountCreateWriter.WriteCreatedAccountsAsync(response.Result).ConfigureAwait(false)) : response;
        }

        private async Task<AdapterResponse<IList<AccountDeleteInformation>>> ProcessDeleteItemsAsync(IList<CdpEventWrapper> deleteItems)
        {
            if (!deleteItems.Any())
            {
                return new AdapterResponse<IList<AccountDeleteInformation>>();
            }

            // Malformed events will never successfully be processed, so we mark them as complete
            List<(string AggregationKey, ulong Puid)> malformed = deleteItems.Where(
                item => !this.deleteEventProcessor.EventHelper.TryGetCid(item.Event, out long _) ||
                        !this.deleteEventProcessor.EventHelper.TryGetGdprPreVerifierToken(item.Event, out string _)).Select(
                item => (AggregationKey: item.AggregationId, Puid: item.Puid)).ToList();

            // Send well formed events to the delete processor
            IList<string> malformedKeys = malformed.Select(id => id.AggregationKey).ToList();
            List<CDPEvent2> wellformed = deleteItems.Where(item => !malformedKeys.Contains(item.AggregationId)).Select(item => item.Event).ToList();
            Task<AdapterResponse<IList<AdapterResponse<AccountDeleteInformation>>>> tasks = this.deleteEventProcessor.ProcessDeleteItemsAsync(wellformed);

            try
            {
                this.IncrementCounter(CounterCategoryNames.Aqs, "missingverifier", CounterType.Rate, (uint)malformed.Count);

                if (malformed.Any())
                {
                    var clearMalformedTask = new List<Task>();
                    foreach ((string aggregationId, ulong puid) in malformed)
                    {
                        var evt = new MsaAccountCloseMissingData
                        {
                            Details = "Missing Cid/PreVerifier"
                        };

                        evt.LogError(new MsaId((long)puid));

                        try
                        {
                            await this.deadLetterTable.InsertAsync(
                                new MsaDeadLetterStorage((long)puid)
                                {
                                    DataActual = new MsaAccountDeadLetterInformation
                                    {
                                        Puid = (long)puid,
                                        EventType = MsaAccountEventType.AccountClose
                                    }
                                }).ConfigureAwait(false);
                            this.IncrementCounter(CounterCategoryNames.Aqs, "deadletter", CounterType.Number, 1, "accountclose");
                            clearMalformedTask.Add(this.aqsClient.CompleteWorkAsync(this.processorConfiguration.QueueName, aggregationId));
                        }
                        catch (Exception e)
                        {
                            var error = new ErrorEvent
                            {
                                ComponentName = nameof(CdpEventQueueProcessor),
                                ErrorMessage = e.Message,
                                ErrorMethod = nameof(this.ProcessDeleteItemsAsync),
                                ErrorName = e.GetType().Name,
                                CallStack = e.StackTrace,
                                ErrorDetails = DeadLetterErrorMessage,
                                ErrorCode = e.HResult.ToString()
                            };

                            error.LogError();
                        }
                    }

                    await Task.WhenAll(clearMalformedTask).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                // This is an attempt to clean up invalid data, so it's not a critical failure, but we should be aware of it failing
                var error = new ErrorEvent
                {
                    ComponentName = nameof(CdpEventQueueProcessor),
                    ErrorMessage = e.Message,
                    ErrorMethod = nameof(this.ProcessDeleteItemsAsync),
                    ErrorName = e.GetType().Name,
                    CallStack = e.StackTrace,
                    ErrorCode = e.HResult.ToString()
                };

                error.LogError();
            }

            // Anything that failed will expire their lease
            var resultsWrapper = await tasks.ConfigureAwait(false);
            if (!resultsWrapper.IsSuccess)
            {
                return new AdapterResponse<IList<AccountDeleteInformation>>
                {
                    Error = resultsWrapper.Error
                };
            }

            var results = resultsWrapper.Result;

            return await this.accountDeleteWriter.WriteDeletesAsync(results.Where(res => res.IsSuccess).Select(res => res.Result).ToList(), this.requesterId).ConfigureAwait(false);
        }
    }
}
