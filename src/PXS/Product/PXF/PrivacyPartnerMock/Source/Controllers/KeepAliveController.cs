// <copyright company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyMockService.Controllers
{
    using System.Web.Http;

    /// <summary>
    /// A controller for keep alive ping.
    /// </summary>
    [Route("keepalive")]
    public class KeepAliveController : ApiController
    {
        /// <summary>
        /// An API that always returns an empty HTTP statuc code 200 OK success response.
        /// Use this as a heartbeat for the service and register with software load balancer
        /// to maintain VIP status.
        /// </summary>
        /// <returns>An action result representing a 200 OK success response.</returns>
        [OverrideAuthentication]
        [AllowAnonymous]
        public IHttpActionResult Get()
        {
            // OkResult does not contain a body
            // https://msdn.microsoft.com/en-us/library/system.web.http.results.aspx
            return this.Ok();
        }
    }
}
