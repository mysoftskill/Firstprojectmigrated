// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Test.Common
{
    /// <summary>
    /// Result containing either a user proxy ticket and puid, or an error message.
    /// </summary>
    public class UserProxyTicketAndPuidResult : UserProxyTicketResult
    {
        /// <summary>
        /// Gets or sets the puid.
        /// </summary>
        public long? Puid { get; internal set; }

        /// <summary>
        /// Gets or sets the cid.
        /// </summary>
        public long? Cid { get; internal set; }
    }
}