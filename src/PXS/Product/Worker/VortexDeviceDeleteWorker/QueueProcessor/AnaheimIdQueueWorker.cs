namespace Microsoft.Membership.MemberServices.Privacy.VortexDeviceDeleteWorker.QueueProcessor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.ComplianceServices.AnaheimIdLib.Schema;
    using Microsoft.ComplianceServices.Common.Queues;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Membership.MemberServices.Common.Worker;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.PcfAdapter;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Predicates;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.Helpers;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    /// <summary>
    /// Process AnaheimId requests.
    /// </summary>
    public class AnaheimIdQueueWorker : BackgroundWorker
    {
        private const string ComponentName = nameof(AnaheimIdQueueWorker);
        private const string GetMessageOperation = nameof(GetMessagesAsync);
        private const string DeleteMessageOperation = nameof(DeleteMessagesAsync);
        private const string RenewMessageLeaseOperation = nameof(RenewMessageLeaseAsync);

        private readonly ICloudQueue<AnaheimIdRequest> cloudQueue;
        private readonly IPcfAdapter pcfAdapter;
        private readonly IVortexDeviceDeleteQueueProccessorConfiguration workerConfiguration;
        private readonly IAppConfiguration appConfiguration;
        private readonly ILogger logger;

        /// <summary>
        /// Creates a new instance of <see cref="AnaheimIdQueueWorker" />
        /// </summary>
        public AnaheimIdQueueWorker(
            ICloudQueue<AnaheimIdRequest> cloudQueue,
            IPcfAdapter pcfAdapter,
            IVortexDeviceDeleteQueueProccessorConfiguration workerConfiguration,
            IAppConfiguration appConfiguration,
            ILogger logger)
        {
            if (cloudQueue is null)
            {
                throw new ArgumentNullException(nameof(cloudQueue));
            }

            if (pcfAdapter is null)
            {
                throw new ArgumentNullException(nameof(pcfAdapter));
            }

            if (workerConfiguration is null)
            {
                throw new ArgumentNullException(nameof(workerConfiguration));
            }

            if (appConfiguration is null)
            {
                throw new ArgumentNullException(nameof(appConfiguration));
            }

            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            this.cloudQueue = cloudQueue;
            this.pcfAdapter = pcfAdapter;
            this.workerConfiguration = workerConfiguration;
            this.appConfiguration = appConfiguration;
            this.logger = logger;
        }

        /// <inheritdoc/>>
        public override async Task<bool> DoWorkAsync()
        {
            // read messages from the cloud queue and send them to pcfadapter
            try
            {
                bool anaheimIdQueueWorker_Enabled = await this.appConfiguration.IsFeatureFlagEnabledAsync(FeatureNames.PXS.AnaheimIdQueueWorker_Enabled).ConfigureAwait(false);

                if (!anaheimIdQueueWorker_Enabled)
                {
                    this.logger.Warning(ComponentName, $"DoWorkAsync. {FeatureNames.PXS.AnaheimIdQueueWorker_Enabled} is disabled. accountName={this.cloudQueue.StorageAccountName}, queueName={this.cloudQueue.QueueName}.");
                    await Task.Delay(TimeSpan.FromSeconds(30));
                    return false;
                }

                var messages = await this.ActionOnMessagesAsync<IList<ICloudQueueItem<AnaheimIdRequest>>>(BuildOutgoingApiEvent(GetMessageOperation), async ev => await GetMessagesAsync(ev)).ConfigureAwait(false);
                this.logger.Information(ComponentName, $"MessageCount={messages.Count}, AccountName={this.cloudQueue.StorageAccountName}, QueueName={this.cloudQueue.QueueName}.");

                if (messages == null || messages.Count == 0)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(this.workerConfiguration.WaitOnQueueEmptyMilliseconds)).ConfigureAwait(false);
                    return true;
                }

                foreach (var message in messages)
                {
                    // Feature flag PXS.AnaheimIdRequestToPcfEnabled is configured with percentage filter used for throttling.
                    // Allows about X% of traffic goes through and send the rest back to queue if we set its value to be X.
                    // Each time returns true/false according to value comparsion between a random value and value X.
                    if (await appConfiguration.IsFeatureFlagEnabledAsync(FeatureNames.PXS.AnaheimIdRequestToPcfEnabled, useCached: false).ConfigureAwait(false))
                    {
                        if (!message.Data.AnaheimIds.Any())
                        {
                            this.logger.Warning(ComponentName, $"DoWorkAsync. No AnaheimIds for requestId={message.Data.DeleteDeviceIdRequest.RequestId}.");

                            // remove unapplicable AnaheimIdRequest
                            await this.ActionOnMessagesAsync<bool>(BuildOutgoingApiEvent(DeleteMessageOperation), async ev => await DeleteMessagesAsync(ev, new List<ICloudQueueItem<AnaheimIdRequest>> { message }).ConfigureAwait(false));
                            continue;
                        }

                        IList<PrivacyRequest> edgeBrowserIdRequests;
                        try
                        {
                            edgeBrowserIdRequests = CreateEdgeBrowserIdRequests(message.Data);
                        }
                        catch (Exception ex)
                        {
                            // will retry later as we already set visibilityTimeout at the time of getting messages
                            this.logger.Error(ComponentName, ex, $"Unexpected exception while creating EdgeBrowserIdRequests.");
                            continue;
                        }

                        string commandIds = string.Join(", ", edgeBrowserIdRequests.Select(c => c?.RequestId));

                        AdapterResponse result;
                        try
                        {
                            result = await this.pcfAdapter.PostCommandsAsync(edgeBrowserIdRequests).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            // will retry later as we already set visibilityTimeout at the time of getting messages
                            this.logger.Error(ComponentName, ex, $"Unexpected exception while sending to PCF. id's:. {commandIds}. accountName={this.cloudQueue.StorageAccountName}, queueName={this.cloudQueue.QueueName}.");
                            continue;
                        }
                        
                        if (!result.IsSuccess)
                        {
                            // will retry later as we already set visibilityTimeout at the time of getting messages
                            this.logger.Error(ComponentName, $"Failed to send to PCF. {result.Error}. id's:. {commandIds}. accountName={this.cloudQueue.StorageAccountName}, queueName={this.cloudQueue.QueueName}.");
                        }
                        else
                        {
                            this.logger.Information(ComponentName, $"Sent commands to PCF. id's: {commandIds}. accountName={this.cloudQueue.StorageAccountName}, queueName={this.cloudQueue.QueueName}");
                            await this.ActionOnMessagesAsync<bool>(BuildOutgoingApiEvent(DeleteMessageOperation), async ev => await DeleteMessagesAsync(ev, new List<ICloudQueueItem<AnaheimIdRequest>> { message })).ConfigureAwait(false);
                        }

                        int anaheimIdQueueWorkerDelayInMilliSeconds = this.appConfiguration.GetConfigValue<int>(ConfigNames.PXS.AnaheimIdQueueWorkerDelayInMilliSeconds, defaultValue: 1000);
                        this.logger.Information(ComponentName, $"Delay={anaheimIdQueueWorkerDelayInMilliSeconds}, AccountName={this.cloudQueue.StorageAccountName}, QueueName={this.cloudQueue.QueueName}.");
                        await Task.Delay(TimeSpan.FromMilliseconds(anaheimIdQueueWorkerDelayInMilliSeconds));
                    }
                    else
                    {
                        this.logger.Warning(ComponentName, $"Outgoing traffic got throttled. Extending lease of throttled requests.");
                        await this.ActionOnMessagesAsync<bool>(BuildOutgoingApiEvent(RenewMessageLeaseOperation), async ev => await RenewMessageLeaseAsync(ev, new List<ICloudQueueItem<AnaheimIdRequest>> { message })).ConfigureAwait(false);
                    }
                }
                return true;
            }
            catch (Exception exception)
            {
                // log and swallow unhandled exception otherwise it will exit from the worker thread
                this.logger.Error(ComponentName, exception, $"DoWorkAsync unhandled exception. accountName={this.cloudQueue.StorageAccountName}, queueName={this.cloudQueue.QueueName}");
                return false;
            }
        }

        /// <summary>
        /// Create Privacy requests.
        /// </summary>
        /// <param name="anaheimIdRequest">Anaheim id request.</param>
        /// <returns>List of the privacy requests.</returns>
        public IList<PrivacyRequest> CreateEdgeBrowserIdRequests(AnaheimIdRequest anaheimIdRequest)
        {
            IList<PrivacyRequest> privacyRequests = new List<PrivacyRequest>();
            bool isTestRequest = anaheimIdRequest.DeleteDeviceIdRequest.TestSignal;

            foreach (var anaheimId in anaheimIdRequest.AnaheimIds)
            {
                foreach (var dataTypeId in PxsContractsHelpers.DeviceDeleteDataTypeIds)
                {
                    var commandId = Guid.NewGuid();
                    var deleteRequest = new DeleteRequest
                    {
                        AuthorizationId = anaheimIdRequest.DeleteDeviceIdRequest.AuthorizationId,
                        RequestId = commandId,
                        RequestType = RequestType.Delete,
                        Subject = new EdgeBrowserSubject { EdgeBrowserId = anaheimId },
                        PrivacyDataType = dataTypeId.Value,
                        Predicate = PxsContractsHelpers.CreatePrivacyPredicate(dataTypeId),
                        Timestamp = anaheimIdRequest.DeleteDeviceIdRequest.CreateTime,
                        RequestGuid = anaheimIdRequest.DeleteDeviceIdRequest.RequestId,
                        CorrelationVector = anaheimIdRequest.DeleteDeviceIdRequest.CorrelationVector,
                        TimeRangePredicate = new TimeRangePredicate { EndTime = anaheimIdRequest.DeleteDeviceIdRequest.CreateTime },
                        VerificationToken = null,
                        Requester = "EdgeBrowser",
                        Portal = Portals.EdgeBrowserDeviceDeleteSignal,
                        IsTestRequest = isTestRequest,
                        ControllerApplicable = true,
                        ProcessorApplicable = false
                    };

                    this.logger.Information(ComponentName, $"CreateEdgeBrowserIdRequests: RequestGuid={deleteRequest.RequestGuid}, RequeustId(CommandId)={deleteRequest.RequestId},CV={deleteRequest.CorrelationVector}");
                    privacyRequests.Add(deleteRequest);
                }
            }

            return privacyRequests;
        }

        public async Task<IList<ICloudQueueItem<AnaheimIdRequest>>> GetMessagesAsync(OutgoingApiEventWrapper apiEvent)
        {
            IList<ICloudQueueItem<AnaheimIdRequest>> messages = new List<ICloudQueueItem<AnaheimIdRequest>>();

            int minVisibilityTimeoutInSeconds = appConfiguration.GetConfigValue<int>(ConfigNames.PXS.AnaheimIdQueueWorkerMinVisibilityTimeoutInSeconds, defaultValue: 5 * 60);
            int maxVisibilityTimeoutInSeconds = appConfiguration.GetConfigValue<int>(ConfigNames.PXS.AnaheimIdQueueWorkerMaxVisibilityTimeoutInSeconds, defaultValue: 60 * 60);
            int maxCount = appConfiguration.GetConfigValue<int>(ConfigNames.PXS.AnaheimIdQueueWorkerMaxCount, defaultValue: 32);

            // retry after if cannot process this.
            var visibilityTimeout = TimeSpan.FromSeconds(RandomHelper.Next(minVisibilityTimeoutInSeconds, maxVisibilityTimeoutInSeconds));

            messages = await this.cloudQueue.DequeueBatchAsync(
                visibilityTimeout: visibilityTimeout,
                maxCount: maxCount,
                CancellationToken.None).ConfigureAwait(false);

            apiEvent.ExtraData["MessageCount"] = messages.Count.ToString();

            return messages;
        }

        public async Task<bool> DeleteMessagesAsync(OutgoingApiEventWrapper apiEvent, IList<ICloudQueueItem<AnaheimIdRequest>> messages)
        {
            StringBuilder commandIds = new StringBuilder();
            foreach (var message in messages)
            {
                try
                {
                    await message.DeleteAsync().ConfigureAwait(false);
                    commandIds.Append(message.Data.DeleteDeviceIdRequest.RequestId).Append(",");
                }
                //  log and swallow exception
                catch (Exception exception)
                {
                    apiEvent.ErrorMessage += exception.Message + ",";
                    this.logger.Error(ComponentName, exception, $"Fail to delete message from queue. accountName={this.cloudQueue.StorageAccountName}, queueName={this.cloudQueue.QueueName}, messageId={message.MessageId}, commandId={message.Data.DeleteDeviceIdRequest.RequestId}.");
                }
            }

            apiEvent.ExtraData["CommandIds"] = commandIds.ToString();
            return string.IsNullOrEmpty(apiEvent.ErrorMessage);
        }

        public async Task<bool> RenewMessageLeaseAsync(OutgoingApiEventWrapper apiEvent, IList<ICloudQueueItem<AnaheimIdRequest>> messages)
        {
            int maxVisibilityTimeout = appConfiguration.GetConfigValue<int>(ConfigNames.PXS.AnaheimIdThrottledRequestsMaxVisibilityTimeoutInMinutes, defaultValue: 60 * 24);
            StringBuilder commandIds = new StringBuilder();

            foreach (var message in messages)
            {
                try
                {
                    // messages will be available for pick-up after visibilityTimeout time
                    var lease = TimeSpan.FromMinutes(RandomHelper.Next(0, maxVisibilityTimeout));
                    await message.UpdateAsync(lease).ConfigureAwait(false);
                    commandIds.Append(message.Data.DeleteDeviceIdRequest.RequestId).Append(",");
                }
                //  log and swallow exception
                catch (Exception exception)
                {
                    apiEvent.ErrorMessage += exception.Message + ",";
                    this.logger.Error(ComponentName, exception, $"Fail to renew message lease. accountName={this.cloudQueue.StorageAccountName}, queueName={this.cloudQueue.QueueName}, messageId={message.MessageId}, commandId={message.Data.DeleteDeviceIdRequest.RequestId}.");
                }
            }

            apiEvent.ExtraData["CommandIds"] = commandIds.ToString();
            return string.IsNullOrEmpty(apiEvent.ErrorMessage);
        }

        /// <summary>
        /// A shared method with api event wrapped for various actions we perform on messages, e.g. Get/Delete/Update messages
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="apiEvent"></param>
        /// <param name="actionFunc"></param>
        /// <returns></returns>
        private async Task<T> ActionOnMessagesAsync<T>(OutgoingApiEventWrapper apiEvent, Func<OutgoingApiEventWrapper, Task<T>> actionFunc)
        {
            try
            {
                var result = await actionFunc(apiEvent).ConfigureAwait(false);

                if (string.IsNullOrEmpty(apiEvent.ErrorMessage))
                {
                    apiEvent.Success = true;
                }
                return result;
            }
            catch (Exception exception)
            {
                apiEvent.ExceptionTypeName = exception.GetType().Name;
                apiEvent.ErrorMessage = exception.ToString();
                throw;
            }
            finally
            {
                apiEvent.Finish();
            }
        }

        private OutgoingApiEventWrapper BuildOutgoingApiEvent(string OperationName)
        {
            OutgoingApiEventWrapper apiEvent = new OutgoingApiEventWrapper
            {
                DependencyOperationVersion = string.Empty,
                DependencyOperationName = OperationName,
                DependencyName = ComponentName,
                DependencyType = "AzureQueue",
                PartnerId = $"{this.cloudQueue.StorageAccountName}.{this.cloudQueue.QueueName}",
                Success = false,
            };
            apiEvent.Start();
            apiEvent.ExtraData["AccountName"] = this.cloudQueue.StorageAccountName;
            apiEvent.ExtraData["QueueName"] = this.cloudQueue.QueueName;
            return apiEvent;
        }
    }
}
