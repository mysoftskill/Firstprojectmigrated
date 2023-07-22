// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.AQS
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;

    /// <summary>
    ///     Delete writers are used for translating and forwarding account delete events
    /// </summary>
    public interface IAccountDeleteWriter
    {
        /// <summary>
        ///     Writes out the <see cref="AccountDeleteInformation" />
        /// </summary>
        /// <param name="deleteInfo"> The <see cref="AccountDeleteInformation" /> to write </param>
        /// <param name="requester"> The name of the requester for the deletes </param>
        Task<AdapterResponse<AccountDeleteInformation>> WriteDeleteAsync(AccountDeleteInformation deleteInfo, string requester);

        /// <summary>
        ///     Writes out a bulk delete of <see cref="AccountDeleteInformation" />
        /// </summary>
        /// <param name="deleteInfos">The collection of delete information </param>
        /// <param name="requester"> The name of the requester for the deletes </param>
        /// <returns>An async task</returns>
        Task<AdapterResponse<IList<AccountDeleteInformation>>> WriteDeletesAsync(IList<AccountDeleteInformation> deleteInfos, string requester);
    }
}
