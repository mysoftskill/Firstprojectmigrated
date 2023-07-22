// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service
{
    using System.Collections.Generic;

    /// <summary>
    ///     Api Names. These names are visible in telemetry such as SLL Xpert Incoming QoS
    /// </summary>
    public static class ApiNames
    {
        /// <summary>
        ///     ExportPersonalData API Name
        /// </summary>
        public const string ExportPersonalData = "ExportPersonalData";

        /// <summary>
        ///     InboundSharedUserProfiles ExportPersonalData API Name
        /// </summary>
        public const string InboundSharedUserProfilesExportPersonalData = "InboundSharedUserProfiles_ExportPersonalData";

        /// <summary>
        ///     InboundShareddUserProfiles RemovePersonalData API Name
        /// </summary>
        public const string InboundSharedUserProfilesRemovePersonalData = "InboundSharedUserProfiles_RemovePersonalData";

        /// <summary>
        ///     OutboundSharedUserProfiles RemovePersonalData API Name
        /// </summary>
        public const string OutboundSharedUserProfilesRemovePersonalData = "OutboundSharedUserProfiles_RemovePersonalData";

        /// <summary>
        ///     GetDataPolicyOperation API Name
        /// </summary>
        public const string GetDataPolicyOperation = "GetDataPolicyOperation";

        /// <summary>
        ///     GetDataPolicyOperations API Name
        /// </summary>
        public const string GetDataPolicyOperations = "GetDataPolicyOperations";

        /// <summary>
        ///     API names that are authenticated via AAD
        /// </summary>
        public static readonly HashSet<string> AadAuthenticatedApiNames = new HashSet<string>
        {
            GetDataPolicyOperations,
            GetDataPolicyOperation,
            ExportPersonalData,
            InboundSharedUserProfilesExportPersonalData,
            InboundSharedUserProfilesRemovePersonalData,
            OutboundSharedUserProfilesRemovePersonalData
        };
    }
}
