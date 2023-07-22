// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.QuickExportWorker.Host
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Scheduler;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Scheduler.Interfaces;
    using Microsoft.Membership.MemberServices.Privacy.QuickExportWorker.ScheduledWorkerTasks;
    using Microsoft.Membership.MemberServices.PrivacyHost;
    using Microsoft.PrivacyServices.Common.Azure;

    public class SchedulerDecorator : HostDecorator
    {
        private readonly IAzureStorageProvider azureStorageProvider;

        private readonly IExportConfiguration exportConfiguration;

        private readonly IExportStorageProvider exportStorageProvider;

        private readonly ILogger logger;

        private readonly IPrivacyExperienceServiceConfiguration serviceConfiguration;

        /// <summary>
        ///     Constructor for SchedulerDecorator
        /// </summary>
        public SchedulerDecorator(
            IPrivacyConfigurationManager configurationManager,
            ILogger logger,
            IAzureStorageProvider azureStorageProvider,
            IExportStorageProvider exportStorage)
        {
            this.serviceConfiguration = configurationManager.PrivacyExperienceServiceConfiguration ??
                                        throw new ArgumentNullException(nameof(configurationManager.PrivacyExperienceServiceConfiguration));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.azureStorageProvider = azureStorageProvider ?? throw new ArgumentNullException(nameof(azureStorageProvider));
            this.exportStorageProvider = exportStorage ?? throw new ArgumentNullException(nameof(exportStorage));
            this.exportConfiguration = configurationManager.ExportConfiguration ?? throw new ArgumentNullException(nameof(configurationManager.ExportConfiguration));
        }

        public override ConsoleSpecialKey? Execute()
        {
            Trace.TraceInformation("Initializing SchedulerDecorator");
            this.azureStorageProvider.InitializeAsync(this.serviceConfiguration).Wait();
            CloudTableWrapper cloudTable = this.azureStorageProvider.GetCloudTable(DailyWorkerTrackingStorageFactory.PrivacyWorkerTableName);
            IDailyWorkerTrackingStorage dailyWorkerTrackingStorage = DailyWorkerTrackingStorageFactory.CreateDailyWorkerTrackingStorage(cloudTable);
            var exportCleanupWorker = new ExportCleanupDailyWorker(
                dailyWorkerTrackingStorage,
                this.exportStorageProvider,
                this.exportConfiguration,
                this.logger);

            var workerList = new List<Func<WorkerBase>>
            {
                () => exportCleanupWorker
            };

            var scheduler = new WorkScheduler(workerList, this.logger);
            scheduler.Start();
            return base.Execute();
        }
    }
}
