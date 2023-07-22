namespace Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.ComplianceServices.Common.Queues;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Membership.MemberServices.Common.Worker;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.ScheduleDbClient;
    using Microsoft.Membership.MemberServices.ScheduleDbClient.Exceptions;
    using Microsoft.Membership.MemberServices.ScheduleDbClient.Model;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// BaseRecurringDeleteQueueWorker
    /// </summary>
    public abstract class BaseRecurringDeleteQueueWorker : BackgroundWorker
    {
        protected readonly string componentName;
        protected readonly ICloudQueue<RecurrentDeleteScheduleDbDocument> cloudQueue;
        protected readonly ILogger logger;
        protected readonly IScheduleDbConfiguration scheduleDbConfig;
        protected readonly IScheduleDbClient scheduleDbClient;
        private readonly ICloudQueueConfiguration cloudQueueConfig;
        protected readonly IAppConfiguration appConfiguration;

        /// <summary>
        /// Creates an instance of <see cref="BaseRecurringDeleteQueueWorker" />
        /// </summary>
        /// <param name="cloudQueue"></param>
        /// <param name="cloudQueueConfiguration"></param>
        /// <param name="scheduleDbConfiguration"></param>
        /// <param name="appConfiguration"></param>
        /// <param name="scheduleDbClient"></param>
        /// <param name="logger"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public BaseRecurringDeleteQueueWorker(
            ICloudQueue<RecurrentDeleteScheduleDbDocument> cloudQueue,
            ICloudQueueConfiguration cloudQueueConfiguration,
            IScheduleDbConfiguration scheduleDbConfiguration,
            IAppConfiguration appConfiguration,
            IScheduleDbClient scheduleDbClient,
            ILogger logger)
        {
            this.componentName = this.GetType().Name;
            this.cloudQueue = cloudQueue ?? throw new ArgumentNullException(nameof(cloudQueue));
            this.cloudQueueConfig = cloudQueueConfiguration ?? throw new ArgumentNullException(nameof(cloudQueueConfiguration));
            this.appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));
            this.scheduleDbConfig = scheduleDbConfiguration ?? throw new ArgumentNullException(nameof(scheduleDbConfiguration));
            this.scheduleDbClient = scheduleDbClient ?? throw new ArgumentNullException(nameof(scheduleDbClient));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public override async Task<bool> DoWorkAsync()
        {
            var apiEvent = new OutgoingApiEventWrapper
            {
                DependencyOperationVersion = string.Empty,
                DependencyOperationName = "DoWorkAsync",
                DependencyName = this.componentName,
                PartnerId = $"{cloudQueue.StorageAccountName}.{cloudQueue.QueueName}",
                Success = false,
            };

            bool isWorkDone = await DoWorkInstrumentedAsync(
                apiEvent,
                async ev =>
                {
                    try
                    {
                        bool recurringDeleteAPI_Enabled =
                            await appConfiguration.IsFeatureFlagEnabledAsync(FeatureNames.PXS.RecurringDeleteAPIEnabled).ConfigureAwait(false);

                        if (!recurringDeleteAPI_Enabled)
                        {
                            logger.Warning(this.componentName, $"DoWorkAsync. {FeatureNames.PXS.RecurringDeleteAPIEnabled} is disabled. accountName={cloudQueue.StorageAccountName}, queueName={cloudQueue.QueueName}.");
                            ev.ExtraData["RecurringDeleteAPIEnabled"] = false.ToString();
                            await Task.Delay(TimeSpan.FromSeconds(30));
                            return false;
                        }
                        ev.ExtraData["RecurringDeleteAPIEnabled"] = true.ToString();
                        ev.Success = true;

                        // Max time try out posion queue??
                        var messages = await this.GetMessagesAsync().ConfigureAwait(false);
                        ev.ExtraData["AccountName"] = cloudQueue.StorageAccountName;
                        ev.ExtraData["QueueName"] = cloudQueue.QueueName;
                        ev.ExtraData["GetMessageCount"] = messages.Count.ToString();

                        int messagesFailedToProcess = 0;

                        foreach (var message in messages)
                        {
                            // lets process as many as we can
                            var recurrentDeleteScheduleDbDoc = message.Data;
                            try
                            {
                                await this.ProcessRecurrentDeleteScheduleDbDocumentAsync(recurrentDeleteScheduleDbDoc).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                // just log the error and continue
                                // we do not need to update visibility timeout it was already set up in get messages
                                messagesFailedToProcess++;
                                this.logger.Error(this.componentName, ex, $"Failed {messagesFailedToProcess}/{messages.Count}.");
                            }
                            finally
                            {
                                try
                                {
                                    await message.DeleteAsync().ConfigureAwait(false);
                                }
                                catch (Exception e)
                                {
                                    // If fails to delete the message, wait for serveral seconds and try again
                                    this.logger.Error(this.componentName, e, $"Failed to delete message:DocumentId={recurrentDeleteScheduleDbDoc.DocumentId},DataType={recurrentDeleteScheduleDbDoc.DataType}.");
                                    await Task.Delay(TimeSpan.FromSeconds(RandomHelper.Next(1, 5))).ConfigureAwait(false);
                                    await message.DeleteAsync().ConfigureAwait(false);
                                }
                            }
                        }

                        ev.ExtraData["MessagesFailedToProcess"] = messagesFailedToProcess.ToString();

                        // return true if there are any messages found otherwise we should delay.
                        return messages.Any();
                    }
                    catch (Exception ex)
                    {
                        logger.Error(this.componentName, ex, $"{nameof(DoWorkInstrumentedAsync)} unhandled exception. accountName={cloudQueue.StorageAccountName}, queueName={cloudQueue.QueueName}");
                        throw;
                    }
                });

            // To save some CPU and mitigate huge number of events generated,
            // we should delay if there are no messages found or we fail 
            // TODO: refactor this and call BackgroundWorker.Start(TimeSpan delay) instead
            if (!isWorkDone)
            {
                await Task.Delay(TimeSpan.FromSeconds(RandomHelper.Next(10, 60))).ConfigureAwait(false);
            }

            return isWorkDone;
        }

        /// <summary>
        /// ProcessRecurrentDeleteScheduleDbDocumentInstrumentedAsync
        /// </summary>
        /// <param name="recurrentDeleteScheduleDbDocument"></param>
        /// <param name="outgoingApi"></param>
        /// <returns></returns>
        public abstract Task ProcessRecurrentDeleteScheduleDbDocumentInstrumentedAsync(
            RecurrentDeleteScheduleDbDocument recurrentDeleteScheduleDbDocument,
            OutgoingApiEventWrapper outgoingApi);

        /// <summary>
        /// Update schedule db with an updated doc record
        /// In the case of schedule db exception we get the most recent doc, update its properties if necessary and try again
        /// </summary>
        /// <param name="recurrentDeleteScheduleDbDocument"></param>
        /// <param name="UpdateDocAction"></param>
        /// <returns></returns>
        public async Task UpdateScheduleDbAsync(
            RecurrentDeleteScheduleDbDocument recurrentDeleteScheduleDbDocument,
            Action<RecurrentDeleteScheduleDbDocument> UpdateDocAction=null)
        {
            UpdateDocAction?.Invoke(recurrentDeleteScheduleDbDocument);
            try
            {
                // send updated doc to schedule db
                await this.scheduleDbClient.CreateOrUpdateRecurringDeletesScheduleDbAsync(recurrentDeleteScheduleDbDocument).ConfigureAwait(false);
            }
            // fails for outdated doc
            catch (ScheduleDbClientException)
            {
                try
                {
                    // get up-to-date schedule doc and do a second try
                    var mostRecentScheduleDbDoc = await this.scheduleDbClient.GetRecurringDeletesScheduleDbDocumentAsync(recurrentDeleteScheduleDbDocument.DocumentId, CancellationToken.None);
                    UpdateDocAction?.Invoke(mostRecentScheduleDbDoc);
                    await this.scheduleDbClient.CreateOrUpdateRecurringDeletesScheduleDbAsync(mostRecentScheduleDbDoc).ConfigureAwait(false);
                }
                catch (ScheduleDbClientException ex)
                {
                    this.logger.Error(this.componentName, ex, $"Failed to send the updated recurrentDeleteScheduleDb doc the second time. {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// Get message from cloud queue
        /// </summary>
        /// <returns></returns>
        private async Task<IList<ICloudQueueItem<RecurrentDeleteScheduleDbDocument>>> GetMessagesAsync()
        {
            IList<ICloudQueueItem<RecurrentDeleteScheduleDbDocument>> messages = new List<ICloudQueueItem<RecurrentDeleteScheduleDbDocument>>();

            int minVisibilityTimeoutInSeconds = cloudQueueConfig.MinVisibilityTimeoutInSeconds;
            int maxVisibilityTimeoutInSeconds = cloudQueueConfig.MaxVisibilityTimeoutInSeconds;

            // retry after if cannot process this.
            var visibilityTimeout = TimeSpan.FromSeconds(RandomHelper.Next(minVisibilityTimeoutInSeconds, maxVisibilityTimeoutInSeconds));

            try
            {
                messages = await this.cloudQueue.DequeueBatchAsync(
                    visibilityTimeout: visibilityTimeout,
                    maxCount: cloudQueueConfig.MaxCount,
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.Error(this.componentName, ex, $"Failed to get messages. accountName={cloudQueue.StorageAccountName}, queueName={cloudQueue.QueueName}.");
                throw;
            }

            return messages;
        }

        private async Task ProcessRecurrentDeleteScheduleDbDocumentAsync(RecurrentDeleteScheduleDbDocument recurrentDeleteScheduleDbDocument)
        {
            var apiEvent = new OutgoingApiEventWrapper
            {
                DependencyOperationVersion = string.Empty,
                DependencyOperationName = nameof(ProcessRecurrentDeleteScheduleDbDocumentAsync),
                DependencyName = this.componentName,
                PartnerId = $"{this.cloudQueue.StorageAccountName}.{this.cloudQueue.QueueName}",
                Success = false,
            };

            try
            {
                apiEvent.Start();
                apiEvent.ExtraData["ScheduleDbId"] = recurrentDeleteScheduleDbDocument.DocumentId;
                await this.ProcessRecurrentDeleteScheduleDbDocumentInstrumentedAsync(recurrentDeleteScheduleDbDocument, apiEvent).ConfigureAwait(false);
                apiEvent.Success = true;
            }
            catch (Exception ex)
            {
                apiEvent.Success = false;
                apiEvent.ExceptionTypeName = ex.GetType().Name;
                apiEvent.ErrorMessage = ex.ToString();
                apiEvent.RequestStatus = Ms.Qos.ServiceRequestStatus.ServiceError;
                throw;
            }
            finally
            {
                apiEvent.Finish();
            }
        }
    }
}

