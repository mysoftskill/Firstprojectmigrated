// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyMockService.Security
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Web.Http.Filters;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Unhandled exception AspNet filter atttribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public sealed class UnhandledExceptionFilterAttribute : ExceptionFilterAttribute
    {
        private const string ComponentName = nameof(UnhandledExceptionFilterAttribute);

        /// <summary>
        /// The logger instance.
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnhandledExceptionFilterAttribute"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public UnhandledExceptionFilterAttribute(ILogger logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Raises the exception event.
        /// </summary>
        /// <param name="actionExecutedContext">The context for the action.</param>
        public override void OnException(HttpActionExecutedContext actionExecutedContext)
        {
            this.logger.Error(ComponentName, actionExecutedContext.Exception, "Unhandled exception");
            Error error = new Error(ErrorCode.Unknown, actionExecutedContext.Exception.ToString());
            actionExecutedContext.Response = actionExecutedContext.Request.CreateResponse(HttpStatusCode.InternalServerError, error);
        }
    }
}