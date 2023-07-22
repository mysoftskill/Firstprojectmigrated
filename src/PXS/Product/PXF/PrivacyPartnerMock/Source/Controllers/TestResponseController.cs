// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.VortexListenerService
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http;

    [RoutePrefix("test/response")]
    public class TestResponseController : ApiController
    {
        // test/response
        // test/response?delay=1000
        // test/response/500
        // test/response/500?delay=1000
        [Route("{statusCode:int?}")]
        [OverrideAuthentication]
        [AllowAnonymous]
        public async Task<IHttpActionResult> Get(HttpStatusCode statusCode = HttpStatusCode.OK, int delay = 0)
        {
            await Task.Delay(delay);

            return this.StatusCode(statusCode);
        }
    }
}
