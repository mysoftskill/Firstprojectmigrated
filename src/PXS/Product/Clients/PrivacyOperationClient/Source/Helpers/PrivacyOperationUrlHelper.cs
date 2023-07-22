// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.PrivacyOperation.Client.Helpers
{
    using System;
    using System.Net;

    /// <summary>
    ///     PrivacyOperationUrlHelper
    /// </summary>
    public static class PrivacyOperationUrlHelper
    {
        /// <summary>
        ///     Creates the list path v1.
        /// </summary>
        /// <returns></returns>
        public static Uri CreateListPathV1(string requestTypes)
        {
            string query = string.Empty;
            if (!string.IsNullOrEmpty(requestTypes))
                query = $"?requestTypes={requestTypes}";
            return new Uri("v1/privacyrequest/list" + query, UriKind.Relative);
        }

        /// <summary>
        ///     Creates the delete request path v1.
        /// </summary>
        /// <param name="dataTypes">Data types.</param>
        /// <param name="startTime">Start time.</param>
        /// <param name="endTime">End time.</param>
        /// <returns></returns>
        public static Uri CreatePostDeletePathV1(string dataTypes, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null)
        {
            return new Uri($"v2/privacyrequest/delete?dataTypes={dataTypes}&startTime={WebUtility.UrlEncode(startTime.ToString())}&endTime={WebUtility.UrlEncode(endTime.ToString())}", UriKind.Relative);
        }

        /// <summary>
        ///     Creates the export request path v1.
        /// </summary>
        /// <param name="dataTypes">Data types.</param>
        /// <param name="startTime">Start time.</param>
        /// <param name="endTime">End time.</param>
        /// <returns></returns>
        public static Uri CreatePostExportPathV1(string dataTypes, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null)
        {
            return new Uri($"v2/privacyrequest/export?dataTypes={dataTypes}&startTime={WebUtility.UrlEncode(startTime.ToString())}&endTime={WebUtility.UrlEncode(endTime.ToString())}", UriKind.Relative);
        }
    }
}
