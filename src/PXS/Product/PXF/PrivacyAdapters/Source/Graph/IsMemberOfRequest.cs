// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
using Newtonsoft.Json;

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Graph
{
    /// <summary>
    ///     CheckMemberGroupsRequest.
    /// </summary>
    public class IsMemberOfRequest
    {
        /// <summary>
        ///     Gets or sets group Id.
        /// </summary>
        [JsonProperty("groupId")]
        public string GroupId { get; set; }

        /// <summary>
        ///     Gets or sets member Id.
        /// </summary>
        [JsonProperty("memberId")]
        public string MemberId { get; set; }
    }
}
