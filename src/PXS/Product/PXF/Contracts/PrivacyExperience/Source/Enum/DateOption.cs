// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts
{
    /// <summary>
    /// Indicates the date option
    /// </summary>
    public enum DateOption
    {
        /// <summary>
        /// Single day (use date specified in 'startDate')
        /// </summary>
        SingleDay = 0,

        /// <summary>
        /// Dates before specified dateTime (use 'startDate')
        /// </summary>
        Before = 1,

        /// <summary>
        /// Dates after specified dateTime (use 'startDate')
        /// </summary>
        After = 2,

        /// <summary>
        /// Dates in the specified range ('startDate' to 'endDate'). Only uses Date part, time is ignored.
        /// </summary>
        Between = 3,
    }
}