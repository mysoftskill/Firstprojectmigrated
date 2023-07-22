// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts
{
    /// <summary>
    /// Location Category
    /// </summary>
    public enum LocationCategory
    {
        /// <summary>
        /// Unknown (default)
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Device location
        /// </summary>
        Device = 1,

        /// <summary>
        /// Search location
        /// </summary>
        Search = 2,

        /// <summary>
        /// Favorite location
        /// </summary>
        Favorite = 3,

        /// <summary>
        /// Inferred location
        /// </summary>
        Inferred = 4,

        /// <summary>
        /// Processed log location
        /// </summary>
        ProcessedLog = 5,

        /// <summary>
        /// Fitness location
        /// </summary>
        Fitness = 6,

        /// <summary>
        /// Mixed category (used in aggregation to indicate more than one category are aggregated together)
        /// </summary>
        Mixed = 7
    }
}