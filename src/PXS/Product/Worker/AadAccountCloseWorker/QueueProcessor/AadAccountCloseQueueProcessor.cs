// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.QueueProcessor
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Cloud.InstrumentationFramework;
    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Worker;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.TableStorage;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAccountClose;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    /// <summary>
    ///     AadAccountCloseQueueProcessor
    /// </summary>
    public class AadAccountCloseQueueProcessor : BackgroundWorker
    {
        private const string ConcurrencyConflictStrVal = "ConcurrencyConflict";

        private const string Forbidden = "Forbidden";

        private const string TooManyRequestsStrVal = "TooManyRequests";

        private const string UnauthorizedStrVal = "Unauthorized";

        private static readonly TimeSpan DefaultLeaseExtension = TimeSpan.FromMinutes(5);

        private readonly IAadAccountCloseQueueProccessorConfiguration config;

        private readonly ICounterFactory counterFactory;

        private readonly ITable<AccountCloseDeadLetterStorage> deadLetterTable;

        private readonly IList<TimeSpan> leaseExtensionSetHours;

        private readonly IList<TimeSpan> leaseExtensionSetMins;

        private readonly ILogger logger;

        private readonly TimeSpan MaxTimeoutGetMessages = TimeSpan.FromSeconds(10);

        private readonly TimeSpan MaxTimeoutInsertMessages = TimeSpan.FromSeconds(60);

        private readonly IAadAccountCloseService pcfProxyService;

        private readonly IAccountCloseQueueManager queueManager;

        private readonly IAppConfiguration appConfiguration;

        private readonly TimeSpan WaitOnQueueEmpty;

        /// <summary>
        ///     Creates a new instance of <see cref="AadAccountCloseQueueProcessor" />
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="configuration">The queue processor configuration</param>
        /// <param name="queueManager">The queue</param>
        /// <param name="aadAccountCloseService">The aad account close service.</param>
        /// <param name="deadLetterTable">The dead letter table for items that were not processable.</param>
        /// <param name="counterFactory">The counter factory for making perf counters</param>
        /// <param name="appConfiguration">The Azure App Configuration instance</param>
        public AadAccountCloseQueueProcessor(
            ILogger logger,
            IAadAccountCloseQueueProccessorConfiguration configuration,
            IAccountCloseQueueManager queueManager,
            IAadAccountCloseService aadAccountCloseService,
            ITable<AccountCloseDeadLetterStorage> deadLetterTable,
            ICounterFactory counterFactory,
            IAppConfiguration appConfiguration)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.queueManager = queueManager ?? throw new ArgumentNullException(nameof(queueManager));
            this.pcfProxyService = aadAccountCloseService ?? throw new ArgumentNullException(nameof(aadAccountCloseService));
            this.deadLetterTable = deadLetterTable ?? throw new ArgumentNullException(nameof(deadLetterTable));
            this.counterFactory = counterFactory ?? throw new ArgumentNullException(nameof(counterFactory));
            this.appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));

            this.config = configuration;

            this.WaitOnQueueEmpty = TimeSpan.FromMilliseconds(this.config.WaitOnQueueEmptyMilliseconds);

            this.leaseExtensionSetMins = PrivacyConfigurationHelper.BuildFullLeaseExtensionSet(
                this.config.LeaseExtensionMinuteSet,
                PrivacyConfigurationHelper.LeaseExtensionTimeType.Minutes);
            this.leaseExtensionSetHours = PrivacyConfigurationHelper.BuildFullLeaseExtensionSet(
                this.config.LeaseExtensionHourSet,
                PrivacyConfigurationHelper.LeaseExtensionTimeType.Hours);
        }

        /// <inheritdoc />
        /// <summary>
        ///     Grabs work from the queue and processes them async
        /// </summary>
        /// <returns>
        ///     <c>true</c> if there were events to process, otherwise <c>false</c>
        /// </returns>
        public override async Task<bool> DoWorkAsync()
        {
            this.logger.Verbose(nameof(AadAccountCloseQueueProcessor), "Doing work.");

            try
            {
                if (!appConfiguration.GetConfigValue(ConfigNames.PXS.AadAccountCloseWorker_EnableDequeuing, defaultValue: true))
                {
                    return false;
                }

                IList<IQueueItem<AccountCloseRequest>> accountCloseRequests = new List<IQueueItem<AccountCloseRequest>>();
                using (var cancellationTokenSource = new CancellationTokenSource(this.MaxTimeoutGetMessages))
                {
                    CancellationToken cancellationToken = cancellationTokenSource.Token;
                    accountCloseRequests = await this.queueManager.GetMessagesAsync(this.config.GetMessagesDequeueCount, cancellationToken).ConfigureAwait(false);

                    // Check if we need to remove some specific signals.
                    await this.FilterAccountCloseRequestForDroppedSignals(accountCloseRequests);
                }

                if (accountCloseRequests == null || accountCloseRequests.Count == 0)
                {
                    // Did not receive work from the queue
                    this.logger.Information(nameof(AadAccountCloseQueueProcessor), $"Zero messages found in queue: {nameof(AccountCloseRequest)}");
                    return false;
                }

                this.logger.Information(
                    nameof(AadAccountCloseQueueProcessor),
                    $"Successfully retrieved {accountCloseRequests.Count} messages from queue: {nameof(AccountCloseRequest)}. {string.Join(", ", accountCloseRequests.Select(c => c?.Data?.RequestId))}");
                var taskList = new List<Task>();

                if (appConfiguration.GetConfigValue(ConfigNames.PXS.AadAccountCloseWorker_EnableProcessing, defaultValue: true))
                {
                    IList<ServiceResponse<IQueueItem<AccountCloseRequest>>> responses = await this.pcfProxyService.PostBatchAccountCloseAsync(
                        accountCloseRequests).ConfigureAwait(false);

                    foreach (ServiceResponse<IQueueItem<AccountCloseRequest>> serviceResponse in responses)
                    {
                        this.UpdateQueueItemAgePerformanceCounter(serviceResponse.Result);
                        if (serviceResponse.IsSuccess)
                        {
                            taskList.Add(this.DeleteMessageAsync(serviceResponse.Result, "Successfully deleted message from queue.", IfxTracingLevel.Informational));
                        }
                        else
                        {
                            taskList.Add(this.ProcessErroredMessageAsync(serviceResponse));
                            this.logger.Error(nameof(AadAccountCloseQueueProcessor), serviceResponse?.Error?.ToString());
                        }
                    }
                }
                else
                {
                    taskList.AddRange(accountCloseRequests.Select(c => this.DeleteMessageAsync(c, "Queue processing disabled. Deleting from queue", IfxTracingLevel.Warning)));
                }

                await Task.WhenAll(taskList).ConfigureAwait(false);
                return true;
            }
            catch (OperationCanceledException e)
            {
                this.logger.Error(nameof(AadAccountCloseQueueProcessor), e, $"{nameof(this.DoWorkAsync)} was canceled.");
                return false;
            }

            catch (Exception e)
            {
                this.logger.Error(nameof(AadAccountCloseQueueProcessor), e, "An unhandled exception occurred.");
                return false;
            }
        }

        /// <inheritdoc />
        /// <summary>
        ///     Starts this instance.
        /// </summary>
        public override void Start()
        {
            base.Start(this.WaitOnQueueEmpty);
        }

        private Task DeleteMessageAsync(IQueueItem<AccountCloseRequest> queueItem, string logMessagePrefix, IfxTracingLevel traceLevel)
        {
            this.logger.Log(traceLevel, nameof(AadAccountCloseQueueProcessor), $"{logMessagePrefix}. {nameof(queueItem.Data.RequestId)}: {queueItem.Data?.RequestId}");
            return queueItem.CompleteAsync();
        }

        /// <summary>
        ///     Gets the amount of time to wait until the next time command feed should hand us the command
        /// </summary>
        /// <param name="extensionTimeType">extension Time Type</param>
        /// <param name="dequeueCount">dequeue count</param>
        /// <returns>lease extension time for the number of dequeues seen so far</returns>
        private TimeSpan GetLeaseExtension(PrivacyConfigurationHelper.LeaseExtensionTimeType extensionTimeType, int dequeueCount)
        {
            switch (extensionTimeType)
            {
                case PrivacyConfigurationHelper.LeaseExtensionTimeType.Minutes:
                    if (this.leaseExtensionSetMins.Count > 0)
                    {
                        int index = Math.Min(Math.Max(dequeueCount, 0), this.leaseExtensionSetMins.Count - 1);
                        return this.leaseExtensionSetMins[Math.Max(index - 1, 0)];
                    }

                    break;
                case PrivacyConfigurationHelper.LeaseExtensionTimeType.Hours:
                    if (this.leaseExtensionSetHours.Count > 0)
                    {
                        int index = Math.Min(Math.Max(dequeueCount, 0), this.leaseExtensionSetHours.Count - 1);
                        return this.leaseExtensionSetHours[Math.Max(index - 1, 0)];
                    }

                    break;
            }

            return DefaultLeaseExtension;
        }

        private void LogDeadLetterErrorEvent(ServiceResponse<IQueueItem<AccountCloseRequest>> serviceResponse, string errorDetails, AadSubject aadSubject)
        {
            if (serviceResponse?.Result == null)
            {
                this.logger.Error(
                    nameof(AadAccountCloseQueueProcessor),
                    $"Unable to log detailed error because {nameof(serviceResponse)} or {nameof(serviceResponse.Result)} was null.");
                return;
            }

            // Log SLL event pertaining to the account that was dead-lettered for easier debugging of ICM incidents.
            var errorEvent = new ErrorEvent
            {
                ComponentName = nameof(AadAccountCloseQueueProcessor),
                ErrorMethod = nameof(this.MoveMessageToDeadLetterAndDeleteAsync),
                ErrorMessage = serviceResponse.Error.Message,
                ErrorType = "DeadLetter",
                ErrorCode = "AadAccountCloseDeadLetter",
                ServerActivityId = serviceResponse.Result.Data.RequestGuid.ToString(),
                ErrorDetails = errorDetails
            };
            errorEvent.ExtraData?.Add("CommandId", serviceResponse.Result.Data.RequestId.ToString());
            errorEvent.ExtraData?.Add("TenantId", aadSubject.TenantId.ToString());
            errorEvent.ExtraData?.Add("DequeueCount", serviceResponse.Result.DequeueCount.ToString());
            errorEvent.LogError(
                e =>
                {
                    var userInfo = new UserInfo();
                    userInfo.SetId(UserIdType.AzureAdId, aadSubject.ObjectId.ToString());
                    userInfo.FillEnvelope(e);
                });
        }

        /// <summary>
        ///     Move the message to Dead letter table and delete from queue
        /// </summary>
        /// <param name="serviceResponse">message to process</param>
        /// <param name="errorDetails">The error details to log</param>
        /// <returns>true on success</returns>
        private async Task<bool> MoveMessageToDeadLetterAndDeleteAsync(ServiceResponse<IQueueItem<AccountCloseRequest>> serviceResponse, string errorDetails)
        {
            var aadSubject = (AadSubject)serviceResponse.Result.Data.Subject;

            // Check if the item is already in Deadletter storage. If it is, update the existing item with the new info
            var existingItem = await this.deadLetterTable.GetItemAsync(partitionId: aadSubject.TenantId.ToString(), rowId: aadSubject.ObjectId.ToString());
            var newItem = new AccountCloseDeadLetterStorage
                {
                    DataActual = serviceResponse.Result.Data,
                    ErrorCode = serviceResponse.Error.Code,
                    ErrorMessage = serviceResponse.Error.Message,
                    PartitionKey = aadSubject.TenantId.ToString(),
                    RowKey = aadSubject.ObjectId.ToString(),
                    ETag = existingItem?.ETag,
                };

            var succeeded = (existingItem == null) ?
                await this.deadLetterTable.InsertAsync(newItem).ConfigureAwait(false) :
                await this.deadLetterTable.ReplaceAsync(newItem).ConfigureAwait(false);

            string commandId = serviceResponse?.Result?.Data?.RequestId.ToString() ?? "UNKNOWN";
            if (succeeded)
            {
                // Increment the number of items in dead letter storage
                this.counterFactory.GetCounter(CounterCategoryNames.AadAccountClose, "DeadLetterCount", CounterType.Number).Increment();

                // Moved to dead-letter. Complete the message
                this.logger.Log(IfxTracingLevel.Error, nameof(AadAccountCloseQueueProcessor), $"Moved to Dead Letter and Deleted Message CommandId: {commandId}");
                this.LogDeadLetterErrorEvent(serviceResponse, errorDetails, aadSubject);

                await this.DeleteMessageAsync(
                    serviceResponse.Result,
                    $"Deleting message from queue due to processing error. CommandId: {commandId}",
                    IfxTracingLevel.Error).ConfigureAwait(false);
                return true;
            }
            else
            {
                this.logger.Error(nameof(AadAccountCloseQueueProcessor), $"Adding request to DeadLetter table failed. CommandId: {commandId}");
                return false;
            }
        }

        /// <summary>
        ///     Process the message
        /// </summary>
        /// <param name="serviceResponse">service response details</param>
        /// <returns>true if process succeeded</returns>
        private async Task<bool> ProcessErroredMessageAsync(ServiceResponse<IQueueItem<AccountCloseRequest>> serviceResponse)
        {
            string DeadLetterMaxRetryErrorMessage()
            {
                return "Exceeded retry count. " +
                       $"{nameof(serviceResponse.Result.DequeueCount)} ({serviceResponse?.Result?.DequeueCount}) >= {nameof(this.config.MaxDequeueCountToDeadLetter)} ({this.config.MaxDequeueCountToDeadLetter})";
            }

            var taskList = new List<Task>();

            try
            {
                switch (serviceResponse?.Error?.InnerError?.Code)
                {
                    case TooManyRequestsStrVal:
                        this.logger.Log(
                            IfxTracingLevel.Warning,
                            nameof(AadAccountCloseQueueProcessor),
                            $"Received TooManyRequests response for" +
                            $" {nameof(serviceResponse.Result.Data.RequestId)}: {serviceResponse?.Result?.Data?.RequestId}" +
                            $" {nameof(serviceResponse.Result.DequeueCount)}: {serviceResponse?.Result?.DequeueCount} ");

                        // Increment the RVS TooManyRequestsCounter
                        this.counterFactory.GetCounter(CounterCategoryNames.AadAccountClose, "AadRVSTooManyRequests", CounterType.Number).Increment();

                        // Renew Lease with throttling retry interval or re queue
                        taskList.Add(
                            serviceResponse?.Result?.DequeueCount > this.config.MaxDequeueCountBeforeRequeue
                                ? this.RequeueMessageAsync(serviceResponse)
                                : serviceResponse?.Result?.RenewLeaseAsync(
                                    this.GetLeaseExtension(PrivacyConfigurationHelper.LeaseExtensionTimeType.Hours, serviceResponse.Result.DequeueCount)));

                        break;

                    case ConcurrencyConflictStrVal:
                        this.logger.Log(
                            IfxTracingLevel.Warning,
                            nameof(AadAccountCloseQueueProcessor),
                            $"Received ConcurrencyConflict response for" +
                            $" {nameof(serviceResponse.Result.Data.RequestId)}: {serviceResponse?.Result?.Data?.RequestId}" +
                            $" {nameof(serviceResponse.Result.DequeueCount)}: {serviceResponse?.Result?.DequeueCount} ");

                        // Increment the RVS ConcurrencyConflicts Counter
                        this.counterFactory.GetCounter(CounterCategoryNames.AadAccountClose, "AadRVSConcurrencyConflicts", CounterType.Number).Increment();

                        // Dead letter or Renew Lease with conflict retry interval
                        if (serviceResponse?.Result?.DequeueCount >= this.config.MaxDequeueCountForConflicts)
                        {
                            return await this.MoveMessageToDeadLetterAndDeleteAsync(serviceResponse, DeadLetterMaxRetryErrorMessage()).ConfigureAwait(false);
                        }
                        else
                        {
                            serviceResponse?.Result?.RenewLeaseAsync(
                                this.GetLeaseExtension(
                                    PrivacyConfigurationHelper.LeaseExtensionTimeType.Minutes,
                                    serviceResponse.Result.DequeueCount));
                        }

                        break;

                    case Forbidden:

                        if (serviceResponse?.Result?.DequeueCount >= this.config.MaxDequeueCountForForbidden)
                        {
                            // Forbidden messages per http://www.w3.org/Protocols/rfc2616/rfc2616-sec10.html are non-retriable, 
                            // however AAD RVS has sent them to us in transient conditions, so retry here is a workaround to prevent dead-letter in cases where it should not be.
                            return await this.MoveMessageToDeadLetterAndDeleteAsync(
                                    serviceResponse,
                                    $"Http StatusCode: {serviceResponse?.Error?.InnerError?.Code} encountered from AAD RVS. Dequeue Count: {serviceResponse?.Result?.DequeueCount}")
                                .ConfigureAwait(false);
                        }

                        // else do nothing, will retry again when lease expires.

                        break;

                    case UnauthorizedStrVal:

                        // Unauthorized is non-retriable
                        return await this.MoveMessageToDeadLetterAndDeleteAsync(serviceResponse, DeadLetterMaxRetryErrorMessage()).ConfigureAwait(false);

                    default:

                        // If De-queue Count reached the max tries - Move to Dead letter
                        if (serviceResponse?.Result?.DequeueCount >= this.config.MaxDequeueCountToDeadLetter)
                        {
                            return await this.MoveMessageToDeadLetterAndDeleteAsync(serviceResponse, DeadLetterMaxRetryErrorMessage()).ConfigureAwait(false);
                        }
                        else // Exponentially increase the lease time for the item based on how many times it's been de-queued (32 seconds to 16384 seconds)
                        {
                            taskList.Add(serviceResponse?.Result?.RenewLeaseAsync(TimeSpan.FromSeconds(Math.Pow(2, (serviceResponse.Result.DequeueCount + 4)))));
                        }

                        break;
                }

                await Task.WhenAll(taskList).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                this.logger.Error(nameof(AadAccountCloseQueueProcessor), e, $"{nameof(this.ProcessErroredMessageAsync)} An unhandled exception occurred.");
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Requeue the message
        /// </summary>
        /// <param name="serviceResponse">Message to Process</param>
        /// <returns>true upon success</returns>
        private async Task<bool> RequeueMessageAsync(ServiceResponse<IQueueItem<AccountCloseRequest>> serviceResponse)
        {
            // Message to re-insert - temporary storage as serviceResponse.Result would be null after deletion
            AccountCloseRequest messageToReinsert = serviceResponse.Result.Data;

            using (var cancellationTokenSource = new CancellationTokenSource(this.MaxTimeoutInsertMessages))
            {
                CancellationToken cancellationToken = cancellationTokenSource.Token;

                this.logger.Warning(
                    nameof(AadAccountCloseQueueProcessor),
                    $"Re-add message to queue: {nameof(AccountCloseRequest)} {nameof(messageToReinsert.RequestId)}: {messageToReinsert?.RequestId}");

                //Re-add the message to Queue
                await this.queueManager.EnqueueAsync(
                    new List<AccountCloseRequest>
                    {
                        messageToReinsert
                    },
                    cancellationToken
                ).ConfigureAwait(false);

                // Delete the message from Queue
                await this.DeleteMessageAsync(
                    serviceResponse.Result,
                    $"Successfully deleted retry-able message from queue after max tries:{this.config.MaxDequeueCountBeforeRequeue} , this message is re added to queue.",
                    IfxTracingLevel.Informational).ConfigureAwait(false);
            }

            return true;
        }

        /// <summary>
        ///     Updates the queue item age performance counter.
        /// </summary>
        /// <param name="queueItem">The queue item.</param>
        private void UpdateQueueItemAgePerformanceCounter(IQueueItem<AccountCloseRequest> queueItem)
        {
            if (queueItem?.InsertionTime != null)
            {
                int itemHoursInQueue = (DateTimeOffset.UtcNow - (DateTimeOffset)queueItem.InsertionTime).Hours;
                ICounter counter = this.counterFactory.GetCounter(CounterCategoryNames.AzureQueue, "AadAccountCloseQueueItemAge", CounterType.Number);
                counter.SetValue((ulong)itemHoursInQueue);
            }
        }

        /// <summary>
        /// Support dropping of specific account close requests.
        /// </summary>
        /// <param name="accountCloseRequests"></param>
        /// <returns></returns>
        private async Task FilterAccountCloseRequestForDroppedSignals(IList<IQueueItem<AccountCloseRequest>> accountCloseRequests)
        {
            if (accountCloseRequests == null) return;

            for (int i = accountCloseRequests.Count - 1; i >= 0; i--)
            {
                if (accountCloseRequests[i].Data.Subject is AadSubject aadSubject)
                {
                    string userId = aadSubject.ObjectId.ToString();

                    if (await appConfiguration.IsFeatureFlagEnabledAsync(FeatureNames.PXS.DropAccountCloseSignalForUser,
                        CustomOperatorContextFactory.CreateDefaultStringComparisonContext(userId), true))
                    {
                        this.logger.Warning(
                           nameof(AadAccountCloseQueueProcessor),
                           $"AccountClose signal was dropped: {nameof(AccountCloseRequest)} userId = { userId}");
                        // drop.
                        await accountCloseRequests[i].CompleteAsync();

                        accountCloseRequests.RemoveAt(i);
                    }
                }
            }
        }
    }
}
