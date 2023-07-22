// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.Controllers
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Web.Http;
    using System.Web.Http.Results;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Host;

    /// <summary>
    ///     A controller for keep alive ping.
    /// </summary>
    public class KeepAliveController : ApiController
    {
        private readonly IAppConfiguration appConfiguration;

        public KeepAliveController(IAppConfiguration appConfiguration)
        {
            this.appConfiguration = appConfiguration;
        }

        /// <summary>
        ///     An API that always returns an empty HTTP status code 200 OK success response.
        ///     Use this as a heartbeat for the service and register with software load balancer
        ///     to maintain VIP status.
        /// </summary>
        /// <remarks>May return <see cref="HttpStatusCode">NotFound</see> if the server is preparing to stop.</remarks>
        /// <returns>An action result representing a 200 OK success response..</returns>
        /// <group>Keep Alive</group>
        /// <verb>get</verb>
        /// <url>https://pxs.api.account.microsoft.com/keepalive</url>        
        /// <response code="200"></response>
        [HttpGet]
        [OverrideAuthentication]
        [AllowAnonymous]
        [Route(RouteNames.KeepAliveRoute)]
        public IHttpActionResult Get()
        {
            if (HostApplication.Instance?.CancellationToken.IsCancellationRequested == true)
            {
                // Report as caller error to avoid tripping QOS alarms
                return new ResponseMessageResult(this.Request.CreateResponse(HttpStatusCode.NotFound, "This process is preparing to stop."));
            }

            // OkResult does not contain a body
            // https://msdn.microsoft.com/en-us/library/system.web.http.results.aspx
            return this.Ok();
        }

        [HttpGet]
        [OverrideAuthentication]
        [AllowAnonymous]
        [Route("featureflag/{feature}/{value?}/{key?}")]
        public HttpResponseMessage GetFeatureFlag(string feature, string value=null, string key=null)
        {
            try
            {
                bool enabled = false;
                if (key != null)
                {
                    enabled = appConfiguration.IsFeatureFlagEnabledAsync(feature, CustomOperatorContextFactory.CreateDefaultStringComparisonContextWithKeyValue(key, value)).GetAwaiter().GetResult();
                }
                else if(value != null)
                {
                    enabled = appConfiguration.IsFeatureFlagEnabledAsync(feature, CustomOperatorContextFactory.CreateDefaultStringComparisonContext(value)).GetAwaiter().GetResult();
                }
                else 
                {
                    // Temp test hook, will remove
                    enabled = appConfiguration.IsFeatureFlagEnabledAsync(feature).GetAwaiter().GetResult();
                }
                
                HttpResponseMessage response = this.Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(enabled.ToString(), Encoding.UTF8, "text/html");

                return response;
            }
            catch (Exception ex)
            {
                HttpResponseMessage response = this.Request.CreateResponse(HttpStatusCode.InternalServerError);
                response.Content = new StringContent(ex.Message, Encoding.UTF8, "text/html");
                return response;
            }
        }
    }
}
