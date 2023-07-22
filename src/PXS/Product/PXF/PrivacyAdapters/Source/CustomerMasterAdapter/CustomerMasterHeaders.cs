// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.CustomerMasterAdapter
{
    /// <summary>
    ///     CustomerMaster-Headers
    /// </summary>
    public static class CustomerMasterHeaders
    {
        internal const string ApiVersion = "api-version";

        internal const string AuthHeaderMSAAuth1 = "MSAAuth1.0";

        internal const string CorrelationId = "x-ms-correlation-id";

        /// <summary>
        ///     Header name for forwarding the Family Service JWT
        /// </summary>
        internal const string FamilyService = "x-ms-jwt";

        internal const string IfMatch = "If-Match";

        internal const string TrackingId = "x-ms-tracking-id";
    }
}
