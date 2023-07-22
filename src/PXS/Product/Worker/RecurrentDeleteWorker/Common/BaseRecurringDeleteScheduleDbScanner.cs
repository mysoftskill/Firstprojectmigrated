namespace Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.Common
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.DistributedLocking;
    using Microsoft.ComplianceServices.Common.Queues;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Membership.MemberServices.Common.Worker;
    using Microsoft.Membership.MemberServices.ScheduleDbClient;
    using Microsoft.Membership.MemberServices.ScheduleDbClient.Model;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// BaseRecurringDeleteScheduleDbScanner
    /// </summary>
    public abstract class BaseRecurringDeleteScheduleDbScanner : DistributedBackgroundWorker
    {
        protected readonly IScheduleDbClient scheduleDbClient;
        protected readonly IPrivacyConfigurationManager configuration;
        protected readonly int maxItemCountOfScheduleDbRecords;
        protected readonly IAppConfiguration appConfiguration;
        protected readonly ILogger logger;
        protected string componentName;

        private readonly ICloudQueue<RecurrentDeleteScheduleDbDocument> cloudQueue;

        /// <summary>
        /// Creates an instance of <see cref="BaseRecurringDeleteScheduleDbScanner" />
        /// </summary>
        /// <param name="distributedLock"></param>
        /// <param name="lockPrimitives"></param>
        /// <param name="lockState"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="cloudQueue"></param>
        /// <param name="appConfiguration"></param>
        /// <param name="configuration"></param>
        /// <param name="scheduleDbClient"></param>
        /// <param name="logger"></param>
        public BaseRecurringDeleteScheduleDbScanner(
            DistributedLock<LockState> distributedLock,
            IDistributedLockPrimitives<DistributedBackgroundWorker.LockState> lockPrimitives,
            LockState lockState,
            CancellationToken cancellationToken,
            ICloudQueue<RecurrentDeleteScheduleDbDocument> cloudQueue,
            IAppConfiguration appConfiguration,
            IPrivacyConfigurationManager configuration,
            IScheduleDbClient scheduleDbClient,
            ILogger logger)
            : base(distributedLock, lockPrimitives, lockState, cancellationToken)
        {
            this.logger = logger;
            this.cloudQueue = cloudQueue;
            this.scheduleDbClient = scheduleDbClient;
            this.appConfiguration = appConfiguration;
            this.configuration = configuration;
            this.maxItemCountOfScheduleDbRecords = this.configuration.RecurringDeleteWorkerConfiguration.ScheduleDbConfig.MaxItemCountOfScheduleDbRecords;
            this.componentName = this.GetType().Name;
        }

        /// <summary>
        /// Do work only if the distributed lock is acquired
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task<bool> DoDistributedWorkAsync(CancellationToken cancellationToken)
        {
            OutgoingApiEventWrapper apiEvent = new OutgoingApiEventWrapper
            {
                DependencyOperationVersion = string.Empty,
                DependencyOperationName = "DoDistributedWorkAsync",
                DependencyName = componentName,
                PartnerId = $"{cloudQueue.StorageAccountName}.{cloudQueue.QueueName}",
                Success = false,
            };

            return await DoWorkInstrumentedAsync(
                apiEvent,
                async ev => await DoWorkInstrumentedPrivateAsync(apiEvent, cancellationToken).ConfigureAwait(false));
        }

        /// <summary>
        /// Get applicable records
        /// </summary>
        /// <param name="continuationToken"></param>
        /// <param name="apiEvent"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected abstract Task<(IList<RecurrentDeleteScheduleDbDocument>, string continuationToken)> GetScheduleDbRecordsInstrumentedAsync(
            string continuationToken,
            OutgoingApiEventWrapper apiEvent,
            CancellationToken cancellationToken);

        /// <summary>
        /// Enqueue given applicable docs into the cloud queue
        /// </summary>
        /// <param name="applicableDocs"></param>
        /// <returns></returns>
        private async Task EnqueueApplicableDocsAsync(IList<RecurrentDeleteScheduleDbDocument> applicableDocs)
        {
            foreach (var doc in applicableDocs)
            {
                await cloudQueue.EnqueueAsync(doc).ConfigureAwait(false);
            }
        }

        private async Task<bool> DoWorkInstrumentedPrivateAsync(OutgoingApiEventWrapper ev, CancellationToken cancellationToken)
        {
            string continuationToken = null;

            try
            {
                bool recurringDeleteAPI_Enabled =
                    await appConfiguration.IsFeatureFlagEnabledAsync(FeatureNames.PXS.RecurringDeleteAPIEnabled).ConfigureAwait(false);

                if (!recurringDeleteAPI_Enabled)
                {
                    logger.Warning(componentName, $"{nameof(DoWorkInstrumentedPrivateAsync)}. {FeatureNames.PXS.RecurringDeleteAPIEnabled} is disabled. accountName={cloudQueue.StorageAccountName}, queueName={cloudQueue.QueueName}.");
                    await Task.Delay(TimeSpan.FromSeconds(30));
                    return false;
                }

                while (!cancellationToken.IsCancellationRequested)
                {
                    IList<RecurrentDeleteScheduleDbDocument> applicableDocs = new List<RecurrentDeleteScheduleDbDocument>();
                    string updatedContinuationToken;

                    (applicableDocs, updatedContinuationToken) = await this.GetScheduleDbRecordsInstrumentedAsync(
                        continuationToken,
                        ev,
                        cancellationToken);

                    ev.ExtraData["RecordsFound"] = applicableDocs != null ? applicableDocs.Count.ToString() : "NULL";
                    this.logger.Information(componentName, $"RecordsFound={applicableDocs.Count}.");

                    await EnqueueApplicableDocsAsync(applicableDocs).ConfigureAwait(false);

                    if (updatedContinuationToken == null)
                    {
                        DualLogger.Instance.Information(componentName, $"Scanning job finished for current run.");
                        break;
                    }
                    continuationToken = updatedContinuationToken;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                this.logger.Error(componentName, ex, $"Fail to run {nameof(DoWorkInstrumentedPrivateAsync)}.");
                throw;
            }
        }
    }
}
