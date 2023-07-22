// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Common
{
    /// <summary>
    ///     Interface to determine authorization based on age
    /// </summary>
    public interface IAgeAuthZRules
    {
        /// <summary>
        ///     Determines whether the target user can DELETE.
        /// </summary>
        /// <param name="identity">The request identity</param>
        /// <returns><c>true</c> if the user is authorized to delete, else <c>false</c></returns>
        bool CanDelete(MsaSelfIdentity identity);

        /// <summary>
        ///     Determines whether the target user can VIEW.
        /// </summary>
        /// <param name="identity">The request identity</param>
        /// <returns><c>true</c> if the user is authorized to view, else <c>false</c></returns>
        bool CanView(MsaSelfIdentity identity);
    }
}
