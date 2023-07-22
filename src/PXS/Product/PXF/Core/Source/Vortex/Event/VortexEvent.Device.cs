// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Vortex.Event
{
    using Newtonsoft.Json;

    /// <summary>
    /// VortexEvents
    /// </summary>
    public partial class VortexEvent
    {
        /// <summary>
        /// Device
        /// </summary>
        public class Device
        {
            /// <summary>
            /// OrganizationId
            /// </summary>
            [JsonProperty("orgId", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string OrganizationId;

            /// <summary>
            /// Id
            /// </summary>
            [JsonProperty("id")]
            public string Id { get; set; }
        }
    }
}
