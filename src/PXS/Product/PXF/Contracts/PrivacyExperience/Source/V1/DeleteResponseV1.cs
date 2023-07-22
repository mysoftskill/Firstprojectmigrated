// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Delete Status V1
    /// </summary>
    public enum DeleteStatusV1
    {
        /// <summary>
        /// Unknown status
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Pending Delete
        /// </summary>
        PendingDelete = 1,

        /// <summary>
        /// Deleted
        /// </summary>
        Deleted = 2,
    }

    /// <summary>
    /// Delete Response V1
    /// </summary>
    public class DeleteResponseV1
    {
        /// <summary>
        /// Delete Status
        /// </summary>
        [JsonProperty("status"), JsonConverter(typeof(StringEnumConverter))]
        public DeleteStatusV1 Status { get; set; }
    }
}