// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.Controllers
{
    using System.Web.Http;
    using Microsoft.AspNet.OData;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Extensions;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Filters;

    /// <summary>
    ///     Odata Privacy Controller.
    /// </summary>
    [OdataUnhandledExceptionFilter]
    public class ODataPrivacyController : ODataController
    {
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

            return this.ResponseMessage(this.Request.CreateODataErrorResponse(serviceResponse.Error, true));
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

            return this.ResponseMessage(this.Request.CreateODataErrorResponse(serviceResponse.Error, true));
        }
    }
}
