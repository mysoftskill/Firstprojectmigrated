// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.PcfAdapter
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.PXS.Command.CommandStatus;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    /// <summary>
    ///     An adapter for talking to PCF.
    /// </summary>
    public interface IPcfAdapter
    {
        /// <summary>
        ///     Forces the completion of a command, regardless of whether all agents have completed.
        /// </summary>
        Task<AdapterResponse> ForceCompleteAsync(Guid commandId);

        /// <summary>
        ///     Gets the queue stats for a given agent id
        /// </summary>
        Task<AdapterResponse<AgentQueueStatisticsResponse>> GetAgentQueueStatsAsync(Guid agentId);

        /// <summary>
        ///     Gets the PCF managed storage Uris from PCF.
        /// </summary>
        /// <returns>A list of Uris to storage accounts, sans credentials or any sub path, such as https://fooaccount.blob.core.windows.net</returns>
        Task<AdapterResponse<IList<Uri>>> GetPcfStorageUrisAsync();

        /// <summary>
        ///     Gets a particular command id
        /// </summary>
        /// <param name="commandId">The command to retrieve.</param>
        /// <param name="redacted">Whether or not private data is redacted.</param>
        Task<AdapterResponse<CommandStatusResponse>> GetRequestByIdAsync(
            Guid commandId, 
            bool redacted);

        /// <summary>
        ///     Gets a command by command id and agent
        /// </summary>
        /// <param name="agentId">agent id</param>
        /// <param name="assetGroupId">asset group id</param>
        /// <param name="commandId">command id</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>resulting value</returns>
        Task<AdapterResponse<QueryCommandByIdResult>> QueryCommandByCommandIdAsync(
            string agentId,
            string assetGroupId,
            string commandId,
            CancellationToken cancellationToken);

        /// <summary>
        ///     Submits a list of privacy requests to PCF.
        /// </summary>
        Task<AdapterResponse> PostCommandsAsync(IList<PrivacyRequest> requests);

        /// <summary>
        ///     Gets a list of existing requests by the optional parameters. Any null parameter matches all on that dimension.
        /// </summary>
        Task<AdapterResponse<IList<CommandStatusResponse>>> QueryCommandStatusAsync(
            IPrivacySubject subject,
            string requester,
            IList<RequestType> requestTypes,
            DateTimeOffset oldestCommand);

        /// <summary>
        /// Deletes the export archive based on given commandId
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        Task<AdapterResponse> DeleteExportArchiveAsync(DeleteExportArchiveParameters parameters);
    }
}
