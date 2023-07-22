// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;

    /// <summary>
    ///     Public interface for IXboxAccountsAdapter
    /// </summary>
    public interface IXboxAccountsAdapter
    {
        /// <summary>
        ///     Gets Xbox Live user Xuid by the user's Puid.
        /// </summary>
        /// <param name="requestContext">The request object.</param>
        /// <returns>Xbox Live user lookup info.</returns>
        Task<AdapterResponse<string>> GetXuidAsync(IPxfRequestContext requestContext);

        /// <summary>
        ///     Gets XUIDs for multiple users.
        /// </summary>
        /// <param name="puids">The list of PUIDs.</param>
        /// <returns>A dictionary of XUIDs indexed by PUIDs.</returns>
        Task<AdapterResponse<Dictionary<long, string>>> GetXuidsAsync(IEnumerable<long> puids);
    }
}
