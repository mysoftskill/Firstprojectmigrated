// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;

    /// <summary>
    ///     command feed client contract
    /// </summary>
    public interface ICommandClient : ICommandFeedClient
    {
        /// <summary>
        ///     Updates the status of the given command
        /// </summary>
        /// <param name="command">command</param>
        /// <param name="commandStatus">command status</param>
        /// <param name="affectedRowCount">affected row count</param>
        /// <param name="leaseExtension">lease extension</param>
        /// <param name="nonTransientFailures">non-transient failures</param>
        /// <param name="exportedFileSizeDetails">Exported File Size Details</param>
        /// <returns>new lease receipt for the command</returns>
        Task CheckpointAsync(
            IPrivacyCommand command,
            CommandStatus commandStatus,
            TimeSpan? leaseExtension,
            int affectedRowCount,
            IEnumerable<string> nonTransientFailures,
            IEnumerable<ExportedFileSizeDetails> exportedFileSizeDetails = null);

        /// <summary>
        ///     Determines whether the specified command is synthetic or not
        /// </summary>
        /// <param name="commandId">command id</param>
        /// <returns>true if it is a test command, false if it is not a test command, null if no value could be determined</returns>
        Task<bool?> IsTestCommandAsync(string commandId);

        /// <summary>
        ///     Retrieves details about a Command previously received from GetCommandsAsync()
        /// </summary>
        /// <param name="commandId">command id</param>
        /// <param name="leaseReceipt">lease receipt</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>full command or null if command could not be found</returns>
        Task<IPrivacyCommand> QueryCommandAsync(
            string commandId,
            string leaseReceipt,
            CancellationToken cancellationToken);

        /// <summary>
        ///     Retrieves details about a Command previously received from GetCommandsAsync()
        /// </summary>
        /// <param name="agentId">agent id</param>
        /// <param name="assetGroupId">asset group id</param>
        /// <param name="commandId">command id</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>command object and status details</returns>
        Task<QueryCommandResult> QueryCommandAsync(
            string agentId,
            string assetGroupId,
            string commandId,
            CancellationToken cancellationToken);
    }
}
