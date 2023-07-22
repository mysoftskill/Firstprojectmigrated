// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.CustomerMasterAdapter
{
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Newtonsoft.Json.Linq;

    public interface ICustomerMasterAdapter
    {
        /// <summary>
        ///     Creates the privacy profile.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="content">The content used to create the profile.</param>
        /// <returns><see cref="AdapterError" /> if any occurred; else null</returns>
        Task<AdapterResponse<PrivacyProfile>> CreatePrivacyProfileAsync(IPxfRequestContext requestContext, PrivacyProfile content);

        /// <summary>
        ///     Gets the privacy on-behalf-of consent setting.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The privacy obo consent setting</returns>
        Task<AdapterResponse<bool?>> GetOboPrivacyConsentSettingAsync(IPxfRequestContext requestContext);

        /// <summary>
        ///     Gets the privacy profile as <see cref="PrivacyProfile" />.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="operationName">The operation name used in logging SLL outgoing operations.</param>
        /// <returns>The <see cref="AdapterResponse{PrivacyProfile}" /> of the user</returns>
        Task<AdapterResponse<PrivacyProfile>> GetPrivacyProfileAsync(IPxfRequestContext requestContext, string operationName = "GetPrivacyProfile");

        /// <summary>
        ///     Gets the privacy profile as <see cref="JObject" />.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The <see cref="AdapterResponse{JObject}" /> of the user</returns>
        Task<AdapterResponse<JObject>> GetPrivacyProfileJObjectAsync(IPxfRequestContext requestContext);

        /// <summary>
        ///     Update the privacy profile
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="updatedContent">The content to update in the profile</param>
        /// <param name="existingProfile">The existing profile.</param>
        /// <returns><see cref="AdapterError" /> if any occurred; else null</returns>
        Task<AdapterResponse<PrivacyProfile>> UpdatePrivacyProfileAsync(IPxfRequestContext requestContext, PrivacyProfile updatedContent, JObject existingProfile);
    }
}
