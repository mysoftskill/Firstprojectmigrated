// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Adapters.Common
{
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Helper methods specifically for use by View Adapters.
    /// </summary>
    internal static class ViewAdapterHelper
    {
        public const string GetServiceDetailApiName = "GetServiceDetail";

        /// <summary>
        /// Logs the start of a get service details API execution. Logs the context of the request such as which adapter
        /// is handling the request and input arguments.
        /// </summary>
        /// <param name="logger">The client that executes logging.</param>
        /// <param name="adapter">The adapter that is handling the request.</param>
        /// <param name="userId">The ID of the user that was passed into the API.</param>
        /// <param name="postalCode">The postal code passed into the API.</param>
        public static void LogStart(ILogger logger, string adapter, MsaId userId, string postalCode)
        {
            logger.MethodEnter(adapter, GetServiceDetailApiName);
            logger.Information(adapter, "Input: (UserId: {0}, PostalCode: {1})", userId, postalCode);
        }
    }
}
