// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.Filters
{
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;
    using System.Web.Http.Filters;
    using Microsoft.AspNet.OData.Extensions;
    
    using Microsoft.OData;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     Unhandled exception for ODATA APIs.
    /// </summary>
    public class OdataUnhandledExceptionFilterAttribute : ExceptionFilterAttribute
    {
        /// <inheritdoc />
        public override void OnException(HttpActionExecutedContext context)
        {
            ILogger logger = context.Request?.GetDependencyScope()?.GetService(typeof(ILogger)) as ILogger;
            logger?.Error(nameof(OdataUnhandledExceptionFilterAttribute), context.Exception?.ToString());

            if (context.Exception is TaskCanceledException)
                throw new HttpResponseException(
                    context.Request.CreateErrorResponse(
                        HttpStatusCode.InternalServerError,
                        new ODataError
                        {
                            ErrorCode = "TaskCanceledException",
                            Message = "A partner did not respond within the timeout period."
                        }));

            throw new HttpResponseException(
                context.Request.CreateErrorResponse(
                    HttpStatusCode.InternalServerError,
                    new ODataError
                    {
                        ErrorCode = "Unhandled exception",
                        Message = "The service encountered an unexpected error"
                    }));
        }
    }
}
