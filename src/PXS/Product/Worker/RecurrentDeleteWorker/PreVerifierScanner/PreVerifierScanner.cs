namespace Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.PreVerifierScanner
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.DistributedLocking;
    using Microsoft.ComplianceServices.Common.Queues;
    using Microsoft.Extensions.Azure;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Membership.MemberServices.Common.Worker;
    using Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.Common;
    using Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.LockPrimitives;
    using Microsoft.Membership.MemberServices.ScheduleDbClient;
    using Microsoft.Membership.MemberServices.ScheduleDbClient.Model;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// PreVerifierScanner
    /// </summary>
    public class PreVerifierScanner : BaseRecurringDeleteScheduleDbScanner
    {
        /// <summary>
        /// PreVerifierScanner
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
        public PreVerifierScanner(
            DistributedLock<LockState> distributedLock,
            IDistributedLockPrimitives<DistributedBackgroundWorker.LockState> lockPrimitives,
            LockState lockState, 
            CancellationToken cancellationToken, 
            ICloudQueue<RecurrentDeleteScheduleDbDocument> cloudQueue,
            IAppConfiguration appConfiguration,
            IPrivacyConfigurationManager configuration,
            IScheduleDbClient scheduleDbClient, 
            ILogger logger) 
            : base(distributedLock, lockPrimitives, lockState, cancellationToken, cloudQueue, appConfiguration, configuration, scheduleDbClient, logger)
        {
        }

        protected override async Task<(IList<RecurrentDeleteScheduleDbDocument>, string continuationToken)> GetScheduleDbRecordsInstrumentedAsync(
            string continuationToken,
            OutgoingApiEventWrapper apiEvent,
            CancellationToken cancellationToken)
        {
            int expirationOffsetDays = this.configuration.RecurringDeleteWorkerConfiguration.ScheduleDbConfig.PreVerifierExpirationDaysOffset;
            DateTimeOffset expirationDateTimeOffset = DateTimeOffset.UtcNow.AddDays(expirationOffsetDays);

            this.logger.Information($"{this.componentName}.{nameof(GetScheduleDbRecordsInstrumentedAsync)}", $"expirationDateTimeOffset={expirationDateTimeOffset}, expirationOffsetDays={expirationOffsetDays}, maxItemCountOfScheduleDbRecords={this.maxItemCountOfScheduleDbRecords}");

            return await this.scheduleDbClient.GetExpiredPreVerifiersRecurringDeletesScheduleDbAsync(
                preVerifierExpirationDate: expirationDateTimeOffset,
                continuationToken: continuationToken,
                maxItemCount: this.maxItemCountOfScheduleDbRecords);
        }
    }
}
