namespace Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandReplay;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    /// <summary>
    /// Work item that is responsible for inserting replay request into batch job queue.
    /// </summary>
    public class ReplayRequestWorkItem
    {
        /// <summary>
        /// A list of asset group ids that applied to this replay request.
        /// </summary>
        public IEnumerable<AssetGroupId> AssetGroupIds { get; set; }

        /// <summary>
        /// The date replay starts from.
        /// </summary>
        public DateTimeOffset ReplayFromDate { get; set; }

        /// <summary>
        /// The date replay end to.
        /// </summary>
        public DateTimeOffset ReplayToDate { get; set; }

        /// <summary>
        /// A subject type that applied to this replay request.
        /// Null or empty means all subject types apply.
        /// </summary>
        public string SubjectType { get; set; }

        /// <summary>
        /// If true, include export commands also while replaying.
        /// </summary>
        public bool? IncludeExportCommands { get; set; }
    }

    public class ReplayRequestWorkItemHandler : IAzureWorkItemQueueHandler<ReplayRequestWorkItem>
    {
        private readonly ICommandReplayJobRepository replayJobRepo;

        public ReplayRequestWorkItemHandler(ICommandReplayJobRepository replayJobRepo)
        {
            this.replayJobRepo = replayJobRepo;
        }

        public SemaphorePriority WorkItemPriority => SemaphorePriority.Background;

        public async Task<QueueProcessResult> ProcessWorkItemAsync(QueueWorkItemWrapper<ReplayRequestWorkItem> wrapper)
        {
            var workItem = wrapper.WorkItem;
            var processDate = workItem.ReplayFromDate;

            IncomingEvent.Current?.SetProperty("ReplayFromDate", processDate.ToString());
            IncomingEvent.Current?.SetProperty("ReplayToDate", workItem.ReplayToDate.ToString());
            IncomingEvent.Current?.SetProperty("AssetGroupCount", workItem.AssetGroupIds.Count().ToString());
            IncomingEvent.Current?.SetProperty("IncludeExportCommands", workItem.IncludeExportCommands.ToString());

            bool includeExportCommands = workItem.IncludeExportCommands ?? false;

            while (processDate <= workItem.ReplayToDate)
            {
                long defaultNvt;
                if (Config.Instance.CommandReplay.ReplayProcessDelayEnabled && 
                    !FlightingUtilities.IsEnabled(FlightingNames.CommandReplayDisableProdReplayDelay))
                {
                    // This is trying to batch the same day replay jobs for a single target replay date into one single item in the DocDB
                    // defaultNvt set to next day 18:00 UTC, so that it runs in Redmond's office hours (10:00 PST)
                    var nextDay = DateTimeOffset.UtcNow.AddDays(1);
                    defaultNvt = new DateTimeOffset(nextDay.Year, nextDay.Month, nextDay.Day, 18, 0, 0, nextDay.Offset).ToUnixTimeSeconds();
                }
                else
                {
                    defaultNvt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                }

                string jobId = $"{defaultNvt}.{processDate.ToUnixTimeSeconds()}";

                var existingReplayJob = await this.replayJobRepo.QueryAsync(jobId);

                if (existingReplayJob == null)
                {
                    // if the replay job does not exist, create a new one.
                    var newReplayJob = new ReplayJobDocument
                    {
                        Id = jobId,
                        ReplayDate = processDate,
                        CreatedTime = DateTimeOffset.UtcNow,
                        UnixNextVisibleTimeSeconds = defaultNvt,
                        IsCompleted = false,
                        AssetGroupIds = workItem.AssetGroupIds.ToArray(),
                        SubjectType = workItem.SubjectType
                    };

                    if (includeExportCommands)
                    {
                        newReplayJob.AssetGroupIdsForExportCommands = workItem.AssetGroupIds.ToArray();
                    }
                    await this.replayJobRepo.InsertAsync(newReplayJob);
                }
                else
                {
                    // if the replay job already exists, check if it contains the asset group ids in this work item
                    // if yes, no need to update.
                    // if not, update the existing replay job with the new list of asset group ids.
                    var needUpdate = false;
                    List<AssetGroupId> assetGroupIds = AddAssetGroupsAsNeeded(workItem.AssetGroupIds, existingReplayJob.AssetGroupIds, ref needUpdate);

                    List<AssetGroupId> exportAssetGroupIds;
                    if (includeExportCommands)
                    {
                        exportAssetGroupIds = AddAssetGroupsAsNeeded(workItem.AssetGroupIds, existingReplayJob.AssetGroupIdsForExportCommands, ref needUpdate);
                    }
                    else
                    {
                        exportAssetGroupIds = existingReplayJob.AssetGroupIdsForExportCommands.ToList();
                    }

                    // Second, check the subject type
                    if (string.Compare(existingReplayJob.SubjectType, workItem.SubjectType, StringComparison.OrdinalIgnoreCase) != 0)
                    {
                        needUpdate = true;
                    }

                    if (needUpdate)
                    {
                        existingReplayJob.SubjectType = workItem.SubjectType;
                        existingReplayJob.AssetGroupIds = assetGroupIds.ToArray();
                        existingReplayJob.AssetGroupIdsForExportCommands = exportAssetGroupIds.ToArray();
                        
                        await this.replayJobRepo.ReplaceAsync(existingReplayJob, existingReplayJob.ETag);
                    }
                }

                processDate = processDate.AddDays(1);
            }

            return QueueProcessResult.Success();
        }

        private static List<AssetGroupId> AddAssetGroupsAsNeeded(IEnumerable<AssetGroupId> assetGroupIds, AssetGroupId[] existingAssetGroupIds, ref bool needUpdate)
        {
            List<AssetGroupId> agIds = existingAssetGroupIds.ToList();
            foreach (var gid in assetGroupIds)
            {
                if (!agIds.Contains<AssetGroupId>(gid))
                {
                    needUpdate = true;
                    agIds.Add(gid);
                }
            }

            return agIds;
        }
    }
}