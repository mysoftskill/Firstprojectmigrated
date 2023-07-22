// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility.ManifestParsers
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem.Cosmos;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks;

    /// <summary>
    ///     collection of validity codes for commands
    /// </summary>
    public enum CommandStatusCode
    {
        /// <summary>
        ///     the command's status has not yet been determined
        /// </summary>
        Undetermined = 0,

        /// <summary>
        ///     the command can be processed
        /// </summary>
        Actionable,

        /// <summary>
        ///     the command has been completed in command feed
        /// </summary>
        Completed,

        /// <summary>
        ///     the command is not applicable to the given agent
        /// </summary>
        NotApplicable,

        /// <summary>
        ///     the command is known to command feed and applicable to the agent, but has not yet been sufficiently
        ///      processed to be made available to the agent 
        /// </summary>
        NotAvailable,

        /// <summary>
        ///     the command is not known to command feed 
        /// </summary>
        UnknownCommand,

        /// <summary>
        ///     the command was determined to be ignorable on first delivery from command feed (usually due to be a test command)
        /// </summary>
        Ignored,

        /// <summary>
        ///     the command has not been delivered by command feed and command feed could not be queried for it
        /// </summary>
        Missing,
    };

    public class RequestCommandsInfo
    {
        /// <summary>
        ///     Gets or sets a collection of commands and their statuses
        /// </summary>
        public IDictionary<CommandStatusCode, ICollection<string>> Commands { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether this instance has missing commands
        /// </summary>
        public bool HasUndetermined { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether this instance has commands that exist but PCF has not made available yet
        /// </summary>
        public bool HasNotAvailable { get; set; }
        
        /// <summary>
        ///     Gets or sets a value indicating whether this instance has missing commands
        /// </summary>
        public bool HasMissing { get; set; }

        /// <summary>
        ///     Gets or sets the total count of commands
        /// </summary>
        public int CommandCount { get; set; }
    }

    /// <summary>
    ///     contract for objects that provide utilties for analyzing request manifests and comparing the contained commands
    ///      with what was received from command feed
    /// </summary>
    public interface IRequestCommandUtilities
    {
        /// <summary>
        ///     Analyzes the command against internal state asynchronous
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
        Task<RequestCommandsInfo> DetermineCommandStatusAsync(
            OperationContext ctx,
            string agentId,
            ICollection<string> commands,
            ILeaseRenewer renewer,
            CancellationToken cancelToken,
            bool abortIfMissingOrUnavailable);

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
        Task ReportCommandSummaryAsync(
            OperationContext ctx,
            IFileProgressTracker tracker,
            ICosmosFileSystem activityLogFileSys,
            ITaskTracer tracer,
            string agentId,
            RequestCommandsInfo commandInfo,
            string requestManifesName,
            TimeSpan ageBatch,
            TimeSpan maxAgeBatch,
            bool hasWaitTimedOut);
    }
}
