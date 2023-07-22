// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.NgpProxy.PrivacyExperienceServiceProxy
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.PrivacyAdapters;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts;

    /// <summary>
    ///     NotSupported instance of Xbox Accounts Adapter
    /// </summary>
    public class NotSupportedXboxAccountsAdapter : IXboxAccountsAdapter

    {
        /// <inheritdoc />
        public Task<AdapterResponse<string>> GetXuidAsync(IPxfRequestContext requestContext)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public Task<AdapterResponse<Dictionary<long, string>>> GetXuidsAsync(IEnumerable<long> puids)
        {
            throw new NotSupportedException();
        }
    }
}
