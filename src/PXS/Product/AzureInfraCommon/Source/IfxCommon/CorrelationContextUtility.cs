// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.AzureInfraCommon.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;

    /// <summary>
    ///     Utility methods for Correlation Context
    /// </summary>
    public static class CorrelationContextUtility
    {
        public const string CorrelationContextHeaderName = "X-CorrelationContext";
        public const string RootActivityIdHeaderName = "x-ms-client-request-id";

        /// <summary>
        ///     Receives a correlation context from request header and sets it in Ifx
        /// </summary>
        /// <param name="request">HTTP request</param>
        /// <param name="correlationContext">Correlation Context instance</param>
        public static void PopulateCorrelationContextFromRequest(
            HttpRequestMessage request,
            ICorrelationContext correlationContext)
        {
            IEnumerable<string> headerValues;
            if (request.Headers.TryGetValues(CorrelationContextUtility.CorrelationContextHeaderName, out headerValues))
            {
                string value = headerValues.FirstOrDefault();
                if (string.IsNullOrEmpty(value) == false)
                {
                    correlationContext.Set(value);
                }
            }       
        }

        /// <summary>
        ///     Retrieves a correlation context from Ifx and adds it to a HTTP equest
        /// </summary>
        /// <param name="request">HTTP request</param>
        /// <param name="correlationContext">Correlation Context instance</param>
        public static void AddCorrelationContextToRequestMessage(
            HttpRequestMessage request,
            ICorrelationContext correlationContext)
        {
            if (correlationContext != null)
            {
                try
                {
                    request.Headers.Add(CorrelationContextUtility.CorrelationContextHeaderName, correlationContext.GetString());
                }
                catch (ArgumentException)
                {
                    // Swallow the exception since Correlation Context failure is not a fatal error
                }
            }
        }
    }
}
