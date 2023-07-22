// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models;

    /// <summary>
    /// Interface for UserSettingsClient
    /// </summary>
    public interface IUserSettingsClient
    {
        /// <summary>
        /// Gets the user0settings asynchronously.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <param name="method">The HttpMethod.</param>
        ///  <returns>A success result contains <see cref="PagedResponse{ResourceSettingV1}"/></returns>
        Task<ResourceSettingV1> GetUserSettingsAsync(GetUserSettingsArgs args, HttpMethod method = default);

        /// <summary>
        /// Updates the user-settings asynchronously.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>A Task</returns>
        Task<ResourceSettingV1> UpdateUserSettingsAsync(UpdateUserSettingsArgs args);
    }
}