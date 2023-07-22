namespace Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks.CommandStatusAggregation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Kusto.Cloud.Platform.Utils;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.Azure.ComplianceServices.Common.Interfaces;
    using Microsoft.Azure.Storage;
    using Microsoft.Azure.Storage.Auth;
    using Microsoft.Azure.Storage.Blob;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeedv2.Common.Storage;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Work item that is used as a trigger to check for overall completion of the given command ID.
    /// </summary>
    public class CheckCompletionWorkItem
    {
        /// <summary>
        /// The command ID to check for completion.
        /// </summary>
        public CommandId CommandId { get; set; }
    }

    /// <summary>
    /// Processes CheckCompletionWorkItems out of an Azure Queue.
    /// </summary>
    public class CheckCompletionWorkItemQueueHandler : IAzureWorkItemQueueHandler<CheckCompletionWorkItem>
    {
        // The name of the agent map file dropped in the root of the zip file.
        private const string AgentMapFileName = "agentMap.json";

        private const string PcfKustoTableForMalwareFound = "PCFAgentsWithMalwareInMSAExports";

        // How long to wait if things aren't finished yet.
        private static readonly TimeSpan NotCompleteYetDelay = TimeSpan.FromDays(1);

        private static readonly TimeSpan WaitForV2ExpecationWorkerRunDelay = TimeSpan.FromHours(2);

        private readonly ICommandHistoryRepository repository;

        private readonly IAppConfiguration appConfiguration;

        private readonly ICosmosDbClientFactory cosmosDbClientFactory;

        private readonly IKustoClient kustoClient;

        private ICosmosDbClient<ExportExpectationEventEntry> exportExpectationEventCosmosDbClient;
        private ICosmosDbClient<CompletedExportEventEntry> completedExportEventCosmosDbClient;

        // expectationWorker runtime.
        private DateTime cacheExpectationWorkerRuntime = DateTime.MinValue;
        // time at which expectationworker runtime was updated.
        private DateTime cacheExpectationWorkerRuntimeCachedTime = DateTime.MinValue;

        private IPCFv2ExportInfoProvider exportInfoProvider;

        public CheckCompletionWorkItemQueueHandler(ICommandHistoryRepository repository, IKustoClient kustoClient, IAppConfiguration appConfiguration, ICosmosDbClientFactory cosmosDbClientFactory, IPCFv2ExportInfoProvider exportInfoProvider)
        {
            this.repository = repository;
            this.kustoClient = kustoClient;
            this.appConfiguration = appConfiguration;
            this.cosmosDbClientFactory = cosmosDbClientFactory;
            this.completedExportEventCosmosDbClient =
                                cosmosDbClientFactory.GetCosmosDbClient<CompletedExportEventEntry>(Config.Instance.PCFv2.CosmosDbCompletedCommandsContainerName,
                                Config.Instance.PCFv2.CosmosDbDatabaseName, Config.Instance.PCFv2.CosmosDbEndpointName);
            this.exportExpectationEventCosmosDbClient = cosmosDbClientFactory.GetCosmosDbClient<ExportExpectationEventEntry>(Config.Instance.PCFv2.CosmosDbExportExpectationsContainerName,
                            Config.Instance.PCFv2.CosmosDbDatabaseName, Config.Instance.PCFv2.CosmosDbEndpointName);
            this.exportInfoProvider = exportInfoProvider;
        }

        public SemaphorePriority WorkItemPriority => SemaphorePriority.High;

        /// <summary>
        /// Checks that a command has all of it's sub-items completed, and, if so, raises a notification.
        /// </summary>
        public async Task<QueueProcessResult> ProcessWorkItemAsync(QueueWorkItemWrapper<CheckCompletionWorkItem> wrapper)
        {
            var workItem = wrapper.WorkItem;

            var record = await this.repository.QueryAsync(
                workItem.CommandId,
                CommandHistoryFragmentTypes.Core | CommandHistoryFragmentTypes.Status | CommandHistoryFragmentTypes.ExportDestinations);

            IncomingEvent.Current?.SetProperty("CommandId", workItem.CommandId.Value);

            if (record == null)
            {
                // No record found for this command id, likely due to command is expired and deleted from commandhistory
                IncomingEvent.Current?.SetProperty("Status", "CommandExpired");
                return QueueProcessResult.Success();
            }

            if (record.Core.IsGloballyComplete)
            {
                // Already done. No bananas here.
                IncomingEvent.Current?.SetProperty("Status", "AlreadyComplete");
                return QueueProcessResult.Success();
            }

            if (!record.StatusMap.All(x => x.Value.CompletedTime != null))
            {
                // Not everyone has finished. Check later.
                IncomingEvent.Current?.SetProperty("Status", "NotYetComplete");
                return QueueProcessResult.RetryAfter(NotCompleteYetDelay);
            }

            if (!record.StatusMap.Any(x => x.Value.IngestionTime != null))
            {
                // We don't have any firm ingestion records, so we're definitely not ready to proclaim completeness.
                IncomingEvent.Current?.SetProperty("Status", "NotYetIngested");
                return QueueProcessResult.RetryAfter(NotCompleteYetDelay);
            }

            CommandHistoryFragmentTypes changedFragment = CommandHistoryFragmentTypes.Core;

            // Check for export expectations in PCFV2 cosmos db
            if ( record.Core.CommandType == PrivacyCommandType.Export && await appConfiguration.IsFeatureFlagEnabledAsync(FeatureNames.PCFV2.ReadExportExpectations).ConfigureAwait(false))
            {
                try
                {
                    // Convert the command id guid to a string representation with dashes
                    string commandIdAsString = Guid.Parse(record.CommandId.Value).ToString();

                    var queryTextForCompletedCommand = $"SELECT * FROM c WHERE c.id = \"{commandIdAsString}\"";
                    IList<CompletedExportEventEntry> exportCompletedEventEntries = await completedExportEventCosmosDbClient.ReadEntriesAsync(queryTextForCompletedCommand).ConfigureAwait(false);

                    bool isCommandForceCompleted = false;
                    if (exportCompletedEventEntries.Count > 0)
                    {
                        // this is force completed. No need to wait for v2 expectations.
                        isCommandForceCompleted = true;
                    }

                    if (!isCommandForceCompleted)
                    {
                        // check that expectation worker was run.
                        if (cacheExpectationWorkerRuntime == DateTime.MinValue || DateTime.UtcNow.Subtract(this.cacheExpectationWorkerRuntimeCachedTime) > TimeSpan.FromHours(2))
                        {
                            this.cacheExpectationWorkerRuntime = await exportInfoProvider.GetExpectionWorkerLastestRunTimeAsync().ConfigureAwait(false);
                            this.cacheExpectationWorkerRuntimeCachedTime = DateTime.UtcNow;
                        }

                        // allow only if CreatedTime > cacheExpectationWorkerRuntime
                        if (record.Core.CreatedTime.CompareTo(this.cacheExpectationWorkerRuntime) > 0)
                        {
                            DualLogger.Instance.Information(nameof(CheckCompletionWorkItem), $"pcfv2 expectation worker did not run, need to wait CreatedTime = {record.Core.CreatedTime} cacheExpectationWorkerRuntime={cacheExpectationWorkerRuntime}");
                            // expectation worker did not run, need to wait
                            IncomingEvent.Current?.SetProperty("Status", "pcfv2 expectation worker did not run, need to wait");
                            return QueueProcessResult.RetryAfter(WaitForV2ExpecationWorkerRunDelay);
                        }
                    }

                    DualLogger.Instance.Information(nameof(CheckCompletionWorkItem), $"pcfv2 expectation worker run time check pass, CreatedTime = {record.Core.CreatedTime} cacheExpectationWorkerRuntime={cacheExpectationWorkerRuntime}");

                    // Check if the command id exists in the PCF V2 export expectation database for atleast one record
                    var queryTextForExpectations = $"SELECT * FROM c WHERE c.commandId = \"{commandIdAsString}\"";
                    IList<ExportExpectationEventEntry> exportExpectationEventEntries = await exportExpectationEventCosmosDbClient.ReadEntriesAsync(queryTextForExpectations).ConfigureAwait(false);

                    // check V2 batch export status only if not force completed yet.
                    if (!isCommandForceCompleted)
                    {
                        foreach (ExportExpectationEventEntry exp in exportExpectationEventEntries)
                        {
                            if (exp.ExportStatus == 0)
                            {
                                // Some agents are still working on it, so we're definitely not ready to proclaim completeness.
                                IncomingEvent.Current?.SetProperty("Status", "PCFV2 batch agents NotYetComplete");
                                return QueueProcessResult.RetryAfter(NotCompleteYetDelay);
                            }
                        }
                    }
                    
                    // populate destination record for batch agents.
                    if(UpdatePCFV2ExportDestinationRecordsForCommandAsync(record, exportExpectationEventEntries))
                    {
                        changedFragment |= CommandHistoryFragmentTypes.ExportDestinations;
                    }
                }
                catch (Exception ex)
                {
                    DualLogger.Instance.Error(nameof(CheckCompletionWorkItem), $"There was an error with the cosmos db client: {ex.Message}");
                    IncomingEvent.Current?.SetProperty("Status", "PCFV2CosmosDBError");
                    return QueueProcessResult.RetryAfter(NotCompleteYetDelay);
                }
            }

            IncomingEvent.Current?.SetProperty("Status", "Completed");
            record.Core.IsGloballyComplete = true;

            if (record.Core.CommandType == PrivacyCommandType.Export)
            {
                using (CancellationTokenSource cancellationtokenSource = new CancellationTokenSource())
                {
                    Task extendLeaseTask = ExtendLeaseAsync(wrapper, cancellationtokenSource);
                    try
                    {
                        DualLogger.Instance.Information(nameof(CheckCompletionWorkItem), $"Completing export command {record.CommandId}");
                        await this.CompleteExportAsync(record, cancellationtokenSource.Token);
                    }
                    finally
                    {
                        cancellationtokenSource.Cancel();
                    }

                    await CleanupExportAsync(record);
                    DualLogger.Instance.Information(nameof(CheckCompletionWorkItem), $"Completed export command {record.CommandId}");
                }
            }

            record.Core.CompletedTime = DateTimeOffset.UtcNow;
            try
            {
                await this.repository.ReplaceAsync(record, changedFragment);
                return QueueProcessResult.Success();
            }
            catch (CommandFeedException ex)
            {
                if (ex.ErrorCode == CommandFeedInternalErrorCode.Conflict)
                {
                    // Conflicts are expected from time to time.
                    IncomingEvent.Current?.SetProperty("Conflict", "true");
                    return QueueProcessResult.TransientFailureRandomBackoff();
                }

                throw;
            }
        }

        private static async Task CleanupExportAsync(CommandHistoryRecord record)
        {
            DualLogger.Instance.Information(nameof(CheckCompletionWorkItem), $"Cleaning up export staging for {record.CommandId}");

            // Go through each staging destination
            foreach (var source in record.ExportDestinations)
            {
                // Attempt to clean it up. The staging storage manager will not cleanup containers it doesn't own.
                await ExportStorageManager.Instance.CleanupContainerAsync(source.Value.ExportDestinationUri, record.CommandId);
            }
        }

        private async Task CompleteExportAsync(CommandHistoryRecord record, CancellationToken cancellationToken)
        {
            // If the final destination is not storage owned by us, there is no copying to do, we should have been writing directly here already.
            if (!ExportStorageManager.Instance.IsManaged(record.Core.FinalExportDestinationUri))
            {
                PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "ExportComplete:ExternalStorage").Increment();
                return;
            }

            // First, get a reference to the target zipfile blob
            CloudBlobContainer destinationContainer = ExportStorageManager.Instance.GetFullAccessContainer(record.Core.FinalExportDestinationUri);
            CloudBlockBlob exportFileBlob = destinationContainer.GetBlockBlobReference($"Export-{record.CommandId}.zip");

            // Check if it's already done.
            DualLogger.Instance.Information(nameof(CheckCompletionWorkItem), $"Checking for existing zip file for {record.CommandId}");

            if (await exportFileBlob.ExistsAsync(cancellationToken))
            {
                // The export was already successfully aggregated. Do nothing.
                DualLogger.Instance.Information(nameof(CheckCompletionWorkItem), $"Export zip file already completed for {record.CommandId}");
                PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "ExportComplete:AlreadyComplete").Increment();
                return;
            }

            DualLogger.Instance.Information(nameof(CheckCompletionWorkItem), $"Beginning export zip for {record.CommandId}");

            // Create the destination zip, only if it doesn't exist, since we just deleted it or it's never been worked on.
            CloudBlobStream blobStream = await exportFileBlob.OpenWriteAsync(AccessCondition.GenerateIfNotExistsCondition(), null, null, cancellationToken);
            ExportArchive archive = new ExportArchive(blobStream, new ExportMalwareScannerFactory(new Defender()), kustoClient);
            await archive.BuildAsync(ExportStorageManager.Instance, record, cancellationToken);

            // Dispose commits the blob blocks. Do NOT dispose and cause a commit if there is an exception, which is why
            // this isn't using the using pattern. Do NOT change this behavior of only Disposing in the success case.
            blobStream.Dispose();

            DualLogger.Instance.Information(nameof(CheckCompletionWorkItem), $"Finalized export zip file for {record.CommandId}");

            PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "ExportComplete").Increment();
            IPerformanceCounter performanceCounter = PerfCounterUtility.GetOrCreate(PerformanceCounterType.Number, "ExportComplete");
            performanceCounter.Set("Sources", archive.SourceCount > int.MaxValue ? int.MaxValue : (int)archive.SourceCount);
            performanceCounter.Set("Files", archive.SourceFileCount > int.MaxValue ? int.MaxValue : (int)archive.SourceFileCount);
            performanceCounter.Set("Bytes", archive.TotalSourceBytes > int.MaxValue ? int.MaxValue : (int)archive.TotalSourceBytes);
        }

        public bool UpdatePCFV2ExportDestinationRecordsForCommandAsync(CommandHistoryRecord record, IList<ExportExpectationEventEntry> expectations)
        {
            // If the final destination is not storage owned by us, there is no copying to do, we should have been writing directly here already.
            if (!ExportStorageManager.Instance.IsManaged(record.Core.FinalExportDestinationUri))
            {
                return false;
            }

            DualLogger.Instance.Information(nameof(CheckCompletionWorkItem), $"Adding PCFV2 export destinations");

            bool exportDestinationWereAdded = false;

            // Convert the command id guid to a string representation with dashes
            if (expectations.Count > 0)
            {
                expectations.ForEach(entry => {
                    if (entry.FinalContainerUri != null)
                    {
                        string[] compoundKeyParts = entry.CompoundKey.Split('.');
                        AgentId agentId = new AgentId(compoundKeyParts[0]);
                        AssetGroupId assetGroupId = new AssetGroupId(compoundKeyParts[1]);
                        // only add exports that are completed.
                        if (entry.ExportStatus == 1)
                        {
                            record.ExportDestinations[(agentId, assetGroupId)] = new CommandHistoryExportDestinationRecord(agentId, assetGroupId, entry.FinalContainerUri, entry.FinalDestinationPath);
                            DualLogger.Instance.Information(nameof(CheckCompletionWorkItem), $"Added CommandHistoryExportDestinationRecord with CommandId={record.CommandId}, AgentId={agentId} , AssetGroupId={assetGroupId}, Uri = {entry.FinalContainerUri}");
                            exportDestinationWereAdded = true;
                        }
                        else
                        {
                            DualLogger.Instance.Information(nameof(CheckCompletionWorkItem), $"export for command={record.CommandId} was not completed by agent={agentId} for assetgroup={assetGroupId}");
                        }
                    }
                    else
                    {
                        throw new Exception("finalContainerURI cannot be null");
                    }
                });
            }
            else 
            {
                DualLogger.Instance.Information(nameof(CheckCompletionWorkItem), $"No batch agent completion records for command with CommandId={record.CommandId}");
            }

            return exportDestinationWereAdded;
        }
        
        private static async Task ExtendLeaseAsync(QueueWorkItemWrapper<CheckCompletionWorkItem> wrapper, CancellationTokenSource tokenSource)
        {
            while (!tokenSource.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), tokenSource.Token);
                    if (wrapper.RemainingLeaseTime < TimeSpan.FromMinutes(5))
                    {
                        // Try to extend our lease, but don't update the contents of the message.
                        await wrapper.UpdateAsync(TimeSpan.FromMinutes(30), updateContent: false);
                    }
                }
                catch
                {
                    DualLogger.Instance.Information(nameof(CheckCompletionWorkItem), $"Failed to extend the lease for command [{wrapper.WorkItem.CommandId}]");

                    // Cancel if we fail. We've lost our lease.
                    tokenSource.Cancel();
                }
            }
        }
    }
}
