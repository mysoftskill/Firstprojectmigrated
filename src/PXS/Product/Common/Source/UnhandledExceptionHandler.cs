// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Common
{
    using System.Diagnostics;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http.ExceptionHandling;
    using Microsoft.Membership.MemberServices.Common.Utilities;

    // DelegatingHandlers do not catch exceptions thrown by controller actions because they are automatically consumed and converted to HttpResponseMessages .
    // Use IExceptionHandler as they are guaranteed to see exceptions before they are converted.
    // https://www.asp.net/web-api/overview/error-handling/web-api-global-error-handling
    public class UnhandledExceptionHandler : IExceptionHandler
    {
        public Task HandleAsync(ExceptionHandlerContext context, CancellationToken cancellationToken)
        {
            // context.ExceptionContext.ActionContext is not populated at this point. Cannot retrieve action name.
            Trace.TraceError(
                "Unhandled exception caught in catch-block \"{0}\" from controller \"{1}\". Exception = {2}", 
                context?.CatchBlock?.ToString() ?? "<unknown catch block>", 
                context?.ExceptionContext?.ControllerContext?.ControllerDescriptor?.ControllerName ?? "<unknown controller>", 
                context?.Exception?.ToString() ?? "<unknown exception>");

            HttpResponseMessage response = context.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, context.Exception);
            context.Result = response.ToHttpActionResult();
            return TaskUtilities.CompletedTask;
        }
    }
}
