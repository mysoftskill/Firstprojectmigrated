// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts
{
    /// <summary>
    /// Specifies the field to order results by
    /// </summary>
    public enum OrderByType
    {
        /// <summary>
        /// Sort by DateTime
        /// </summary>
        DateTime = 0,

        /// <summary>
        /// Sort by Search terms
        /// </summary>
        SearchTerms = 1,
    }
}