// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.UserSettings
{
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;

    /// <summary>
    /// Interface for UserSettings
    /// </summary>
    public interface IUserSettingsService
    {
        /// <summary>
        /// Gets the user settings asynchronously.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <returns>Task response contains a <see cref="ResourceSettingV1" />, or <see cref="Error"/></returns>
        Task<ServiceResponse<ResourceSettingV1>> GetAsync(IRequestContext requestContext);

        /// <summary>
        /// Gets or creates the user settings asynchronously.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <returns>Task response contains a <see cref="ResourceSettingV1" />, or <see cref="Error"/></returns>
        Task<ServiceResponse<ResourceSettingV1>> GetOrCreateAsync(IRequestContext requestContext);

        /// <summary>
        /// Update the user settings asynchronously.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="settings">The requested user settings to change.</param>
        /// <returns>Task response contains a <see cref="ResourceSettingV1" />, or <see cref="Error"/></returns>
        Task<ServiceResponse<ResourceSettingV1>> UpdateAsync(IRequestContext requestContext, ResourceSettingV1 settings);
    }
}