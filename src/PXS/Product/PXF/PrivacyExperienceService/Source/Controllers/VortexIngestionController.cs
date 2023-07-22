// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.Core.Vortex;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Extensions;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Security;

    /// <inheritdoc />
    /// <summary>
    ///     Vortex controller for handling
    /// </summary>
    [Authorize]
    [PrivacyExperienceVortexAuthorization]
    public class VortexIngestionController : ApiController
    {
        /// <summary>
        ///     The vortex event service
        /// </summary>
        private readonly IVortexEventService vortexEventService;

        /// <inheritdoc />
        /// <summary>
        ///     Initializes an instance of the <see cref="T:Microsoft.Membership.MemberServices.PrivacyExperience.Service.Controllers.VortexIngestionController" /> class
        /// </summary>
        /// <param name="vortexEventService"> Implementation of <see cref="T:Microsoft.Membership.MemberServices.Privacy.Core.Vortex.IVortexEventService" /> for handling vortex events</param>
        public VortexIngestionController(IVortexEventService vortexEventService)
        {
            this.vortexEventService = vortexEventService;
        }

        /// <summary>
        ///     Delete all privacy data for a specific device.
        /// </summary>
        /// <returns>The result</returns>
        /// <group>Vortex Ingestion</group>
        /// <verb>post</verb>
        /// <url>https://pxs.api.account.microsoft.com/v1/vortex/devicedelete</url>        
        /// <response code="200"></response>
        [HttpPost]
        [Route(RouteNames.VortexIngestionDeviceDeleteV1)]
        public async Task<IHttpActionResult> DeleteDeviceAsync()
        {
            HttpContent content = this.Request.Content;
            var info = new VortexRequestInformation();

            // Contents may be compressed
            if (content.Headers.TryGetValues(HeaderNames.ContentEncoding, out IEnumerable<string> encodings))
            {
                // Content-Encoding can come in multiples in the order that they were applied 
                foreach (string encoding in encodings)
                {
                    // Currently we only expect to see GZip
                    if (string.Equals(encoding, ContentCodings.GZip, StringComparison.InvariantCultureIgnoreCase))
                    {
                        content = await content.DecompressGZip().ConfigureAwait(false);
                        info.WasCompressed = true;
                    }
                }
            }

            if (this.Request.Headers.TryGetValues(HeaderNames.VortexServedBy, out IEnumerable<string> serverNames))
            {
                // Expecting only one server name
                info.ServedBy = serverNames.FirstOrDefault();
                info.HadServerName = true;
            }

            if (this.Request.Headers.TryGetValues(HeaderNames.UserAgent, out IEnumerable<string> userAgents))
            {
                // Expecting only one user agent value
                info.UserAgent = userAgents.FirstOrDefault();
                info.HadUserAgent = true;
            }

            info.IsWatchdogRequest = this.Request.Headers.Contains(HeaderNames.WatchdogRequest);

            // Set request time as server time
            info.RequestTime = DateTimeOffset.UtcNow;

            byte[] jsonEvents = await content.ReadAsByteArrayAsync().ConfigureAwait(false);
            ServiceResponse response = await this.vortexEventService.QueueValidEventsAsync(jsonEvents, info).ConfigureAwait(false);

            return this.CreateHttpActionResult(response);
        }

        /// <summary>
        ///     Creates the http action result.
        /// </summary>
        /// <param name="serviceResponse">The service response.</param>
        /// <returns>Response Message Result</returns>
        private IHttpActionResult CreateHttpActionResult(ServiceResponse serviceResponse)
        {
            if (serviceResponse == null)
            {
                return this.InternalServerError();
            }

            if (serviceResponse.IsSuccess)
            {
                return this.Ok();
            }

            return this.ResponseMessage(this.Request.CreateErrorResponse(serviceResponse.Error));
        }
    }
}
