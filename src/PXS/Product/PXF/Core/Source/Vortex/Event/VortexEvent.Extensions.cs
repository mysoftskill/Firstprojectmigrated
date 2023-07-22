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
        /// Extensions
        /// </summary>
        public class Extensions
        {
            /// <summary>
            /// Device
            /// </summary>
            [JsonProperty("device")]
            public Device Device { get; set; }

            /// <summary>
            /// User
            /// </summary>
            [JsonProperty("user")]
            public User User { get; set; }
        }
    }
}
