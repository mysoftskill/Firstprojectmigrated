// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue.QueueStorageCommandQueue
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.Azure.Storage;
    using Microsoft.Azure.Storage.Queue;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    using Newtonsoft.Json;

    /// <summary>
    /// Interacts with a single Azure Queue Storage on behalf of an agent.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces")]
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    public class AzureQueueStorageCommandQueue : CommandQueue, ICommandQueue
    {
        private readonly IAzureCloudQueue azureQueue;

        private readonly PrivacyCommandType commandType;

        private readonly SubjectType subjectType;

        private readonly IClock clock;

        private readonly IAssetGroupAzureQueueTrackerCache assetGroupAzureQueueTrackerCache;

        private readonly TimeSpan defaultLeaseDuration = TimeSpan.FromSeconds(900);

        private readonly string moniker;

        private bool QueueExists => this.assetGroupAzureQueueTrackerCache.QueueExists(this.azureQueue);

        public static IList<PrivacyCommandType> SupportedCommandTypes { get; } = new List<PrivacyCommandType> { PrivacyCommandType.AgeOut };

        /// <inheritdoc />
        public CommandQueuePriority QueuePriority => CommandQueuePriority.Low;

        /// <summary>
        /// Method is used across the project to create Azure Queue name for <see cref="AzureQueueStorageCommandQueue" />.
        /// </summary>
        /// <remarks>
        /// Azure restricts naming to lowercase, with special characters limited to the '-' (dash). See all rules here:
        /// https://docs.microsoft.com/en-us/rest/api/storageservices/naming-queues-and-metadata
        /// </remarks>
        /// <remarks>Important note! Queue names can only be max of 63 characters long (see doc link)</remarks>
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase")]
        public static string CreateAzureQueueName(AssetGroupId assetGroupId, SubjectType subjectType, PrivacyCommandType commandType)
        {
            // Example, an MSA AgeOut queue would look like cq-0-4-7baa875b2d9440089d0fccbffcfad856
            // As mentioned in the remarks, there is a max queue name length enforced by azure, otherwise friendlier names would be preferred.
            return $"cq-{(int)subjectType}-{(int)commandType}-{assetGroupId}".ToLowerInvariant();
        }

        public AzureQueueStorageCommandQueue(
            IAzureCloudQueue azureQueue,
            AgentId agentId,
            AssetGroupId assetGroupId,
            PrivacyCommandType commandType,
            SubjectType subjectType,
            IClock clock,
            IAssetGroupAzureQueueTrackerCache assetGroupAzureQueueTrackerCache)
        {
            this.azureQueue = azureQueue;
            this.commandType = commandType;
            this.subjectType = subjectType;
            this.clock = clock;
            this.assetGroupAzureQueueTrackerCache = assetGroupAzureQueueTrackerCache;
            this.AssetGroupId = assetGroupId;
            this.AgentId = agentId;
            this.moniker = this.azureQueue.AccountName;

            if (!SupportedCommandTypes.Contains(commandType))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(commandType), 
                    commandType, 
                    $"Supported CommandTypes for {nameof(AzureQueueStorageCommandQueue)} are: {string.Join(", ", SupportedCommandTypes)}");
            }
        }

        public async Task AddQueueStatisticsAsync(ConcurrentBag<AgentQueueStatistics> resultBag, bool getDetailedStatistics, CancellationToken token)
        {
            if (this.QueueExists)
            {
                await Logger.InstrumentAsync(
                    new OutgoingEvent(SourceLocation.Here()),
                    async ev =>
                    {
                        LogCommonQueueMessageInfo(ev, this.azureQueue, null, null);

                        var queueStats = new AgentQueueStatistics
                        {
                            AssetGroupId = this.AssetGroupId,
                            AgentId = this.AgentId,
                            DbMoniker = this.moniker,
                            QueryDate = this.clock.UtcNow.DateTime,
                            SubjectType = this.subjectType,
                            CommandType = this.commandType
                        };

                        var count = await this.azureQueue.GetCountAsync(token).ConfigureAwait(false);
                        ev["QueueDepth"] = count.ToString();
                        queueStats.PendingCommandCount = count;
                        resultBag.Add(queueStats);
                    });
            }
            else
            {
                this.assetGroupAzureQueueTrackerCache.StartQueueTracker(this.azureQueue, this.commandType);
            }
        }

        public Task DeleteAsync(LeaseReceipt leaseReceipt)
        {
            this.CheckLeaseReceipt(leaseReceipt);
            var messagePopReceipt = leaseReceipt.DeserializeToken();
            var message = new CloudQueueMessage(messagePopReceipt.MessageId, messagePopReceipt.PopReceipt);

            return Logger.InstrumentAsync(
                new OutgoingEvent(SourceLocation.Here()),
                async ev =>
                {
                    LogCommonQueueMessageInfo(ev, this.azureQueue, message, this.clock);
                    LogCommonLeaseReceiptInfo(ev, leaseReceipt);

                    try
                    {
                        await this.azureQueue.DeleteMessageAsync(message);
                    }
                    catch (StorageException ex)
                    {
                        (CommandFeedInternalErrorCode statusCode, bool isExpected) = ClassifyUpdateException(ev, ex);

                        if (statusCode == CommandFeedInternalErrorCode.Unknown)
                        {
                            // Unable to classify
                            throw;
                        }

                        throw new CommandFeedException(ex)
                        {
                            ErrorCode = statusCode,
                            IsExpected = isExpected
                        };
                    }
                });
        }

        /// <summary>
        /// Enqueues the given command into the shard represented by the given moniker.
        /// </summary>
        public async Task EnqueueAsync(string moniker, PrivacyCommand command)
        {
            if (this.moniker != moniker)
            {
                throw new InvalidOperationException($"Queue for moniker {this.moniker} does not work for moniker {moniker}");
            }

            // Throws if doesn't exist; completes synchronously most of the time.
            await this.azureQueue.EnsureQueueExistsAsync().ConfigureAwait(false);

            var timeToLive = DateTimeHelper.GetTimeToLiveSeconds(command.AbsoluteExpirationTime);
            StoragePrivacyCommand jsonCommand = new StorageCommandSerializer().Process(command);
            CloudQueueMessage message = QueueStorageCommandConverter.ToCloudQueueMessage(jsonCommand);

            await Logger.InstrumentAsync(
                new OutgoingEvent(SourceLocation.Here()),
                async ev =>
                {
                    LogCommonQueueMessageInfo(ev, this.azureQueue, message, this.clock);
                    ev["TimeToLiveSeconds"] = timeToLive.ToString();

                    await this.azureQueue.AddMessageAsync(message, null, TimeSpan.FromSeconds(timeToLive)).ConfigureAwait(false);
                });

        }

        public Task FlushAgentQueueAsync(DateTimeOffset flushDate, CancellationToken token)
        {
            throw new CommandFeedException($"{nameof(flushDate)} value of: {flushDate} cannot be honored for flushing this queue type.")
            {
                ErrorCode = CommandFeedInternalErrorCode.NotSupported
            };
        }

        public Task<PrivacyCommand> QueryCommandAsync(LeaseReceipt leaseReceipt)
        {
            throw new CommandFeedException($"Query command is not supported for {leaseReceipt.CommandType} with storage type: {leaseReceipt.QueueStorageType}")
            {
                ErrorCode = CommandFeedInternalErrorCode.NotSupported
            };
        }

        /// <summary>
        /// Pops at most "maxToPop" items off of the queue.
        /// </summary>
        public async Task<CommandQueuePopResult> PopAsync(int maxToPop, TimeSpan? requestedLeaseDuration, CommandQueuePriority commandQueuePriority)
        {
            int remainingCommandsToPop = maxToPop;
            var errors = new List<Exception>();
            var commands = new List<PrivacyCommand>();

            // The real Calculation is Math.Ceiling(((decimal)maxToPop / (decimal)32)) but this always results in 4.
            // If for some reason our max changes, or azure changes, we could calculate this - but that's unlikely to ever change.
            const int MaxAttemptCount = 4;
            const int MaxAzureQueueGetMessageLimit = 32;
            int attemptCount = 0;

            if (this.QueueExists)
            {
                if (!requestedLeaseDuration.HasValue)
                {
                    requestedLeaseDuration = this.defaultLeaseDuration;
                }

                do
                {
                    if (remainingCommandsToPop <= 0)
                    {
                        break;
                    }

                    attemptCount++;
                    CommandQueuePopResult queueResponse = await Logger.InstrumentAsync(
                        new OutgoingEvent(SourceLocation.Here()),
                        async ev =>
                        {
                            LogCommonQueueMessageInfo(ev, this.azureQueue, null, null);

                            // max batch size set to azure limit, otherwise azure will tell you 'bad request'
                            int getMessageRequestCount = Math.Min(Math.Max(remainingCommandsToPop, 0), MaxAzureQueueGetMessageLimit);
                            IEnumerable<CloudQueueMessage> result = await this.azureQueue.GetMessagesAsync(getMessageRequestCount, requestedLeaseDuration)
                                .ConfigureAwait(false);
                            CommandQueuePopResult popResult = this.ConvertToCommandQueuePopResult(result);
                            ev["PopCount"] = popResult.Commands.Count.ToString();
                            ev["GetMessageRequestCount"] = getMessageRequestCount.ToString();
                            ev["GetMessageAttemptCount"] = attemptCount.ToString();

                            return popResult;
                        });

                    if (queueResponse?.Errors?.Count > 0)
                    {
                        errors.AddRange(queueResponse.Errors);
                    }

                    if (queueResponse?.Commands?.Count > 0)
                    {
                        commands.AddRange(queueResponse.Commands);

                        // We found commands, so reduce the remainingCommandsToPop.
                        remainingCommandsToPop -= queueResponse.Commands.Count;
                    }
                    else
                    {
                        // stop trying to pop from this queue, it's empty.
                        break;
                    }

                    if (queueResponse?.Commands?.Count < MaxAzureQueueGetMessageLimit)
                    {
                        // also break, because the queue doesn't have anything else to give.
                        break;
                    }

                    if (attemptCount >= MaxAttemptCount)
                    {
                        // limit how many times this can be attempted
                        break;
                    }
                    
                    // Make sure this doesn't go negative.
                } while (remainingCommandsToPop > 0);
            }
            else
            {
                this.assetGroupAzureQueueTrackerCache.StartQueueTracker(this.azureQueue, this.commandType);
                return new CommandQueuePopResult(commands, null);
            }
            
            // For the error response, it shouldn't matter if it's null or an empty list but existing behavior set to null when there is no errors, so keeping parity with exiting behavior.
            return new CommandQueuePopResult(commands, errors.Count == 0 ? null : errors);
        }

        /// <summary>
        /// Replaces the given command.
        /// </summary>
        public async Task<LeaseReceipt> ReplaceAsync(LeaseReceipt leaseReceipt, PrivacyCommand command, CommandReplaceOperations commandReplaceOperations)
        {
            this.CheckLeaseReceipt(leaseReceipt);

            StoragePrivacyCommand jsonCommand = new StorageCommandSerializer().Process(command);
            var messagePopReceipt = leaseReceipt.DeserializeToken();
            var message = QueueStorageCommandConverter.ToCloudQueueMessage(jsonCommand, messagePopReceipt.MessageId, messagePopReceipt.PopReceipt);
            MessageUpdateFields messageUpdateFields = ConvertToMessageUpdateFields(commandReplaceOperations);
            TimeSpan visibilityTimeout = CreateVisibilityTimeout(this.clock.UtcNow, command.NextVisibleTime);

            await Logger.InstrumentAsync(
                new OutgoingEvent(SourceLocation.Here()),
                async ev =>
                {
                    LogCommonQueueMessageInfo(ev, this.azureQueue, message, this.clock);
                    LogCommonLeaseReceiptInfo(ev, leaseReceipt);
                    LogVisibilitySettings(ev, visibilityTimeout, command.NextVisibleTime);

                    try
                    {
                        // Azure Queue requires all calls for UpdateMessage at least specify MessageUpdateFields.Visibility
                        await this.azureQueue.UpdateMessageAsync(message, visibilityTimeout, messageUpdateFields | MessageUpdateFields.Visibility).ConfigureAwait(false);
                    }
                    catch (StorageException ex)
                    {
                        (CommandFeedInternalErrorCode statusCode, bool isExpected) = ClassifyUpdateException(ev, ex);

                        if (statusCode == CommandFeedInternalErrorCode.Unknown)
                        {
                            // Unable to classify
                            throw;
                        }

                        throw new CommandFeedException(ex)
                        {
                            ErrorCode = statusCode,
                            IsExpected = isExpected
                        };
                    }
                });

            // Azure Queue updates the message id/pop receipt of the message
            leaseReceipt.Token = JsonConvert.SerializeObject(new LeaseReceipt.AzureQueueMessageToken(message.Id, message.PopReceipt));
            return leaseReceipt;
        }

        // Storage REST API requires VisibilityTimeout to be an integer
        // Unfortunately, due to a bug in Azure Storage C# API, we have to do the round down by ourseleves 
        internal static TimeSpan CreateVisibilityTimeout(DateTimeOffset currentTime, DateTimeOffset nextVisibleTime)
        {
            return currentTime > nextVisibleTime
                ? TimeSpan.Zero
                : TimeSpan.FromSeconds((int)nextVisibleTime.Subtract(currentTime).TotalSeconds);
        }

        internal static MessageUpdateFields ConvertToMessageUpdateFields(CommandReplaceOperations commandReplaceOperations)
        {
            MessageUpdateFields messageUpdateFields = 0;

            if (commandReplaceOperations.HasFlag(CommandReplaceOperations.CommandContent))
            {
                messageUpdateFields |= MessageUpdateFields.Content;
            }

            if (commandReplaceOperations.HasFlag(CommandReplaceOperations.LeaseExtension))
            {
                messageUpdateFields |= MessageUpdateFields.Visibility;
            }

            return messageUpdateFields;
        }

        /// <summary>
        /// Indicates if the queue supports queue flush by date
        /// </summary>
        /// <returns>bool indicating if this is supported</returns>
        public bool SupportsQueueFlushByDate => false;

        public override bool SupportsLeaseReceipt(LeaseReceipt leaseReceipt)
        {
            return leaseReceipt.QueueStorageType == QueueStorageType.AzureQueueStorage &&
                   leaseReceipt.DatabaseMoniker == this.moniker &&
                   this.AgentId == leaseReceipt.AgentId &&
                   this.AssetGroupId == leaseReceipt.AssetGroupId &&
                   this.commandType == leaseReceipt.CommandType;
        }

        /// <summary>
        /// Upsert the given command into the shard represented by the given moniker.
        /// </summary>
        public Task UpsertAsync(string moniker, PrivacyCommand command)
        {
            // If command leaseReceipt or MessageId or PopReceipt is null, update message in queue will always fail.
            // So we should just try insert instead.
            // Currently only Replay calls UpsertAsync() and LeaseReceipt is always null in that case.
            var messagePopReceipt = command.LeaseReceipt?.DeserializeToken();
            if (messagePopReceipt?.MessageId == null || messagePopReceipt?.PopReceipt == null)
            {
                return EnqueueAsync(moniker, command);
            }

            try
            {
                return ReplaceAsync(command.LeaseReceipt, command, CommandReplaceOperations.CommandContent);
            }
            catch (CommandFeedException ex)
            {
                // If update failed due to PopReceipt not valid, try insert instead.
                if (ex.ErrorCode == CommandFeedInternalErrorCode.Conflict)
                {
                    return EnqueueAsync(moniker, command);
                }

                throw;
            }
        }

        /// <summary>
        /// The agent ID.
        /// </summary>
        protected AgentId AgentId { get; }

        /// <summary>
        /// The asset group ID.
        /// </summary>
        protected AssetGroupId AssetGroupId { get; }

        private CommandQueuePopResult ConvertToCommandQueuePopResult(IEnumerable<CloudQueueMessage> result)
        {
            List<PrivacyCommand> items = new List<PrivacyCommand>();
            foreach (var cloudQueueMessage in result)
            {
                StoragePrivacyCommand storageCommand = QueueStorageCommandConverter.FromCloudQueueMessage(cloudQueueMessage);
                PrivacyCommand command = new StorageCommandParser(this.AgentId, this.AssetGroupId, QueueStorageType.AzureQueueStorage).Process(storageCommand);

                var token = JsonConvert.SerializeObject(new LeaseReceipt.AzureQueueMessageToken(cloudQueueMessage.Id, cloudQueueMessage.PopReceipt));
                LeaseReceipt leaseReceipt = new LeaseReceipt(this.moniker, token, command, QueueStorageType.AzureQueueStorage);
                command.LeaseReceipt = leaseReceipt;
                items.Add(command);
            }

            return new CommandQueuePopResult(items, null);
        }

        private static void LogCommonQueueMessageInfo(OutgoingEvent ev, IAzureCloudQueue cloudQueue, CloudQueueMessage message, IClock clock)
        {
            if (ev != null)
            {
                ev["AccountName"] = cloudQueue.AccountName;
                ev["QueueName"] = cloudQueue.QueueName;

                if (message != null)
                {
                    ev["MessageId"] = message.Id;

                    if (message.ExpirationTime != null)
                    {
                        int hoursUntilExpiration = (int)(message.ExpirationTime.Value - clock.UtcNow).TotalHours;
                        ev["HoursUntilExpiration"] = hoursUntilExpiration.ToString();
                    }

                    if (message.InsertionTime != null)
                    {
                        int hoursPending = (int)(clock.UtcNow - message.InsertionTime.Value).TotalHours;
                        ev["HoursPending"] = hoursPending.ToString();
                    }

                    if (message.NextVisibleTime != null)
                    {
                        int minutesUntilVisible = (int)(message.NextVisibleTime.Value - clock.UtcNow).TotalMinutes;
                        ev["MinutesUntilVisible"] = minutesUntilVisible.ToString();
                    }
                }
            }
        }

        private static void LogCommonLeaseReceiptInfo(OutgoingEvent ev, LeaseReceipt receipt)
        {
            if (ev != null)
            {
                ev["LeaseReceiptCommandId"] = receipt.CommandId.ToString();
                ev["LeaseReceiptExpireTime"] = receipt.ApproximateExpirationTime.ToString();
                ev["LeaseReceiptAgentId"] = receipt.AgentId.ToString();
            }
        }

        private static void LogVisibilitySettings(OutgoingEvent ev, TimeSpan visibilityDelay, DateTimeOffset nextVisibleTime)
        {
            if (ev != null)
            {
                ev["VisibilityDelaySeconds"] = visibilityDelay.TotalSeconds.ToString();
                ev["NextVisibleTime"] = nextVisibleTime.ToString();
            }
        }

        private static (CommandFeedInternalErrorCode pcfErrorCode, bool IsExpected) ClassifyUpdateException(OutgoingEvent ev, StorageException ex)
        {
            ev.StatusCode = ex.RequestInformation.HttpStatusCode.ToString();

            // Azure Queue throw 400 BadRequest if the PopReceipt is malformed. 
            if (ex.RequestInformation.HttpStatusCode == 400)
            {
                return (CommandFeedInternalErrorCode.InvalidLeaseReceipt, false);
            }

            // Azure Queue throw 404 NotFound could due to either message actually not exist in queue, or PopReceipt is expired/invalid.
            // Return as conflict would be less confusing than command does not exist.
            if (ex.RequestInformation.HttpStatusCode == 404)
            {
                return (CommandFeedInternalErrorCode.Conflict, true);
            }

            return (CommandFeedInternalErrorCode.Unknown, false);
        }
    }

    public class CommandAzureCloudQueue : AzureQueueCloudQueue
    {
        /// <summary>
        /// The name of the storage account
        /// </summary>
        public override string AccountName => this.innerQueue.ServiceClient.Credentials.AccountName;

        /// <inheritdoc />
        public CommandAzureCloudQueue(CloudQueue innerQueue, TimeSpan defaultLeasePeriod)
            : base(innerQueue, defaultLeasePeriod)
        {
        }

        /// <inheritdoc />
        public override Task ClearAsync(CancellationToken token)
        {
            return this.innerQueue.ClearAsync(token);
        }

        public override Task<IEnumerable<CloudQueueMessage>> GetMessagesAsync(int batchSize, TimeSpan? visibilityTimeout = null)
        {
            return this.innerQueue.GetMessagesAsync(batchSize, visibilityTimeout, null, null);
        }
    }
}
