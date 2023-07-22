// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility.ManifestParsers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem.Cosmos;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Data;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility.ProgressTracker;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.PcfAdapter;

    /// <summary>
    ///     utilties for analyzing request manifests and comparing the contained commands with what was received from command feed
    /// </summary>
    public class RequestCommandUtilities : IRequestCommandUtilities
    {
        private const int DefaultBatchSize = 75;

        private readonly ICommandMonitorConfig config;
        private readonly ITable<CommandState> commandState;
        private readonly ICommandClient commandClient;
        private readonly int batchSize;
        private readonly IAppConfiguration appConfig;

        /// <summary>
        ///     Initializes a new instance of the RequestCommandUtilities class
        /// </summary>
        /// <param name="config">command feed configuration</param>
        /// <param name="commandClientFactory">command client factory</param>
        /// <param name="commandState">command state</param>
        /// <param name="appConfig">app config</param>
        /// <param name="commandStateQueryBatchSize">command state query batch size</param>
        public RequestCommandUtilities(
            ICommandMonitorConfig config,
            ICommandObjectFactory commandClientFactory,
            ITable<CommandState> commandState,
            IAppConfiguration appConfig,
            int commandStateQueryBatchSize)
        {
            ArgumentCheck.ThrowIfNull(commandClientFactory, nameof(commandClientFactory));

            this.commandState = commandState ?? throw new ArgumentNullException(nameof(commandState));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));

            this.commandClient = commandClientFactory.CreateCommandFeedClient();
            this.batchSize = commandStateQueryBatchSize;
        }

        /// <summary>
        ///     Initializes a new instance of the RequestCommandUtilities class
        /// </summary>
        /// <param name="config">command feed configuration</param>
        /// <param name="commandClientFactory">command client factory</param>
        /// <param name="commandState">command state</param>
        public RequestCommandUtilities(
            ICommandMonitorConfig config,
            ICommandObjectFactory commandClientFactory,
            ITable<CommandState> commandState,
            IAppConfiguration appConfig) :
            this(config, commandClientFactory, commandState, appConfig, RequestCommandUtilities.DefaultBatchSize)
        {
        }

        /// <summary>
        ///     Analyzes the command against internal state
        /// </summary>
        /// <param name="ctx">operation context</param>
        /// <param name="agentId">agent id</param>
        /// <param name="commands">request manifest commands</param>
        /// <param name="renewer">operation lease renewer</param>
        /// <param name="cancelToken">cancellation token</param>
        /// <param name="abortIfMissingOrUnavailable">
        ///     true to abort if a command is missing or unavailable; false to keep querying
        /// </param>
        /// <returns>set of commands organized by status code</returns>
        public async Task<RequestCommandsInfo> DetermineCommandStatusAsync(
            OperationContext ctx,
            string agentId,
            ICollection<string> commands,
            ILeaseRenewer renewer,
            CancellationToken cancelToken,
            bool abortIfMissingOrUnavailable)
        {
            const string RowKeyFilterFmt = "RowKey eq '{0}'";
            const string QueryFmt = "PartitionKey eq '{0}' and ({1})";

            RequestCommandsInfo result;
            bool processIds = true;
            int start;

            result = new RequestCommandsInfo
            {
                Commands = new Dictionary<CommandStatusCode, ICollection<string>>()
            };

            // there is a max query length, so we have to break the query up into groups to get them all

            ctx.Op = "Fetching commands from command state store and comparing to required commands";

            for (start = 0; start < commands.Count && processIds; start += this.batchSize)
            {
                Task<ICollection<CommandState>> queryTask;
                HashSet<string> queryIds = new HashSet<string>(commands.Skip(start).Take(this.batchSize));

                string query = string.Format(
                    QueryFmt,
                    agentId,
                    string.Join(" or ", queryIds.Select(o => string.Format(RowKeyFilterFmt, o))));

                cancelToken.ThrowIfCancellationRequested();

                // no reason not to do the query and manifest manifestLease renewal in parallel
                await Task
                    .WhenAll(
                        queryTask = this.commandState.QueryAsync(query),
                        renewer.RenewAsync())
                    .ConfigureAwait(false);

                foreach (CommandState state in queryTask.Result)
                {
                    CommandStatusCode code = RequestCommandUtilities.TranslateStateDataToStatusCode(state);

                    // note that we've got back this command
                    queryIds.Remove(state.CommandId);

                    RequestCommandUtilities.AddCommandToCodeSet(result.Commands, code, state.CommandId);
                }

                // iterate over each of the commands remaining in the original query batch and add them as missing
                foreach (string id in queryIds)
                {
                    CommandStatusCode code = CommandStatusCode.Undetermined;

                    if (processIds)
                    {
                        code = await this.ProcessUnknownCommandAsync(ctx, agentId, id, renewer, cancelToken);
                        RequestCommandUtilities.AddCommandToCodeSet(result.Commands, code, id);
                    }
                    else
                    {
                        RequestCommandUtilities.AddCommandToCodeSet(result.Commands, CommandStatusCode.Undetermined, id);
                        result.HasUndetermined = true;
                    }

                    // Temporarily allow non-ingested commands to be processed
                    // This is added as we see issues with command history update only, the commands are ingested into agent queues.
                    // Cosmos Export will checkpoint the command if it finds the command state in the command state table 
                    // to be completed. Check CosmosDataAgent.ProcessExportInternalAsync
                    if (code == CommandStatusCode.NotAvailable && !await this.appConfig.IsFeatureFlagEnabledAsync(FeatureNames.PXS.ProcessNonIngestedExportCommandsEnabled))
                    {
                        processIds = abortIfMissingOrUnavailable == false;
                        result.HasNotAvailable = true;
                    }
                    else if (code == CommandStatusCode.Missing)
                    {
                        processIds = abortIfMissingOrUnavailable == false;
                        result.HasMissing = true;
                    }
                }
            }

            // if we stopped above, then add all the currently unprocessed commands to the result set.
            if (start < commands.Count)
            {
                foreach (string id in commands.Skip(start))
                {
                    RequestCommandUtilities.AddCommandToCodeSet(result.Commands, CommandStatusCode.Undetermined, id);
                }
            }

            result.CommandCount = commands.Count;

            return result;
        }

        /// <summary>
        ///     Reports the command summary
        /// </summary>
        /// <param name="ctx">operation context</param>
        /// <param name="tracker">activity log tracker</param>
        /// <param name="activityLogFileSys">activity log file system</param>
        /// <param name="tracer">trace logger</param>
        /// <param name="agentId">agent id</param>
        /// <param name="commandInfo">command info</param>
        /// <param name="requestManifesName">request manifest file name</param>
        /// <param name="ageBatch">age of the batch</param>
        /// <param name="maxAgeBatch">maximum age of the batch</param>
        /// <param name="hasWaitTimedOut">true if the age based wait has timed out; false otherwise</param>
        /// <returns>resulting value</returns>
        public async Task ReportCommandSummaryAsync(
            OperationContext ctx,
            IFileProgressTracker tracker,
            ICosmosFileSystem activityLogFileSys,
            ITaskTracer tracer,
            string agentId,
            RequestCommandsInfo commandInfo,
            string requestManifesName,
            TimeSpan ageBatch,
            TimeSpan maxAgeBatch,
            bool hasWaitTimedOut)
        {
            const int TraceCmdLimit = 50;

            IEnumerable<KeyValuePair<CommandStatusCode, ICollection<string>>> traceSet;
            StringBuilder trackerCmdSummary;
            StringBuilder logCmdSummary;
            ICollection<string> valid;
            string commandStatusPath;

            if (commandInfo.HasMissing || commandInfo.HasUndetermined || commandInfo.HasNotAvailable)
            {
                await this.ReportMissingCommandsAsync(
                    ctx,
                    tracker,
                    activityLogFileSys,
                    tracer,
                    agentId,
                    commandInfo,
                    requestManifesName,
                    ageBatch,
                    maxAgeBatch,
                    hasWaitTimedOut);
            }

            commandInfo.Commands.TryGetValue(CommandStatusCode.Actionable, out valid);

            // if we only have one set of commands and it's the valid list, then all are of course valid.
            if (valid != null && commandInfo.Commands.Count == 1)
            {
                tracker?.AddMessage(
                    TrackerTypes.BatchCommands, 
                    $"All {commandInfo.CommandCount} commands in request manifest found. Starting to process data files.");

                return;
            }

            trackerCmdSummary = new StringBuilder(
                $"Summary of the {commandInfo.CommandCount} commands listed in the request manifest by classification: ");

            logCmdSummary = new StringBuilder();

            traceSet = commandInfo.Commands.Where(o => o.Value?.Count > 0).OrderBy(o => o.Key);
            foreach (KeyValuePair<CommandStatusCode, ICollection<string>> kvp in traceSet)
            {
                trackerCmdSummary.Append($"{kvp.Key}: {kvp.Value.Count}; ");

                tracer.TraceWarning(
                    "[{0}] is has {1} commands classified as {2}. Sampling: {3}",
                    ctx.Item,
                    kvp.Value.Count.ToStringInvariant(),
                    kvp.Key.ToStringInvariant(),
                    string.Join(",", kvp.Value.Take(TraceCmdLimit)));

                foreach (string id in kvp.Value)
                {
                    logCmdSummary.Append($"{id}\t{kvp.Key.ToStringInvariant()}\n");
                }
            }

            tracker?.AddMessage(TrackerTypes.BatchCommands, trackerCmdSummary.ToString());

            // write the summary only on the last pass before we start processing the batch.  If we have any missing commands and
            //  we're not on the final attempt, then we will make further attempts, so skip the summary write
            if ((commandInfo.HasMissing || commandInfo.HasNotAvailable) && 
                hasWaitTimedOut == false)
            {
                return;
            }

            commandStatusPath =
                activityLogFileSys.RootDirectory +
                Utility.EnsureHasTrailingSlashButNoLeadingSlash(agentId) +
                requestManifesName.Insert(
                    Constants.RequestManifestNamePrefix.Length, Constants.RequestManifestNameDeadLetterInsert);

            try
            {
                IFile file;

                ctx.Op = "Writing command summary log";

                file = await activityLogFileSys
                    .CreateFileAsync(commandStatusPath, activityLogFileSys.DefaultLifetime, FileCreateMode.CreateAlways)
                    .ConfigureAwait(false);

                await file.AppendAsync(logCmdSummary.ToString()).ConfigureAwait(false);
            }
            catch (IOException e)
            {
                tracer.TraceError($"Failed to write command summary log {commandStatusPath}: {e}");
            }
        }

        /// <summary>
        ///     Adds the command to code set
        /// </summary>
        /// <param name="commands">command collection containing the set to add the command to</param>
        /// <param name="code">status code for command to add</param>
        /// <param name="id">id of command to add</param>
        private static void AddCommandToCodeSet(
            IDictionary<CommandStatusCode, ICollection<string>> commands,
            CommandStatusCode code,
            string id)
        {
            ICollection<string> set;

            if (commands.TryGetValue(code, out set) == false)
            {
                commands[code] = set = new List<string>();
            }

            set.Add(id);
        }

        /// <summary>
        ///     Translates the info in a command state data object to a status code
        /// </summary>
        /// <param name="state">command state data</param>
        /// <returns>command status code</returns>
        private static CommandStatusCode TranslateStateDataToStatusCode(CommandState state)
        {
            CommandStatusCode result;

            if (state.IsComplete)
            {
                result = CommandStatusCode.Completed;
            }
            else if (state.NotApplicable)
            {
                result = CommandStatusCode.NotApplicable;
            }
            else if (state.IgnoreCommand)
            {
                result = CommandStatusCode.Ignored;
            }
            else
            {
                result = CommandStatusCode.Actionable;
            }

            return result;
        }

        /// <summary>
        ///     Updates missing commands by querying PCF
        /// </summary>
        /// <param name="ctx">operation context</param>
        /// <param name="agentId">agent id</param>
        /// <param name="commandId">command id</param>
        /// <param name="renewer">operation lease renewer</param>
        /// <param name="cancelToken">cancellation token</param>
        /// <returns>resulting value</returns>
        private async Task<CommandStatusCode> ProcessUnknownCommandAsync(
            OperationContext ctx,
            string agentId,
            string commandId,
            ILeaseRenewer renewer,
            CancellationToken cancelToken)
        {
            Task<QueryCommandResult> response;
            CommandStatusCode result;
            string leaseReceipt = string.Empty;
            bool attemptInsert = false;

            cancelToken.ThrowIfCancellationRequested();

            ctx.Op = "Querying PCF for missing command [" + commandId + "]";

            // the agent id parameter needs to be the Cosmos Export Agent's agent id, while the asset group id is the real
            //  agent id. This is due to the magic done by PDMS to enable the Cosmos Export agent to not have to query 
            //  PCF individually for every real agent.
            await Task
                .WhenAll(
                    response = this.commandClient.QueryCommandAsync(this.config.AgentId, agentId, commandId, cancelToken),
                    renewer.RenewAsync())
                .ConfigureAwait(false);

            switch (response.Result.ResponseCode)
            {
                case ResponseCode.OK:
                    attemptInsert = true;
                    leaseReceipt = response.Result.Command.LeaseReceipt;
                    result = CommandStatusCode.Actionable;
                    break;

                case ResponseCode.CommandNotFound:
                    result = CommandStatusCode.UnknownCommand;
                    break;

                case ResponseCode.CommandNotApplicable:
                    result = CommandStatusCode.NotApplicable;
                    break;

                case ResponseCode.CommandNotYetDelivered:
                    result = CommandStatusCode.NotAvailable;
                    break;

                case ResponseCode.UnableToResolveLocation:
                    result = CommandStatusCode.Missing;
                    break;

                case ResponseCode.CommandNotFoundInQueue:
                case ResponseCode.CommandAlreadyCompleted:
                    result = CommandStatusCode.Completed;
                    break;

                default:
                    result = CommandStatusCode.Missing;
                    break;
            }

            if (attemptInsert)
            {
                InsertOrFetch insertFetch;
                CommandState state;

                ctx.Op = "Inserting command state for missing command [" + commandId + "]";

                (insertFetch, state) = await CommandStateUtility
                    .InsertOrFetchCommandStateAsync(
                        ctx,
                        this.commandState,
                        commandId,
                        agentId,
                        leaseReceipt,
                        false,
                        false,
                        false,
                        true)
                    .ConfigureAwait(false);

                // if we could not insert but did fetch a command, then update the new code accordingly
                if (insertFetch == InsertOrFetch.Fetched)
                {
                    result = RequestCommandUtilities.TranslateStateDataToStatusCode(state);
                }
            }

            return result;
        }

        /// <summary>
        ///     Reports the missing commands
        /// </summary>
        /// <param name="ctx">operation context</param>
        /// <param name="tracker">activity log tracker</param>
        /// <param name="activityLogFileSys">activity log file system</param>
        /// <param name="tracer">trace logger</param>
        /// <param name="agentId">agent id</param>
        /// <param name="cmdInfo">command info</param>
        /// <param name="reqManifestName">request manifest file name</param>
        /// <param name="ageBatch">age of the batch</param>
        /// <param name="maxAgeBatch">maximum age of the batch</param>
        /// <param name="hasWaitTimedOut">true if the age based wait has timed out; false otherwise</param>
        /// <returns>resulting value</returns>
        private async Task ReportMissingCommandsAsync(
            OperationContext ctx,
            IFileProgressTracker tracker,
            ICosmosFileSystem activityLogFileSys,
            ITaskTracer tracer,
            string agentId,
            RequestCommandsInfo cmdInfo,
            string reqManifestName,
            TimeSpan ageBatch,
            TimeSpan maxAgeBatch,
            bool hasWaitTimedOut)
        {
            const string UnavailableUndeterminedBlockContinueMsgFmt =
                "At least {0} of {1} commands have not yet been delivered by or discovered directly from command feed, " +
                "and while the batch's age of {2} is older than the allowed wait time of {3}, at least one missing command " +
                "is still being processed by command feed and requires a continued wait to ensure data is not missed. Will " +
                "retry in a short while.";

            const string ContinueMsgFmt =
                "{0} of {1} commands have not yet been delivered by or discovered directly from by command feed, but the " +
                "batch's age of {2} is older than the allowed wait time of {3}, so processing will continue and data for " +
                "any missing commands in the batch will be written to the missing command holding storage. Starting to " +
                "process data files.";

            const string SkipMsgFmt =
                "At least {0} of {1} commands have not yet been delivered by or discovered directly from command feed, " +
                "and the batch's age of {2} is not yet older than the allowed wait time of {3}. Will retry in a short " +
                "while.";

            ICollection<string> missing;
            string commandStatusPath;
            string message;
            string fmt;

            cmdInfo.Commands.TryGetValue(CommandStatusCode.Missing, out missing);

            if (hasWaitTimedOut)
            {
                fmt = cmdInfo.HasUndetermined || cmdInfo.HasNotAvailable ?
                    UnavailableUndeterminedBlockContinueMsgFmt :
                    ContinueMsgFmt;
            }
            else
            {
                fmt = SkipMsgFmt;
            }

            message = fmt.FormatInvariant(
                missing?.Count.ToStringInvariant() ?? "0",
                cmdInfo.CommandCount.ToStringInvariant(),
                ageBatch.ToString(@"d\.hh\:mm\:ss"),
                maxAgeBatch.ToString(@"d\.hh\:mm\:ss"));

            tracer.TraceWarning($"[{ctx.Item}]{message}");

            tracker?.AddMessage(TrackerTypes.BatchCommands, message);

            // skip writing the full summary file unless we're skipping the missing ones
            if (hasWaitTimedOut == false)
            {
                return;
            }

            commandStatusPath =
                activityLogFileSys.RootDirectory +
                Utility.EnsureHasTrailingSlashButNoLeadingSlash(agentId) +
                reqManifestName.Insert(
                    Constants.RequestManifestNamePrefix.Length, Constants.RequestManifestNameDeadLetterInsert);

            if (missing != null)
            {
                try
                {
                    IFile file;

                    ctx.Op = "Writing to log of missing command ids";

                    file = await activityLogFileSys
                        .CreateFileAsync(commandStatusPath, activityLogFileSys.DefaultLifetime, FileCreateMode.CreateAlways)
                        .ConfigureAwait(false);

                    await file.AppendAsync(string.Join("\n", missing)).ConfigureAwait(false);
                }
                catch (IOException e)
                {
                    tracer.TraceError($"Failed to write missing commands to activity log {commandStatusPath}: {e}");
                }
            }
        }
    }
}
