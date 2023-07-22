namespace Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks.DeleteExportArchive
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.PXS.Command.CommandStatus;

    /// <summary>
    /// Work item triggered to delete an export archive on user demand
    /// </summary>
    public class DeleteFullExportArchiveWorkItem
    {
        /// <summary>
        /// The command ID.
        /// </summary>
        public CommandId CommandId { get; set; }

        public DeleteFullExportArchiveWorkItem(CommandId cId)
        {
            this.CommandId = cId;
        }
    }

    /// <summary>
    /// Handles InsertIntoQueueWorkItem instances.
    /// </summary>
    public class DeleteFullExportArchiveWorkItemHandler : IAzureWorkItemQueueHandler<DeleteFullExportArchiveWorkItem>
    {

        private readonly ICommandHistoryRepository commandHistoryRepository;

        public DeleteFullExportArchiveWorkItemHandler(ICommandHistoryRepository commandHistoryRepository)
        {
            this.commandHistoryRepository = commandHistoryRepository;
        }

        public SemaphorePriority WorkItemPriority => SemaphorePriority.RealTime;

        public async Task<QueueProcessResult> ProcessWorkItemAsync(QueueWorkItemWrapper<DeleteFullExportArchiveWorkItem> wrapper)
        {
            var workItem = wrapper.WorkItem;
            var record = await this.commandHistoryRepository.QueryAsync(workItem.CommandId, CommandHistoryFragmentTypes.Core);
            if (record == null || (record.Core.ExportArchivesDeleteStatus == ExportArchivesDeleteStatus.DeleteCompleted && record.Core.FinalExportDestinationUri == null))
            {
                // this means the command is already deleted by other task, so we will simply discard the message and return success
                DualLogger.Instance.Information(nameof(DeleteFullExportArchiveWorkItem), $"Already deleted the record for commandId - {workItem.CommandId.Value}");
                return QueueProcessResult.Success();
            }
            DualLogger.Instance.Information(nameof(DeleteFullExportArchiveWorkItem), $"Deleting export container for {workItem.CommandId.Value}");

            await ExportStorageManager.Instance.CleanupContainerAsync(record.Core.FinalExportDestinationUri, workItem.CommandId);
            record.Core.ExportArchivesDeleteStatus = ExportArchivesDeleteStatus.DeleteCompleted;
            record.Core.FinalExportDestinationUri = null;   // update the destination URI, make it null so to avoid dead linking
            record.Core.DeletedTime = DateTimeOffset.UtcNow;

            try
            {
                DualLogger.Instance.Information(nameof(DeleteFullExportArchiveWorkItem), $"Updating CommandHistoryDB after deleting export archive for export - {workItem.CommandId.Value}");
                await this.commandHistoryRepository.ReplaceAsync(record, CommandHistoryFragmentTypes.Core);
            }
            catch (CommandFeedException ex)
            {
                DualLogger.Instance.Error(nameof(DeleteFullExportArchiveWorkItem), $"Error in updating CommandHistoryDB after deleting export archive for export - {workItem.CommandId.Value}");
                throw ex;
            }
            DualLogger.Instance.Information(nameof(DeleteFullExportArchiveWorkItem), $"Successfully Updated CommandHistoryDB after deleting export archive for export - {workItem.CommandId.Value}");
            return QueueProcessResult.Success();
        }
    }
}
