// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers
{
    /// <summary>
    /// Resource collection type 
    /// </summary>
    public enum ResourceCollection
    {
        /// <summary>
        /// Not specified / Unknown
        /// </summary>
        Unspecified,

        /// <summary>
        /// Browse history logs
        /// </summary>
        BrowseHistory,

        /// <summary>
        /// AppUsage logs
        /// </summary>
        AppUsage,

        /// <summary>
        /// Location history logs
        /// </summary>
        LocationHistory,

        /// <summary>
        /// Search history logs
        /// </summary>
        SearchHistory,

        /// <summary>
        /// Voice history logs
        /// </summary>
        VoiceHistory,

        /// <summary>
        /// Cortana Notebook user feature entity
        /// </summary>
        CortanaNotebookUserFeatureEntity
    }
}
