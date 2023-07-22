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
        /// VortexData
        /// </summary>
        public class VortexData
        {
            /// <summary>
            /// IsInitiatedByUser: when this property is missing its default value will be set to 1 to indicate it's not initiated by system
            /// so it goes to the duduping cache for user initiated and unknown signals
            /// </summary>
            [JsonProperty("IsInitiatedByUser", DefaultValueHandling = DefaultValueHandling.Include)]
            public int IsInitiatedByUser { get; set; } = 1;
        }
    }
}
