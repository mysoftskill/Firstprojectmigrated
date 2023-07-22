// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.EventHubProcessor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging.Extensions;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.QueueProcessor;
    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.TableStorage;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Membership.MemberServices.Privacy.Core.PrivacyCommand;
    using Microsoft.Practices.ObjectBuilder2;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.ServiceBus.Messaging;

    using Newtonsoft.Json;

    /// <summary>
    ///     Interface for Partition Context
    /// </summary>
    public interface IPartitionContext
    {
        /// <summary>
        ///     The Lease on the partition
        /// </summary>
        Lease Lease { get; }

        /// <summary>
        ///     Checkpoints progress of an Event Hubs message stream. Make sure to call this method once all the messages in a batch are processed.
        /// </summary>
        /// <returns>The task representing the asynchronous operation.</returns>
        Task CheckpointAsync();
    }

    internal class EventHubPartitionContext : IPartitionContext
    {
        private readonly PartitionContext context;

        public Lease Lease => this.context.Lease;

        /// <summary>
        ///     Creates a new instance of EventHub PartitionContext.
        /// </summary>
        /// <param name="context">The <see cref="PartitionContext" /></param>
        public EventHubPartitionContext(PartitionContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc />
        public Task CheckpointAsync()
        {
            return this.context.InstrumentedCheckpointAsync();
        }
    }

    /// <summary>
    ///     A class for processing AAD events.
    /// </summary>
    internal class AadEventProcessor : IEventProcessor
    {
        private const LogOption DefaultLogOption = LogOption.Realtime;

        private const string EventsFilteredTotal = "total";

        private static readonly TimeSpan MaxEnqueueTimeout = TimeSpan.FromSeconds(60);

        private readonly IAccountCloseQueueManager accountCloseQueueManager;

        private readonly IClock clock;

        private readonly string cloudInstance;

        private readonly ICounterFactory counterFactory;

        private readonly ITable<NotificationDeadLetterStorage> deadLetterTable;

        private readonly string eventHubsEndpoint;

        private readonly string hubId;

        /// <summary>
        ///     The logger.
        /// </summary>
        private readonly ILogger logger;

        private readonly IRequestClassifier requestClassifier;

        // A group of tenant ids that we will skip processing
        private readonly HashSet<string> tenantIdFilterDisallowedList;

        private readonly IAppConfiguration appConfiguration;

        /// <summary>
        ///     Gets the reason why the processor was closed.
        /// </summary>
        public CloseReason? CloseReason { get; private set; }

        /// <summary>
        ///     Creates a new instance of <see cref="AadEventProcessor" />.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="accountCloseQueueManager">The account close queue manager</param>
        /// <param name="counterFactory">The counter factory</param>
        /// <param name="clock">The clock</param>
        /// <param name="hubId">The identifier for which event hub this processor is processing.</param>
        /// <param name="cloudInstance">The cloud instance</param>
        /// <param name="tenantFilterList">A list of tenant IDs that are skipped for processing</param>
        /// <param name="requestClassifier">Classifies requests based on the identity of the authenticated user.</param>
        /// <param name="deadLetterTable">The dead letter table for items that were not processable.</param>
        /// <param name="eventHubsEndpoint">The endpoint for this processor.</param>
        /// <param name="appConfiguration">The configuration for the application.</param>
        public AadEventProcessor(
            ILogger logger,
            IAccountCloseQueueManager accountCloseQueueManager,
            ICounterFactory counterFactory,
            IClock clock,
            string hubId,
            string cloudInstance,
            IList<string> tenantFilterList,
            IRequestClassifier requestClassifier,
            ITable<NotificationDeadLetterStorage> deadLetterTable,
            string eventHubsEndpoint,
            IAppConfiguration appConfiguration)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.accountCloseQueueManager = accountCloseQueueManager ?? throw new ArgumentNullException(nameof(accountCloseQueueManager));
            this.counterFactory = counterFactory ?? throw new ArgumentNullException(nameof(counterFactory));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.hubId = hubId ?? throw new ArgumentNullException(nameof(hubId));
            this.deadLetterTable = deadLetterTable ?? throw new ArgumentNullException(nameof(deadLetterTable));
            this.cloudInstance = cloudInstance;

            ValidateFilters(logger, tenantFilterList);
            if (tenantFilterList != null)
            {
                this.tenantIdFilterDisallowedList = new HashSet<string>();
                tenantFilterList.ForEach(filter => this.tenantIdFilterDisallowedList.Add(filter));
            }

            this.requestClassifier = requestClassifier ?? throw new ArgumentNullException(nameof(requestClassifier));
            this.eventHubsEndpoint = eventHubsEndpoint;
            this.appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));

        }

        /// <inheritdoc />
        public Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            const string msg = "Event processor closed for PartitionId '{0}' with Owner '{1}' and reason '{2}'.";

            this.CloseReason = reason;

            this.logger.Information(
                nameof(AadEventProcessor),
                msg,
                context?.Lease?.PartitionId,
                context?.Lease?.Owner,
                reason);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task OpenAsync(PartitionContext context)
        {
            const string msg = "Event processor was opened for PartitionId '{0}' and Owner '{1}'.";
            this.logger.Information(
                nameof(AadEventProcessor),
                msg,
                context?.Lease?.PartitionId,
                context?.Lease?.Owner);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            return this.ProcessEventsAsync(new EventHubPartitionContext(context), messages);
        }

        internal async Task ProcessEventsAsync(IPartitionContext context, IEnumerable<EventData> messages)
        {
            try
            {
                ulong eventsRead = 0;
                var eventsFiltered = new Dictionary<string, ulong> { [EventsFilteredTotal] = 0 };
                foreach (EventData message in messages)
                {
                    // Deserialize the event data into a notification object
                    string data = Encoding.UTF8.GetString(message.GetBytes());
                    var notifications = JsonConvert.DeserializeObject<Notification[]>(data);
                    var batch = new List<AccountCloseRequest>();

                    eventsRead += (ulong)notifications.Length;

                    // Create the request context from the notification
                    foreach (Notification notification in notifications)
                    {
                        // Each account close request should be treated as a separate "request"
                        Guid requestGuid = Guid.NewGuid();
                        string currentTenantId = notification.ResourceData.TenantId.ToString();
                        if (this.tenantIdFilterDisallowedList?.Any(
                                filterId => string.Equals(filterId, currentTenantId, StringComparison.InvariantCultureIgnoreCase)) ?? false)
                        {
                            // the notification belongs to a tenant that is on the filter list, don't process it
                            ++eventsFiltered[EventsFilteredTotal];
                            eventsFiltered.TryGetValue(currentTenantId, out ulong currentValue);
                            eventsFiltered[currentTenantId] = ++currentValue;

                            continue;
                        }                        

                        IRequestContext requestContext = new RequestContext(
                            new AadIdentity(notification.ResourceData.Id, notification.ResourceData.TenantId, notification.ResourceData.OrgPuid));

                        // Create the account close request
                        var subject = PrivacyRequestConverter.CreateAadSubjectFromIdentity(requestContext.RequireIdentity<AadIdentity>(), notification.ResourceData.HomeTenantId);
                        if (!await this.appConfiguration.IsFeatureFlagEnabledAsync(FeatureNames.MultiTenantCollaboration).ConfigureAwait(false) &&
                            subject is AadSubject2)
                        {
                            this.logger.Error(nameof(AadEventProcessor), $"Unsupported command for TenantId = {subject.TenantId}, ObjectId = {subject.ObjectId}, HomeTenantId = {notification.ResourceData.HomeTenantId}");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(notification.Token))
                        {
                            const string ErrorMessage = "Preverifier token is missing in 'Token' property after reading from EventHub.";
                            this.LogDeadLetterSllEvents(notification, requestGuid, ErrorMessage);

                            if (await this.deadLetterTable.InsertAsync(
                                new NotificationDeadLetterStorage
                                {
                                    DataActual = notification,
                                    ErrorCode = "PreverifierTokenMissing",
                                    ErrorMessage = ErrorMessage,
                                    PartitionKey = subject.TenantId.ToString(),
                                    RowKey = subject.ObjectId.ToString()
                                }).ConfigureAwait(false))
                            {
                                this.counterFactory.GetCounter(CounterCategoryNames.AadAccountClose, "DeadLetterCount", CounterType.Number).Increment();
                                this.logger.Error(nameof(AadEventProcessor), ErrorMessage);

                                // do not continue processing for this notification; it's been dead-lettered.
                                continue;
                            }

                            this.counterFactory.GetCounter(CounterCategoryNames.AadAccountClose, "FailedToStoreDeadLetterCount", CounterType.Number).Increment();
                            this.logger.Error(nameof(AadEventProcessor), "Failed to store to dead letter storage.");

                            // Don't checkpoint in this situation; deadletter failed to store.
                            return;
                        }

                        AccountCloseRequest accountCloseRequest = PrivacyRequestConverter.CreatePcfAccountCloseRequest(
                            subject,
                            requestContext,
                            requestGuid,
                            new CorrelationVector().Value,
                            DateTimeOffset.UtcNow,
                            this.cloudInstance,
                            Portals.AadAccountCloseEventSource,
                            notification.Token,
                            this.requestClassifier.IsTestRequest(Portals.AadAccountCloseEventSource, requestContext.Identity));

                        this.UpdateEventAgePerfCounter(notification);

                        // Add the item to the batch.
                        batch.Add(accountCloseRequest);
                    }

                    try
                    {
                        using (var cancellationTokenSource = new CancellationTokenSource(MaxEnqueueTimeout))
                        {
                            CancellationToken cancellationToken = cancellationTokenSource.Token;
                            await this.accountCloseQueueManager.EnqueueAsync(batch, cancellationToken).ConfigureAwait(false);
                            this.LogProcessSuccess(batch, context?.Lease?.PartitionId);
                        }

                        const string msg = "Processed and wrote '{0}' events for PartitionId '{1}' and Owner '{2}'.";
                        this.logger.Information(
                            nameof(AadEventProcessor),
                            msg,
                            batch?.Count,
                            context?.Lease?.PartitionId,
                            context?.Lease?.Owner);
                    }
                    catch (Exception e)
                    {
                        // Any errors mean the batch couldn't be processed.
                        const string msg = "Failed to process and write all '{0}' events for PartitionId '{1}' and Owner '{2}'.";
                        this.logger.Error(
                            nameof(AadEventProcessor),
                            e,
                            msg,
                            batch.Count,
                            context?.Lease?.PartitionId,
                            context?.Lease?.Owner);
                        throw;
                    }
                }

                // Log the number of AAD events processed.
                var aadEvent = new AADAccountCloseEvent
                {
                    EventHubId = this.hubId,
                    EventHubsEndpoint = this.eventHubsEndpoint,
                    Count = eventsRead
                };
                aadEvent.LogInformational(LogOption.Realtime);

                this.UpdateEventsRead(eventsRead);
                this.UpdateEventsFiltered(eventsFiltered, eventsRead);
            }
            catch (Exception e)
            {
                const string msg = "An exception occurred while processing events for PartitionId '{0}' and Owner '{1}'.";
                this.logger.Error(
                    nameof(AadEventProcessor),
                    e,
                    msg,
                    context?.Lease?.PartitionId,
                    context?.Lease?.Owner);
                throw;
            }

            // Perform checkpoint after going through messages in this batch
            await context.CheckpointAsync().ConfigureAwait(false);
        }

        private int GetAgeOfEventInHourFromNow(DateTimeOffset resourceDataEventTime)
        {
            DateTimeOffset now = this.clock.UtcNow;

            if (now > resourceDataEventTime)
            {
                return (int)now.Subtract(resourceDataEventTime).TotalHours;
            }

            // If event time is in the future (clock skew) compared to now, return back 0
            return 0;
        }

        private void LogDeadLetterSllEvents(Notification notification, Guid requestGuid, string errorMessage)
        {
            this.logger.Error(nameof(AadEventProcessor), errorMessage);
            AadAccountCloseMissingData accountCloseMissingDataEvent =
                new AadAccountCloseMissingData { TenantId = notification.ResourceData.TenantId.ToString(), Details = errorMessage };
            UserInfo userInfo = new UserInfo();
            userInfo.SetId(UserIdType.AzureAdId, notification.ResourceData.Id.ToString());
            accountCloseMissingDataEvent.LogError(userInfo.FillEnvelope);

            // Log SLL event pertaining to the account that was dead-lettered for easier debugging of ICM incidents.
            var errorEvent = new ErrorEvent
            {
                ComponentName = nameof(AadAccountCloseQueueProcessor),
                ErrorMethod = nameof(this.ProcessEventsAsync),
                ErrorMessage = errorMessage,
                ErrorType = "DeadLetter",
                ErrorCode = nameof(NotificationDeadLetterStorage),
                ServerActivityId = requestGuid.ToString()
            };
            errorEvent.ExtraData?.Add("TenantId", notification.ResourceData.TenantId.ToString());
            errorEvent.LogError(userInfo.FillEnvelope);
        }

        /// <summary>
        ///     Logs an event for each item in the batch.
        ///     This is useful to aid in debugging to know where an account close request came from, as there are many different event hubs and partitions we subscribe to.
        ///     If an account shows up in more than one EventHubs and/or partition, then we can assert that a duplicate originated upstream from the publisher to the EventHubs.
        /// </summary>
        private void LogProcessSuccess(List<AccountCloseRequest> batch, string partitionId)
        {
            foreach (var request in batch)
            {
                if (request == null)
                {
                    continue;
                }

                var log = new AadAccountCloseEventHubsNotification
                {
                    CloudInstance = this.cloudInstance,
                    RequestGuid = request.RequestGuid.ToString(),
                    RequestTimestamp = request.Timestamp.ToString("u"),
                    CommandId = request.RequestId.ToString(),
                    EventHubEndpoint = this.eventHubsEndpoint ?? string.Empty,
                    EventHubPartitionId = partitionId
                };

                if (request.Subject is AadSubject2 aadSubject2)
                {
                    log.TenantId = aadSubject2?.TenantId.ToString();
                    log.HomeTenantId = aadSubject2?.HomeTenantId.ToString();
                    log.LogInformational(DefaultLogOption, SllLoggingHelper.CreateUserInfo(UserIdType.AzureAdId, aadSubject2?.ObjectId.ToString()).FillEnvelope);
                }
                else if (request.Subject is AadSubject aadSubject)
                {
                    log.TenantId = aadSubject?.TenantId.ToString();
                    log.LogInformational(DefaultLogOption, SllLoggingHelper.CreateUserInfo(UserIdType.AzureAdId, aadSubject?.ObjectId.ToString()).FillEnvelope);
                }
                else
                {
                    // All of these should be AAD subject, but in the future if there's ever another subject type here, the logging method should not throw any exceptions.
                    log.LogInformational(DefaultLogOption);
                }
            }
        }

        private void UpdateEventAgePerfCounter(Notification notification)
        {
            int eventHoursFromNow = this.GetAgeOfEventInHourFromNow(notification.ResourceData.EventTime);
            ICounter counter = this.counterFactory.GetCounter(CounterCategoryNames.AzureEventHub, "AadAccountCloseEventAge", CounterType.Number);
            counter.SetValue((ulong)eventHoursFromNow);
        }

        private void UpdateEventsFiltered(IDictionary<string, ulong> eventsFiltered, ulong totalEvents)
        {
            ICounter rateCount = this.counterFactory.GetCounter(CounterCategoryNames.AzureEventHub, "AadAccountCloseTenantFilteredEvents", CounterType.Rate);

            ulong totalFiltered = eventsFiltered[EventsFilteredTotal];
            rateCount.IncrementBy(totalFiltered);

            this.logger.Information(nameof(AadEventProcessor), $"Filtered {totalFiltered} events out of {totalEvents}");

            // Remove the total filtered count from the dictionary so it does get it's own instance (it's the overall value)
            eventsFiltered.Remove(EventsFilteredTotal);

            // Add an instance for the tenant GUID and how many events were filtered for that tenant.
            eventsFiltered.ForEach(evt => rateCount.IncrementBy(evt.Value, evt.Key));
        }

        private void UpdateEventsRead(ulong eventsRead)
        {
            ICounter counter = this.counterFactory.GetCounter(CounterCategoryNames.AzureEventHub, "AadAccountCloseEventsRead", CounterType.Rate);
            counter.IncrementBy(eventsRead);
            counter.IncrementBy(eventsRead, this.hubId);
        }

        internal static void ValidateFilters(ILogger logger, IList<string> tenantFilterList)
        {
            // Check that all filters are valid
            bool validGuids = tenantFilterList?.All(filter => Guid.TryParse(filter, out Guid _)) ?? true;
            if (!validGuids)
            {
                logger.Error(nameof(AadEventProcessorFactory), "Tenant Filter List has non-GUID string value");
                throw new ArgumentException("Tenant filter list has non-GUID string value", nameof(tenantFilterList));
            }
        }
    }
}
