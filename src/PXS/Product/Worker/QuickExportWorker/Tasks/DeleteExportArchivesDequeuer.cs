namespace Microsoft.Membership.MemberServices.Privacy.QuickExportWorker.Tasks
{
    using System;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.DataContracts.ExportTypes;
    using Microsoft.PrivacyServices.Common.Azure;

    public class DeleteExportArchivesDequeuer
    {

        public const string ExportArchivesDeleteCounterCategoryName = "PXSExportArchivesDelete";
        private const int BoundedCapacity = 100;
        private const int MaxDegreeOfParallelism = 10;

        private IDependencyManager dependencyManager;
        private ILogger logger;
        private ICounterFactory counterFactory;
        private DateTimeOffset started;
        private IExportStorageProvider exportStorageProvider;
        private int idlePollingMilliseconds;

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
                this.logger.Error(nameof(DeleteExportArchivesDequeuer), $"Initialize exception {ex}");
                throw;
            }
        }

        public async Task ProcessAsync()
        {
            this.logger.Information(nameof(DeleteExportArchivesDequeuer), "ProcessAsync Starting");
            try
            {
                var privacyConfigManger = (IPrivacyConfigurationManager)this.dependencyManager.GetService(typeof(IPrivacyConfigurationManager));
                this.idlePollingMilliseconds = privacyConfigManger.ExportConfiguration.IdlePollingMilliseconds;
                await this.exportStorageProvider.InitializeAsync(privacyConfigManger.PrivacyExperienceServiceConfiguration).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.logger.Error(nameof(DeleteExportArchivesDequeuer), $"Failed to initialize Storage Provider {ex}");
                throw ex;
            }

            try
            {
                while (true)
                {
                    ActionBlock<DeleteExportArchivesTask> exportTaskBlock = this.GetNewBlock();
                    try
                    {
                        while (true)
                        {
                            DeleteExportArchivesTask task = await this.GetTaskFromQueueAsync().ConfigureAwait(false);
                            if (!await exportTaskBlock.SendAsync(task).ConfigureAwait(false))
                                throw new Exception("ActionBlock denied the message. This action block should never deny messages");
                        }
                    }
                    catch (Exception ex)
                    {
                        this.logger.Warning(nameof(DeleteExportArchivesDequeuer), $"Queuing task threw exception {ex}");
                    }

                    try
                    {
                        if (!exportTaskBlock.Completion.IsCompleted)
                            exportTaskBlock.Complete();
                        await exportTaskBlock.Completion.ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        this.logger.Error(nameof(DeleteExportArchivesDequeuer), $"Closing ActionBlock failure {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.Error(nameof(DeleteExportArchivesDequeuer), $"ProcessAsync Exception {ex}");
            }
        }

        private async Task<DeleteExportArchivesTask> GetTaskFromQueueAsync()
        {
            while (true)
            {
                this.counterFactory.GetCounter(ExportArchivesDeleteCounterCategoryName, "WorkPoll", CounterType.Number).Increment();

                BaseQueueMessage msg = await this.exportStorageProvider.ExportArchiveDeletionQueue.GetMessageAsync().ConfigureAwait(false);
                if (msg != null)
                {
                    this.counterFactory.GetCounter(ExportArchivesDeleteCounterCategoryName, "WorkPollFoundWork", CounterType.Number).Increment();
                    return new DeleteExportArchivesTask(this.exportStorageProvider, msg, this.dependencyManager, this.logger);
                }

                await Task.Delay(this.idlePollingMilliseconds).ConfigureAwait(false);
            }
        }

        private ActionBlock<DeleteExportArchivesTask> GetNewBlock()
        {
            return new ActionBlock<DeleteExportArchivesTask>(
                async task =>
                {
                    try
                    {
                        await task.ProcessAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        this.logger.Error(
                            nameof(DeleteExportArchivesDequeuer),
                            $"GetNewBlock Task.ProcessAsync {ex}");
                        throw ex;
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
