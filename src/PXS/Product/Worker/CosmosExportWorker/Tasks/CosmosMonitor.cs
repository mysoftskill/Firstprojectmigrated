// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Tasks
{
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.Common.Cosmos;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.CosmosHelpers;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem.Cosmos;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Data;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility;
    using Microsoft.PrivacyServices.Common.Azure;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    ///     task to continuously monitor a file system for completed manifests and enqueue them to
    ///      the file set processor queue
    /// </summary>
    public class CosmosMonitor : TrackCountersBaseTask<ICosmosMonitorConfig>
    {
        private static readonly Task<TimeSpan?> PeriodicCounterUpdateFreq = Task.FromResult<TimeSpan?>(TimeSpan.FromSeconds(30));

        private const string PlaceholderFileName = "readme.txt";

        private readonly (string Tag, string ExportRoot, string ExportRootAdls, TaskCounters Counters)[] taskData;
        private readonly ITable<ManifestFileSetState> manifestFileState;
        private readonly IQueue<ManifestFileSet> fileSetQueue;
        private readonly IFileSystemManager fileSystemManager;
        private readonly TimeSpan manifestEnqueueInterval;
        private readonly TimeSpan maxEnqeueAge;
        private readonly TimeSpan minBatchAge;
        private readonly TimeSpan deleteAge;
        private readonly IClock clock;
        private readonly IAppConfiguration appConfig;

        /// <summary>
        ///     Initializes a new cosmosTag of the CosmosMonitor class
        /// </summary>
        /// <param name="manifestFileState">manifest file set state table</param>
        /// <param name="fileSetQueue">file set queue</param>
        /// <param name="config">task config</param>
        /// <param name="counterFactory">perf counter factory</param>
        /// <param name="fileSystemManager">file system manager</param>
        /// <param name="logger">trace logger</param>
        /// <param name="clock">time clock</param>
        /// <param name="appConfig">App config to read config/feature status.</param>
        public CosmosMonitor(
            ITable<ManifestFileSetState> manifestFileState,
            IQueue<ManifestFileSet> fileSetQueue,
            ICosmosMonitorConfig config,
            IFileSystemManager fileSystemManager,
            ICounterFactory counterFactory,
            ILogger logger,
            IClock clock,
            IAppConfiguration appConfig) :
            base(config, counterFactory, logger)
        {
            ArgumentCheck.ThrowIfNull(config, nameof(config));

            this.fileSystemManager = fileSystemManager ?? throw new ArgumentNullException(nameof(fileSystemManager));
            this.manifestFileState = manifestFileState ?? throw new ArgumentNullException(nameof(manifestFileState));
            this.fileSetQueue = fileSetQueue ?? throw new ArgumentNullException(nameof(fileSetQueue));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));

            this.manifestEnqueueInterval = TimeSpan.FromMinutes(config.MinimumManifestEnqueueIntervalMinutes);
            this.maxEnqeueAge = TimeSpan.FromHours(this.Config.MaxEnqueueAgeHours);
            this.minBatchAge = TimeSpan.FromMinutes(this.Config.MinBatchAgeMinutes);
            this.deleteAge = TimeSpan.FromHours(this.Config.DeleteAgeHours);
            this.appConfig = appConfig;

            this.taskData = new (string Tag, string BasePath, string BasePathAdls, TaskCounters Counters)[this.Config.CosmosVcs.Count];
        }

        /// <summary>
        ///     Starts the global background tasks
        /// </summary>
        /// <remarks>
        ///     this method is called before any instances of the primary task have started
        ///     this method should not spawn any background tasks- the RegisterGlobalBackgroundTasks() callback must be used for that
        /// </remarks>
        protected override void SetupGlobalState()
        {
            string suffix =
                Utility.EnsureNoLeadingSlash(this.fileSystemManager.CosmosPathsAndExpiryTimes.BasePath) +
                Utility.EnsureNoLeadingSlash(this.fileSystemManager.CosmosPathsAndExpiryTimes.AgentOutput);

            for (int i = 0; i < this.Config.CosmosVcs.Count; ++i)
            {
                this.taskData[i].ExportRoot = Utility.EnsureTrailingSlash(this.Config.CosmosVcs[i].CosmosVcPath) + suffix;
                this.taskData[i].ExportRootAdls = suffix;
                this.taskData[i].Tag = this.Config.CosmosVcs[i].CosmosTag;
                this.taskData[i].Counters = new TaskCounters();
            }
        }

        /// <summary>
        ///     Starts the global background tasks
        /// </summary>
        /// <remarks>
        ///     this method is called after all instances of the primary task have started
        ///     this method should not setup global state- the SetupGlobalState() callback must be used for that
        /// </remarks>
        protected override void RegisterGlobalBackgroundTasks()
        {
            const string TagAndId = "FileProcessor.UpdatePeriodicCounters";
            this.SpawnGlobalTask(this.UpdatePeriodicCounters, TagAndId, TagAndId);
        }

        /// <summary>
        ///     Updates a set of counters periodically
        /// </summary>
        /// <param name="ctx">work item context</param>
        /// <returns>a task whose result is the amount of time to wait before running the method again</returns>
        private Task<TimeSpan?> UpdatePeriodicCounters(OperationContextBasic ctx)
        {
            TaskCounterData data = new TaskCounterData();

            for (int i = 0; i < this.taskData.Length; ++i)
            {
                this.taskData[i].Counters.CombineDataInto(data);
            }

            data.EmitCounters(this.GetCounter, null);

            return CosmosMonitor.PeriodicCounterUpdateFreq;
        }

        /// <summary>
        ///     Runs the task
        /// </summary>
        /// <param name="ctx">operation context</param>
        /// <returns>resulting value</returns>
        protected override async Task<TimeSpan?> RunOnceAsync(OperationContext ctx)
        {

            string cosmosPath = this.taskData[ctx.WorkerIndex].ExportRoot;
            this.TraceInfo("Starting pass to look for agent export batches in " + cosmosPath);
            
            try
            {
                DateTimeOffset start = this.clock.UtcNow;
                ICosmosFileSystem fileSystem;
                IDirectory root;
                TimeSpan waitTime;

                ctx.Item = cosmosPath;
                ctx.Op = "fetching file system";

                fileSystem = this.fileSystemManager.GetFileSystem(this.taskData[ctx.WorkerIndex].Tag);
                
                ICosmosClient client = fileSystem.Client;

                // let test pass through temporarily, will be removed when 
                // vcclient code is cleaned up.
                if (client != null)
                {
                    cosmosPath = client.ClientTechInUse() == ClientTech.Adls ? this.taskData[ctx.WorkerIndex].ExportRootAdls : cosmosPath;
                }

                ctx.Op = "fetching root directory";

                root = await fileSystem.OpenExistingDirectoryAsync(cosmosPath).ConfigureAwait(false);
                if (root == null)
                {
                    this.TraceWarning("Failed to open " + cosmosPath + " while looking for new files in Cosmos");
                    return TimeSpan.FromMinutes(this.Config.RepeatDelayMinutes);
                }

                await this.ProcessRootDirectoryAsync(ctx, root).ConfigureAwait(false);

                waitTime = TimeSpan.FromMinutes(this.Config.RepeatDelayMinutes) - (this.clock.UtcNow - start);
                return waitTime > TimeSpan.Zero ? waitTime : TimeSpan.Zero;
            }
            finally
            {
                this.TraceInfo("Pass to look for agent export batches in " + cosmosPath + " has completed");
            }
        }

        /// <summary>
        ///     Processes the root directory
        /// </summary>
        /// <param name="ctx">operation context</param>
        /// <param name="root">root directory</param>
        /// <returns>resulting value</returns>
        private async Task ProcessRootDirectoryAsync(
            OperationContext ctx,
            IDirectory root)
        {
            ICollection<IFileSystemObject> fileSystemObjects;
            TaskCounters counters = this.taskData[ctx.WorkerIndex].Counters;
            string tag = this.taskData[ctx.WorkerIndex].Tag;

            ctx.Op = $"Enumerating {tag} root directory {root.Path}";

            try
            {
                fileSystemObjects = await root.EnumerateAsync().ConfigureAwait(false);
            }
            catch (DirectoryNotFoundException)
            {
                this.TraceInfo("Cosmos export root path " + root.Path + " does not exist");
                return;
            }

            foreach (IFileSystemObject fso in fileSystemObjects)
            {
                this.CancelToken.ThrowIfCancellationRequested();

                if (fso.Type == FileSystemObjectType.Directory)
                {
                    this.TraceInfo($"Processing agent dir {fso.Path}");
                    await this.ProcessAgentDirectoryAsync(ctx, (IDirectory)fso).ConfigureAwait(false);
                }
                else if (CosmosMonitor.PlaceholderFileName.EqualsIgnoreCase(fso.Name) == false)
                {
                    this.TraceVerbose("Found unexpected file in root of export tree: " + fso.Path);
                }
            }

            counters.CommitChanges().EmitCounters(this.GetCounter, tag);
        }

        /// <summary>
        ///     Processes the agent directory
        /// </summary>
        /// <param name="ctx">operation context</param>
        /// <param name="agentDir">agent directory</param>
        /// <returns>resulting value</returns>
        private async Task ProcessAgentDirectoryAsync(
            OperationContext ctx,
            IDirectory agentDir)
        {
            (string type, string suffix) SplitName(string name)
            {
                int index = name.IndexOf('_');
                return index > 0 ? (name.Substring(0, index), name.Substring(index)) : (name, string.Empty);
            }

            const string DataFileManifestCommonIncorrectPrefix = "DataManifest";

            ICollection<IFileSystemObject> fileSystemObjects;
            IDictionary<string, IFile> foundManifests = new Dictionary<string, IFile>(StringComparer.OrdinalIgnoreCase);
            TaskCounters counters = this.taskData[ctx.WorkerIndex].Counters;
            string agentId = agentDir.Name;
            string tag = this.taskData[ctx.WorkerIndex].Tag;

            // Disabling enqueue operation temporarily for the agent.
            // We are seeing multiple entries for the same Data/Request Manifest files which is causing the delay for new files to be processed in the dir.
            // This is a temporary mitigation to fasten the Deque operation for the agent.
            if (await appConfig.IsFeatureFlagEnabledAsync(FeatureNames.PXS.DisableEnqueueExportFilesForAgent, CustomOperatorContextFactory.CreateDefaultStringComparisonContext(agentId), true))
            {
                this.TraceInfo($"FeatureFlag:DisableEnqueueExportFilesForAgent, Skipping enqueue operation for agentId: {agentId}");
                return;
            }

            ctx.Op = $"Enumerating {tag} agent directory {agentDir.Path}";

            this.TraceInfo(ctx.Op);

            try
            {
                fileSystemObjects = await agentDir.EnumerateAsync().ConfigureAwait(false);
            }
            catch (DirectoryNotFoundException)
            {
                this.TraceInfo($"Cosmos export agent path {agentDir.Path} does not exist");
                return;
            }

            foreach (IFileSystemObject fso in fileSystemObjects)
            {
                this.CancelToken.ThrowIfCancellationRequested();

                if (fso.Type == FileSystemObjectType.File)
                {
                    (string name, string suffix) = SplitName(fso.Name);
                    ManifestFileSetState currentState;
                    ManifestFileSetState request;
                    DateTimeOffset now = this.clock.UtcNow;
                    TimeSpan age;
                    string search;
                    string fileTag;
                    IFile requestManifest = null;
                    IFile dataManifest = null;
                    IFile otherManifest;
                    IFile file = (IFile)fso;
                    bool isDataManifest;

                    age = now - file.Created;

                    if (age > this.maxEnqeueAge)
                    {
                        fileTag = Utility.GenerateFileTag(tag, agentId, file.Name);

                        if (age < this.deleteAge)
                        {
                            this.TraceInfo(
                                "File [{0}]'s age of {1} is older than {2} and will not be processed further",
                                fileTag,
                                age,
                                this.maxEnqeueAge);

                            counters.AddPreDeleted();
                        }
                        else
                        {
                            this.TraceWarning(
                                "File [{0}]'s age of {1} has exceeded the delete age of {2} and will be deleted",
                                fileTag,
                                age,
                                this.deleteAge);

                            // we'll try this again in the next pass if it fails, so just trace an error and continue (we don't
                            //  want to get stuck trying to delete the file endlessly)
                            try
                            {
                                const string Fmt =
                                    "File [{0}] (created on {1}) has an age of {2} which exceeds the max allowed age of {3}. " + 
                                    "It has been deleted.";

                                await file.DeleteAsync();

                                this.TraceWarning(
                                    Fmt,
                                    file.Path,
                                    file.Created.ToString("yyyy-MM-dd HH:mm:ss"),
                                    age.ToString(@"d\.hh\:mm\:ss"),
                                    this.deleteAge.ToString(@"d\.hh\:mm\:ss"));

                                counters.AddDeleted();
                            }
                            catch (IOException e)
                            {
                                this.TraceError("Failed to delete overaged file " + fileTag + ": " + e.ToString());
                            }
                        }

                        continue;
                    }

                    if (Constants.DataFileManifestNamePrefix.EqualsIgnoreCase(name))
                    {
                        if (string.IsNullOrWhiteSpace(suffix))
                        {
                            fileTag = Utility.GenerateFileTag(tag, agentId, file.Name);

                            this.TraceWarning("No time suffix found for data manifest file [" + fileTag + "]. Ignoring.");
                            continue;
                        }

                        dataManifest = file;
                        search = Constants.RequestManifestNamePrefix + suffix;
                        isDataManifest = true;
                        this.TraceInfo($"Found data manifest at path {file.Path}");
                    }
                    else if (Constants.RequestManifestNamePrefix.EqualsIgnoreCase(name))
                    {
                        if (string.IsNullOrWhiteSpace(suffix))
                        {
                            fileTag = Utility.GenerateFileTag(tag, agentId, file.Name);

                            this.TraceWarning("No time suffix found for request manifest file [" + fileTag + "]. Ignoring.");
                            continue;
                        }

                        requestManifest = file;
                        search = Constants.DataFileManifestNamePrefix + suffix;
                        isDataManifest = false;
                        this.TraceInfo($"Found Request manifest at path {file.Path}");
                    }
                    else
                    {
                        if (name != null)
                        {
                            if (name.StartsWithIgnoreCase(Constants.DataFileManifestNamePrefix) ||
                                name.StartsWithIgnoreCase(Constants.RequestManifestNamePrefix))
                            {
                                fileTag = Utility.GenerateFileTag(tag, agentId, file.Name);

                                this.TraceWarning("Data file [" + fileTag + "] found starting with reserved names.");
                            }
                            else if (name.StartsWithIgnoreCase(DataFileManifestCommonIncorrectPrefix) == false)
                            {
                                counters.AddNonManifest(this.fileSystemManager.FileSizeThresholds, age, file.Size);
                            }
                        }

                        this.TraceInfo("Moving to next file, did not find data or request file.");

                        // this should be a data file, so just move onto the next item
                        continue;
                    }

                    // we only want to process when both the request manifest and data file manifest are written, so keep track of 
                    //  both and only enqueue processing work when we find both.  This prevents us from processing batches that 
                    //  agents are still in the process of finishing up (i.e. written one manifest but not the other yet)
                    if (foundManifests.TryGetValue(search, out otherManifest) == false)
                    {
                        this.TraceInfo("Found one file, adding it to the queue, searching for another");
                        // we didn't find the other manifest file, so just add this one to the dictionary keeping track of 
                        foundManifests.Add(file.Name, file);
                        continue;
                    }

                    this.TraceInfo("We have a pair of data & request manifest we can process");

                    age = Comparer.Min(age, now - otherManifest.Created);

                    counters.AddPendingManifest(age);

                    requestManifest = isDataManifest == false ? requestManifest : otherManifest;
                    dataManifest = isDataManifest ? dataManifest : otherManifest;

                    fileTag = Utility.GenerateFileTag(tag, agentId, dataManifest.Name);

                    if (age < this.minBatchAge)
                    {
                        const string Fmt =
                            "Skipping enqueue of data manifest {0}. It's age is {1} and the minimum batch age before processing " +
                            "can start is {2}";

                        this.TraceInfo(Fmt, fileTag, age, this.minBatchAge);
                        continue;
                    }

                    ctx.Op = "Querying to see if manifest " + fileTag + " has been recently processed";

                    request = new ManifestFileSetState
                    {
                        AgentId = agentId,
                        ManifestPath = dataManifest.Path,

                        RequestManifestPath = requestManifest.Path,

                        DataFileManifestCreateTime = dataManifest.Created,
                        RequestManifestCreateTime = requestManifest.Created,

                        // processing has not happened yet, must set to -1
                        Counter = -1,
                    };

                    currentState = await this.manifestFileState
                        .GetItemAsync(request.PartitionKey, request.RowKey)
                        .ConfigureAwait(false);
                    this.TraceInfo($"Current state: {currentState}");

                    if (currentState != null && currentState.Timestamp.Add(this.manifestEnqueueInterval) > now)
                    {
                        const string Fmt =
                            "Skipping enqueue of data manifest {0}. It was processed {1} ago and the minimum time between " +
                            "enqueues is {2}";

                        TimeSpan since = now - currentState.Timestamp;
                        this.TraceInfo(Fmt, fileTag, since, this.manifestEnqueueInterval);
                        continue;
                    }

                    ctx.Op = "Enqueueing manifest " + Utility.GenerateFileTag(tag, agentId, dataManifest.Name);
                    this.TraceInfo(ctx.Op);

                    await this.fileSetQueue
                        .EnqueueAsync(new ManifestFileSet(agentId, tag, requestManifest.Path, dataManifest.Path), this.CancelToken)
                        .ConfigureAwait(false);

                    // Add to the ManifestFileState table with counter = -1, to avoid multiple entries in 
                    if (currentState == null && await this.appConfig.IsFeatureFlagEnabledAsync(FeatureNames.PXS.PersistStateAfterAppendToManifestFileSetQueue))
                    {
                        // Add to the manifestFileSetState.
                        await this.manifestFileState.InsertAsync(request).ConfigureAwait(false);

                        this.TraceInfo("No existing manifestfilestate record was found, creating one before append");
                    }
                }
                else
                {
                    this.TraceWarning("Found unexpected directory in agent export directory: " + fso.Path);
                }
            }

            this.TraceInfo("Terminating processing of directory for agent " + agentId);
        }

        /// <summary>
        ///     Becuase we have many machines enumerating the same files over a period of time, allowing the counters to 
        ///      aggregate the data doesn't work that great. 
        ///     What we want are things like "of all the pending files, this is the average", but that doesn't necessarily
        ///      work if the counters are aggregating at time based intervals instead of "per file system enumeration" based
        ///      intervals. So we collect all the stats for the full enumeration and emit them at once
        /// </summary>
        private class TaskCounters
        {
            private TaskCounterData working = new TaskCounterData();
            private TaskCounterData commit = new TaskCounterData();

            public TaskCounterData CommitChanges()
            {
                TaskCounterData temp = this.working;
                this.working = new TaskCounterData();
                this.commit = temp;

                return temp;
            }

            public void AddPendingManifest(TimeSpan newAge)
            {
                this.working.AddPendingManifest(newAge);
            }

            public void AddDeleted()
            {
                this.working.AddDeleted();
            }

            public void AddPreDeleted()
            {
                this.working.AddPreDeleted();
            }

            public void AddNonManifest(
                ICosmosFileSizeThresholds thresholds,
                TimeSpan newAge,
                long newSize)
            {
                this.working.AddNonManifest(thresholds, newAge, newSize);
            }

            public void CombineDataInto(TaskCounterData data)
            {
                this.commit.CombineDataInto(data);
            }
        }

        /// <summary>
        ///     instance of collection of counter date
        /// </summary>
        private class TaskCounterData
        {
            private const string DataFileAvgSizeCounter = "Average Data File Size";
            private const string DataFileMaxSizeCounter = "Max Data File Size";

            private const string DataFileAvgAgeCounter = "Average Data File Age (minutes)";
            private const string DataFileMaxAgeCounter = "Max Data File Age (minutes)";
            
            private const string BatchAvgAgeCounter = "Average Batch Age (minutes)";
            private const string BatchMaxAgeCounter = "Max Batch Age (minutes)";

            private const string DataFileCountCounter = "Data File Counts";
            private const string FilePrePurgeCounter = "Files Close to Deletion";
            private const string FilePendingCounter = "Batches Pending or In Progress";
            private const string FilePurgedCounter = "Files Deleted Due to Age";

            private readonly SortedList<FileSizePartition, FilePartitionData> sizeAndCounts =
                new SortedList<FileSizePartition, FilePartitionData>();

            private long ageManifestsMax;
            private long ageManifests;
            private int countManifests;

            private int countPreDelete;
            private int countDeleted;

            public void EmitCounters(
                Func<string, ICounter> counterCreator,
                string cosmosTag)
            {
                void SetCounter(
                    string name,
                    long value,
                    FileSizePartition partition = FileSizePartition.Invalid)
                {
                    ICounter counter = counterCreator(name);
                    ulong valueActual = Convert.ToUInt64(value);

                    if (string.IsNullOrWhiteSpace(cosmosTag) == false)
                    {
                        counter.SetValue(valueActual, cosmosTag);
                    }
                    else if (partition != FileSizePartition.Invalid)
                    {
                        counter.SetValue(valueActual, partition.ToString());
                    }
                    else
                    {
                        counter.SetValue(valueActual);
                    }
                }

                SetCounter(TaskCounterData.FilePrePurgeCounter, this.countPreDelete);
                SetCounter(TaskCounterData.FilePurgedCounter, this.countDeleted);

                SetCounter(TaskCounterData.FilePendingCounter, this.countManifests);

                SetCounter(TaskCounterData.BatchMaxAgeCounter, this.ageManifestsMax);
                SetCounter(TaskCounterData.BatchAvgAgeCounter, this.GetAvg(this.ageManifests, this.countManifests));

                if (this.sizeAndCounts.Values.Count > 0)
                {
                    IList<FilePartitionData> partitions = this.sizeAndCounts.Values;
                    long total = partitions.Sum(o => o.Count);

                    SetCounter(TaskCounterData.DataFileCountCounter, total);

                    SetCounter(TaskCounterData.DataFileAvgSizeCounter, this.GetAvg(partitions.Sum(o => o.Size), total));
                    SetCounter(TaskCounterData.DataFileAvgAgeCounter, this.GetAvg(partitions.Sum(o => o.Age), total));

                    // if we're outputting global counters, then also output per file size partition counters- we can't emit
                    //  per-size partition and per-cosmos instance couters because AP counters only support one pivot 
                    if (string.IsNullOrWhiteSpace(cosmosTag))
                    {
                        IEnumerable<KeyValuePair<FileSizePartition, FilePartitionData>> enumer =
                            this.sizeAndCounts.Where(o => o.Value.Count > 0);

                        foreach (KeyValuePair<FileSizePartition, FilePartitionData> kvp in enumer)
                        {
                            FilePartitionData data = kvp.Value;

                            SetCounter(TaskCounterData.DataFileMaxSizeCounter, data.MaxSize, kvp.Key);
                            SetCounter(TaskCounterData.DataFileMaxAgeCounter, data.MaxAge, kvp.Key);

                            SetCounter(TaskCounterData.DataFileCountCounter, data.Count, kvp.Key);

                            SetCounter(TaskCounterData.DataFileAvgSizeCounter, this.GetAvg(data.Size, data.Count), kvp.Key);
                            SetCounter(TaskCounterData.DataFileAvgAgeCounter, this.GetAvg(data.Age, data.Count), kvp.Key);
                        }
                    }
                }
            }

            public void AddPendingManifest(TimeSpan newAge)
            {
                long ageMinutes = Convert.ToInt64(newAge.TotalMinutes);

                this.countManifests += 1;

                this.ageManifestsMax = Math.Max(this.ageManifestsMax, ageMinutes);
                this.ageManifests += ageMinutes;
            }

            public void AddPreDeleted()
            {
                this.countPreDelete += 1;
            }

            public void AddDeleted()
            {
                this.countDeleted += 1;
            }

            public void AddNonManifest(
                ICosmosFileSizeThresholds thresholds,
                TimeSpan newAge,
                long newSize)
            {
                FileSizePartition partition = Utility.GetPartition(thresholds, newSize);
                FilePartitionData item;
                long ageMinutes = Convert.ToInt64(newAge.TotalMinutes);
                long size = Math.Max(0, newSize);

                if (this.sizeAndCounts.TryGetValue(partition, out item) == false)
                {
                    this.sizeAndCounts[partition] = item = new FilePartitionData();
                }

                item.MaxSize = Math.Max(item.MaxSize, newSize);
                item.MaxAge = Math.Max(item.MaxAge, ageMinutes);

                item.Count += 1;
                item.Size += size;
                item.Age += ageMinutes;
            }

            public void CombineDataInto(TaskCounterData data)
            {
                data.ageManifestsMax = Math.Max(data.ageManifestsMax, this.ageManifestsMax);
                data.ageManifests += this.ageManifests;

                data.countManifests += this.countManifests;

                data.countPreDelete += this.countPreDelete;
                data.countDeleted += this.countDeleted;

                foreach (KeyValuePair<FileSizePartition, FilePartitionData> v in this.sizeAndCounts)
                {
                    FilePartitionData item;
                    if (data.sizeAndCounts.TryGetValue(v.Key, out item) == false)
                    {
                        data.sizeAndCounts[v.Key] = item = new FilePartitionData();
                    }

                    item.MaxSize = Math.Max(item.MaxSize, v.Value.Size);
                    item.MaxAge = Math.Max(item.MaxAge, v.Value.Age);

                    item.Count += v.Value.Count;
                    item.Size += v.Value.Size;
                    item.Age += v.Value.Age;
                }
            }

            private long GetAvg(
                long sum,
                long count)
            {
                return count > 0 ? sum / count : 0;
            }

            /// <summary>
            ///     counter data for a single file partition
            /// </summary>
            private class FilePartitionData
            {
                public long MaxSize { get; set; }
                public long MaxAge { get; set; }
                public long Size { get; set; }
                public long Age { get; set; }
                public int Count { get; set; }
            }
        }
    }
}
