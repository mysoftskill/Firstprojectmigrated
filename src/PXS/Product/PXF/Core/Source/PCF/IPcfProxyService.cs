// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.PCF
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.PcfAdapter;
    using Microsoft.PrivacyServices.PXS.Command.CommandStatus;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    /// <summary>
    ///     This is where code that interacts with PCF should go. Timeline will use this during deletes, as well as the various portals
    ///     both enterprise and consumer such as PCD. Export and status APIs will need to be added here primarily.
    ///     This class abstracts fetching xuids from xbox, deviceids from DDS, as well as minting verifier tokens.
    /// </summary>
    public interface IPcfProxyService
    {
        /// <summary>
        ///     Gets the request status for a given request from PCF
        /// </summary>
        Task<ServiceResponse<PrivacyRequestStatus>> ListMyRequestByIdAsync(IRequestContext requestContext, Guid commandId);

        /// <summary>
        ///     This is used to retrieve a command given its ID.
        /// </summary>
        Task<ServiceResponse<CommandStatusResponse>> ListRequestByIdAsync(IRequestContext requestContext, Guid commandId);

        /// <summary>
        ///     Gets the request status for a given MSA from PCF
        /// </summary>
        /// <param name="requestContext">Request context that containers the MSA to look up by.</param>
        /// <param name="requestTypes">The request types to filter to.</param>
        Task<ServiceResponse<IList<PrivacyRequestStatus>>> ListRequestsByCallerMsaAsync(IRequestContext requestContext, params RequestType[] requestTypes);

        /// <summary>
        ///     Gets the request status for a given requester from PCF
        /// </summary>
        /// <param name="requestContext">Request context that containers the caller to look up by.</param>
        /// <param name="requestTypes">The request types to filter to.</param>
        Task<ServiceResponse<IList<PrivacyRequestStatus>>> ListRequestsByCallerSiteAsync(IRequestContext requestContext, params RequestType[] requestTypes);

        /// <summary>
        ///     Post delete requests to PCF.
        /// </summary>
        /// <param name="requestContext">The request context, primarily where the MSA subject comes from.</param>
        /// <param name="requests">The list of delete requests to post.</param>
        Task<ServiceResponse<IList<Guid>>> PostDeleteRequestsAsync(
            IRequestContext requestContext,
            List<DeleteRequest> requests);

        /// <summary>
        ///     Post MSA recurring delete requests to PCF.
        /// </summary>
        /// <param name="deleteRequest"></param>
        /// <param name="requestContext"></param>
        /// <param name="preverifier"></param>
        Task<ServiceResponse> PostMsaRecurringDeleteRequestsAsync(IRequestContext requestContext, DeleteRequest deleteRequest, string preverifier);

        /// <summary>
        ///     Post export request to PCF.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="request">The export request to post.</param>
        /// <returns>The request Id.</returns>
        Task<ServiceResponse<Guid>> PostExportRequestAsync(IRequestContext requestContext, ExportRequest request);

        /// <summary>
        ///     Post an account cleanup request to PCF. The actual request sent to PCF
        ///     is an AccountCloseRequest, but this method verifies that it is
        ///     for a user in a resource tenant.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="request">The account close request to post.</param>
        /// <returns>The request Id.</returns>
        Task<ServiceResponse<Guid>> PostAccountCleanupRequestAsync(IRequestContext requestContext, AccountCloseRequest request);

        /// <summary>
        ///     Get the queue stats for a given agent.
        /// </summary>
        Task<ServiceResponse<IList<AssetGroupQueueStatistics>>> TestAgentQueueStatsAsync(IRequestContext requestContext, Guid agentId);

        /// <summary>
        ///     Force completion of a command.
        /// </summary>
        Task<ServiceResponse> TestForceCommandCompletionAsync(IRequestContext requestContext, Guid commandId);

        /// <summary>
        ///     This is used to debug a given command. This is only for test page scenario.
        /// </summary>
        Task<ServiceResponse<CommandStatusResponse>> TestRequestByIdAsync(IRequestContext requestContext, Guid commandId);

        /// <summary>
        ///     This is used to debug all commands the current user is the subject of.
        /// </summary>
        Task<ServiceResponse<IList<CommandStatusResponse>>> TestRequestByUserAsync(IRequestContext requestContext);

        /// <summary>
        /// This is used to delete export archive on user demand
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        Task<ServiceResponse> DeleteExportsAsync(DeleteExportArchiveParameters param);
    }
}
