// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Common
{
    /// <summary>
    ///     Miscellaneous MVS http-header-names.
    /// </summary>
    public static class HeaderNames
    {
        #region Azure REST API Guideline Headers

        /// <summary>
        ///     Member-services client-request-id http-header-names.
        /// </summary>
        public const string ClientRequestId = "client-request-id";

        /// <summary>
        ///     Member-services return-client-request-id http-header-names.
        /// </summary>
        public const string ReturnClientRequestId = "return-client-request-id";

        /// <summary>
        ///     Member-services request-id http-header-names.
        /// </summary>
        public const string ServerRequestId = "request-id";

        /// <summary>
        ///     Member-services server-machine-id http-header-names.
        /// </summary>
        public const string MachineId = "server-machine-id";

        /// <summary>
        ///     Member-services server-version http-header-names.
        /// </summary>
        public const string ServerVersion = "server-version";

        #endregion

        #region MemberServices Headers

        /// <summary>
        ///     Member-services service-to-service access-token http-header-names.
        /// </summary>
        public const string AccessToken = "X-S2S-Access-Token";

        /// <summary>
        ///     Member-services service-to-service ticket for a partner site (used in INT environments for calling production M$ endpoints)
        /// </summary>
        public const string PartnerTicket = "X-S2S-Partner-Ticket";

        /// <summary>
        ///     Member-services service-to-service proxy-token http-header-names.
        /// </summary>
        public const string ProxyTicket = "X-S2S-Proxy-Ticket";

        /// <summary>
        ///     Member-services user PUID http-header-names.
        /// </summary>
        public const string Puid = "PUID";

        /// <summary>
        ///     Member-services user CID http-header-names.
        /// </summary>
        public const string Cid = "CID";

        /// <summary>
        ///     Member-services flight ids.
        /// </summary>
        public const string FlightIdentifier = "x-mvs-flight-id";

        /// <summary>
        ///     The flights HTTP header name.
        /// </summary>
        public const string Flights = "X-Flights";

        /// <summary>
        ///     The watchdog request header-name.
        /// </summary>
        public const string WatchdogRequest = "X-PXS-WATCHDOG";

        /// <summary>
        ///     Member-services subscription-service risk-service risk-token http-header-names.
        /// </summary>
        public const string RiskToken = "x-mvs-risk-token";

        /// <summary>
        ///     Member-services subscription-service risk-service test-in-production http-header-names.
        /// </summary>
        public const string RiskTests = "x-mvs-risk-testscenarios";

        /// <summary>
        ///     Member-services subscription-service risk-service session-id http-header-names.
        /// </summary>
        public const string RiskSessionId = "x-mvs-risk-sessionid";

        /// <summary>
        ///     Member-services subscription-service get-refund-amount test-in-production http-header-names.
        /// </summary>
        public const string RefundAmount = "x-mvs-refund-amount";

        /// <summary>
        ///     Member-services subscription-service test-in-production http-header-names.
        /// </summary>
        public const string SubscriptionTests = "x-mvs-subscription-testscenarios";

        #endregion

        #region Standard Headers

        /// <summary>
        ///     The accept http-header-names.
        /// </summary>
        public const string Accept = "Accept";

        /// <summary>
        ///     The accept-language http-header-names.
        /// </summary>
        public const string AcceptLanguage = "Accept-Language";

        /// <summary>
        ///     The authorization http-header-names.
        /// </summary>
        public const string Authorization = "Authorization";

        /// <summary>
        ///     The accept-encoding http-header-names
        /// </summary>
        public const string AcceptEncoding = "Accept-Encoding";

        #endregion
    }
}
