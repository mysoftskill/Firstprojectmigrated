// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary
{
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Formatting;
    using System.Threading.Tasks;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Extensions;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Helpers;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models;

    /// <summary>
    /// Privacy-Experience Client
    /// </summary>
    public partial class PrivacyExperienceClient : IUserSettingsClient
    {
        /// <summary>
        /// Gets the settings asynchronously.
        /// </summary>
        /// <param name="args">The get-settings arguments.</param>
        /// <param name="method">The HttpMethod.</param>
        /// <returns>A success result contains <see cref="ResourceSettingV1"/></returns>
        public async Task<ResourceSettingV1> GetUserSettingsAsync(GetUserSettingsArgs args, HttpMethod method = default)
        {
            args.ThrowOnNull("args");
            args.UserProxyTicket.ThrowOnNull("userProxyTicket");

            HttpRequestMessage request = await PrivacyExperienceRequestHelper.CreateS2SRequestAsync(
                method == default ? HttpMethod.Get : method,
                PrivacyExperienceUrlHelper.CreateGetSettingsPathV1(),
                OperationNames.GetSettings,
                args.RequestId,
                args.CorrelationVector,
                this.authClient,
                args.UserProxyTicket,
                args.FamilyTicket);

            HttpResponseMessage response = await this.httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead);

            return await HttpHelper.HandleHttpResponseAsync<ResourceSettingV1>(response);
        }

        /// <summary>
        /// Update the settings asynchronously.
        /// </summary>
        /// <param name="args">The arguments.</param>
        ///  <returns>A Task</returns>
        public async Task<ResourceSettingV1> UpdateUserSettingsAsync(UpdateUserSettingsArgs args)
        {
            args.ThrowOnNull(nameof(args));
            args.UserProxyTicket.ThrowOnNull(nameof(args.UserProxyTicket));
            args.ResourceSettings.ThrowOnNull(nameof(args.ResourceSettings));
            args.ResourceSettings.ETag.ThrowOnNull(nameof(args.ResourceSettings.ETag));

            HttpRequestMessage request = await PrivacyExperienceRequestHelper.CreateS2SRequestAsync(
                new HttpMethod("PATCH"), 
                PrivacyExperienceUrlHelper.CreatePatchSettingsPathV1(),
                OperationNames.UpdateSettings,
                args.RequestId,
                args.CorrelationVector,
                this.authClient,
                args.UserProxyTicket,
                args.FamilyTicket);

            request.Content = new ObjectContent<ResourceSettingV1>(args.ResourceSettings, new JsonMediaTypeFormatter());
            request.Headers.TryAddWithoutValidation(IfMatchHeader, args.ResourceSettings.ETag);

            HttpResponseMessage response = await this.httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead);

            return await HttpHelper.HandleHttpResponseAsync<ResourceSettingV1>(response);
        }
    }
}