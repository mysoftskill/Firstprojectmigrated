// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts
{
    /// <summary>
    ///     HeaderNames used by Privacy Experience Service
    /// </summary>
    public static class HeaderNames
    {
        /// <summary>
        ///     Privacy-Experience-Service service-to-service access-token http-header-name.
        /// </summary>
        public const string AccessToken = "X-S2S-Access-Token";

        /// <summary>
        ///     Privacy-Experience-Service client-request-id http-header-name.
        /// </summary>
        public const string ClientRequestId = "client-request-id";

        /// <summary>
        ///     The correlation vector header-name.
        /// </summary>
        public const string CorrelationVector = "MS-CV";

        /// <summary>
        ///     The correlation context header-name.
        /// </summary>
        public const string CorrelationContext = "Correlation-Context";

        /// <summary>
        ///     Privacy-Experience-Service - family on-behalf-of token http header name.
        /// </summary>
        public const string FamilyTicket = "X-Family-Json-Web-Token";

        /// <summary>
        ///     The flights HTTP header name.
        /// </summary>
        public const string Flights = "X-Flights";

        /// <summary>
        ///     Privacy-Experience-Service server-machine-id http-header-names.
        /// </summary>
        public const string MachineId = "server-machine-id";

        /// <summary>
        ///     Privacy-Experience-Service  service-to-service proxy-token http-header-name.
        /// </summary>
        public const string ProxyTicket = "X-S2S-Proxy-Ticket";

        /// <summary>
        ///     Privacy-Experience-Service server-version http-header-names.
        /// </summary>
        public const string ServerVersion = "server-version";

        /// <summary>
        ///     The watchdog request header-name.
        /// </summary>
        public const string WatchdogRequest = "X-PXS-WATCHDOG";

        /// <summary>
        ///     GDPR Operation Location header-name.
        /// </summary>
        public const string OperationLocation = "Operation-Location";

        /// <summary>
        ///     GDPR Retry After header-name.
        /// </summary>
        public const string RetryAfter = "Retry-After";

        /// <summary>
        ///     The content encoding of the HTTP contents. 
        /// </summary>
        public const string ContentEncoding = "Content-Encoding";

        /// <summary>
        ///     The custom vortex header to identify the current vortex machine being talked to
        /// </summary>
        public const string VortexServedBy = "X-Served-By";

        /// <summary>
        ///     The user agent value that comes from Vortex
        /// </summary>
        public const string UserAgent = "User-Agent";

        /// <summary>
        ///     The custom header for the targer ObjectId in AAD Privacy Requests.
        /// </summary>
        public const string TargetObjectId = "X-TargetOid";

        /// <summary>
        ///     The header for ms graph service root.
        /// </summary>
        public const string MsGraphServiceRoot = "x-ms-gateway-serviceRoot";

        /// <summary>
        ///     MS Graph Location header-name.
        /// </summary>
        public const string Location = "Location";

        /// <summary>
        ///     Privacy-Experience-Service request-id http-header-name.
        /// </summary>
        public const string RequestId = "request-id";
    }
}
