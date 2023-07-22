// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Vortex.Event
{
    using Newtonsoft.Json;

    /// <summary>
    /// VortexEvent
    /// </summary>
    public partial class VortexEvent
    {
        /// <summary>
        /// User
        /// </summary>
        public class User
        {
            /// <summary>
            /// Id
            /// </summary>
            [JsonProperty("id")]
            public string Id { get; set; }
        }
    }
}
