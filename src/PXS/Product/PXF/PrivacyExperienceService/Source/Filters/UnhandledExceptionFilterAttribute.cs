// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.Filters
{
    using System;
    using System.Web.Http.Filters;
    using Microsoft.AspNet.OData.Extensions;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.Helpers;
    using Microsoft.Membership.MemberServices.PrivacyAdapters;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Extensions;

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
            Error error;
            PxfAdapterException pxfAdapterException = actionExecutedContext.Exception as PxfAdapterException;

            if (pxfAdapterException != null)
            {
                int statusCode = pxfAdapterException.GetHttpCode();

                if (statusCode > 499 && statusCode < 600)
                {
                    // treat 5xx errors as partner errors
                    this.logger.Error(ComponentName, pxfAdapterException, $"{statusCode} error from outbound request.");
                    error = new Error(ErrorCode.PartnerError, pxfAdapterException.ToString());
                }
                else if (statusCode == 429)
                {
                    // maps 429 to a specific error code, since some outbound partners (ie MSH) limit requests
                    this.logger.Warning(ComponentName, pxfAdapterException, $"{statusCode} returned from outbound request.");
                    error = new Error(ErrorCode.TooManyRequests, pxfAdapterException.ToString());
                }
                else if (
                    statusCode == 400 &&
                    string.Equals(pxfAdapterException.PartnerName, PrivacySourceId.PdpSearchHistory, StringComparison.OrdinalIgnoreCase) && // For now, very targeted, only search history
                    pxfAdapterException.AdapterErrorCode == AdapterErrorCode.PdpSearchHistoryBotDetection) // to target the bot detection throttling search history does
                {
                    // maps bot detection in PDP to a too many requests code.
                    this.logger.Warning(ComponentName, pxfAdapterException, $"{statusCode} indicating bot detection returned from outbound request.");
                    error = new Error(ErrorCode.TooManyRequests, pxfAdapterException.ToString());
                }
                else
                {
                    // anything else is unknown
                    this.logger.Error(ComponentName, pxfAdapterException, "Unhandled PxfAdapterException");
                    error = new Error(ErrorCode.Unknown, pxfAdapterException.ToString());
                }
            }
            else if (actionExecutedContext.Exception is OperationCanceledException)
            {
                this.logger.Error(ComponentName, actionExecutedContext.Exception, "Unhandled OperationCanceledException");
                error = new Error(ErrorCode.PartnerTimeout, actionExecutedContext.Exception.ToString());
            }
            else
            {
                this.logger.Error(ComponentName, actionExecutedContext.Exception, "Unhandled exception");
                error = new Error(ErrorCode.Unknown, actionExecutedContext.Exception.ToString());
            }

            actionExecutedContext.Response = actionExecutedContext.Request.CreateErrorResponse(error);
        }
    }
}