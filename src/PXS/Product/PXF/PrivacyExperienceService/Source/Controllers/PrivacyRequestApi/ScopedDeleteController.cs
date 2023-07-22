// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.Controllers
{
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Privacy.Core.ScopedDelete;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.PrivacySubject;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Security;

    // This using should not be used: (Check Delete/Export V1 vs V2 endpoints, they use different namespaces with the same named types)
    // Instead make sure the using above (Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.PrivacySubject) is kept
    //// using Microsoft.PrivacyServices.PrivacyOperation.Contracts.PrivacySubject;

    /// <summary>
    ///     This controller handles ScopedDelete requests from Bing
    /// </summary>
    [Authorize]
    public class ScopedDeleteController : PrivacyController
    {
        private readonly IScopedDeleteService scopedDeleteService;

        /// <summary>
        ///     Creates a new <see cref="ScopedDeleteController"/>
        /// </summary>
        public ScopedDeleteController(IScopedDeleteService scopedDeleteService)
        {
            this.scopedDeleteService = scopedDeleteService;
        }

        /// <summary>
        ///     This allows a partner to execute a scoped delete for search requests and query.
        /// </summary>
        /// <group>Scoped Delete Api</group>
        /// <verb>post</verb>
        /// <url>https://pxs.api.account.microsoft.com/v1/scopeddelete/searchrequestsandquery</url>        
        /// <header name="X-Family-Json-Web-Token" cref="string" required="false">The family JWT with information required for on behalf of.</header>
        /// <header name="client-request-id" cref="string" required="false">The client activity ID.</header>
        /// <header name="MS-CV" cref="string" required="false">The correlation vector for the request.</header>
        /// <header name="Correlation-Context" cref="string" required="false">The correlation context for the request.</header>
        /// <header name="X-Flights" cref="string" required="false">The flights to apply to the request applied.</header>
        /// <header name="server-machine-id" cref="string" required="false">The server machine ID.</header>
        /// <header name="X-S2S-Proxy-Ticket" cref="string" required="false">The MSA User Proxy Ticket.</header>
        /// <header name="X-S2S-Access-Token" cref="string" required="false">The MSA S2S Access Token.</header>
        /// <param in="body" name="searchRequestsAndQueryIds" required="false" cref="T:System.String[]">A list of impression IDs.</param>
        /// <response code="200"></response>
        [HttpPost]
        [Route(RouteNames.ScopedDeleteSearchRequestsAndQuery)]
        [PrivacyExperienceIdentityAuthorization(typeof(AadIdentityWithMsaUserProxyTicket))]
        [PrivacyExperienceBingAuthorization]
        public async Task<IHttpActionResult> SearchRequestsAndQueryScopedDeleteAsync([FromBody] string[] searchRequestsAndQueryIds)
        {
            ServiceResponse response = await this.scopedDeleteService.SearchRequestsAndQueryScopedDeleteAsync(this.CurrentRequestContext, searchRequestsAndQueryIds).ConfigureAwait(false);
            return this.CreateHttpActionResult(response);
        }

        /// <summary>
        ///     This allows a partner to execute a bulk scoped delete for search requests and query.
        /// </summary>
        /// <group>Scoped Delete Api</group>
        /// <verb>post</verb>
        /// <url>https://pxs.api.account.microsoft.com/v1/bulkscopeddelete/searchrequestsandquery</url>        
        /// <header name="X-Family-Json-Web-Token" cref="string" required="false">The family JWT with information required for on behalf of.</header>
        /// <header name="client-request-id" cref="string" required="false">The client activity ID.</header>
        /// <header name="MS-CV" cref="string" required="false">The correlation vector for the request.</header>
        /// <header name="Correlation-Context" cref="string" required="false">The correlation context for the request.</header>
        /// <header name="X-Flights" cref="string" required="false">The flights to apply to the request applied.</header>
        /// <header name="server-machine-id" cref="string" required="false">The server machine ID.</header>
        /// <header name="X-S2S-Proxy-Ticket" cref="string" required="false">The MSA User Proxy Ticket.</header>
        /// <header name="X-S2S-Access-Token" cref="string" required="false">The MSA S2S Access Token.</header>
        /// <response code="200"></response>
        [HttpPost]
        [Route(RouteNames.BulkScopedDeleteSearchRequestsAndQuery)]
        [PrivacyExperienceIdentityAuthorization(typeof(AadIdentityWithMsaUserProxyTicket))]
        [PrivacyExperienceBingAuthorization]
        public async Task<IHttpActionResult> BulkSearchRequestsAndQueryScopedDeleteAsync()
        {
            // A null list of IDs result in all history being deleted.
            ServiceResponse response = await this.scopedDeleteService.SearchRequestsAndQueryScopedDeleteAsync(this.CurrentRequestContext, null).ConfigureAwait(false);
            return this.CreateHttpActionResult(response);
        }
    }
}
