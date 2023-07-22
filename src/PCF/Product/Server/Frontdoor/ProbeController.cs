namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Web.Http;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    /// <summary>
    /// Controller for probing the PCF service.
    /// </summary>
    [RoutePrefix("")]
    public class ProbeController : ApiController
    {
        /// <summary>
        /// A basic HTTP endpoint that returns OK.
        /// </summary>        
        /// <url>https://pcf.privacy.microsoft.com/keepalive</url>
        /// <verb>get</verb>
        /// <group>Probe</group>
        /// <response code="200"></response>
        [HttpGet]
        [Route("keepalive")]
        [IncomingRequestActionFilter("API", "Probe", "1.0")]
        public HttpResponseMessage RunProbe()
        {
            if (PrivacyApplication.Instance?.CancellationToken.IsCancellationRequested == true)
            {
                // Report as caller error to avoid tripping QOS alarms. 498 doesn't mean anything specific.
                return this.Request.CreateResponse((HttpStatusCode)498, "This process is preparing to stop.");
            }

            return this.Request.CreateResponse(HttpStatusCode.OK);
        }
    }
}
