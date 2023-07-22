// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.QuickExportWorker
{
    using System;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.DataContracts.ExportTypes;
    using Microsoft.Membership.MemberServices.Privacy.QuickExportWorker.Tasks;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     ExportDequeuer pulls export request messages off the queue and executes them in via an ActionBlock
    /// </summary>
    public class ExportDequeuer
    {
        public const string ExportCounterCategoryName = "PXSExport";

        private const int BoundedCapacity = 100;

        private const int MaxDegreeOfParallelism = 10;

        private ICounterFactory counterFactory;

        private IDependencyManager dependencyManager;

        private IExportStorageProvider exportStorageProvider;

        private int idlePollingMilliseconds;

        private ILogger logger;

        private DateTimeOffset started;

        public void Initialize(IDependencyManager dependencyManager)
        {
            this.dependencyManager = dependencyManager ?? throw new ArgumentNullException(nameof(dependencyManager));
            this.logger = (ILogger)dependencyManager.GetService(typeof(ILogger));
            this.counterFactory = (ICounterFactory)dependencyManager.GetService(typeof(ICounterFactory));
            // TraceLogger.TraceSwitch.Level = TraceLevel.Verbose;
            try
            {
                this.started = DateTimeOffset.UtcNow;
                this.exportStorageProvider = (IExportStorageProvider)dependencyManager.GetService(typeof(IExportStorageProvider));
            }
            catch (Exception ex)
            {
                this.logger.Error(nameof(ExportDequeuer), $"Initialize exception {ex}");
                throw;
            }
        }

        public async Task ProcessAsync()
        {
            this.logger.Information(nameof(ExportDequeuer), "ProcessAsync Starting");
            try
            {
                var privacyConfigManger = (IPrivacyConfigurationManager)this.dependencyManager.GetService(typeof(IPrivacyConfigurationManager));
                this.idlePollingMilliseconds = privacyConfigManger.ExportConfiguration.IdlePollingMilliseconds;
                await this.exportStorageProvider.InitializeAsync(privacyConfigManger.PrivacyExperienceServiceConfiguration).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.logger.Error(nameof(ExportDequeuer), $"Failed to initialize Storage Provider {ex}");
                throw;
            }

            try
            {
                while (true)
                {
                    ActionBlock<CreateExportTask> exportTaskBlock = this.GetNewBlock();
                    try
                    {
                        while (true)
                        {
                            CreateExportTask task = await this.GetMsgFromQueueAsync().ConfigureAwait(false);
                            if (!await exportTaskBlock.SendAsync(task).ConfigureAwait(false))
                                throw new Exception("ActionBlock denied the message. This action block should never deny messages");
                        }
                    }
                    catch (Exception ex)
                    {
                        this.logger.Warning(nameof(ExportDequeuer), $"Queuing task threw exception {ex}");
                    }

                    try
                    {
                        if (!exportTaskBlock.Completion.IsCompleted)
                            exportTaskBlock.Complete();
                        await exportTaskBlock.Completion.ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        this.logger.Error(nameof(ExportDequeuer), $"Closing ActionBlock failure {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.Error(nameof(ExportDequeuer), $"ProcessAsync Exception {ex}");
            }
        }

        private async Task<CreateExportTask> GetMsgFromQueueAsync()
        {
            while (true)
            {
                this.counterFactory.GetCounter(ExportCounterCategoryName, "WorkPoll", CounterType.Number).Increment();

                BaseQueueMessage msg = await this.exportStorageProvider.ExportCreationQueue.GetMessageAsync().ConfigureAwait(false);
                if (msg != null)
                {
                    this.counterFactory.GetCounter(ExportCounterCategoryName, "WorkPollFoundWork", CounterType.Number).Increment();
                    return new CreateExportTask(this.exportStorageProvider, msg, this.dependencyManager, this.logger);
                }

                await Task.Delay(this.idlePollingMilliseconds).ConfigureAwait(false);
            }
        }

        private ActionBlock<CreateExportTask> GetNewBlock()
        {
            return new ActionBlock<CreateExportTask>(
                async task =>
                {
                    try
                    {
                        await task.ProcessAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        this.logger.Error(
                            nameof(ExportDequeuer),
                            $"GetNewBlock Task.ProcessAsync {ex}");
                    }
                },
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = MaxDegreeOfParallelism,
                    BoundedCapacity = BoundedCapacity
                });
        }
    }
}
