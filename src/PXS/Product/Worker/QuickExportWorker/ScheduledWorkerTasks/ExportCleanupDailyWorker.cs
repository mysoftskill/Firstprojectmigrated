// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.QuickExportWorker.ScheduledWorkerTasks
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Scheduler;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Scheduler.Interfaces;
    using Microsoft.PrivacyServices.Common.Azure;

    public class ExportCleanupDailyWorker : DailyWorkerBase
    {
        private readonly IExportConfiguration exportConfiguration;

        private readonly IExportStorageProvider storageProvider;

        /// <summary>
        ///     How the RunInterval is applied
        /// </summary>
        public override RunIntervalType IntervalType => RunIntervalType.FromLastFinish;

        /// <summary>
        ///     Name of the operation (this appears in telemetry events as the 'operation')
        /// </summary>
        public override string OperationName => "ExportCleanup";

        /// <summary>
        ///     How often the scheduler should run this work
        /// </summary>
        public override TimeSpan RunInterval => TimeSpan.FromSeconds(this.exportConfiguration.ExportcleanupCompleteRunIntervalSeconds);

        /// <summary>
        ///     Maximum time that this work item is expected to take to process
        /// </summary>
        /// <remarks>After this amount of time, it is assumed that the work failed if not complete and new instance may be started.</remarks>
        public override TimeSpan WorkExpirationTimeSpan => TimeSpan.FromSeconds(this.exportConfiguration.ExportcleanupCompleteWorkExpirationSeconds);

        /// <summary>
        ///     Name of the work item (this appears in QoS as the "Partner")
        /// </summary>
        public override string WorkItemName => "ExportCleanup";

        /// <summary>
        ///     Time added to the current UTC time to derive the work date
        /// </summary>
        public override TimeSpan WorkUtcOffset => TimeSpan.FromSeconds(this.exportConfiguration.ExportcleanupCompleteWorkUtcOffsetSeconds);

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExportCleanupDailyWorker" /> class.
        /// </summary>
        /// <param name="workTrackingStorage">The work tracking storage.</param>
        /// <param name="provider">The provider.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="logger">The logger.</param>
        public ExportCleanupDailyWorker(
            IDailyWorkerTrackingStorage workTrackingStorage,
            IExportStorageProvider provider,
            IExportConfiguration configuration,
            ILogger logger)
            : base(workTrackingStorage, logger)
        {
            this.storageProvider = provider ?? throw new ArgumentNullException(nameof(provider));
            this.exportConfiguration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <inheritdoc />
        public override async Task<WorkResult> DoWorkAsync(DateTime startTime, WorkOperationEvent workEvent, CancellationToken cancellationToken)
        {
            DateTime oldestStorage = DateTime.UtcNow.AddDays(this.exportConfiguration.MaxStorageAgeForCleanupInDays * -1);
            int totalDeleted;
            try
            {
                totalDeleted = await this.storageProvider.CleanupOldStorageAsync(
                    oldestStorage,
                    this.exportConfiguration.ExportcleanupCompleteWorkExpirationSeconds,
                    this.exportConfiguration.MaxCleanupIterations,
                    this.exportConfiguration.MaxBlobsToCleanupPerIteration,
                    this.exportConfiguration.CleanupIterationDelayMilliseconds,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.logger.Error(nameof(ExportCleanupDailyWorker), $"Failed {ex}");
                return WorkResult.Failed(ex);
            }
            this.logger.Information(nameof(ExportCleanupDailyWorker), $"Ended, deleted {totalDeleted}");
            return WorkResult.Succeeded;
        }
    }
}
