// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2
{
    public enum RecurrentDeleteStatus
    {
        /// <summary>
        ///     Unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        ///     Active.
        /// </summary>
        Active = 1, 

        /// <summary>
        ///     Paused.
        /// </summary>
        Paused = 2,

        /// <summary>
        ///     Last run failed.
        /// </summary>
        Failed = 3
    }
}
