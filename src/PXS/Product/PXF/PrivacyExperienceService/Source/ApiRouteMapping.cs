// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service
{
    using System.Collections.Generic;
    using System.Net.Http;

    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;

    /// <summary>
    ///     ApiRouteMapping
    /// </summary>
    public static class ApiRouteMapping
    {
        // TODO: Should move all the api names to ApiNames.cs

        /// <summary>
        ///     The default API name
        /// </summary>
        public const string DefaultApiName = "NoName";

        /// <summary>
        ///     The keep alive API name
        /// </summary>
        public const string KeepAliveApiName = "KeepAlive";

        /// <summary>
        ///     The OpenApi API name
        /// </summary>
        public const string OpenApiName = "OpenApi";

        /// <summary>
        ///     Maps routes and methods to an ApiName string
        /// </summary>
        internal static readonly Dictionary<string, string> PathToApiNameMapping = new Dictionary<string, string>
        {
            { NormalizeRouteMethodKey(RouteNames.KeepAliveRoute, HttpMethod.Get), KeepAliveApiName },
            { NormalizeRouteMethodKey(RouteNames.OpenApiRoute, HttpMethod.Get), OpenApiName },
            { NormalizeRouteMethodKey(RouteNames.GetTimelineV3, HttpMethod.Get), "GetTimeline" },
            { NormalizeRouteMethodKey(RouteNames.GetTimelineAggregateCount, HttpMethod.Get), "GetTimelineAggregateCount" },
            { NormalizeRouteMethodKey(RouteNames.WarmupTimelineV1, HttpMethod.Get), "WarmupTimeline" },
            { NormalizeRouteMethodKey(RouteNames.DeleteTimelineV2, HttpMethod.Delete), "DeleteTimeline" },
            { NormalizeRouteMethodKey(RouteNames.DeleteTimelineV2, HttpMethod.Post), "DeleteTimeline" },
            { NormalizeRouteMethodKey(RouteNames.GetVoiceCardAudioV2, HttpMethod.Get), "GetVoiceCardAudio" },

            // TODO: Task 15919156: Remove old voice API once we confirm AMC ux is not using it
            { NormalizeRouteMethodKey(RouteNames.GetVoiceHistoryV1, HttpMethod.Get), "GetVoiceHistory" },
            { NormalizeRouteMethodKey(RouteNames.GetVoiceHistoryAudioV1, HttpMethod.Get), "GetVoiceHistoryAudio" },
            { NormalizeRouteMethodKey(RouteNames.GetSettingsV1, HttpMethod.Get), "GetSettingsV1" },
            { NormalizeRouteMethodKey(RouteNames.UpdateSettingsV1, new HttpMethod("PATCH")), "UpdateSettingsV1" }, // workaround, HttpMethod doesn't contain 'HttpMethod.Patch'
            { NormalizeRouteMethodKey(RouteNames.PostExportRequest, HttpMethod.Post), "PostExportRequest" },
            { NormalizeRouteMethodKey(RouteNames.PostExportCancel, HttpMethod.Post), "PostExportCancel" },
            { NormalizeRouteMethodKey(RouteNames.DeleteExportArchives, HttpMethod.Delete), "DeleteExportArchives" },
            { NormalizeRouteMethodKey(RouteNames.ListExportHistory, HttpMethod.Get), "ListExportHistory" },
            { NormalizeRouteMethodKey(RouteNames.PrivacyRequestApiDelete, HttpMethod.Post), "PrivacyRequestApiDelete" },
            { NormalizeRouteMethodKey(RouteNames.PrivacyRequestApiDeleteV2, HttpMethod.Post), "PrivacyOperationApiDeleteV2" },
            { NormalizeRouteMethodKey(RouteNames.PrivacyRequestApiExport, HttpMethod.Post), "PrivacyRequestApiExport" },
            { NormalizeRouteMethodKey(RouteNames.PrivacyRequestApiExportV2, HttpMethod.Post), "PrivacyOperationApiExportV2" },
            { NormalizeRouteMethodKey(RouteNames.PrivacyRequestApiList, HttpMethod.Get), "PrivacyRequestApiList" },
            { NormalizeRouteMethodKey(RouteNames.PrivacyRequestApiListByCallerMsa, HttpMethod.Get), "PrivacyRequestApiListByCallerMsa" },
            { NormalizeRouteMethodKey(RouteNames.PrivacyRequestApiTestMsaClose, HttpMethod.Post), "PrivacyRequestApiTestMsaClose" },
            { NormalizeRouteMethodKey(RouteNames.PrivacyRequestApiTestRequestsByUser, HttpMethod.Get), "PrivacyRequestApiTestRequestsByUser" },
            { NormalizeRouteMethodKey(RouteNames.PrivacyRequestApiTestRequestById, HttpMethod.Get), "PrivacyRequestApiTestRequestById" },
            { NormalizeRouteMethodKey(RouteNames.PrivacyRequestApiListRequestById, HttpMethod.Get), "PrivacyOperationApiListRequestById" },
            { NormalizeRouteMethodKey(RouteNames.PrivacyRequestApiTestAgentQueueStats, HttpMethod.Get), "PrivacyRequestApiTestAgentQueueStats" },
            { NormalizeRouteMethodKey(RouteNames.PrivacyRequestApiTestForceComplete, HttpMethod.Post), "PrivacyRequestApiTestRequestForceComplete" },
            { NormalizeRouteMethodKey(RouteNames.ScopedDeleteSearchRequestsAndQuery, HttpMethod.Post), "ScopedDeleteSearchRequestsAndQuery" },
            { NormalizeRouteMethodKey(RouteNames.DataPolicyOperations, HttpMethod.Get), ApiNames.GetDataPolicyOperations },
            { NormalizeODataRouteMethodKey(RouteNames.DataPolicyOperation, HttpMethod.Get), ApiNames.GetDataPolicyOperation },
            { NormalizeODataRouteMethodKey(RouteNames.ExportPersonalData, HttpMethod.Post), ApiNames.ExportPersonalData },
            { NormalizeODataRouteMethodKey(RouteNames.InboundSharedUserProfilesExportPersonalData, HttpMethod.Post), ApiNames.InboundSharedUserProfilesExportPersonalData },
            { NormalizeODataRouteMethodKey(RouteNames.InboundSharedUserProfilesRemovePersonalData, HttpMethod.Post), ApiNames.InboundSharedUserProfilesRemovePersonalData },
            { NormalizeODataRouteMethodKey(RouteNames.OutboundSharedUserProfilesRemovePersonalData, HttpMethod.Post), ApiNames.OutboundSharedUserProfilesRemovePersonalData },
            { NormalizeRouteMethodKey(RouteNames.VortexIngestionDeviceDeleteV1, HttpMethod.Post), "VortexIngestionDeviceDelete" },

            // Recurring deletes
            { NormalizeRouteMethodKey(RouteNames.RecurringDeletesV1, HttpMethod.Get), "GetRecurringDeletes" },
            { NormalizeRouteMethodKey(RouteNames.RecurringDeletesV1, HttpMethod.Delete), "DeleteRecurringDeletes" },
            { NormalizeRouteMethodKey(RouteNames.RecurringDeletesV1, HttpMethod.Post), "CreateOrUpdateRecurringDeletes" }
        };

        /// <summary>
        ///     The certificate based authenticated routes.
        /// </summary>
        internal static readonly List<string> CertificateBasedAuthenticatedRoutes = new List<string>
        {
            RouteNames.VortexIngestionDeviceDeleteV1
        };

        /// <summary>
        ///     These routes are authenticated via MSA Site id.
        /// </summary>
        internal static readonly List<string> SiteIdAuthenticatedRoutes = new List<string>
        {
            RouteNames.PrivacyRequestApiList,

            // These only require site identification, but only in the case of non-MSA subjects, which is handled in the controller.
            RouteNames.PrivacyRequestApiDelete,
            RouteNames.PrivacyRequestApiExport
        };

        /// <summary>
        ///     Normalized the ODATA route/method string for lookup in the mapping table
        /// </summary>
        /// <param name="route">Route for the request</param>
        /// <param name="httpMethod">HTTP method</param>
        /// <returns>Normalized string</returns>
        public static string NormalizeODataRouteMethodKey(string route, HttpMethod httpMethod)
        {
            return (route.Trim('/') + "\\+" + httpMethod)
                .Trim('/', '\\')
                .ToUpperInvariant();
        }

        /// <summary>
        ///     Normalized the route/method string for lookup in the mapping table
        /// </summary>
        /// <param name="route">Route for the request</param>
        /// <param name="httpMethod">HTTP method</param>
        /// <returns>Normalized string</returns>
        public static string NormalizeRouteMethodKey(string route, HttpMethod httpMethod)
        {
            return (route + "+" + httpMethod)
                .Trim('/', '\\')
                .ToUpperInvariant();
        }

        /// <summary>
        ///     Determines whether the route is authenticated via proxy ticket.
        /// </summary>
        /// <param name="routeName">Name of the route.</param>
        internal static bool IsProxyTicketAuthenticatedRoute(string routeName)
        {
            // We only authenticate via proxy ticket or site id (currently). 
            // If this ever changes, a separate list of proxy ticket authenticated routes would have to be maintained.
            return !SiteIdAuthenticatedRoutes.Contains(routeName);
        }

        /// <summary>
        ///     Determines whether the route is authenticated via site id.
        /// </summary>
        /// <param name="routeName">Name of the route.</param>
        internal static bool IsSiteIdAuthenticatedRoute(string routeName)
        {
            return SiteIdAuthenticatedRoutes.Contains(routeName);
        }
    }
}
