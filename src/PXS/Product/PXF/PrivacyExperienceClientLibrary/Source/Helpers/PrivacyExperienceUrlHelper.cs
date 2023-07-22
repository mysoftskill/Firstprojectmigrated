// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Helpers
{
    using System;
    using System.Text;
    using System.Web;

    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;

    /// <summary>
    ///     PrivacyExperience UrlHelper
    /// </summary>
    public static class PrivacyExperienceUrlHelper
    {
        /// <summary>
        ///     Expands the query parameters with the relative path
        /// </summary>
        /// <param name="relativePath">Relative URI</param>
        /// <param name="queryParameters">Query parameters</param>
        /// <returns>Relative Path with Query Parameters</returns>
        private static string ExpandQueryParameters(string relativePath, QueryStringCollection queryParameters)
        {
            var sb = new StringBuilder(relativePath);
            if (queryParameters.Count > 0)
            {
                sb.Append("?");
            }

            bool first = true;
            foreach (string name in queryParameters.AllKeys)
            {
                if (!first)
                {
                    sb.Append("&");
                }

                sb.AppendFormat("{0}={1}", name, HttpUtility.UrlEncode(queryParameters[name]));
                first = false;
            }

            return sb.ToString();
        }

        #region Export

        /// <summary>
        ///     Creates the export request path v1.
        /// </summary>
        /// <returns>The URL for PostExportRequest()</returns>
        public static Uri CreatePostExportRequestPathV1(QueryStringCollection queryParameters)
        {
            return new Uri(ExpandQueryParameters(RouteNames.PostExportRequest, queryParameters), UriKind.Relative);
        }

        /// <summary>
        ///     Creates the post export cancel path v1.
        /// </summary>
        /// <returns>The URL for PostExportCancelRequest()</returns>
        public static Uri CreatePostExportCancelPathV1(QueryStringCollection queryParameters)
        {
            return new Uri(ExpandQueryParameters(RouteNames.PostExportCancel, queryParameters), UriKind.Relative);
        }

        /// <summary>
        ///     Creates the Delete export archives path v1.
        /// </summary>
        /// <returns>The URL for DeleteExportArchiveRequest()</returns>
        public static Uri CreateDeleteExportArchivesPathV1(QueryStringCollection queryParameters)
        {
            return new Uri(ExpandQueryParameters(RouteNames.DeleteExportArchives, queryParameters), UriKind.Relative);
        }

        /// <summary>
        ///     Creates the list export history request path v1.
        /// </summary>
        /// <returns>The URL for ListExportHistory()</returns>
        public static Uri CreateListExportHistoryRequestPathV1()
        {
            return new Uri(RouteNames.ListExportHistory, UriKind.Relative);
        }

        #endregion

        #region Timeline

        /// <summary>
        ///     Creates a url for timeline aggreagte count
        /// </summary>
        /// <returns></returns>
        public static Uri CreateAggregateCountTimelinePathV1(QueryStringCollection queryParameters)
        {
            return new Uri(ExpandQueryParameters(RouteNames.GetTimelineAggregateCount, queryParameters), UriKind.Relative);
        }

        /// <summary>
        ///     Creates a url for timeline
        /// </summary>
        public static Uri CreateGetTimelinePathV3(QueryStringCollection queryParameters)
        {
            return new Uri(ExpandQueryParameters(RouteNames.GetTimelineV3, queryParameters), UriKind.Relative);
        }

        /// <summary>
        ///     Creates a delete url for timeline
        /// </summary>
        public static Uri CreateDeleteTimelinePathV2(QueryStringCollection queryParameters)
        {
            return new Uri(ExpandQueryParameters(RouteNames.DeleteTimelineV2, queryParameters), UriKind.Relative);
        }

        /// <summary>
        ///     Creates a voice card audio url for timeline
        /// </summary>
        public static Uri CreateGetVoiceCardAudioPathV2(QueryStringCollection queryParameters)
        {
            return new Uri(ExpandQueryParameters(RouteNames.GetVoiceCardAudioV2, queryParameters), UriKind.Relative);
        }

        #endregion

        #region Settings

        /// <summary>
        ///     Creates the get-settings path v1.
        /// </summary>
        /// <returns>The URL for GetVoiceHistoryV1()</returns>
        public static Uri CreateGetSettingsPathV1()
        {
            return new Uri(RouteNames.GetSettingsV1, UriKind.Relative);
        }

        /// <summary>
        ///     Creates the get-settings path v1.
        /// </summary>
        /// <returns>The URL for GetVoiceHistoryV1()</returns>
        public static Uri CreatePatchSettingsPathV1()
        {
            return new Uri(RouteNames.UpdateSettingsV1, UriKind.Relative);
        }

        #endregion

        #region Privacy Subject Requests

        /// <summary>
        ///     Creates URL for privacy subject delete request.
        /// </summary>
        public static Uri CreatePrivacySubjectDeleteRequest(QueryStringCollection queryParameters)
        {
            return new Uri(ExpandQueryParameters(RouteNames.PrivacyRequestApiDelete, queryParameters), UriKind.Relative);
        }

        /// <summary>
        ///     Creates URL for privacy subject export request.
        /// </summary>
        public static Uri CreatePrivacySubjectExportRequest(QueryStringCollection queryParameters)
        {
            return new Uri(ExpandQueryParameters(RouteNames.PrivacyRequestApiExport, queryParameters), UriKind.Relative);
        }

        #endregion

        #region Test Page APIs

        /// <summary>
        ///     Creates URL for privacy subject MSA close request.
        /// </summary>
        public static Uri CreatePrivacySubjectTestMsaCloseRequest()
        {
            return new Uri(RouteNames.PrivacyRequestApiTestMsaClose, UriKind.Relative);
        }

        /// <summary>
        ///     Creates URL for getting request by id
        /// </summary>
        public static Uri CreateTestRequestByIdRequest(QueryStringCollection queryParameters)
        {
            return new Uri(ExpandQueryParameters(RouteNames.PrivacyRequestApiTestRequestById, queryParameters), UriKind.Relative);
        }

        /// <summary>
        ///     Creates URL for forcing command completion
        /// </summary>
        public static Uri CreateTestForceCompleteRequest(QueryStringCollection queryParameters)
        {
            return new Uri(ExpandQueryParameters(RouteNames.PrivacyRequestApiTestForceComplete, queryParameters), UriKind.Relative);
        }

        /// <summary>
        ///     Creates URL for getting agent queue stats by agent id
        /// </summary>
        public static Uri CreateTestAgentQueueStatsRequest(QueryStringCollection queryParameters)
        {
            return new Uri(ExpandQueryParameters(RouteNames.PrivacyRequestApiTestAgentQueueStats, queryParameters), UriKind.Relative);
        }

        /// <summary>
        ///     Creates URL for getting requests by user
        /// </summary>
        public static Uri CreateTestRequestsByUserRequest()
        {
            return new Uri(RouteNames.PrivacyRequestApiTestRequestsByUser, UriKind.Relative);
        }

        #endregion

        #region RecurringDelete

        /// <summary>
        ///     Creates GetRecurringDelete relative path.
        /// </summary>
        /// <returns>The URL for GetRecurringDelete request</returns>
        public static Uri CreateGetRecurringDeletesPathV1()
        {
            return new Uri(RouteNames.RecurringDeletesV1, UriKind.Relative);
        }

        /// <summary>
        ///     Creates DeleteRecurringDeletes relative path.
        /// </summary>
        /// <returns>The URL for GetRecurringDelete request</returns>
        public static Uri CreateDeleteRecurringDeletesPathV1(QueryStringCollection queryStringCollection)
        {
            return new Uri(ExpandQueryParameters(RouteNames.RecurringDeletesV1, queryStringCollection), UriKind.Relative);
        }

        /// <summary>
        ///     Creates CreateOrUpdateRecurringDeletes relative path.
        /// </summary>
        /// <returns>The URL for GetRecurringDelete request</returns>
        public static Uri CreateOrUpdateRecurringDeletesPathV1(QueryStringCollection queryStringCollection)
        {
            return new Uri(ExpandQueryParameters(RouteNames.RecurringDeletesV1, queryStringCollection), UriKind.Relative);
        }

        #endregion
    }
}
