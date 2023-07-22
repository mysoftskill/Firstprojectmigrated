// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts
{
    /// <summary>
    ///     Route Names
    ///     When modifying this class, you probably want to update ApiRouteMapping as well!
    /// </summary>
    public static class RouteNames
    {
        /// <summary>
        ///     The delete-timeline v2 relative path
        /// </summary>
        public const string DeleteTimelineV2 = "v2/timeline";

        /// <summary>
        ///     The get-browse-history v1 relative-path
        /// </summary>
        public const string GetBrowseHistoryV1 = "v1/browsehistory";

        /// <summary>
        ///     The get-settings v1 relative path
        /// </summary>
        public const string GetSettingsV1 = "v1/settings";

        /// <summary>
        ///     The get-timelineaggregatecount v1 relative path
        /// </summary>
        public const string GetTimelineAggregateCount = "v1/timelineaggregatecount";

        /// <summary>
        ///     The get-timeline v3 relative path
        /// </summary>
        public const string GetTimelineV3 = "v3/timeline";

        /// <summary>
        ///     The get-timeline-voicecard-audio relative path
        /// </summary>
        public const string GetVoiceCardAudioV2 = "v2/voicecardaudio";

        /// <summary>
        ///     The get-voice-history-audio v1 relative-path
        /// </summary>
        public const string GetVoiceHistoryAudioV1 = "v1/voicehistoryaudio";

        /// <summary>
        ///     The get-voice-history v1 relative-path
        /// </summary>
        public const string GetVoiceHistoryV1 = "v1/voicehistory";

        // KeepAlive APIs

        /// <summary>
        ///     The keep-alive relative-path
        /// </summary>
        public const string KeepAliveRoute = "keepalive";

        public const string OpenApiRoute = "v1/openapi";

        public const string ListExportHistory = "v1/exporthistory";

        public const string PostExportCancel = "v1/exportcancel";

        public const string PostExportRequest = "v1/export";

        public const string DeleteExportArchives = "v1/deleteexport";

        /// <summary>
        ///     The delete action for privacy requests
        /// </summary>
        public const string PrivacyRequestApiDelete = "v1/privacyrequest/delete";

        /// <summary>
        ///     The delete action for privacy requests
        /// </summary>
        public const string PrivacyRequestApiDeleteV2 = "v2/privacyrequest/delete";

        /// <summary>
        ///     The export action for privacy requests
        /// </summary>
        public const string PrivacyRequestApiExport = "v1/privacyrequest/export";

        /// <summary>
        ///     The export action for privacy requests
        /// </summary>
        public const string PrivacyRequestApiExportV2 = "v2/privacyrequest/export";

        /// <summary>
        ///     The list action for privacy requests
        /// </summary>
        public const string PrivacyRequestApiList = "v1/privacyrequest/list";

        /// <summary>
        ///     The list action for privacy requests for a specific user
        /// </summary>
        public const string PrivacyRequestApiListByCallerMsa = "v1/privacyrequest/listmsa";

        /// <summary>
        ///     The get request via request id action.
        /// </summary>
        public const string PrivacyRequestApiListRequestById = "v1/privacyrequest/listrequestbyid";

        /// <summary>
        ///     The debug agent queue stats for privacy requests
        /// </summary>
        public const string PrivacyRequestApiTestAgentQueueStats = "v1/privacyrequest/agentqueuestats";

        /// <summary>
        ///     The debug force complete action for privacy requests
        /// </summary>
        public const string PrivacyRequestApiTestForceComplete = "v1/privacyrequest/forcecomplete";

        /// <summary>
        ///     The account close action for privacy requests
        /// </summary>
        public const string PrivacyRequestApiTestMsaClose = "v1/privacyrequest/testmsaclose";

        /// <summary>
        ///     The debug by id action for privacy requests
        /// </summary>
        public const string PrivacyRequestApiTestRequestById = "v1/privacyrequest/testrequestbyid";

        /// <summary>
        ///     The debug by current user action for privacy requests
        /// </summary>
        public const string PrivacyRequestApiTestRequestsByUser = "v1/privacyrequest/testrequestsbyuser";

        /// <summary>
        ///     The scoped delete relative path
        /// </summary>
        public const string ScopedDeleteSearchRequestsAndQuery = "v1/scopeddelete/searchrequestsandquery";

        /// <summary>
        ///     The bulk scoped delete relative path
        /// </summary>
        public const string BulkScopedDeleteSearchRequestsAndQuery = "v1/bulkscopeddelete/searchrequestsandquery";

        /// <summary>
        ///     The update-settings v1 relative path
        /// </summary>
        public const string UpdateSettingsV1 = "v1/settings";

        /// <summary>
        ///     The vortex device delete handler
        /// </summary>
        public const string VortexIngestionDeviceDeleteV1 = "v1/vortex/devicedelete";

        /// <summary>
        ///     The warmup-timeline relative path
        /// </summary>
        public const string WarmupTimelineV1 = "v1/warmup-timeline";

        #region DSR routes in Regex format since they are ODATA-formatted

        /// <summary>
        ///     The ExportPersonalData relative path.
        /// </summary>
        public const string ExportPersonalData = @"users\('[A-Z0-9\-]{36}'\)/exportPersonalData";

        /// <summary>
        ///     The DataPolicyOperations relative path.
        /// </summary>
        public const string DataPolicyOperations = @"dataPolicyOperations";

        /// <summary>
        ///     The DataPolicyOperation relative path.
        /// </summary>
        public const string DataPolicyOperation = @"dataPolicyOperations\('[A-Z0-9\-]{36}'\)";

        /// <summary>
        ///     The InboundSharedUserProfilesExportPersonalData relative path.
        /// </summary>
        public const string InboundSharedUserProfilesExportPersonalData = @"directory/inboundSharedUserProfiles\('[A-Z0-9\-]{36}'\)/exportPersonalData";

        /// <summary>
        ///     The InboundSharedUserProfilesRemovePersonalData relative path.
        /// </summary>
        public const string InboundSharedUserProfilesRemovePersonalData = @"directory/inboundSharedUserProfiles\('[A-Z0-9\-]{36}'\)/removePersonalData";

        /// <summary>
        ///     The OutboundSharedUserProfilesRemovePersonalData relative path.
        /// </summary>
        public const string OutboundSharedUserProfilesRemovePersonalData = @"directory/outboundSharedUserProfiles\('[A-Z0-9\-]{36}'\)/tenants\('[A-Z0-9\-]{36}'\)/removePersonalData";

        #endregion

        /// <summary>
        ///     The Recurring Deletes relative path
        /// </summary>
        public const string RecurringDeletesV1 = "v1/recurring-deletes";
    }
}
