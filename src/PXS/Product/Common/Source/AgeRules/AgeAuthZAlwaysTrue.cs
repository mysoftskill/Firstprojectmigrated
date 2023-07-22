// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Common
{
    using System;

    /// <summary>
    ///     Temporary class to by pass majority Age check.
    /// </summary>
    public class AgeAuthZAlwaysTrue : IAgeAuthZRules
    {
        /// <summary>
        ///     Determines whether the target user can DELETE.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if the user is authorized to delete, else <c>false</c>
        /// </returns>
        public bool CanDelete(MsaSelfIdentity _)
        {
            // Return true for all types -- This is temporary and needs to be changed once MSA based Statutory age logic is finalized
            return true;
        }

        /// <summary>
        ///     Determines whether the target user can VIEW.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if the user is authorized to view, else <c>false</c>
        /// </returns>
        public bool CanView(MsaSelfIdentity _)
        {
            // Return true for all types -- This is temporary and needs to be changed once MSA based Statutory age logic is finalized
            return true;
        }
    }
}
