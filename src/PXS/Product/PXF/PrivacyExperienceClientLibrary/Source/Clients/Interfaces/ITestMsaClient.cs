// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Clients.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.PrivacySubject;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models;
    using Microsoft.PrivacyServices.PXS.Command.CommandStatus;

    /// <summary>
    ///     <summary>
    ///         Client operations for testing MSA.
    ///     </summary>
    /// </summary>
    public interface ITestMsaClient
    {
        /// <summary>
        ///     Force command completion by a given command id.
        /// </summary>
        Task TestForceCommandCompletionAsync(TestForceCompletionArgs args);

        /// <summary>
        ///     Gets agent status by a given agent id.
        /// </summary>
        Task<IList<AssetGroupQueueStatistics>> TestGetAgentStatisticsAsync(TestGetAgentStatisticsArgs args);

        /// <summary>
        ///     Gets command status by a given command id. Will return redacted info in the case the caller is not the subject.
        /// </summary>
        Task<CommandStatusResponse> TestGetCommandStatusByIdAsync(TestGetCommandStatusByIdArgs args);

        /// <summary>
        ///     Gets command statuses by the caller's MSA.
        /// </summary>
        Task<IList<CommandStatusResponse>> TestGetCommandStatusesAsync(PrivacyExperienceClientBaseArgs args);

        /// <summary>
        ///     Closes subject's MSA.
        /// </summary>
        Task<OperationResponse> TestMsaCloseAsync(TestMsaCloseArgs args);
    }
}
