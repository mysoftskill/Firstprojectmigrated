// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility
{
    using Microsoft.Membership.MemberServices.PrivacyAdapters.PcfAdapter;
    using Microsoft.PrivacyServices.CommandFeed.Client;

    /// <summary>
    ///     represents the result of calling command feed by agent id &amp; command id
    /// </summary>
    public class QueryCommandResult
    {
        /// <summary>
        ///     Gets or sets response code
        /// </summary>
        public ResponseCode ResponseCode { get; set; }

        /// <summary>
        ///     Gets or sets the command
        /// </summary>
        /// <remarks>may be null depending on ResponseCode</remarks>
        public IPrivacyCommand Command { get; set; }
    }
}
