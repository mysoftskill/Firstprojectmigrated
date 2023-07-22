// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.Controllers
{
    using System.Threading.Tasks;
    using System.Web.Http;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Privacy.Core.UserSettings;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Security;

    /// <summary>
    ///     UserSettings V1 Controller
    /// </summary>
    [Authorize]
    [CorrelationVectorRequired]
    public class UserSettingsV1Controller : MsaOnlyPrivacyController
    {
        private readonly IUserSettingsService userSettingsService;

        /// <summary>
        ///     Initializes a new instance of the <see cref="UserSettingsV1Controller" /> class.
        /// </summary>
        /// <param name="userSettingsService">The user settings.</param>
        public UserSettingsV1Controller(IUserSettingsService userSettingsService)
        {
            this.userSettingsService = userSettingsService;
        }

        /// <summary>
        ///     Gets the user's resource settings.
        /// </summary>
        /// <group>User Settings V1</group>
        /// <verb>get</verb>
        /// <url>https://pxs.api.account.microsoft.com/v1/settings</url>       
        /// <header name="X-Family-Json-Web-Token" cref="string" required="false">The family JWT with information required for on behalf of.</header>
        /// <header name="client-request-id" cref="string" required="false">The client activity ID.</header>
        /// <header name="MS-CV" cref="string" required="false">The correlation vector for the request.</header>
        /// <header name="Correlation-Context" cref="string" required="false">The correlation context for the request.</header>
        /// <header name="X-Flights" cref="string" required="false">The flights to apply to the request applied.</header>
        /// <header name="server-machine-id" cref="string" required="false">The server machine ID.</header>
        /// <header name="X-S2S-Proxy-Ticket" cref="string" required="true">The MSA User Proxy Ticket.</header>
        /// <header name="X-S2S-Access-Token" cref="string" required="true">The MSA S2S Access Token.</header>
        /// <response code="200"><see cref="ResourceSettingV1" />The user's resource settings.</response>
        [HttpGet]
        [Route(RouteNames.GetSettingsV1)]
        public async Task<IHttpActionResult> GetAsync()
        {
            ServiceResponse<ResourceSettingV1> response = await this.userSettingsService.GetOrCreateAsync(this.CurrentRequestContext);

            if (response?.IsSuccess == true)
            {
                return this.CreateHttpActionResult(response);
            }

            if (response?.Error.Code?.Equals(ErrorCode.ResourceNotFound.ToString()) == true ||
                response?.Error.Code?.Equals(ErrorCode.UpdateConflict.ToString()) == true ||
                response?.Error.Code?.Equals(ErrorCode.CreateConflict.ToString()) == true)
            {
                response = await this.userSettingsService.GetAsync(this.CurrentRequestContext);
            }

            return this.CreateHttpActionResult(response);
        }

        /// <summary>
        ///     Updates the resource settings for the user
        /// </summary>
        /// <group>User Settings V1</group>
        /// <verb>patch</verb>
        /// <url>https://pxs.api.account.microsoft.com/v1/settings</url>        
        /// <header name="client-request-id" cref="string" required="false">The client activity ID.</header>
        /// <header name="MS-CV" cref="string" required="false">The correlation vector for the request.</header>
        /// <header name="Correlation-Context" cref="string" required="false">The correlation context for the request.</header>
        /// <header name="X-Flights" cref="string" required="false">The flights to apply to the request applied.</header>
        /// <header name="server-machine-id" cref="string" required="false">The server machine ID.</header>
        /// <header name="X-S2S-Proxy-Ticket" cref="string" required="true">The MSA User Proxy Ticket.</header>
        /// <header name="X-S2S-Access-Token" cref="string" required="true">The MSA S2S Access Token.</header>
        /// <param name="patchSettingsRequest" in="body"><see cref="ResourceSettingV1" />The new resource settings to patch.</param>
        /// <response code="200"><see cref="ResourceSettingV1" />The nresulting resource settings.</response>
        [HttpPatch, PrivacyExperienceAgeAuthZAuthorization(typeof(AgeAuthZLegalAgeGroup), PrivacyAction.View)]
        [Route(RouteNames.UpdateSettingsV1)]
        public async Task<IHttpActionResult> PatchAsync([FromBody] ResourceSettingV1 patchSettingsRequest)
        {
            ServiceResponse<ResourceSettingV1> response = await this.userSettingsService.UpdateAsync(this.CurrentRequestContext, patchSettingsRequest).ConfigureAwait(false);

            return this.CreateHttpActionResult(response);
        }
    }
}
