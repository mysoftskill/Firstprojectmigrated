// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.Common
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;

    /// <summary>
    ///     Used for writing <see cref="AccountCreateInformation"/> to cosmos
    /// </summary>
    public interface IAccountCreateWriter
    {
        /// <summary>
        ///     Writes the <see cref="AccountCreateInformation" />s out asynchronously.
        /// </summary>
        /// <param name="createdAccountsInformation"> The <see cref="AccountCreateInformation" />s to write </param>
        Task<AdapterResponse<IList<AccountCreateInformation>>> WriteCreatedAccountsAsync(IList<AccountCreateInformation> createdAccountsInformation);

        /// <summary>
        ///     Writes the <see cref="AccountCreateInformation" /> out asynchronously.
        /// </summary>
        /// <param name="createdAccountInformation"> The <see cref="AccountCreateInformation" /> to write </param>
        Task<AdapterResponse<AccountCreateInformation>> WriteCreatedAccountAsync(AccountCreateInformation createdAccountInformation);
    }
}
