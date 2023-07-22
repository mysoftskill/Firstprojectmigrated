// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.Controllers
{
    using System.Web.Http;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Extensions;

    /// <summary>
    ///     Privacy Controller
    /// </summary>
    public abstract class PrivacyController : ApiController
    {
        /// <summary>
        ///     If match header
        /// </summary>
        protected const string IfMatchHeader = "If-Match";

        private RequestContext currentRequestContext;

        /// <summary>
        ///     Gets the current request context.
        /// </summary>
        protected RequestContext CurrentRequestContext
        {
            get
            {
                if (this.currentRequestContext == null)
                {
                    this.currentRequestContext = this.GetCurrentUserRequestContext();
                }

                return this.currentRequestContext;
            }
        }

        /// <summary>
        ///     Creates the http action result.
        /// </summary>
        /// <param name="serviceResponse">The service response.</param>
        /// <returns>Response Message Result</returns>
        protected IHttpActionResult CreateHttpActionResult<T>(ServiceResponse<T> serviceResponse)
        {
            if (serviceResponse == null)
            {
                return this.InternalServerError();
            }

            if (serviceResponse.IsSuccess)
            {
                return this.Ok(serviceResponse.Result);
            }

            return this.ResponseMessage(this.Request.CreateErrorResponse(serviceResponse.Error));
        }

        /// <summary>
        ///     Creates the http action result.
        /// </summary>
        /// <param name="serviceResponse">The service response.</param>
        /// <returns>Response Message Result</returns>
        protected IHttpActionResult CreateHttpActionResult(ServiceResponse serviceResponse)
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
