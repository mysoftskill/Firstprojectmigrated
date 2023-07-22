// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Data;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Microsoft.PrivacyServices.Identity;
    using Microsoft.Azure.Storage;

    using Newtonsoft.Json;

    using OperationContext = Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.OperationContext;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.Cloud.InstrumentationFramework;
    using Constants = Utility.Constants;

    /// <summary>
    ///     Cosmos export package processor data agent
    /// </summary>
    public class CosmosDataAgent : IPrivacyDataAgent
    {
        private const string CommandsReceived = "Commands Received";
        private const string AgentErrors = "Agent Errors";

        private static readonly TimeSpan PendingTraceWaitDuration = TimeSpan.FromHours(6);
        private static readonly TimeSpan DefaultLeaseExtension = TimeSpan.FromMinutes(60);

        private readonly ITable<CommandFileState> commandFileState;
        private readonly ITable<CommandState> commandState;
        private readonly ICommandMonitorConfig config;
        private readonly ICounterFactory counterFactory;
        private readonly IList<TimeSpan> leaseExtensionSet;
        private readonly ICommandClient client;
        private readonly ILogger logger;
        private readonly IClock clock;
        private readonly string taskId;

        private readonly string component = nameof(CosmosDataAgent);

        /// <summary>
        ///     Initializes a new instance of the CosmosDataAgent class
        /// </summary>
        /// <param name="config">agent config</param>
        /// <param name="commandFileState">command file state accessor</param>
        /// <param name="commandState">command state accessor</param>
        /// <param name="counterFactory">counter factory</param>
        /// <param name="commandClient">command client</param>
        /// <param name="logger">Geneva trace logger</param>
        /// <param name="clock">time of day clock</param>
        /// <param name="taskId">task taskId</param>
        public CosmosDataAgent(
            ICommandMonitorConfig config,
            ITable<CommandFileState> commandFileState,
            ITable<CommandState> commandState,
            ICounterFactory counterFactory,
            ICommandClient commandClient,
            ILogger logger,
            IClock clock,
            string taskId)
        {
            this.commandFileState = commandFileState ?? throw new ArgumentNullException(nameof(commandFileState));
            this.counterFactory = counterFactory ?? throw new ArgumentNullException(nameof(counterFactory));
            this.commandState = commandState ?? throw new ArgumentNullException(nameof(commandState));
            this.client = commandClient ?? throw new ArgumentNullException(nameof(commandClient));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.taskId = ArgumentCheck.ReturnIfNotNullEmptyOrWhiteSpace(taskId, nameof(taskId));

            this.component += "." + taskId;

            this.leaseExtensionSet = CosmosDataAgent.BuildFullLeaseExtensionSet(this.config.LeaseExtensionMinuteSet);
        }

        /// <summary>
        ///     Processes a command to delete a given type of data between a range of dates
        /// </summary>
        /// <param name="command">command to process</param>
        /// <returns>task for completion</returns>
        public async Task ProcessDeleteAsync(IDeleteCommand command)
        {
            ArgumentCheck.ThrowIfNull(command, nameof(command));

            await this.client
                .CheckpointAsync(command, CommandStatus.UnexpectedCommand, null, 0, null)
                .ConfigureAwait(false);
        }

        /// <summary>
        ///     Processes a command that exports the given data types to a location in Azure Blob storage
        /// </summary>
        /// <param name="command">command to process</param>
        /// <returns>task for completion</returns>
        public async Task ProcessExportAsync(IExportCommand command)
        {
            OperationContext ctx;

            ArgumentCheck.ThrowIfNull(command, nameof(command));

            ctx = new OperationContext(this.taskId, 0)
            {
                Item = "[command: " + command.CommandId + "]",
                Op = "Initializing"
            };

            try
            {
                await this.ProcessExportInternalAsync(ctx, command).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                ICounter errors = this.counterFactory.GetCounter(
                    Constants.CounterCategory, CosmosDataAgent.AgentErrors, CounterType.Number);

                errors.Increment();

                this.logger.Error(this.component, $"Failed processing command {ctx.Item} while {ctx.Op}: {e})");

                // PCF wants us to catch everything, report fail, and then return
                await this.client
                    .CheckpointAsync(command, CommandStatus.Failed, this.GetLeaseExtensionMinutes(0), 0, null)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        ///     Processes a command that indicates the account of the command's subject is irrevocably closed
        /// </summary>
        /// <param name="command">command to process</param>
        /// <returns>task for completion</returns>
        public async Task ProcessAccountClosedAsync(IAccountCloseCommand command)
        {
            ArgumentCheck.ThrowIfNull(command, nameof(command));

            await this.client
                .CheckpointAsync(command, CommandStatus.UnexpectedCommand, null, 0, null)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task ProcessAgeOutAsync(IAgeOutCommand command)
        {
            ArgumentCheck.ThrowIfNull(command, nameof(command));

            return this.client
                .CheckpointAsync(command, CommandStatus.UnexpectedCommand, null, 0, null);
        }

        /// <summary>
        ///      Builds the full lease extension set from the config set
        /// </summary>
        /// <param name="leaseExtensionItems">config lease extension items</param>
        /// <returns>resulting list</returns>
        private static IList<TimeSpan> BuildFullLeaseExtensionSet(ICollection<string> leaseExtensionItems)
        {
            List<TimeSpan> result = new List<TimeSpan>();

            if (leaseExtensionItems != null)
            {
                foreach (string entry in leaseExtensionItems)
                {
                    string[] parts = entry.Split(new[] { '*' }, StringSplitOptions.RemoveEmptyEntries);
                    int value;

                    if (parts.Length > 0 && int.TryParse(parts[0], out value) && value > 0)
                    {
                        TimeSpan fullVal = TimeSpan.FromMinutes(value);
                        int repeat;

                        if (parts.Length == 1 || int.TryParse(parts[1], out repeat) == false)
                        {
                            repeat = 1;
                        }

                        for (int i = 0; i < repeat; ++i)
                        {
                            result.Add(fullVal);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        ///     determines whether the the command is a test command or not
        /// </summary>
        /// <param name="command">command to test</param>
        /// <param name="agentId">agent id the command is assigned to</param>
        /// <returns>true if it is a test command, false if it is not a test command, null if no value could be determined</returns>
        private async Task<bool?> IsTestCommandAsync(
            IPrivacyCommand command,
            string agentId)
        {
            if (this.config.SyntheticCommandAgents != null &&
                this.config.SyntheticCommandAgents.Contains(agentId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return await this.client.IsTestCommandAsync(command.CommandId).ConfigureAwait(false);
        }

        /// <summary>
        ///     Actual processing logic for export
        /// </summary>
        /// <param name="ctx">operation context</param>
        /// <param name="command">command to process</param>
        /// <returns>resulting value</returns>
        private async Task ProcessExportInternalAsync(
            OperationContext ctx,
            IExportCommand command)
        {
            IEnumerable<ExportedFileSizeDetails> exportedFileSizeDetails = null;
            ICollection<CommandFileState> commandFileStateRows = null;
            CosmosExportAgentState agentState;
            ICollection<string> nonTransientErrs = null;
            CommandStatus newStatus = CommandStatus.Pending;
            InsertOrFetch insertFetch;
            CommandState state;
            TimeSpan? leaseExtension;
            ICounter commandCounter;
            string commandId;
            string agentId;
            int rowCount = 0;

            ArgumentCheck.ThrowIfNull(command, nameof(command));

            commandCounter = this.counterFactory.GetCounter(
                Constants.CounterCategory, CosmosDataAgent.CommandsReceived, CounterType.Number);

            commandId = Utility.CanonicalizeCommandId(command.CommandId);
            agentId = this.ExtractRealAgentId(command.AssetGroupQualifier) ?? this.config.AgentId;

            ctx.Item = $"[command: {commandId}][agent: {agentId}]";

            // extract or create new agent state, update it with new dequeue count, and update the command object
            if (string.IsNullOrWhiteSpace(command.AgentState))
            {
                agentState = new CosmosExportAgentState(await this.IsTestCommandAsync(command, agentId), 1);
            }
            else
            {
                try
                {
                    agentState = JsonConvert.DeserializeObject<CosmosExportAgentState>(command.AgentState);

                    if (agentState.Version == 0 ||
                        (agentState.Version == 1 && agentState.IsTestCommand == false) ||
                        (agentState.Version >= 2 && 
                         agentState.Version <= 3 && 
                         agentState.HasTestCommandBeenRetrieved == false))
                    {
                        bool? isTest = await this.IsTestCommandAsync(command, agentId);

                        agentState.HasTestCommandBeenRetrieved = isTest.HasValue;
                        agentState.IsTestCommand = isTest ?? false;

                        agentState.Version = CosmosExportAgentState.CurrentVersion;
                    }
                }
                catch (JsonSerializationException)
                {
                    // we have to have dequeued at least once before to have any agent state
                    agentState = new CosmosExportAgentState(await this.IsTestCommandAsync(command, agentId), 1);
                }

                agentState.DequeueCount++;
            }

            // if we have agent state, then try fetching first as we should have inserted when we created the agent state (though
            //  we will try to insert if the fetch fails- this is just an optimization to minimize the number of calls to storage)
            (insertFetch, state) = await CommandStateUtility
                .InsertOrFetchCommandStateAsync(
                    ctx, 
                    this.commandState, 
                    commandId, 
                    agentId, 
                    command.LeaseReceipt, 
                    agentState.IsTestCommand,
                    agentState.IsTestCommand,
                    false,
                    string.IsNullOrWhiteSpace(command.AgentState))
                .ConfigureAwait(false);

            leaseExtension = this.GetLeaseExtensionMinutes(agentState.DequeueCount);

            commandCounter.Increment();

            if (insertFetch != InsertOrFetch.Error)
            {
                if (agentState.IsTestCommand == false && state.IsComplete)
                {
                    ctx.Op = "fetching row count & byte count and errors for (agent, command)";

                    commandFileStateRows = await this.GetRowsAsync(agentId, commandId).ConfigureAwait(false);

                    exportedFileSizeDetails = 
                        commandFileStateRows?.Select(o => new ExportedFileSizeDetails(o.FilePath, o.ByteCount, false, o.ByteCount));

                    nonTransientErrs = commandFileStateRows?
                        .Where(o => string.IsNullOrWhiteSpace(o.NonTransientErrorInfo) == false)
                        .Select(o => o.NonTransientErrorInfo)
                        .ToList();

                    if (nonTransientErrs?.Count == 0)
                    {
                        nonTransientErrs = null;
                    }

                    rowCount = commandFileStateRows?.Sum(o => o.RowCount) ?? 0;
                }

                if (state.IsComplete)
                {
                    leaseExtension = null;
                    newStatus = CommandStatus.Complete;
                }

                if (agentState.IsTestCommand)
                {
                    const string Fmt =
                        "CommandFeed delivered new test command {0} for test agent {1}. Added dummy row to state management table " +
                        "and marking complete";

                    this.logger.Information(this.component, Fmt, commandId, agentId);

                    commandCounter.Increment("Synthetic");
                }
                else if (insertFetch == InsertOrFetch.Inserted)
                {
                    const string Fmt =
                        "CommandFeed delivered new command {0} for agent {1}. Added row to state management table and extending " +
                        "lease by {2}";

                    this.logger.Information(this.component, Fmt, commandId, agentId, leaseExtension ?? TimeSpan.Zero);

                    commandCounter.Increment("NewCommand");
                }
                else if (newStatus == CommandStatus.Pending)
                {
                    const string Fmt =
                        "CommandFeed delivered known command {0} xfor agent {1} that is not yet complete (state last " + 
                        "modified: {2}). Extending lease by {3}";

                    // it is very annoying to have to wade through a mountain of 'command pending' trace messages when
                    //  troubleshooting, but it would be good to have some of this information written every once in a while,
                    //  so occasionally emit an information level event but keep most traces to verbose to enable them to be
                    //  easily filtered
                    this.logger.Log(
                        agentState.LastPendingTraceEvent.Add(CosmosDataAgent.PendingTraceWaitDuration) < this.clock.UtcNow ?
                            IfxTracingLevel.Verbose :
                            IfxTracingLevel.Informational,
                        this.component,
                        Fmt,
                        commandId,
                        agentId,
                        state.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                        leaseExtension ?? TimeSpan.Zero);

                    agentState.LastPendingTraceEvent = this.clock.UtcNow;

                    commandCounter.Increment("Pending");
                }
                else if (newStatus == CommandStatus.Complete)
                {
                    if (nonTransientErrs == null || nonTransientErrs.Count == 0)
                    {
                        this.logger.Information(
                            this.component,
                            "CommandFeed delivered known command {0} for agent {1} that has completed. Marking command complete.",
                            commandId,
                            agentId);

                        commandCounter.Increment("Complete");
                    }
                    else
                    {
                        const string Fmt =
                            "CommandFeed delivered known command {0} for agent {1} that has completed with errors. Marking command " +
                            "complete with non-transient errors: {2}";

                        this.logger.Warning(this.component, Fmt, commandId, agentId, string.Join(",", nonTransientErrs));

                        commandCounter.Increment("CompleteWithErrors");
                    }
                }
            }
            else
            {
                const string Fmt =
                    "Failed to insert or fetch command state from table store for command {0} for agent {1}. Reporting transient " +
                    "error to command feed.";

                this.logger.Error(this.component, Fmt, this.component, commandId, agentId);

                newStatus = CommandStatus.Failed;

                commandCounter.Increment("TransientFailure");
            }

            ctx.Op = "updating CommandFeed";

            command.AgentState = JsonConvert.SerializeObject(agentState);

            await this.client
                .CheckpointAsync(command, newStatus, leaseExtension, rowCount, nonTransientErrs, exportedFileSizeDetails)
                .ConfigureAwait(false);

            // the above call updated the lease receipt, so we need to persist that change to the table store. It's not fatal if this
            //  fails as we'll just fetch the command from CommandFeed if we try to complete it early.
            // if we complete a command, don't care about updating the lease receipt 
            if (newStatus != CommandStatus.Complete)
            {
                bool savedState = false;

                for (int i = 0; i < 2 && savedState == false && state != null && state.IsComplete == false; ++i)
                {
                    // this really should never hit as the insert op update the provided entity with the ETag while the get operation
                    //  returns the existing ETag (which in neither case should be a '*')
                    if (state.ETag == "*")
                    {
                        this.logger.Error(
                            this.component,
                            $"Forcibly overwriting existing command for [agent {agentId}][cmd: {commandId}]");
                    }

                    state.LeaseReceipt = command.LeaseReceipt;
                    savedState = await this.commandState.ReplaceAsync(state);

                    if (savedState == false)
                    {
                        this.logger.Error(
                            this.component,
                            $"Failed to update lease receipt for [agent {agentId}][cmd: {commandId}] due to conflict. Refetching");

                        state = await this.commandState.GetItemAsync(state.PartitionKey, state.RowKey);
                    }
                }
            }

            // we can't delete the test command items or any cosmos job that happens to include them will never have its
            //  data pulled. Instead, we'll have the DataWriter delete the state object as soon as writing for the data 
            //  file is finished
            if (insertFetch != InsertOrFetch.Error && newStatus == CommandStatus.Complete && agentState.IsTestCommand == false)
            {
                ICollection<Task> waiters;

                ctx.Op = "removing command state and row counts from table storage";

                waiters = commandFileStateRows?.Count > 0 ? this.DeleteRowCounts(commandFileStateRows, commandId) : new List<Task>();

                if (state != null)
                {
                    waiters.Add(this.commandState.DeleteItemAsync(state));
                }

                await Task.WhenAll(waiters).ConfigureAwait(false);

                this.logger.Information(
                    this.component,
                    "CosmosExportAgent deleted state and row count data for completed command command {0} for agent {1}.",
                    commandId,
                    agentId);
            }
        }

        /// <summary>
        ///     Extracts the real agent taskId from the asset group qualifier
        /// </summary>
        /// <param name="assetGroupQualifier">asset group qualifier</param>
        /// <returns>resulting value</returns>
        private string ExtractRealAgentId(string assetGroupQualifier)
        {
            AssetQualifier qualifier = AssetQualifier.Parse(assetGroupQualifier);
            string orginalAgentId = null;

            return (qualifier.CustomProperties?.TryGetValue("OriginalAgentId", out orginalAgentId) ?? false) ?
                orginalAgentId :
                null;
        }

        /// <summary>
        ///     Gets the rows for all data files processed for the specified (command, agent) tuple
        /// </summary>
        /// <param name="agentId">agent id</param>
        /// <param name="commandId">command id</param>
        /// <returns>resulting value</returns>
        private Task<ICollection<CommandFileState>> GetRowsAsync(
            string agentId,
            string commandId)
        {
            return this.commandFileState.QueryAsync($"PartitionKey eq '{agentId}' and CommandId eq '{commandId}'");
        }

        /// <summary>
        ///     Gets the amount of time to wait until the next time command feed should hand us the command
        /// </summary>
        /// <param name="dequeueCount">dequeue count</param>
        /// <returns>lease extension time for the number of dequeues seen so far</returns>
        private TimeSpan GetLeaseExtensionMinutes(int dequeueCount)
        {
            if (this.leaseExtensionSet.Count > 0)
            {
                int index = Math.Min(Math.Max(dequeueCount, 0), this.leaseExtensionSet.Count - 1);
                return this.leaseExtensionSet[index];
            }

            return CosmosDataAgent.DefaultLeaseExtension;
        }

        /// <summary>
        ///     Deletes the row count table entries for this command
        /// </summary>
        /// <param name="rows">rows to delete</param>
        /// <param name="commandId">command id</param>
        /// <returns>resulting value</returns>
        private ICollection<Task> DeleteRowCounts(
            ICollection<CommandFileState> rows,
            string commandId)
        {
            const int BatchSize = 99;

            async Task DeleteRowCountBatchAsync(ICollection<CommandFileState> list)
            {
                try
                {
                    await this.commandFileState.DeleteBatchAsync(list).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    this.logger.Error(nameof(CosmosDataAgent), $"failed to delete row count entires for command {commandId}: {e}");

                    // ignore all storage exceptions for this as we really don't want to have to reprocess a large file
                    //  just because we couldn't write the row count for (some of) its commands
                    if (e is StorageException == false)
                    {
                        throw;
                    }
                }
            }

            // this is kinda confusing, but what is does is create a list of tuples that have a writer and original list
            //  index of the writer.  It then groups the writers by the index / BatchSize (creating groups of no more 
            //  than BatchSize writers) and finally invokes writeRowCountBatch with that group.
            return rows
                .Select((r, i) => (Index: i, Row: r))
                .GroupBy(tuple => tuple.Index / BatchSize)
                .Select(group => DeleteRowCountBatchAsync(group.Select(v => v.Row).ToList()))
                .ToList();
        }

        /// <summary>state added to the export request</summary>
        internal class CosmosExportAgentState
        {
            public const int CurrentVersion = 3;

            /// <summary>
            ///     Initializes a new instance of the CosmosExportAgentState class
            /// </summary>
            /// <param name="isTestCommand">is test command</param>
            /// <param name="dequeueCount">dequeue count</param>
            public CosmosExportAgentState(
                bool? isTestCommand,
                int dequeueCount)
            {
                this.HasTestCommandBeenRetrieved = isTestCommand.HasValue;
                this.IsTestCommand = isTestCommand ?? false;
                this.DequeueCount = dequeueCount;
                this.Version = CosmosExportAgentState.CurrentVersion;
            }

            /// <summary>
            ///     Initializes a new instance of the CosmosExportAgentState class
            /// </summary>
            // ReSharper disable once UnusedMember.Local
            public CosmosExportAgentState()
            {
            }

            /// <summary>
            ///     Gets or sets is test command
            /// </summary>
            public bool IsTestCommand { get; set; }

            /// <summary>
            ///     Gets or sets the number of times we have been told about the command
            /// </summary>
            public int DequeueCount { get; set; }

            /// <summary>
            ///     Gets or sets version
            /// </summary>
            public int Version { get; set; }

            /// <summary>
            ///     Gets or sets a value indicating whether the test command flag has been successfully retrieved
            /// </summary>
            public bool HasTestCommandBeenRetrieved { get; set; }

            /// <summary>
            ///     Gets or sets a value indicating when the last time we wrote a 'command pending' trace event
            /// </summary>
            public DateTimeOffset LastPendingTraceEvent { get; set; } = DateTimeOffset.MinValue;
        }
    }
}
