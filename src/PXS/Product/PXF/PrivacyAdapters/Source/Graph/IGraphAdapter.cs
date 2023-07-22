// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Graph
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;

    /// <summary>
    ///     Interface IGraphAdapter.
    /// </summary>
    public interface IGraphAdapter
    {
        /// <summary>
        ///     Checks whether a member belongs to a set of groups or not.
        /// </summary>
        /// <param name="memberId">The member Id.</param>
        /// <param name="groupId">The group Id.</param>
        /// <returns>A boolean indicates whether the member belongs to the group or not.</returns>
        Task<AdapterResponse<IsMemberOfResponse>> IsMemberOfAsync(Guid memberId, Guid groupId);

        /// <summary>
        ///     Get a directory role provided tenantId and roleTemplateId.
        /// </summary>
        /// <param name="tenantId">The tenant Id.</param>
        /// <param name="roleTemplateId">The role template Id.</param>
        /// <returns>Directory roles.</returns>
        Task<AdapterResponse<GetDirectoryRolesResponse>> GetDirectoryRolesAsync(Guid tenantId, Guid roleTemplateId);

        /// <summary>
        ///     Get members for a directory role provided tenantId and roleObjectId
        /// </summary>
        /// <param name="directoryRoleId">The directory role Id.</param>
        /// <param name="tenantId">The tenant Id.</param>
        /// <returns>Member Ids.</returns>
        Task<AdapterResponse<GetDirectoryRoleMemberResponse>> GetDirectoryRoleMembersAsync(Guid tenantId, Guid directoryRoleId);
    }
}
