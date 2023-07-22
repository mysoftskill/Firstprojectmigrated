namespace Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.ScheduleScanner
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
    using Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.Common;
    using Microsoft.Membership.MemberServices.ScheduleDbClient;
    using Microsoft.Membership.MemberServices.ScheduleDbClient.Model;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// RecurrentDeleteScheduleScanner
    /// </summary>
    public class RecurrentDeleteScheduleScanner : BaseRecurringDeleteScheduleDbScanner
    {
        /// <summary>
        /// RecurrentDeleteScheduleScanner
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
        public RecurrentDeleteScheduleScanner(
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
            DateTimeOffset expectedNextDeleteOccuranceUtc = DateTimeOffset.UtcNow;

            this.logger.Information($"{this.componentName}.{nameof(GetScheduleDbRecordsInstrumentedAsync)}", $"expectedNextDeleteOccuranceUtc={expectedNextDeleteOccuranceUtc}, maxItemCountOfScheduleDbRecords={this.maxItemCountOfScheduleDbRecords}");

            return await this.scheduleDbClient.GetApplicableRecurringDeletesScheduleDbAsync(
                expectedNextDeleteOccuranceUtc: expectedNextDeleteOccuranceUtc,
                continuationToken: continuationToken,
                maxItemCount: this.maxItemCountOfScheduleDbRecords);
        }
    }
}
